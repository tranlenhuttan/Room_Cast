using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoomCast.Data;
using RoomCast.Models;
using RoomCast.Models.ViewModels;
using RoomCast.Services.MediaPreview;
using Microsoft.AspNetCore.Http;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Text;

namespace RoomCast.Controllers
{
    [Authorize]
    public class MediaFilesController : Controller
    {
        private const long MaxFileSizeBytes = 100 * 1024 * 1024; // 100 MB

        private static readonly IReadOnlyDictionary<string, HashSet<string>> AllowedExtensions = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Document"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".txt" },
            ["Picture"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" },
            ["Video"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".mp4", ".mov", ".m4v", ".avi", ".mkv" }
        };

        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<MediaFilesController> _logger;
        private readonly IMediaFilePreviewBuilder _previewBuilder;

        public MediaFilesController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            UserManager<ApplicationUser> userManager,
            ILogger<MediaFilesController> logger,
            IMediaFilePreviewBuilder previewBuilder)
        {
            _context = context;
            _environment = environment;
            _userManager = userManager;
            _logger = logger;
            _previewBuilder = previewBuilder;
        }

        // GET: List Files
        public async Task<IActionResult> Index(string searchTags)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var files = _context.MediaFiles.Where(f => f.UserId == user.Id);

            if (!string.IsNullOrEmpty(searchTags))
            {
                files = files.Where(f => f.Tags.Contains(searchTags));
            }

            var filesList = await files
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            return View(filesList);
        }

        // GET: Upload Page
        public IActionResult Upload()
        {
            var model = new FileUploadViewModel();
            PopulateAllowedExtensions(model);
            return View(model);
        }

        // POST: Upload File
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(FileUploadViewModel model)
        {
            PopulateAllowedExtensions(model);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.File == null || model.File.Length == 0)
            {
                ModelState.AddModelError(nameof(model.File), "Please choose a file to upload.");
                return View(model);
            }

            if (model.File.Length > MaxFileSizeBytes)
            {
                ModelState.AddModelError(nameof(model.File), $"File exceeds the {MaxFileSizeBytes / (1024 * 1024)}MB limit.");
                return View(model);
            }

            var extension = Path.GetExtension(model.File.FileName).ToLowerInvariant();

            var normalizedType = GetFileTypeForExtension(extension);
            if (normalizedType == null)
            {
                ModelState.AddModelError(nameof(model.File), $"Unsupported file format. Allowed formats: {FormatAllowedExtensionsList()}");
                return View(model);
            }

            var title = model.Title?.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                title = Path.GetFileNameWithoutExtension(model.File.FileName);
            }

            if (!string.IsNullOrEmpty(model.Tags) && model.Tags.Length > 500)
            {
                ModelState.AddModelError(nameof(model.Tags), "Tags must be 500 characters or fewer.");
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var media = new MediaFile
            {
                UserId = user.Id,
                Title = title,
                FileType = normalizedType,
                FileFormat = extension,
                OriginalFileName = model.File.FileName,
                StoredFileName = GenerateSafeFileName(title, extension),
                ContentType = model.File.ContentType ?? string.Empty,
                Tags = model.Tags?.Trim() ?? string.Empty,
                FileSize = model.File.Length,
                CreatedAt = DateTime.UtcNow
            };

            var uploadSubFolder = GetUploadFolder(normalizedType);
            var uploadsRoot = Path.Combine(_environment.WebRootPath, "uploads", uploadSubFolder);
            Directory.CreateDirectory(uploadsRoot);

            var physicalPath = Path.Combine(uploadsRoot, media.StoredFileName);

            await using (var stream = System.IO.File.Create(physicalPath))
            {
                await model.File.CopyToAsync(stream);
            }

            media.FilePath = $"/uploads/{uploadSubFolder}/{media.StoredFileName}";

            if (string.Equals(normalizedType, "Video", StringComparison.OrdinalIgnoreCase))
            {
                media.DurationSeconds = await TryExtractVideoDurationAsync(physicalPath);
                media.ThumbnailPath = await TryGenerateVideoThumbnailAsync(physicalPath, media.StoredFileName);
            }

            _context.MediaFiles.Add(media);
            await _context.SaveChangesAsync();

            TempData["Message"] = $"Uploaded '{media.Title}' successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Details
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var file = await _context.MediaFiles
                .Where(m => m.FileId == id && m.UserId == user.Id)
                .FirstOrDefaultAsync();

            if (file == null)
            {
                return NotFound();
            }

            return View(file);
        }

        [HttpGet]
        public async Task<IActionResult> Preview(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var mediaFile = await _context.MediaFiles
                .Where(m => m.FileId == id && m.UserId == user.Id)
                .FirstOrDefaultAsync();

            if (mediaFile == null)
            {
                return NotFound();
        }

            var viewModel = _previewBuilder.Build(mediaFile);

            return View("Preview", viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> EditText(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var mediaFile = await _context.MediaFiles
                .Where(m => m.FileId == id && m.UserId == user.Id)
                .FirstOrDefaultAsync();

            if (mediaFile == null)
            {
                return NotFound();
            }

            if (!IsPlainTextDocument(mediaFile))
            {
                return BadRequest();
            }

            var physicalPath = ResolvePhysicalPath(mediaFile);
            if (physicalPath == null || !System.IO.File.Exists(physicalPath))
            {
                return NotFound();
            }

            TextFileEditViewModel viewModel;
            try
            {
                viewModel = await BuildTextFileEditViewModelAsync(mediaFile, physicalPath);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Failed to load text editor for media file {FileId} belonging to user {UserId}", id, user.Id);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            return View("EditText", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveText(TextFileEditViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var mediaFile = await _context.MediaFiles
                .Where(m => m.FileId == model.FileId && m.UserId == user.Id)
                .FirstOrDefaultAsync();

            if (mediaFile == null)
            {
                return NotFound();
            }

            if (!IsPlainTextDocument(mediaFile))
            {
                return BadRequest();
            }

            var physicalPath = ResolvePhysicalPath(mediaFile);
            if (physicalPath == null || !System.IO.File.Exists(physicalPath))
            {
                ModelState.AddModelError(string.Empty, "We could not find the stored file on disk to update it.");
            }

            if (!ModelState.IsValid)
            {
                var fallbackContent = model.Content ?? string.Empty;
                var fallbackViewModel = await BuildTextFileEditViewModelAsync(mediaFile, physicalPath, fallbackContent);
                return View("EditText", fallbackViewModel);
            }

            var contentToWrite = model.Content ?? string.Empty;
            var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

            try
            {
                await System.IO.File.WriteAllTextAsync(physicalPath!, contentToWrite, encoding);
                mediaFile.FileSize = encoding.GetByteCount(contentToWrite);
                if (string.IsNullOrWhiteSpace(mediaFile.ContentType))
                {
                    mediaFile.ContentType = "text/plain";
                }
                await _context.SaveChangesAsync();
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Failed to save text content for media file {FileId} belonging to user {UserId}", mediaFile.FileId, user.Id);
                ModelState.AddModelError(string.Empty, "We couldn't save your changes right now. Please try again.");
                var errorViewModel = await BuildTextFileEditViewModelAsync(mediaFile, physicalPath, contentToWrite);
                return View("EditText", errorViewModel);
            }

            TempData["Message"] = "Saved changes.";
            return RedirectToAction(nameof(EditText), new { id = mediaFile.FileId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rename(int id, [FromForm] string title)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var mediaFile = await _context.MediaFiles
                .Where(m => m.FileId == id && m.UserId == user.Id)
                .FirstOrDefaultAsync();

            if (mediaFile == null)
            {
                return NotFound();
            }

            var trimmedTitle = (title ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(trimmedTitle))
            {
                return BadRequest(new { error = "Title is required." });
            }

            if (trimmedTitle.Length > 200)
            {
                return BadRequest(new { error = "Title must be 200 characters or fewer." });
            }

            if (string.Equals(mediaFile.Title, trimmedTitle, StringComparison.Ordinal))
            {
                return Ok(new { title = mediaFile.Title });
            }

            mediaFile.Title = trimmedTitle;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Failed to rename media file {FileId} for user {UserId}", id, user.Id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "We could not rename the file right now. Please try again later." });
            }

            return Ok(new { title = mediaFile.Title });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var media = await _context.MediaFiles
                .Where(m => m.FileId == id && m.UserId == user.Id)
                .FirstOrDefaultAsync();

            if (media == null)
            {
                TempData["Error"] = "The selected file could not be found.";
                return RedirectToAction(nameof(Index));
            }

            var title = media.Title;
            var filePath = media.FilePath;
            var thumbnailPath = media.ThumbnailPath;

            _context.MediaFiles.Remove(media);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Failed to delete media file {FileId} for user {UserId}", id, user.Id);
                TempData["Error"] = "We couldn't delete the file right now. Please try again.";
                return RedirectToAction(nameof(Index));
            }

            TryDeletePhysicalFile(filePath);
            TryDeletePhysicalFile(thumbnailPath);

            TempData["Message"] = $"Deleted '{title}' successfully.";
            return RedirectToAction(nameof(Index));
        }

        private static void PopulateAllowedExtensions(FileUploadViewModel model)
        {
            var dictionary = AllowedExtensions.ToDictionary(
                kvp => kvp.Key,
                kvp => (IReadOnlyList<string>)kvp.Value
                    .OrderBy(ext => ext, StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                StringComparer.OrdinalIgnoreCase);

            var flatList = dictionary.Values
                .SelectMany(v => v)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(ext => ext, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            model.AllowedExtensionsByType = dictionary;
            model.AllowedExtensions = flatList;
        }

        private static string GetUploadFolder(string fileType) =>
            fileType switch
            {
                "Video" => "videos",
                "Picture" => "pictures",
                "Document" => "documents",
                _ => "misc"
            };

        private static string? GetFileTypeForExtension(string extension)
        {
            foreach (var kvp in AllowedExtensions)
            {
                if (kvp.Value.Contains(extension))
                {
                    return kvp.Key;
                }
            }

            return null;
        }

        private static string FormatAllowedExtensionsList() =>
            string.Join(", ", AllowedExtensions.Select(kvp => $"{kvp.Key}: {string.Join(", ", kvp.Value.OrderBy(x => x))}"));

        private static string GenerateSafeFileName(string title, string extension)
        {
            var slugSource = title.ToLowerInvariant();
            var slug = Regex.Replace(slugSource, @"[^a-z0-9]+", "-").Trim('-');
            if (string.IsNullOrEmpty(slug))
            {
                slug = "file";
            }

            return $"{slug}-{Guid.NewGuid():N}{extension}";
        }

        private async Task<double?> TryExtractVideoDurationAsync(string videoPhysicalPath)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            startInfo.ArgumentList.Add("-i");
            startInfo.ArgumentList.Add(videoPhysicalPath);

            try
            {
                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    return null;
                }

                var stdoutTask = process.StandardOutput.ReadToEndAsync();
                var stderrTask = process.StandardError.ReadToEndAsync();

                await Task.WhenAll(stdoutTask, stderrTask);
                await process.WaitForExitAsync();

                var stderr = stderrTask.Result;
                var match = Regex.Match(stderr, @"Duration:\s(?<hours>\d{2}):(?<minutes>\d{2}):(?<seconds>\d{2}\.\d+)");
                if (match.Success)
                {
                    var hours = double.Parse(match.Groups["hours"].Value, CultureInfo.InvariantCulture);
                    var minutes = double.Parse(match.Groups["minutes"].Value, CultureInfo.InvariantCulture);
                    var seconds = double.Parse(match.Groups["seconds"].Value, CultureInfo.InvariantCulture);

                    return (hours * 3600) + (minutes * 60) + seconds;
                }

                _logger.LogWarning("Could not parse video duration for {Path}. ffmpeg output: {Output}", videoPhysicalPath, stderr);
            }
            catch (Exception ex) when (ex is Win32Exception || ex is InvalidOperationException)
            {
                _logger.LogWarning(ex, "Failed to extract duration for {Path}", videoPhysicalPath);
            }

            return null;
        }

        private static bool IsPlainTextDocument(MediaFile mediaFile)
        {
            if (!string.Equals(mediaFile.FileType, "Document", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var extension = mediaFile.FileFormat?.Trim().ToLowerInvariant();
            return extension is ".txt" or ".text";
        }

        private string? ResolvePhysicalPath(MediaFile mediaFile)
        {
            if (string.IsNullOrWhiteSpace(mediaFile.FilePath))
            {
                return null;
            }

            var relativePath = mediaFile.FilePath.TrimStart('/', '\\');
            var combinedPath = Path.Combine(_environment.WebRootPath, relativePath);
            var fullPath = Path.GetFullPath(combinedPath);

            var webRootFullPath = Path.GetFullPath(_environment.WebRootPath);
            if (!fullPath.StartsWith(webRootFullPath, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return fullPath;
        }

        private async Task<TextFileEditViewModel> BuildTextFileEditViewModelAsync(
            MediaFile mediaFile,
            string? physicalPath,
            string? overrideContent = null)
        {
            string content;
            if (overrideContent is not null)
            {
                content = overrideContent;
            }
            else if (!string.IsNullOrEmpty(physicalPath) && System.IO.File.Exists(physicalPath))
            {
                content = await System.IO.File.ReadAllTextAsync(physicalPath);
            }
            else
            {
                content = string.Empty;
            }

            var lastWriteUtc = (!string.IsNullOrEmpty(physicalPath) && System.IO.File.Exists(physicalPath))
                ? System.IO.File.GetLastWriteTimeUtc(physicalPath)
                : mediaFile.CreatedAt;

            return new TextFileEditViewModel
            {
                FileId = mediaFile.FileId,
                Title = mediaFile.Title,
                OriginalFileName = mediaFile.OriginalFileName,
                DownloadPath = mediaFile.FilePath,
                ContentType = string.IsNullOrWhiteSpace(mediaFile.ContentType) ? "text/plain" : mediaFile.ContentType,
                FileSize = mediaFile.FileSize,
                LastSavedLabel = lastWriteUtc.ToLocalTime().ToString("f", CultureInfo.CurrentCulture),
                Content = content
            };
        }

        private async Task<string?> TryGenerateVideoThumbnailAsync(string videoPhysicalPath, string storedFileName)
        {
            var thumbnailsRoot = Path.Combine(_environment.WebRootPath, "uploads", "thumbnails");
            Directory.CreateDirectory(thumbnailsRoot);

            var thumbnailFileName = Path.ChangeExtension(storedFileName, ".jpg");
            var thumbnailPhysicalPath = Path.Combine(thumbnailsRoot, thumbnailFileName);

            var startInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            startInfo.ArgumentList.Add("-ss");
            startInfo.ArgumentList.Add("00:00:01");
            startInfo.ArgumentList.Add("-i");
            startInfo.ArgumentList.Add(videoPhysicalPath);
            startInfo.ArgumentList.Add("-frames:v");
            startInfo.ArgumentList.Add("1");
            startInfo.ArgumentList.Add("-q:v");
            startInfo.ArgumentList.Add("2");
            startInfo.ArgumentList.Add("-y");
            startInfo.ArgumentList.Add(thumbnailPhysicalPath);

            try
            {
                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    return null;
                }

                var stdoutTask = process.StandardOutput.ReadToEndAsync();
                var stderrTask = process.StandardError.ReadToEndAsync();

                await Task.WhenAll(stdoutTask, stderrTask);
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    _logger.LogWarning("ffmpeg thumbnail generation failed ({Code}) for {Path}: {Error}", process.ExitCode, videoPhysicalPath, stderrTask.Result);
                    return null;
                }

                return "/uploads/thumbnails/" + thumbnailFileName;
            }
            catch (Exception ex) when (ex is Win32Exception || ex is InvalidOperationException)
            {
                _logger.LogWarning(ex, "Failed to generate thumbnail for {Path}", videoPhysicalPath);
            }

            return null;
        }

        private void TryDeletePhysicalFile(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return;
            }

            string physicalPath;
            if (Path.IsPathRooted(relativePath))
            {
                physicalPath = relativePath;
            }
            else
            {
                var trimmedPath = relativePath.TrimStart('~', '/', '\\')
                    .Replace('/', Path.DirectorySeparatorChar)
                    .Replace('\\', Path.DirectorySeparatorChar);
                physicalPath = Path.Combine(_environment.WebRootPath, trimmedPath);
            }

            try
            {
                if (System.IO.File.Exists(physicalPath))
                {
                    System.IO.File.Delete(physicalPath);
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is NotSupportedException)
            {
                _logger.LogWarning(ex, "Failed to delete file at path {FilePath}", physicalPath);
            }
        }
    }
}
