using BookMS.Models;
using Microsoft.AspNetCore.Identity;

namespace BookMS.Services
{
    public static class RoleSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<AppUser>>();

            // ===== Seed Roles (including SuperAdmin) =====
            string[] roles = { AppRoles.SuperAdmin, AppRoles.Admin, AppRoles.Staff, AppRoles.Customer };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // ===== Seed SuperAdmin =====
            var superAdmin = await userManager.FindByEmailAsync("superadmin@bookstore.com");
            if (superAdmin == null)
            {
                superAdmin = new AppUser
                {
                    FullName = "Super Administrator",
                    UserName = "superadmin@bookstore.com",
                    Email = "superadmin@bookstore.com",
                    IsActive = true,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(superAdmin, "SuperAdmin@123");
            }
            if (!await userManager.IsInRoleAsync(superAdmin, AppRoles.SuperAdmin))
                await userManager.AddToRoleAsync(superAdmin, AppRoles.SuperAdmin);

            // ===== Seed Admin =====
            var admin = await userManager.FindByEmailAsync("admin@bookstore.com");
            if (admin == null)
            {
                admin = new AppUser
                {
                    FullName = "Administrator",
                    UserName = "admin@bookstore.com",
                    Email = "admin@bookstore.com",
                    IsActive = true,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(admin, "Admin@123");
            }
            if (!await userManager.IsInRoleAsync(admin, AppRoles.Admin))
                await userManager.AddToRoleAsync(admin, AppRoles.Admin);

            // ===== Seed Staff =====
            var staff = await userManager.FindByEmailAsync("staff@bookstore.com");
            if (staff == null)
            {
                staff = new AppUser
                {
                    FullName = "Staff Member",
                    UserName = "staff@bookstore.com",
                    Email = "staff@bookstore.com",
                    IsActive = true,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(staff, "Staff@123");
            }
            if (!await userManager.IsInRoleAsync(staff, AppRoles.Staff))
                await userManager.AddToRoleAsync(staff, AppRoles.Staff);

            // ===== Seed Demo Customer =====
            var customer = await userManager.FindByEmailAsync("customer@bookstore.com");
            if (customer == null)
            {
                customer = new AppUser
                {
                    FullName = "Demo Customer",
                    UserName = "customer@bookstore.com",
                    Email = "customer@bookstore.com",
                    IsActive = true,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(customer, "Customer@123");
            }
            if (!await userManager.IsInRoleAsync(customer, AppRoles.Customer))
                await userManager.AddToRoleAsync(customer, AppRoles.Customer);
        }
    }
}
