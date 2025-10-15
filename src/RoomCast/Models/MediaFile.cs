using System;
using System.ComponentModel.DataAnnotations;

namespace RoomCast.Models
{
    public class MediaFile
    {
        [Key]
        public int FileId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string FileType { get; set; } = string.Empty; // Document, Picture, Video

        [Required]
        [MaxLength(20)]
        public string FileFormat { get; set; } = string.Empty; // .docx, .pdf, .jpg, etc.

        [MaxLength(255)]
        public string OriginalFileName { get; set; } = string.Empty;

        [MaxLength(255)]
        public string StoredFileName { get; set; } = string.Empty;

        [MaxLength(200)]
        public string ContentType { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [MaxLength(100)]
        public string? Category { get; set; }

        [Required]
        [MaxLength(20)]
        public string Visibility { get; set; } = "Private";

        [MaxLength(500)]
        public string Tags { get; set; } = string.Empty; // comma-separated tags

        public long FileSize { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(450)]
        public string? UpdatedBy { get; set; }

        [MaxLength(1000)]
        public string FilePath { get; set; } = string.Empty; // /uploads/...

        [MaxLength(1000)]
        public string? ThumbnailPath { get; set; }

        public double? DurationSeconds { get; set; }
    }
}
