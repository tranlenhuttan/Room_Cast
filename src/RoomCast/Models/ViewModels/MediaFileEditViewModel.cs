using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace RoomCast.Models.ViewModels
{
    public class MediaFileEditViewModel
    {
        private static readonly IReadOnlyList<SelectListItem> DefaultVisibilityOptions = new[]
        {
            new SelectListItem("Private", "Private"),
            new SelectListItem("Public", "Public")
        };

        public int FileId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Display(Name = "Tags (comma-separated)")]
        [MaxLength(500)]
        public string Tags { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Category { get; set; }

        [Required]
        [MaxLength(20)]
        public string Visibility { get; set; } = "Private";

        public IReadOnlyList<SelectListItem> VisibilityOptions { get; set; } = DefaultVisibilityOptions;

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public string UpdatedByDisplayName { get; set; } = string.Empty;

        public long FileSize { get; set; }

        public string FileType { get; set; } = string.Empty;

        public string FileFormat { get; set; } = string.Empty;

        public string FilePath { get; set; } = string.Empty;

        public string? ThumbnailPath { get; set; }

        public double? DurationSeconds { get; set; }

        public bool IsTextFile { get; set; }

        public bool IsImageFile { get; set; }

        public bool IsVideoFile { get; set; }

        [Display(Name = "File Content")]
        public string? TextContent { get; set; }

        [Display(Name = "Replace Image")]
        public IFormFile? ImageReplacement { get; set; }

        [Display(Name = "Trim From (seconds)")]
        [Range(0, double.MaxValue, ErrorMessage = "Start time must be zero or positive.")]
        public double? TrimStartSeconds { get; set; }

        [Display(Name = "Trim To (seconds)")]
        [Range(0, double.MaxValue, ErrorMessage = "End time must be zero or positive.")]
        public double? TrimEndSeconds { get; set; }

        [Display(Name = "Overwrite Existing Video")]
        public bool OverwriteExistingVideo { get; set; } = true;

        public double? NewDurationSeconds { get; set; }

        public long MaximumTextFileBytes { get; set; }

        public long MaximumUploadBytes { get; set; }
    }
}
