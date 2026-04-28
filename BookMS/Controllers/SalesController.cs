using BookMS.Data;
using BookMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SalesController : Controller
    {
        private readonly ApplicationDbContext _ctx;
        public SalesController(ApplicationDbContext ctx) => _ctx = ctx;

        public async Task<IActionResult> Index(DateTime? from, DateTime? to)
        {
            from ??= new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            to ??= DateTime.Now;

            var orders = await _ctx.Orders
                .Include(o => o.OrderDetails).ThenInclude(od => od.Book).ThenInclude(b => b!.Category)
                .Include(o => o.Cashier)
                .Where(o => o.Status == OrderStatus.Completed
                         && o.OrderDate >= from.Value.Date
                         && o.OrderDate <= to.Value.Date.AddDays(1))
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            // Top selling books
            var topBooks = orders
                .SelectMany(o => o.OrderDetails)
                .GroupBy(od => new { od.BookId, od.Book!.Title, od.Book.Author })
                .Select(g => new TopBookViewModel
                {
                    BookId = g.Key.BookId,
                    Title = g.Key.Title,
                    Author = g.Key.Author,
                    TotalQty = g.Sum(od => od.Quantity),
                    TotalRevenue = g.Sum(od => od.UnitPrice * od.Quantity)
                })
                .OrderByDescending(x => x.TotalQty)
                .Take(10)
                .ToList();

            // Daily sales chart data
            var dailySales = orders
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new DailySaleViewModel
                {
                    Date = g.Key,
                    OrderCount = g.Count(),
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .OrderBy(x => x.Date)
                .ToList();

            // Payment method breakdown
            var paymentBreakdown = orders
                .GroupBy(o => o.PaymentMethod)
                .Select(g => new PaymentBreakdownViewModel
                {
                    Method = g.Key,
                    Count = g.Count(),
                    Total = g.Sum(o => o.TotalAmount)
                }).ToList();

            var vm = new SalesReportViewModel
            {
                From = from.Value,
                To = to.Value,
                Orders = orders,
                TotalRevenue = orders.Sum(o => o.TotalAmount),
                TotalOrders = orders.Count,
                TotalBooksSold = orders.SelectMany(o => o.OrderDetails).Sum(od => od.Quantity),
                AverageOrderValue = orders.Any() ? orders.Average(o => o.TotalAmount) : 0,
                TopBooks = topBooks,
                DailySales = dailySales,
                PaymentBreakdown = paymentBreakdown
            };

            return View(vm);
        }
    }

    // Sales ViewModels
    public class SalesReportViewModel
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public List<Order> Orders { get; set; } = new();
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TotalBooksSold { get; set; }
        public decimal AverageOrderValue { get; set; }
        public List<TopBookViewModel> TopBooks { get; set; } = new();
        public List<DailySaleViewModel> DailySales { get; set; } = new();
        public List<PaymentBreakdownViewModel> PaymentBreakdown { get; set; } = new();
    }

    public class TopBookViewModel
    {
        public int BookId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public int TotalQty { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class DailySaleViewModel
    {
        public DateTime Date { get; set; }
        public int OrderCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class PaymentBreakdownViewModel
    {
        public PaymentMethod Method { get; set; }
        public int Count { get; set; }
        public decimal Total { get; set; }
    }
}
