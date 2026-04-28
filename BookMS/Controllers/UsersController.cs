using BookMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace BookMS.Controllers
{
    [Authorize]
    public class UsersController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<UsersController> _logger;

        public UsersController(UserManager<AppUser> um, SignInManager<AppUser> sm, IWebHostEnvironment env, ILogger<UsersController> logger)
        {
            _userManager = um;
            _signInManager = sm;
            _env = env;
            _logger = logger;
        }

        [Authorize(Roles = "Admin,Staff,SuperAdmin")]
        public async Task<IActionResult> Index()
        {
            var users = _userManager.Users.ToList();
            return View(users);
        }

        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var vm = new ProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber ?? "",
                ProfileImage = user.ProfileImage
            };

            if (User.IsInRole("Customer")) ViewData["Layout"] = "_LayoutCustomer";
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(10 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)]
        public async Task<IActionResult> Profile(ProfileViewModel vm)
        {
            _logger.LogWarning("=== PROFILE POST CALLED ===");
            _logger.LogWarning("FullName: {FullName}", vm.FullName);
            _logger.LogWarning("PhoneNumber: {Phone}", vm.PhoneNumber);
            _logger.LogWarning("ImageFile is null: {IsNull}", vm.ImageFile == null);
            _logger.LogWarning("ImageFile length: {Len}", vm.ImageFile?.Length ?? 0);

            if (User.IsInRole("Customer")) ViewData["Layout"] = "_LayoutCustomer";

            ModelState.Remove("Email");
            ModelState.Remove("ProfileImage");

            _logger.LogWarning("ModelState.IsValid: {Valid}", ModelState.IsValid);
            foreach (var ms in ModelState)
            {
                if (ms.Value.Errors.Count > 0)
                    _logger.LogWarning("ModelState Error [{Key}]: {Error}", ms.Key, ms.Value.Errors[0].ErrorMessage);
            }

            if (!ModelState.IsValid)
            {
                var cu = await _userManager.GetUserAsync(User);
                if (cu != null) { vm.Email = cu.Email ?? ""; vm.ProfileImage = cu.ProfileImage; }
                TempData["Error"] = "Validation failed. Check your input.";
                return View(vm);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            vm.Email = user.Email ?? "";

            // Handle profile image upload
            if (vm.ImageFile != null && vm.ImageFile.Length > 0)
            {
                _logger.LogWarning("Processing image: {Name}, size: {Size}", vm.ImageFile.FileName, vm.ImageFile.Length);
                try
                {
                    var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                    var ext = Path.GetExtension(vm.ImageFile.FileName).ToLowerInvariant();

                    if (!allowed.Contains(ext))
                    {
                        TempData["Error"] = $"File type '{ext}' not allowed. Use JPG, PNG, WEBP.";
                        vm.ProfileImage = user.ProfileImage;
                        return View(vm);
                    }

                    if (vm.ImageFile.Length > 5 * 1024 * 1024)
                    {
                        TempData["Error"] = "Image must be under 5MB.";
                        vm.ProfileImage = user.ProfileImage;
                        return View(vm);
                    }

                    var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    var folder = Path.Combine(webRoot, "uploads", "profiles");
                    _logger.LogWarning("Saving to folder: {Folder}", folder);

                    Directory.CreateDirectory(folder);

                    if (!string.IsNullOrEmpty(user.ProfileImage))
                    {
                        var oldPath = Path.Combine(webRoot, user.ProfileImage.TrimStart('/'));
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                    }

                    var fileName = $"{user.Id}_{DateTime.Now:yyyyMMddHHmmss}{ext}";
                    var filePath = Path.Combine(folder, fileName);

                    using (var fs = new FileStream(filePath, FileMode.Create))
                    {
                        await vm.ImageFile.CopyToAsync(fs);
                    }

                    user.ProfileImage = $"/uploads/profiles/{fileName}";
                    _logger.LogWarning("Image saved: {Path}", user.ProfileImage);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Image upload failed");
                    TempData["Error"] = $"Image upload failed: {ex.Message}";
                    vm.ProfileImage = user.ProfileImage;
                    return View(vm);
                }
            }
            else
            {
                _logger.LogWarning("No image file submitted — skipping image update");
            }

            user.FullName = vm.FullName;
            user.PhoneNumber = vm.PhoneNumber;

            _logger.LogWarning("Calling UpdateAsync...");
            var result = await _userManager.UpdateAsync(user);
            _logger.LogWarning("UpdateAsync result: {Succeeded}", result.Succeeded);

            if (result.Succeeded)
            {
                TempData["Success"] = "Profile updated successfully!";
                return RedirectToAction(nameof(Profile));
            }

            foreach (var err in result.Errors)
            {
                _logger.LogWarning("UpdateAsync error: {Err}", err.Description);
                ModelState.AddModelError("", err.Description);
            }

            TempData["Error"] = "Failed to save profile. " + string.Join(", ", result.Errors.Select(e => e.Description));
            vm.ProfileImage = user.ProfileImage;
            return View(vm);
        }

        public IActionResult ChangePassword()
        {
            if (User.IsInRole("Customer")) ViewData["Layout"] = "_LayoutCustomer";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel vm)
        {
            if (User.IsInRole("Customer")) ViewData["Layout"] = "_LayoutCustomer";
            if (!ModelState.IsValid) return View(vm);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var result = await _userManager.ChangePasswordAsync(user, vm.CurrentPassword, vm.NewPassword);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                TempData["Success"] = "Password changed successfully!";
                return RedirectToAction(nameof(Profile));
            }

            foreach (var err in result.Errors) ModelState.AddModelError("", err.Description);
            return View(vm);
        }
    }

    public class ProfileViewModel
    {
        [Required] public string FullName { get; set; } = string.Empty;
        [EmailAddress] public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? ProfileImage { get; set; }
        public IFormFile? ImageFile { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required][DataType(DataType.Password)] public string CurrentPassword { get; set; } = string.Empty;
        [Required][DataType(DataType.Password)][MinLength(6)] public string NewPassword { get; set; } = string.Empty;
        [Compare("NewPassword")] public string ConfirmPassword { get; set; } = string.Empty;
    }
}