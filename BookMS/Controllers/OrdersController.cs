using BookMS.Data;
using BookMS.Services;
using BookMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookMS.Models;

namespace BookMS.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class OrdersController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly ApplicationDbContext _ctx;
        private readonly UserManager<AppUser> _userManager;

        public OrdersController(IOrderService orderService, ApplicationDbContext ctx, UserManager<AppUser> userManager)
        {
            _orderService = orderService; _ctx = ctx; _userManager = userManager;
        }

        public async Task<IActionResult> Index()
            => View(await _orderService.GetAllAsync());

        public async Task<IActionResult> Details(int id)
        {
            var order = await _orderService.GetByIdAsync(id);
            if (order == null) return NotFound();
            return View(order);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Books = await _ctx.Books.Include(b => b.Category).Where(b => b.Stock > 0).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderCreateViewModel vm)
        {
            if (!vm.Items.Any())
            {
                ModelState.AddModelError("", "Please add at least one book to the order.");
                ViewBag.Books = await _ctx.Books.Include(b => b.Category).Where(b => b.Stock > 0).ToListAsync();
                return View(vm);
            }

            var user = await _userManager.GetUserAsync(User);
            var order = await _orderService.CreateAsync(vm, user!.Id);
            TempData["Success"] = $"Order {order.OrderNumber} created successfully!";
            return RedirectToAction(nameof(Receipt), new { id = order.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var result = await _orderService.CancelAsync(id);
            TempData[result ? "Success" : "Error"] = result ? "Order cancelled." : "Cannot cancel this order.";
            return RedirectToAction(nameof(Index));
        }

        // Admin/Staff: Mark a customer order as Paid
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsPaid(int id, decimal amountPaid)
        {
            var order = await _ctx.Orders.FindAsync(id);
            if (order == null) return NotFound();

            if (order.PaymentStatus == PaymentStatus.Paid)
            {
                TempData["Error"] = "This order has already been paid.";
                return RedirectToAction(nameof(Details), new { id });
            }

            order.PaymentStatus = PaymentStatus.Paid;
            order.AmountPaid = amountPaid > 0 ? amountPaid : order.TotalAmount;
            order.Change = order.AmountPaid - order.TotalAmount;
            order.Status = OrderStatus.Completed;

            var cashier = await _userManager.GetUserAsync(User);
            if (order.Receipt != null)
                order.Receipt.IssuedBy = cashier!.Id;

            await _ctx.SaveChangesAsync();

            TempData["Success"] = $"Order {order.OrderNumber} marked as paid!";
            return RedirectToAction(nameof(Details), new { id });
        }

        public async Task<IActionResult> Receipt(int id)
        {
            var order = await _orderService.GetByIdAsync(id);
            if (order == null) return NotFound();
            return View(order);
        }
    }
}
