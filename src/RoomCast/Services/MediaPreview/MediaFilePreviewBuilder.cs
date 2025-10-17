using RoomCast.Models;
using RoomCast.Models.ViewModels;

namespace RoomCast.Services.MediaPreview
{
    public class MediaFilePreviewBuilder : IMediaFilePreviewBuilder
    {
        public MediaFilePreviewViewModel Build(MediaFile mediaFile)
        {
            if (mediaFile is null)
            {
                throw new ArgumentNullException(nameof(mediaFile));
            }

            var contentType = string.IsNullOrWhiteSpace(mediaFile.ContentType)
                ? GuessContentType(mediaFile.FileFormat)
                : mediaFile.ContentType;

            var viewModel = new MediaFilePreviewViewModel
            {
                FileId = mediaFile.FileId,
                Title = mediaFile.Title,
                FileType = mediaFile.FileType,
                FileFormat = mediaFile.FileFormat,
                ContentType = contentType,
                FilePath = mediaFile.FilePath,
                ThumbnailPath = mediaFile.ThumbnailPath,
                DurationSeconds = mediaFile.DurationSeconds,
                FileSize = mediaFile.FileSize,
                FileSizeLabel = FormatFileSize(mediaFile.FileSize),
                DocumentPreviewMode = DocumentPreviewMode.None,
                DocumentEmbedUrl = null
            };

            if (string.Equals(mediaFile.FileType, "Document", StringComparison.OrdinalIgnoreCase))
            {
                ApplyDocumentPreview(viewModel);
            }

            return viewModel;
        }

        private static void ApplyDocumentPreview(MediaFilePreviewViewModel viewModel)
        {
            if (string.IsNullOrWhiteSpace(viewModel.FileFormat))
            {
                viewModel.DocumentPreviewMode = DocumentPreviewMode.None;
                return;
            }

            var extension = viewModel.FileFormat.Trim().ToLowerInvariant();

            switch (extension)
            {
                case ".pdf":
                    viewModel.DocumentPreviewMode = DocumentPreviewMode.Pdf;
                    viewModel.DocumentEmbedUrl = viewModel.FilePath;
                    if (string.IsNullOrWhiteSpace(viewModel.ContentType))
                    {
                        viewModel.ContentType = "application/pdf";
                    }
                    break;
                case ".txt":
                case ".text":
                    viewModel.DocumentPreviewMode = DocumentPreviewMode.PlainText;
                    viewModel.DocumentEmbedUrl = viewModel.FilePath;
                    if (string.IsNullOrWhiteSpace(viewModel.ContentType))
                    {
                        viewModel.ContentType = "text/plain";
                    }
                    break;
                default:
                    viewModel.DocumentPreviewMode = DocumentPreviewMode.None;
                    break;
            }
        }

        private static string FormatFileSize(long bytes)
        {
            if (bytes < 0)
            {
                return "0 B";
            }

            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            double size = bytes;
            int suffixIndex = 0;

            while (size >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                size /= 1024;
                suffixIndex++;
            }

            return suffixIndex == 0
                ? $"{size:0} {suffixes[suffixIndex]}"
                : $"{size:0.##} {suffixes[suffixIndex]}";
        }

        private static string GuessContentType(string fileFormat)
        {
            if (string.IsNullOrWhiteSpace(fileFormat))
            {
                return "application/octet-stream";
            }

            return fileFormat.Trim().ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf",
                ".txt" or ".text" => "text/plain",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                ".mp4" => "video/mp4",
                ".mov" => "video/quicktime",
                ".m4v" => "video/x-m4v",
                ".avi" => "video/x-msvideo",
                ".mkv" => "video/x-matroska",
                _ => "application/octet-stream"
            };
        }
    }
}
