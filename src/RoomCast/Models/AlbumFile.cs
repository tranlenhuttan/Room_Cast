using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoomCast.Models
{
    public class AlbumFile
    {
        [Key]
        public int AlbumFileId { get; set; }

        [ForeignKey(nameof(Album))]
        public int AlbumId { get; set; }

        [ForeignKey(nameof(MediaFile))]
        public int FileId { get; set; }

        // Use null-forgiving since EF will populate navigation properties at runtime
        public Album Album { get; set; } = null!;
        public MediaFile MediaFile { get; set; } = null!;
    }
}
