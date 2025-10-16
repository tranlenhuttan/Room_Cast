using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;

namespace RoomCast.Models.ViewModels
{
    public class FileUploadViewModel
    {
        [StringLength(200)]
        [Display(Name = "File Name")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Tags (comma-separated)")]
        [StringLength(500)]
        public string? Tags { get; set; }

        [Required]
        [Display(Name = "File")]
        public IFormFile? File { get; set; }

        [BindNever]
        public IReadOnlyList<string> AllowedExtensions { get; set; } = Array.Empty<string>();

        [BindNever]
        public IReadOnlyDictionary<string, IReadOnlyList<string>> AllowedExtensionsByType { get; set; }
            = new Dictionary<string, IReadOnlyList<string>>();
    }
}
