using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RoomCast.Models;

namespace RoomCast.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        //public DbSet<FileUpload> FileUploads { get; set; }
        public DbSet<Album> Albums { get; set; }
        public DbSet<AlbumFile> AlbumFiles { get; set; }
        public DbSet<MediaFile> MediaFiles { get; set; }
    }
}
