using System.ComponentModel.DataAnnotations;

namespace RoomCast.Models
{
    public class Album
    {
        [Key]
        public int AlbumId { get; set; }

        [Required]
        [MaxLength(255)]
        public string AlbumName { get; set; } = string.Empty;

        [Required]
        public string UserId { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // initialize collection to avoid nullability warnings
        public ICollection<AlbumFile> AlbumFiles { get; set; } = new List<AlbumFile>();
    }
}

