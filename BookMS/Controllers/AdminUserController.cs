using BookMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace BookMS.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class AdminUserController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminUserController(UserManager<AppUser> um, RoleManager<IdentityRole> rm)
        { _userManager = um; _roleManager = rm; }

        public async Task<IActionResult> Index(string? role, string? search)
        {
            var users = await _userManager.Users.ToListAsync();
            var result = new List<UserWithRoleViewModel>();

            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);

                // SuperAdmin sees all; Admin sees only Admin/Staff/Customer (not SuperAdmin)
                if (!User.IsInRole(AppRoles.SuperAdmin) && roles.Contains(AppRoles.SuperAdmin))
                    continue;

                if (!string.IsNullOrEmpty(role) && !roles.Contains(role)) continue;
                if (!string.IsNullOrEmpty(search) &&
                    !u.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) &&
                    !(u.Email ?? "").Contains(search, StringComparison.OrdinalIgnoreCase)) continue;

                result.Add(new UserWithRoleViewModel { User = u, Roles = roles.ToList() });
            }

            ViewBag.RoleFilter = role;
            ViewBag.Search = search;
            ViewBag.IsSuperAdmin = User.IsInRole(AppRoles.SuperAdmin);
            return View(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(string userId, string newRole)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            // Only SuperAdmin can assign SuperAdmin role
            if (newRole == AppRoles.SuperAdmin && !User.IsInRole(AppRoles.SuperAdmin))
            {
                TempData["Error"] = "Only SuperAdmin can assign the SuperAdmin role.";
                return RedirectToAction(nameof(Index));
            }

            // Admin cannot change SuperAdmin users
            var targetRoles = await _userManager.GetRolesAsync(user);
            if (targetRoles.Contains(AppRoles.SuperAdmin) && !User.IsInRole(AppRoles.SuperAdmin))
            {
                TempData["Error"] = "Cannot modify SuperAdmin users.";
                return RedirectToAction(nameof(Index));
            }

            await _userManager.RemoveFromRolesAsync(user, targetRoles);
            await _userManager.AddToRoleAsync(user, newRole);
            TempData["Success"] = $"{user.FullName} role changed to {newRole}";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(string userId)
        {
            var me = await _userManager.GetUserAsync(User);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();
            if (me?.Id == userId) { TempData["Error"] = "Cannot disable yourself!"; return RedirectToAction(nameof(Index)); }

            var targetRoles = await _userManager.GetRolesAsync(user);
            if (targetRoles.Contains(AppRoles.SuperAdmin) && !User.IsInRole(AppRoles.SuperAdmin))
            {
                TempData["Error"] = "Cannot modify SuperAdmin users.";
                return RedirectToAction(nameof(Index));
            }

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);
            TempData["Success"] = $"User {(user.IsActive ? "enabled" : "disabled")} successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string userId)
        {
            var me = await _userManager.GetUserAsync(User);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();
            if (me?.Id == userId) { TempData["Error"] = "Cannot delete yourself!"; return RedirectToAction(nameof(Index)); }

            var targetRoles = await _userManager.GetRolesAsync(user);
            if (targetRoles.Contains(AppRoles.SuperAdmin))
            {
                TempData["Error"] = "Cannot delete SuperAdmin.";
                return RedirectToAction(nameof(Index));
            }
            // Admin cannot delete Admin
            if (targetRoles.Contains(AppRoles.Admin) && !User.IsInRole(AppRoles.SuperAdmin))
            {
                TempData["Error"] = "Only SuperAdmin can delete Admin users.";
                return RedirectToAction(nameof(Index));
            }

            await _userManager.DeleteAsync(user);
            TempData["Success"] = "User deleted.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpGet]
        public IActionResult CreateAdmin() => View(new CreateAdminViewModel());

        [Authorize(Roles = "SuperAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAdmin(CreateAdminViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            if (_userManager.Users.Any(u => u.Email == vm.Email))
            {
                ModelState.AddModelError("Email", "Email is already registered.");
                return View(vm);
            }

            var user = new AppUser
            {
                FullName = vm.FullName,
                UserName = vm.Email,
                Email = vm.Email,
                PhoneNumber = vm.PhoneNumber,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, vm.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, vm.Role);
                TempData["Success"] = $"{vm.Role} account '{vm.FullName}' created successfully!";
                return RedirectToAction(nameof(Index));
            }

            foreach (var err in result.Errors) ModelState.AddModelError("", err.Description);
            return View(vm);
        }
    }

    public class UserWithRoleViewModel
    {
        public AppUser User { get; set; } = null!;
        public List<string> Roles { get; set; } = new();
    }

    public class CreateAdminViewModel
    {
        [Required] public string FullName { get; set; } = string.Empty;
        [Required][EmailAddress] public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        [Required][MinLength(6)] public string Password { get; set; } = string.Empty;
        [Compare("Password")] public string ConfirmPassword { get; set; } = string.Empty;
        public string Role { get; set; } = "Admin";
    }
}
