using System.Linq;
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

        private bool IsAjaxRequest()
        {
            if (Request == null) return false;
            if (Request.Headers.TryGetValue("X-Requested-With", out var requestedWith))
            {
                return string.Equals(requestedWith, "XMLHttpRequest", System.StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        private IActionResult BuildRegisterErrorResponse()
        {
            var errorDictionary = ModelState
                .Where(kvp => kvp.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => string.IsNullOrEmpty(kvp.Key) ? "General" : kvp.Key,
                    kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

            return BadRequest(new { errors = errorDictionary });
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (_signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Index", "MediaFiles");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                if (IsAjaxRequest())
                {
                    return BuildRegisterErrorResponse();
                }

                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.Email,
                Date = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                if (_authOptions.AutoLoginAfterRegistration)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    if (IsAjaxRequest())
                    {
                        return Ok(new
                        {
                            success = true,
                            autoLogin = true,
                            redirectUrl = Url.Action("Index", "MediaFiles")
                        });
                    }

                    return RedirectToAction("Index", "MediaFiles");
                }

                TempData["RegisterSuccess"] = true;
                if (IsAjaxRequest())
                {
                    return Ok(new
                    {
                        success = true,
                        autoLogin = false,
                        message = "Registration successful. Please sign in."
                    });
                }

                return RedirectToAction("Login");
            }

            foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
            if (IsAjaxRequest())
            {
                return BuildRegisterErrorResponse();
            }

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
