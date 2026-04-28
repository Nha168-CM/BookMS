using BookMS.Data;
using BookMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookMS.Controllers
{
    [Authorize(Roles = "Admin,Staff,SuperAdmin")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _ctx;
        public HomeController(ApplicationDbContext ctx) => _ctx = ctx;

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var month = new DateTime(today.Year, today.Month, 1);

            var todayOrders = await _ctx.Orders
                .Where(o => o.OrderDate >= today && o.Status == Models.OrderStatus.Completed)
                .Include(o => o.OrderDetails).ToListAsync();

            var monthlyOrders = await _ctx.Orders
                .Where(o => o.OrderDate >= month && o.Status == Models.OrderStatus.Completed)
                .ToListAsync();

            var vm = new DashboardViewModel
            {
                TotalBooks = await _ctx.Books.CountAsync(),
                TotalCategories = await _ctx.Categories.CountAsync(),
                TodayOrders = todayOrders.Count,
                TodaySales = todayOrders.Sum(o => o.TotalAmount),
                MonthlySales = monthlyOrders.Sum(o => o.TotalAmount),
                LowStockCount = await _ctx.Books.CountAsync(b => b.Stock <= 5),
                RecentOrders = await _ctx.Orders.Include(o => o.Cashier)
                    .OrderByDescending(o => o.OrderDate).Take(5).ToListAsync(),
                LowStockBooks = await _ctx.Books.Include(b => b.Category)
                    .Where(b => b.Stock <= 5).Take(5).ToListAsync()
            };
            return View(vm);
        }
    }
}
