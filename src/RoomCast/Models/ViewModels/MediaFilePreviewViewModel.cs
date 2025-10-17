namespace RoomCast.Models.ViewModels
{
    public enum DocumentPreviewMode
    {
        None = 0,
        Pdf,
        PlainText
    }

    public class MediaFilePreviewViewModel
    {
        public int FileId { get; init; }

        public string Title { get; init; } = string.Empty;

        public string FileType { get; init; } = string.Empty;

        public string FileFormat { get; init; } = string.Empty;

        public string ContentType { get; set; } = string.Empty;

        public string FilePath { get; init; } = string.Empty;

        public string? ThumbnailPath { get; init; }

        public double? DurationSeconds { get; init; }

        public long FileSize { get; init; }

        public string FileSizeLabel { get; init; } = string.Empty;

        public DocumentPreviewMode DocumentPreviewMode { get; set; } = DocumentPreviewMode.None;

        public string? DocumentEmbedUrl { get; set; }

        public bool SupportsPreview =>
            string.Equals(FileType, "Picture", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(FileType, "Video", StringComparison.OrdinalIgnoreCase) ||
            DocumentPreviewMode != DocumentPreviewMode.None;
    }
}
