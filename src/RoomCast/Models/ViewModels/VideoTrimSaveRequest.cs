using System.ComponentModel.DataAnnotations;

namespace RoomCast.Models.ViewModels
{
    public class VideoTrimSaveRequest
    {
        [Required]
        public int FileId { get; set; }

        [Range(0, double.MaxValue)]
        public double StartSeconds { get; set; }

        [Range(0, double.MaxValue)]
        public double EndSeconds { get; set; }

        [Range(0, double.MaxValue)]
        public double? SourceDurationSeconds { get; set; }
    }
}
