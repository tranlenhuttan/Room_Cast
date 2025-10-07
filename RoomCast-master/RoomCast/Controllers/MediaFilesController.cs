using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoomCast.Data;
using RoomCast.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RoomCast.Controllers
{
    [Authorize]
    public class MediaFilesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly UserManager<IdentityUser> _userManager;

        public MediaFilesController(ApplicationDbContext context, IWebHostEnvironment environment, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _environment = environment;
            _userManager = userManager;
        }

        // GET: List Files
        public async Task<IActionResult> Index(string searchTags)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var files = _context.MediaFiles.Where(f => f.UserId == user.Id);

            if (!string.IsNullOrEmpty(searchTags))
            {
                files = files.Where(f => f.Tags.Contains(searchTags));
            }

            return View(await files.ToListAsync());
        }

        // GET: Upload Page
        public IActionResult Upload()
        {
            return View();
        }

        // POST: Upload File
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile file, string tags, string fileType)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("", "Please select a file.");
                return View();
            }

            if (file.Length > 50 * 1024 * 1024)
            {
                ModelState.AddModelError("", "File size exceeds 50MB limit.");
                return View();
            }

            string extension = Path.GetExtension(file.FileName).ToLower();
            string[] allowedDocs = { ".doc", ".docx", ".pdf" };
            string[] allowedImages = { ".jpg", ".jpeg" };
            string[] allowedVideos = { ".mp4" };

            bool validFormat = (fileType == "Document" && allowedDocs.Contains(extension))
                            || (fileType == "Image" && allowedImages.Contains(extension))
                            || (fileType == "Video" && allowedVideos.Contains(extension));

            if (!validFormat)
            {
                ModelState.AddModelError("", "Unsupported file format.");
                return View();
            }

            string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var media = new MediaFile
            {
                UserId = user.Id,
                FileType = fileType,
                FileFormat = extension,
                FileName = file.FileName,
                Tags = tags ?? string.Empty,
                Timestamp = DateTime.UtcNow,
                FilePath = "/uploads/" + uniqueFileName
            };

            _context.MediaFiles.Add(media);
            await _context.SaveChangesAsync();

            TempData["Message"] = "File uploaded successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Details
        public async Task<IActionResult> Details(int id)
        {
            var file = await _context.MediaFiles.FirstOrDefaultAsync(m => m.FileId == id);
            if (file == null) return NotFound();

            return View(file);
        }
    }
}


