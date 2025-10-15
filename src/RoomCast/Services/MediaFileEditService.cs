using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RoomCast.Data;
using RoomCast.Models;
using RoomCast.Models.ViewModels;

namespace RoomCast.Services
{
    public class MediaFileEditService : IMediaFileEditService
    {
        private const long MaxFileSizeBytes = 100 * 1024 * 1024; // 100 MB
        private const long MaxTextFileBytes = 2 * 1024 * 1024; // 2 MB
        private static readonly string[] VisibilityValues = { "Private", "Public" };

        private static readonly HashSet<string> TextFileExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".txt", ".md", ".markdown", ".json", ".yaml", ".yml", ".csv", ".log", ".xml"
        };

        private static readonly HashSet<string> ImageFileExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp"
        };

        private static readonly HashSet<string> VideoFileExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".mov", ".m4v", ".avi", ".mkv"
        };

        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<MediaFileEditService> _logger;

        public MediaFileEditService(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            UserManager<ApplicationUser> userManager,
            ILogger<MediaFileEditService> logger)
        {
            _context = context;
            _environment = environment;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<MediaFileEditViewModel?> BuildEditViewModelAsync(int fileId, ApplicationUser user)
        {
            var media = await _context.MediaFiles
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.FileId == fileId && m.UserId == user.Id);

            if (media == null)
            {
                return null;
            }

            var isImage = IsImageFile(media);
            var isVideo = IsVideoFile(media);
            var isText = IsTextFile(media);

            var viewModel = new MediaFileEditViewModel
            {
                FileId = media.FileId,
                Title = media.Title,
                Description = media.Description,
                Tags = media.Tags,
                Category = media.Category,
                Visibility = NormalizeVisibility(media.Visibility),
                CreatedAt = media.CreatedAt,
                UpdatedAt = media.UpdatedAt,
                FileSize = media.FileSize,
                FileType = media.FileType,
                FileFormat = media.FileFormat,
                FilePath = media.FilePath,
                ThumbnailPath = media.ThumbnailPath,
                DurationSeconds = media.DurationSeconds,
                IsImageFile = isImage,
                IsVideoFile = isVideo,
                IsTextFile = isText,
                TrimStartSeconds = 0,
                TrimEndSeconds = media.DurationSeconds,
                MaximumTextFileBytes = MaxTextFileBytes,
                MaximumUploadBytes = MaxFileSizeBytes
            };

            viewModel.VisibilityOptions = new[]
            {
                new SelectListItem("Private", "Private", string.Equals(viewModel.Visibility, "Private", StringComparison.OrdinalIgnoreCase)),
                new SelectListItem("Public", "Public", string.Equals(viewModel.Visibility, "Public", StringComparison.OrdinalIgnoreCase))
            };

            if (isText)
            {
                var physicalPath = ResolvePhysicalPath(media.FilePath);
                if (physicalPath != null && System.IO.File.Exists(physicalPath))
                {
                    try
                    {
                        viewModel.TextContent = await System.IO.File.ReadAllTextAsync(physicalPath);
                    }
                    catch (IOException ex)
                    {
                        _logger.LogWarning(ex, "Failed to read text content for media file {FileId}", media.FileId);
                        viewModel.TextContent = string.Empty;
                    }
                }
            }

            if (!string.IsNullOrEmpty(media.UpdatedBy))
            {
                var updater = await _userManager.FindByIdAsync(media.UpdatedBy);
                if (updater != null)
                {
                    viewModel.UpdatedByDisplayName = GuessDisplayName(updater);
                }
            }

            if (string.IsNullOrWhiteSpace(viewModel.UpdatedByDisplayName))
            {
                viewModel.UpdatedByDisplayName = GuessDisplayName(user);
            }

            return viewModel;
        }

        public async Task<MediaFileEditResult> UpdateAsync(MediaFileEditViewModel model, ApplicationUser user)
        {
            var result = new MediaFileEditResult();

            var media = await _context.MediaFiles
                .FirstOrDefaultAsync(m => m.FileId == model.FileId && m.UserId == user.Id);

            if (media == null)
            {
                result.Errors.Add(new MediaFileEditError(null, "The requested file could not be found."));
                return result;
            }

            ValidateMetadata(model, result);

            if (IsTextFile(media))
            {
                ValidateTextContent(model.TextContent, result);
            }

            if (IsImageFile(media) && model.ImageReplacement != null)
            {
                ValidateImageReplacement(model.ImageReplacement, result);
            }

            if (IsVideoFile(media))
            {
                ValidateVideoTrim(model, media, result);
            }

            if (!result.Succeeded)
            {
                return result;
            }

            var originalFilePath = media.FilePath;
            var originalThumbnailPath = media.ThumbnailPath;

            string? newPhysicalPath = null;
            string? newRelativePath = null;
            string? newStoredFileName = null;
            string? newThumbnailPath = null;
            string? newFileFormat = null;
            long? newFileSize = null;
            double? newDuration = null;

            if (IsTextFile(media))
            {
                var writeOutcome = await WriteTextContentAsync(media, model.TextContent ?? string.Empty);
                if (!writeOutcome.Succeeded)
                {
                    result.Errors.AddRange(writeOutcome.Errors);
                    return result;
                }

                newFileSize = writeOutcome.FileSize;
            }

            if (IsImageFile(media) && model.ImageReplacement != null)
            {
                var imageOutcome = await ReplaceImageAsync(media, model.ImageReplacement);
                if (!imageOutcome.Succeeded)
                {
                    result.Errors.AddRange(imageOutcome.Errors);
                    return result;
                }

                newPhysicalPath = imageOutcome.PhysicalPath;
                newRelativePath = imageOutcome.RelativePath;
                newStoredFileName = imageOutcome.StoredFileName;
                newThumbnailPath = imageOutcome.ThumbnailPath;
                newFileSize = imageOutcome.FileSize;
                newFileFormat = imageOutcome.FileFormat;
                media.OriginalFileName = model.ImageReplacement.FileName;
                media.ContentType = model.ImageReplacement.ContentType ?? media.ContentType;
            }

            if (IsVideoFile(media) && (model.TrimStartSeconds.HasValue || model.TrimEndSeconds.HasValue))
            {
                var videoOutcome = await TrimVideoAsync(media, model);
                if (!videoOutcome.Succeeded)
                {
                    result.Errors.AddRange(videoOutcome.Errors);
                    return result;
                }

                newPhysicalPath = videoOutcome.PhysicalPath;
                newRelativePath = videoOutcome.RelativePath;
                newStoredFileName = videoOutcome.StoredFileName;
                newThumbnailPath = videoOutcome.ThumbnailPath;
                newFileSize = videoOutcome.FileSize;
                newDuration = videoOutcome.DurationSeconds;
                newFileFormat = videoOutcome.FileFormat;
            }

            media.Title = model.Title.Trim();
            media.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
            media.Tags = string.IsNullOrWhiteSpace(model.Tags) ? string.Empty : model.Tags.Trim();
            media.Category = string.IsNullOrWhiteSpace(model.Category) ? null : model.Category.Trim();
            media.Visibility = NormalizeVisibility(model.Visibility);
            media.UpdatedAt = DateTime.UtcNow;
            media.UpdatedBy = user.Id;

            if (!string.IsNullOrEmpty(newStoredFileName) && !string.IsNullOrEmpty(newRelativePath))
            {
                media.StoredFileName = newStoredFileName;
                media.FilePath = newRelativePath;
            }

            if (!string.IsNullOrEmpty(newThumbnailPath))
            {
                media.ThumbnailPath = newThumbnailPath;
            }

            if (!string.IsNullOrEmpty(newFileFormat))
            {
                media.FileFormat = newFileFormat;
            }

            if (newFileSize.HasValue)
            {
                media.FileSize = newFileSize.Value;
            }

            if (newDuration.HasValue)
            {
                media.DurationSeconds = newDuration.Value;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Failed to persist media file update for {FileId}", media.FileId);
                result.Errors.Add(new MediaFileEditError(null, "We couldn't save your changes. Please try again."));
                return result;
            }

            var originalPhysicalPath = ResolvePhysicalPath(originalFilePath);
            if (!string.IsNullOrEmpty(newPhysicalPath) &&
                originalPhysicalPath != null &&
                !string.Equals(Path.GetFullPath(newPhysicalPath), Path.GetFullPath(originalPhysicalPath), StringComparison.OrdinalIgnoreCase))
            {
                TryDeletePhysicalFile(originalFilePath);
            }

            if (!string.IsNullOrEmpty(newThumbnailPath) &&
                !string.Equals(newThumbnailPath, originalThumbnailPath, StringComparison.OrdinalIgnoreCase))
            {
                TryDeletePhysicalFile(originalThumbnailPath);
            }

            result.UpdatedFile = media;
            return result;
        }

        private void ValidateMetadata(MediaFileEditViewModel model, MediaFileEditResult result)
        {
            if (string.IsNullOrWhiteSpace(model.Title))
            {
                result.Errors.Add(new MediaFileEditError(nameof(model.Title), "Title is required."));
            }
            else if (model.Title.Length > 200)
            {
                result.Errors.Add(new MediaFileEditError(nameof(model.Title), "Title cannot exceed 200 characters."));
            }

            if (!string.IsNullOrEmpty(model.Tags) && model.Tags.Length > 500)
            {
                result.Errors.Add(new MediaFileEditError(nameof(model.Tags), "Tags must be 500 characters or fewer."));
            }

            if (!string.IsNullOrEmpty(model.Category) && model.Category.Length > 100)
            {
                result.Errors.Add(new MediaFileEditError(nameof(model.Category), "Category must be 100 characters or fewer."));
            }

            if (!IsValidVisibility(model.Visibility))
            {
                result.Errors.Add(new MediaFileEditError(nameof(model.Visibility), "Visibility selection is invalid."));
            }

            if (!string.IsNullOrEmpty(model.Description) && model.Description.Length > 2000)
            {
                result.Errors.Add(new MediaFileEditError(nameof(model.Description), "Description must be 2000 characters or fewer."));
            }
        }

        private void ValidateTextContent(string? textContent, MediaFileEditResult result)
        {
            if (string.IsNullOrWhiteSpace(textContent))
            {
                result.Errors.Add(new MediaFileEditError(nameof(MediaFileEditViewModel.TextContent), "File content cannot be empty."));
                return;
            }

            var byteCount = Encoding.UTF8.GetByteCount(textContent);
            if (byteCount > MaxTextFileBytes)
            {
                result.Errors.Add(new MediaFileEditError(nameof(MediaFileEditViewModel.TextContent),
                    $"Text content exceeds the {MaxTextFileBytes / (1024 * 1024)}MB limit."));
            }
        }

        private void ValidateImageReplacement(IFormFile file, MediaFileEditResult result)
        {
            if (file.Length == 0)
            {
                result.Errors.Add(new MediaFileEditError(nameof(MediaFileEditViewModel.ImageReplacement), "Please choose a non-empty image."));
                return;
            }

            if (file.Length > MaxFileSizeBytes)
            {
                result.Errors.Add(new MediaFileEditError(nameof(MediaFileEditViewModel.ImageReplacement),
                    $"Image exceeds the {MaxFileSizeBytes / (1024 * 1024)}MB limit."));
            }

            var extension = Path.GetExtension(file.FileName);
            if (!ImageFileExtensions.Contains(extension))
            {
                result.Errors.Add(new MediaFileEditError(nameof(MediaFileEditViewModel.ImageReplacement),
                    $"Unsupported image format. Supported formats: {string.Join(", ", ImageFileExtensions.OrderBy(x => x))}"));
            }
        }

        private void ValidateVideoTrim(MediaFileEditViewModel model, MediaFile media, MediaFileEditResult result)
        {
            var hasStart = model.TrimStartSeconds.HasValue;
            var hasEnd = model.TrimEndSeconds.HasValue;

            if (!hasStart && !hasEnd)
            {
                return;
            }

            var start = Math.Max(0, model.TrimStartSeconds ?? 0);
            var end = Math.Max(0, model.TrimEndSeconds ?? media.DurationSeconds ?? 0);

            if (media.DurationSeconds.HasValue && start > media.DurationSeconds.Value + 0.001)
            {
                result.Errors.Add(new MediaFileEditError(nameof(model.TrimStartSeconds), "Start time exceeds the video duration."));
            }

            if (media.DurationSeconds.HasValue && end > media.DurationSeconds.Value + 0.001)
            {
                result.Errors.Add(new MediaFileEditError(nameof(model.TrimEndSeconds), "End time exceeds the video duration."));
            }

            if (end <= start)
            {
                result.Errors.Add(new MediaFileEditError(nameof(model.TrimEndSeconds), "End time must be greater than start time."));
            }
        }

        private async Task<(bool Succeeded, List<MediaFileEditError> Errors, long FileSize)> WriteTextContentAsync(MediaFile media, string content)
        {
            var physicalPath = ResolvePhysicalPath(media.FilePath);
            if (physicalPath == null)
            {
                return (false, new List<MediaFileEditError>
                {
                    new MediaFileEditError(nameof(MediaFileEditViewModel.TextContent),
                        "Unable to resolve the file path for the text document.")
                }, 0);
            }

            try
            {
                await System.IO.File.WriteAllTextAsync(physicalPath, content, Encoding.UTF8);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Failed to write text content for media file {FileId}", media.FileId);
                return (false, new List<MediaFileEditError>
                {
                    new MediaFileEditError(nameof(MediaFileEditViewModel.TextContent), "Unable to update the text file on disk.")
                }, 0);
            }

            return (true, new List<MediaFileEditError>(), Encoding.UTF8.GetByteCount(content));
        }

        private async Task<(bool Succeeded, List<MediaFileEditError> Errors, string PhysicalPath, string StoredFileName, string RelativePath, string? ThumbnailPath, long FileSize, string FileFormat)> ReplaceImageAsync(MediaFile media, IFormFile replacement)
        {
            var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "pictures");
            Directory.CreateDirectory(uploadsPath);

            var extension = Path.GetExtension(replacement.FileName);
            var storedFileName = GenerateSafeFileName(Path.GetFileNameWithoutExtension(replacement.FileName), extension);
            var physicalPath = Path.Combine(uploadsPath, storedFileName);

            try
            {
                await using var stream = System.IO.File.Create(physicalPath);
                await replacement.CopyToAsync(stream);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Failed to persist replacement image for media file {FileId}", media.FileId);
                return (false, new List<MediaFileEditError>
                {
                    new MediaFileEditError(nameof(MediaFileEditViewModel.ImageReplacement), "Unable to store the new image.")
                }, string.Empty, string.Empty, string.Empty, null, 0, string.Empty);
            }

            var relativePath = $"/uploads/pictures/{storedFileName}";
            var thumbnailPath = await TryGenerateImageThumbnailAsync(physicalPath, storedFileName);

            return (true, new List<MediaFileEditError>(), physicalPath, storedFileName, relativePath, thumbnailPath, replacement.Length, extension.ToLowerInvariant());
        }

        private async Task<(bool Succeeded, List<MediaFileEditError> Errors, string PhysicalPath, string StoredFileName, string RelativePath, string? ThumbnailPath, long FileSize, double DurationSeconds, string FileFormat)> TrimVideoAsync(MediaFile media, MediaFileEditViewModel model)
        {
            var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "videos");
            Directory.CreateDirectory(uploadsPath);

            var extension = Path.GetExtension(media.StoredFileName);
            if (string.IsNullOrEmpty(extension))
            {
                extension = media.FileFormat;
            }

            if (!extension.StartsWith(".", StringComparison.Ordinal))
            {
                extension = "." + extension.Trim();
            }

            var start = Math.Max(0, model.TrimStartSeconds ?? 0);
            var end = Math.Max(0, model.TrimEndSeconds ?? media.DurationSeconds ?? 0);
            var clipDuration = end - start;

            if (clipDuration <= 0)
            {
                return (false, new List<MediaFileEditError>
                {
                    new MediaFileEditError(nameof(model.TrimEndSeconds), "End time must be greater than start time.")
                }, string.Empty, string.Empty, string.Empty, null, 0, 0, string.Empty);
            }

            var safeBaseName = Path.GetFileNameWithoutExtension(media.StoredFileName);
            var newStoredFileName = model.OverwriteExistingVideo
                ? $"{safeBaseName}-trimmed-temp{extension}"
                : GenerateSafeFileName(media.Title, extension);
            var trimmedPhysicalPath = Path.Combine(uploadsPath, newStoredFileName);
            var originalPhysicalPath = ResolvePhysicalPath(media.FilePath);

            if (originalPhysicalPath == null || !System.IO.File.Exists(originalPhysicalPath))
            {
                return (false, new List<MediaFileEditError>
                {
                    new MediaFileEditError(null, "The original video file could not be located on disk.")
                }, string.Empty, string.Empty, string.Empty, null, 0, 0, string.Empty);
            }

            var startTimestamp = FormatTimestamp(start);
            var durationTimestamp = FormatTimestamp(clipDuration);

            var startInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            startInfo.ArgumentList.Add("-y");
            startInfo.ArgumentList.Add("-ss");
            startInfo.ArgumentList.Add(startTimestamp);
            startInfo.ArgumentList.Add("-i");
            startInfo.ArgumentList.Add(originalPhysicalPath);
            startInfo.ArgumentList.Add("-t");
            startInfo.ArgumentList.Add(durationTimestamp);
            startInfo.ArgumentList.Add("-c");
            startInfo.ArgumentList.Add("copy");
            startInfo.ArgumentList.Add(trimmedPhysicalPath);

            try
            {
                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    return (false, new List<MediaFileEditError>
                    {
                        new MediaFileEditError(null, "We couldn't start the video trimming process.")
                    }, string.Empty, string.Empty, string.Empty, null, 0, 0, string.Empty);
                }

                var stderrTask = process.StandardError.ReadToEndAsync();
                var stdoutTask = process.StandardOutput.ReadToEndAsync();

                await Task.WhenAll(stdoutTask, stderrTask);
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    _logger.LogWarning("ffmpeg exited with code {Code} while trimming video {FileId}. stderr: {Error}", process.ExitCode, media.FileId, stderrTask.Result);
                    return (false, new List<MediaFileEditError>
                    {
                        new MediaFileEditError(null, "Video trimming failed. Please try again.")
                    }, string.Empty, string.Empty, string.Empty, null, 0, 0, string.Empty);
                }
            }
            catch (Exception ex) when (ex is Win32Exception || ex is InvalidOperationException)
            {
                _logger.LogError(ex, "Failed to trim video for media file {FileId}", media.FileId);
                return (false, new List<MediaFileEditError>
                {
                    new MediaFileEditError(null, "Video trimming failed due to a system error.")
                }, string.Empty, string.Empty, string.Empty, null, 0, 0, string.Empty);
            }

            var finalStoredFileName = media.StoredFileName;
            var finalPhysicalPath = trimmedPhysicalPath;
            var relativePath = media.FilePath;

            if (model.OverwriteExistingVideo)
            {
                try
                {
                    System.IO.File.Copy(trimmedPhysicalPath, originalPhysicalPath, overwrite: true);
                    System.IO.File.Delete(trimmedPhysicalPath);
                    finalPhysicalPath = originalPhysicalPath;
                    finalStoredFileName = media.StoredFileName;
                    relativePath = media.FilePath;
                }
                catch (IOException ex)
                {
                    _logger.LogError(ex, "Failed to overwrite original video for media file {FileId}", media.FileId);
                    return (false, new List<MediaFileEditError>
                    {
                        new MediaFileEditError(null, "We couldn't replace the original video file.")
                    }, string.Empty, string.Empty, string.Empty, null, 0, 0, string.Empty);
                }
            }
            else
            {
                finalStoredFileName = newStoredFileName;
                finalPhysicalPath = trimmedPhysicalPath;
                relativePath = $"/uploads/videos/{newStoredFileName}";
            }

            long fileSize;
            try
            {
                fileSize = new FileInfo(finalPhysicalPath).Length;
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Failed to read file info for trimmed video {FileId}", media.FileId);
                return (false, new List<MediaFileEditError>
                {
                    new MediaFileEditError(null, "Unable to finalize the trimmed video file.")
                }, string.Empty, string.Empty, string.Empty, null, 0, 0, string.Empty);
            }

            var thumbnailPath = await TryGenerateVideoThumbnailAsync(finalPhysicalPath, finalStoredFileName);

            return (true, new List<MediaFileEditError>(), finalPhysicalPath, finalStoredFileName, relativePath, thumbnailPath, fileSize, clipDuration, extension.ToLowerInvariant());
        }

        private async Task<string?> TryGenerateImageThumbnailAsync(string imagePhysicalPath, string storedFileName)
        {
            var thumbnailsRoot = Path.Combine(_environment.WebRootPath, "uploads", "thumbnails");
            Directory.CreateDirectory(thumbnailsRoot);

            var thumbnailFileName = $"{Path.GetFileNameWithoutExtension(storedFileName)}-thumb.jpg";
            var thumbnailPhysicalPath = Path.Combine(thumbnailsRoot, thumbnailFileName);

            var startInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            startInfo.ArgumentList.Add("-y");
            startInfo.ArgumentList.Add("-i");
            startInfo.ArgumentList.Add(imagePhysicalPath);
            startInfo.ArgumentList.Add("-vf");
            startInfo.ArgumentList.Add("scale=400:-1");
            startInfo.ArgumentList.Add("-frames:v");
            startInfo.ArgumentList.Add("1");
            startInfo.ArgumentList.Add("-q:v");
            startInfo.ArgumentList.Add("2");
            startInfo.ArgumentList.Add(thumbnailPhysicalPath);

            try
            {
                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    return null;
                }

                var stderrTask = process.StandardError.ReadToEndAsync();
                var stdoutTask = process.StandardOutput.ReadToEndAsync();

                await Task.WhenAll(stdoutTask, stderrTask);
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    _logger.LogWarning("ffmpeg exited with code {Code} while generating image thumbnail for {Path}", process.ExitCode, imagePhysicalPath);
                    return null;
                }

                return "/uploads/thumbnails/" + thumbnailFileName;
            }
            catch (Exception ex) when (ex is Win32Exception || ex is InvalidOperationException)
            {
                _logger.LogWarning(ex, "Failed to generate image thumbnail for {Path}", imagePhysicalPath);
            }

            return null;
        }

        private async Task<string?> TryGenerateVideoThumbnailAsync(string videoPhysicalPath, string storedFileName)
        {
            var thumbnailsRoot = Path.Combine(_environment.WebRootPath, "uploads", "thumbnails");
            Directory.CreateDirectory(thumbnailsRoot);

            var thumbnailFileName = $"{Path.GetFileNameWithoutExtension(storedFileName)}-thumb.jpg";
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

                var stderrTask = process.StandardError.ReadToEndAsync();
                var stdoutTask = process.StandardOutput.ReadToEndAsync();

                await Task.WhenAll(stdoutTask, stderrTask);
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    _logger.LogWarning("ffmpeg exited with code {Code} while generating video thumbnail for {Path}", process.ExitCode, videoPhysicalPath);
                    return null;
                }

                return "/uploads/thumbnails/" + thumbnailFileName;
            }
            catch (Exception ex) when (ex is Win32Exception || ex is InvalidOperationException)
            {
                _logger.LogWarning(ex, "Failed to generate video thumbnail for {Path}", videoPhysicalPath);
            }

            return null;
        }

        private string? ResolvePhysicalPath(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return null;
            }

            if (Path.IsPathRooted(relativePath))
            {
                return relativePath;
            }

            var trimmedPath = relativePath.TrimStart('~', '/', '\\')
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);

            return Path.Combine(_environment.WebRootPath, trimmedPath);
        }

        private void TryDeletePhysicalFile(string? relativePath)
        {
            var physicalPath = ResolvePhysicalPath(relativePath);
            if (physicalPath == null)
            {
                return;
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
                _logger.LogWarning(ex, "Failed to delete file at path {PhysicalPath}", physicalPath);
            }
        }

        private static string GuessDisplayName(ApplicationUser user)
        {
            if (!string.IsNullOrWhiteSpace(user.DisplayName))
            {
                return user.DisplayName;
            }

            if (!string.IsNullOrWhiteSpace(user.FullName))
            {
                return user.FullName;
            }

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                return user.Email!;
            }

            return user.UserName ?? string.Empty;
        }

        private static string GenerateSafeFileName(string title, string extension)
        {
            var slugSource = (title ?? string.Empty).ToLowerInvariant();
            var slug = Regex.Replace(slugSource, @"[^a-z0-9]+", "-").Trim('-');
            if (string.IsNullOrEmpty(slug))
            {
                slug = "file";
            }

            if (!extension.StartsWith(".", StringComparison.Ordinal))
            {
                extension = "." + extension;
            }

            return $"{slug}-{Guid.NewGuid():N}{extension}";
        }

        private static string FormatTimestamp(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0)
            {
                seconds = 0;
            }

            var time = TimeSpan.FromSeconds(seconds);
            return time.ToString(@"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture);
        }

        private static bool IsImageFile(MediaFile media)
        {
            if (string.Equals(media.FileType, "Picture", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return ImageFileExtensions.Contains(media.FileFormat);
        }

        private static bool IsVideoFile(MediaFile media)
        {
            if (string.Equals(media.FileType, "Video", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return VideoFileExtensions.Contains(media.FileFormat);
        }

        private static bool IsTextFile(MediaFile media)
        {
            return TextFileExtensions.Contains(media.FileFormat);
        }

        private static bool IsValidVisibility(string? value) =>
            !string.IsNullOrWhiteSpace(value) &&
            VisibilityValues.Any(v => string.Equals(v, value, StringComparison.OrdinalIgnoreCase));

        private static string NormalizeVisibility(string? value) =>
            VisibilityValues.FirstOrDefault(v => string.Equals(v, value, StringComparison.OrdinalIgnoreCase)) ?? VisibilityValues[0];
    }
}
