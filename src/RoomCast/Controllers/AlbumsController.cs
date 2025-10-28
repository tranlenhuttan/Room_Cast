using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoomCast.Data;
using RoomCast.Models;

namespace RoomCast.Controllers
{
    [Authorize]
    public class AlbumsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AlbumsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: My Albums
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var albums = await _context.Albums
                .Where(a => a.UserId == user.Id)
                .Include(a => a.AlbumFiles)
                .ThenInclude(af => af.MediaFile)
                .ToListAsync();

            return View(albums);
        }

        // GET: Create Album
        public IActionResult Create()
        {
            return View();
        }

        // POST: Create Album
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Album album)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (ModelState.IsValid)
            {
                album.UserId = user.Id;
                album.Timestamp = System.DateTime.UtcNow;
                _context.Albums.Add(album);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(album);
        }

        // GET: Add File to Album
        public async Task<IActionResult> AddFile(int id)
        {
            var album = await _context.Albums.FindAsync(id);
            if (album == null) return NotFound();

            ViewBag.Files = await _context.MediaFiles.ToListAsync();
            ViewBag.AlbumId = id;

            return View();
        }

        // POST: Add File to Album
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFile(int albumId, int fileId)
        {
            var album = await _context.Albums.FindAsync(albumId);
            var file = await _context.MediaFiles.FindAsync(fileId);

            if (album == null || file == null) return NotFound();

            var albumFile = new AlbumFile
            {
                AlbumId = albumId,
                FileId = fileId
            };

            _context.AlbumFiles.Add(albumFile);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Album Details (Playback)
        public async Task<IActionResult> Details(int id)
        {
            var album = await _context.Albums
                .Include(a => a.AlbumFiles)
                .ThenInclude(af => af.MediaFile)
                .FirstOrDefaultAsync(a => a.AlbumId == id);

            if (album == null) return NotFound();

            return View(album);
        }

        // GET: Edit Album
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var album = await _context.Albums.FindAsync(id);
            if (album == null) return NotFound();

            // Ensure user owns this album
            if (album.UserId != user.Id) return Forbid();

            return View(album);
        }

        // POST: Edit Album
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Album album)
        {
            if (id != album.AlbumId) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var existingAlbum = await _context.Albums.FindAsync(id);
            if (existingAlbum == null) return NotFound();

            // Ensure user owns this album
            if (existingAlbum.UserId != user.Id) return Forbid();

            if (ModelState.IsValid)
            {
                existingAlbum.AlbumName = album.AlbumName;
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(album);
        }

        // GET: Delete Album (Confirmation)
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var album = await _context.Albums
                .Include(a => a.AlbumFiles)
                .ThenInclude(af => af.MediaFile)
                .FirstOrDefaultAsync(a => a.AlbumId == id);

            if (album == null) return NotFound();

            // Ensure user owns this album
            if (album.UserId != user.Id) return Forbid();

            return View(album);
        }

        // POST: Delete Album
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var album = await _context.Albums
                .Include(a => a.AlbumFiles)
                .FirstOrDefaultAsync(a => a.AlbumId == id);

            if (album == null) return NotFound();

            // Ensure user owns this album
            if (album.UserId != user.Id) return Forbid();

            // Use a transaction to ensure data consistency
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Remove all album files associations
                _context.AlbumFiles.RemoveRange(album.AlbumFiles);
                
                // Remove the album itself
                _context.Albums.Remove(album);
                
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
