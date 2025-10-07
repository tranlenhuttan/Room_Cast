
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RoomCast.Data
{
    public class DesignTimeFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite("Data Source=app.db")
                .Options;

            return new ApplicationDbContext(options);
        }
    }
}
