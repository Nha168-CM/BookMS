using BookMS.Models;
using BookMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BookMS.Controllers
{
    public class AuthController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        public AuthController(UserManager<AppUser> um, SignInManager<AppUser> sm)
        { _userManager = um; _signInManager = sm; }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            if (User.Identity?.IsAuthenticated == true) return RedirectBasedOnRole();
            return View();
        }

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel vm, string? returnUrl = null)
        {
            if (!ModelState.IsValid) { ViewBag.ReturnUrl = returnUrl; return View(vm); }

            // Support login with phone number OR email
            AppUser? user = null;
            if (vm.EmailOrPhone.Contains('@'))
                user = await _userManager.FindByEmailAsync(vm.EmailOrPhone);
            else
            {
                // find by phone number
                user = _userManager.Users.FirstOrDefault(u => u.PhoneNumber == vm.EmailOrPhone);
            }

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email/phone or password.");
                ViewBag.ReturnUrl = returnUrl;
                return View(vm);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError("", "Your account has been disabled. Contact admin.");
                ViewBag.ReturnUrl = returnUrl;
                return View(vm);
            }

            var result = await _signInManager.PasswordSignInAsync(user.UserName!, vm.Password, vm.RememberMe, false);
            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectBasedOnRole();
            }

            ModelState.AddModelError("", "Invalid email/phone or password.");
            ViewBag.ReturnUrl = returnUrl;
            return View(vm);
        }

        [HttpGet] public IActionResult Register(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel vm, string? returnUrl = null)
        {
            // Must provide at least email OR phone
            if (string.IsNullOrWhiteSpace(vm.Email) && string.IsNullOrWhiteSpace(vm.Phone))
                ModelState.AddModelError("", "Please provide either Email or Phone Number.");

            if (!ModelState.IsValid) { ViewBag.ReturnUrl = returnUrl; return View(vm); }

            // Username = email if provided, else phone
            var username = !string.IsNullOrWhiteSpace(vm.Email) ? vm.Email : vm.Phone!;
            var email    = !string.IsNullOrWhiteSpace(vm.Email) ? vm.Email : $"{vm.Phone}@phone.local";

            // Check duplicate phone
            if (!string.IsNullOrWhiteSpace(vm.Phone) &&
                _userManager.Users.Any(u => u.PhoneNumber == vm.Phone))
            {
                ModelState.AddModelError("Phone", "This phone number is already registered.");
                ViewBag.ReturnUrl = returnUrl;
                return View(vm);
            }

            var user = new AppUser
            {
                FullName    = vm.FullName,
                UserName    = username,
                Email       = email,
                PhoneNumber = vm.Phone,
                IsActive    = true
            };

            var result = await _userManager.CreateAsync(user, vm.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, AppRoles.Customer);
                await _signInManager.SignInAsync(user, false);
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToAction("Index", "Shop");
            }

            foreach (var err in result.Errors) ModelState.AddModelError("", err.Description);
            ViewBag.ReturnUrl = returnUrl;
            return View(vm);
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        { await _signInManager.SignOutAsync(); return RedirectToAction(nameof(Login)); }

        [HttpGet]
        public IActionResult GoogleLogin(string? returnUrl = null)
        {
            var redirectUrl = Url.Action(nameof(GoogleCallback), "Auth", new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl);
            return Challenge(properties, "Google");
        }

        [HttpGet]
        public async Task<IActionResult> GoogleCallback(string? returnUrl = null, string? remoteError = null)
        {
            if (remoteError != null)
            {
                ModelState.AddModelError("", $"Error from Google: {remoteError}");
                return RedirectToAction(nameof(Login));
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null) return RedirectToAction(nameof(Login));

            // Try sign in with existing external login
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false);
            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectBasedOnRole();
            }

            // Get email from Google
            var email = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Could not retrieve email from Google.";
                return RedirectToAction(nameof(Login));
            }

            // Check if user already exists with this email
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                // Link Google login to existing account
                await _userManager.AddLoginAsync(user, info);
                await _signInManager.SignInAsync(user, isPersistent: false);
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectBasedOnRole();
            }

            // Create new account
            var fullName = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? email;
            user = new AppUser
            {
                FullName = fullName,
                UserName = email,
                Email = email,
                IsActive = true
            };

            var createResult = await _userManager.CreateAsync(user);
            if (createResult.Succeeded)
            {
                await _userManager.AddLoginAsync(user, info);
                await _userManager.AddToRoleAsync(user, AppRoles.Customer);
                await _signInManager.SignInAsync(user, isPersistent: false);
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToAction("Index", "Shop");
            }

            foreach (var err in createResult.Errors)
                ModelState.AddModelError("", err.Description);
            return RedirectToAction(nameof(Login));
        }

        private IActionResult RedirectBasedOnRole()
        {
            if (User.IsInRole(AppRoles.SuperAdmin) || User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.Staff))
                return RedirectToAction("Index", "Home");
            return RedirectToAction("Index", "Shop");
        }
    }
}
