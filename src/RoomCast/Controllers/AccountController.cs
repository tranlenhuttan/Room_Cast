using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using RoomCast.Models;
using RoomCast.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using RoomCast.Options;

namespace RoomCast.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly AuthenticationOptions _authOptions;

        public AccountController(UserManager<ApplicationUser> userManager,
                                 SignInManager<ApplicationUser> signInManager,
                                 IOptions<AuthenticationOptions> authOptions)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _authOptions = authOptions.Value;
        }

        [HttpGet]
        public IActionResult SignUp()
        {
            if (_signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Index", "MediaFiles");
            }

            return View(new SignUpViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignUp(SignUpViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var trimmedFirstName = model.FirstName.Trim();
            var trimmedLastName = model.LastName.Trim();
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = trimmedFirstName,
                LastName = trimmedLastName,
                FullName = $"{trimmedFirstName} {trimmedLastName}".Trim(),
                Date = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                if (_authOptions.AutoLoginAfterRegistration)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "MediaFiles");
                }

                TempData["RegisterSuccess"] = true;
                return RedirectToAction(nameof(Login));
            }

            foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
            return View(model);
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (_signInManager.IsSignedIn(User))
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "MediaFiles");
            }

            ViewData["ReturnUrl"] = returnUrl;
            if (TempData.ContainsKey("RegisterSuccess"))
            {
                ViewBag.RegistrationSucceeded = true;
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "MediaFiles");
            }

            if (result.IsLockedOut) ModelState.AddModelError("", "Account locked. Try later.");
            else ModelState.AddModelError("", "Invalid login attempt.");

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }
    }
}
