using Microsoft.AspNetCore.Mvc;

namespace RoomCast.Controllers
{
    public class FileController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}

//THIS FILE CONTROLLER HANDLES UPLOAD,EDIT,VIEW FILES
