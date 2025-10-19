using System.ComponentModel.DataAnnotations;

namespace RoomCast.Models.ViewModels
{
    public class TextFileEditViewModel
    {
        [Required]
        public int FileId { get; set; }

        [Required]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Original file name")]
        public string OriginalFileName { get; set; } = string.Empty;

        public string? DownloadPath { get; set; }

        public string ContentType { get; set; } = string.Empty;

        public long FileSize { get; set; }

        public string LastSavedLabel { get; set; } = string.Empty;

        [Required]
        [Display(Name = "File contents")]
        [DataType(DataType.MultilineText)]
        public string Content { get; set; } = string.Empty;
    }
}
