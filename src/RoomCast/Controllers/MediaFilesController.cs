using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoomCast.Data;
using RoomCast.Models;
using RoomCast.Models.ViewModels;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

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

        public MediaFilesController(ApplicationDbContext context, IWebHostEnvironment environment, UserManager<ApplicationUser> userManager, ILogger<MediaFilesController> logger)
        {
            _context = context;
            _environment = environment;
            _userManager = userManager;
            _logger = logger;
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
