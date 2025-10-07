using System;
using System.ComponentModel.DataAnnotations;

namespace RoomCast.Models
{
    public class MediaFile
    {
        [Key]
        public int FileId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty; // links to AspNetUsers

        [Required]
        public string FileType { get; set; } = string.Empty; // Document, Image, Video

        [Required]
        public string FileFormat { get; set; } = string.Empty; // .docx, .pdf, .jpg, etc.

        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Tags { get; set; } = string.Empty; // comma-separated tags

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [MaxLength(1000)]
        public string FilePath { get; set; } = string.Empty; // /uploads/...
    }
}

