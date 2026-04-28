using BookMS.Data;
using BookMS.Models;
using BookMS.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

        builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 6;
            options.SignIn.RequireConfirmedEmail = false;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        builder.Services.AddAuthentication()
            .AddGoogle(options =>
            {
                options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
                options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
                options.CallbackPath = "/signin-google";
            });

        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Auth/Login";
            options.AccessDeniedPath = "/Auth/AccessDenied";
            options.ExpireTimeSpan = TimeSpan.FromHours(1);
        });

        // ✅ Fix: Allow file uploads up to 5MB (default multipart limit is too small)
        builder.Services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = 5 * 1024 * 1024; // 5MB
            options.ValueLengthLimit = int.MaxValue;
            options.MultipartHeadersLengthLimit = int.MaxValue;
        });

        builder.Services.Configure<KestrelServerOptions>(options =>
        {
            options.Limits.MaxRequestBodySize = 5 * 1024 * 1024; // 5MB
        });

        builder.Services.AddScoped<IBookService, BookService>();
        builder.Services.AddScoped<IOrderService, OrderService>();
        builder.Services.AddScoped<IStockService, StockService>();
        builder.Services.AddScoped<ICartService, CartService>();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddControllersWithViews();

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.Migrate();
            await RoleSeeder.SeedAsync(scope.ServiceProvider);
        }

        if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Home/Error"); app.UseHsts(); }
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        // Inject CartCount into every request for Customer
        app.Use(async (context, next) =>
        {
            if (context.User.Identity?.IsAuthenticated == true && context.User.IsInRole(AppRoles.Customer))
            {
                var cartSvc = context.RequestServices.GetRequiredService<ICartService>();
                var userMgr = context.RequestServices.GetRequiredService<UserManager<AppUser>>();
                var user = await userMgr.GetUserAsync(context.User);
                if (user != null)
                {
                    var count = await cartSvc.GetCartCountAsync(user.Id);
                    context.Items["CartCount"] = count;
                }
            }
            await next();
        });

        app.MapControllerRoute(name: "default", pattern: "{controller=Shop}/{action=Index}/{id?}");
        app.Run();
    }
}