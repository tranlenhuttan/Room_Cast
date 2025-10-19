using System.ComponentModel.DataAnnotations;

namespace RoomCast.Models.ViewModels
{
    public class VideoTrimViewModel
    {
        [Required]
        public int FileId { get; set; }

        [Required]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        public string? OriginalFileName { get; set; }

        [Display(Name = "Video path")]
        public string VideoPath { get; set; } = string.Empty;

        public string? ThumbnailPath { get; set; }

        public string StoredFileName { get; set; } = string.Empty;

        [Display(Name = "File format")]
        public string FileFormat { get; set; } = ".mp4";

        [Display(Name = "Content type")]
        public string ContentType { get; set; } = "video/mp4";

        [Display(Name = "File size")]
        public long FileSize { get; set; }

        [Display(Name = "Duration (seconds)")]
        public double DurationSeconds { get; set; }
    }
}
