using BookMS.Data;
using BookMS.Models;
using BookMS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookMS.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CustomerOrderController : Controller
    {
        private readonly ApplicationDbContext _ctx;
        private readonly ICartService _cart;
        private readonly UserManager<AppUser> _userManager;

        public CustomerOrderController(ApplicationDbContext ctx, ICartService cart, UserManager<AppUser> um)
        { _ctx = ctx; _cart = cart; _userManager = um; }

        // Order History
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var orders = await _ctx.Orders
                .Include(o => o.OrderDetails).ThenInclude(od => od.Book)
                .Where(o => o.CashierId == user!.Id)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            return View(orders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var order = await _ctx.Orders
                .Include(o => o.OrderDetails).ThenInclude(od => od.Book)
                .Include(o => o.Receipt)
                .FirstOrDefaultAsync(o => o.Id == id && o.CashierId == user!.Id);
            if (order == null) return NotFound();
            return View(order);
        }

        // Checkout page
        public async Task<IActionResult> Checkout()
        {
            var user = await _userManager.GetUserAsync(User);
            var items = await _cart.GetCartAsync(user!.Id);
            if (!items.Any()) return RedirectToAction("Index", "Cart");
            ViewBag.Total = items.Sum(i => i.Book!.Price * i.Quantity);
            ViewBag.User = user;
            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(PaymentMethod paymentMethod, string? notes)
        {
            var user = await _userManager.GetUserAsync(User);
            var items = await _cart.GetCartAsync(user!.Id);
            if (!items.Any()) return RedirectToAction("Index", "Cart");

            var order = new Order
            {
                OrderNumber = $"CUST-{DateTime.Now:yyyyMMddHHmmss}",
                CustomerName = user!.FullName,
                CustomerPhone = user.PhoneNumber,
                PaymentMethod = paymentMethod,
                PaymentStatus = PaymentStatus.Unpaid,
                Status = OrderStatus.Pending,
                CashierId = user.Id,
                Notes = notes
            };

            foreach (var item in items)
            {
                order.OrderDetails.Add(new OrderDetail
                {
                    BookId = item.BookId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Book!.Price,
                    Discount = 0
                });
                var book = await _ctx.Books.FindAsync(item.BookId);
                if (book != null)
                {
                    var before = book.Stock;
                    book.Stock -= item.Quantity;
                    _ctx.StockTransactions.Add(new StockTransaction
                    {
                        BookId = book.Id,
                        CategoryId = book.CategoryId,
                        Type = StockType.StockOut,
                        Quantity = item.Quantity,
                        StockBefore = before,
                        StockAfter = book.Stock,
                        Reference = order.OrderNumber,
                        Notes = "Customer Order",
                        UserId = user.Id
                    });
                }
            }

            order.SubTotal = order.OrderDetails.Sum(od => od.UnitPrice * od.Quantity);
            order.TotalAmount = order.SubTotal;
            order.AmountPaid = 0;
            order.Receipt = new Receipt
            {
                ReceiptNumber = $"R-{DateTime.Now:yyyyMMddHHmmss}",
                IssuedBy = user.Id,
                StoreName = "Book Store",
                StoreAddress = "Phnom Penh, Cambodia",
                StorePhone = "+855 12 345 678"
            };

            user.LoyaltyPoints += Math.Floor(order.TotalAmount);
            await _userManager.UpdateAsync(user);

            _ctx.Orders.Add(order);
            await _ctx.SaveChangesAsync();
            await _cart.ClearAsync(user.Id);

            // Redirect to payment page if Card or QRCode
            if (paymentMethod == PaymentMethod.Card || paymentMethod == PaymentMethod.QRCode)
                return RedirectToAction(nameof(Pay), new { id = order.Id });

            TempData["Success"] = $"Order {order.OrderNumber} placed! Please pay on pickup.";
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }

        // Payment page for Card / QR Code
        public async Task<IActionResult> Pay(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var order = await _ctx.Orders
                .Include(o => o.OrderDetails).ThenInclude(od => od.Book)
                .FirstOrDefaultAsync(o => o.Id == id && o.CashierId == user!.Id);

            if (order == null) return NotFound();
            if (order.PaymentStatus == PaymentStatus.Paid)
                return RedirectToAction(nameof(Details), new { id });

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment(int id, string? cardNumber, string? cardHolder)
        {
            var user = await _userManager.GetUserAsync(User);
            var order = await _ctx.Orders
                .FirstOrDefaultAsync(o => o.Id == id && o.CashierId == user!.Id);

            if (order == null) return NotFound();
            if (order.PaymentStatus == PaymentStatus.Paid)
            {
                TempData["Error"] = "This order has already been paid.";
                return RedirectToAction(nameof(Details), new { id });
            }

            order.PaymentStatus = PaymentStatus.Paid;
            order.AmountPaid = order.TotalAmount;
            order.Status = OrderStatus.Completed;
            await _ctx.SaveChangesAsync();

            TempData["Success"] = $"Payment confirmed! Order {order.OrderNumber} is now complete.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Cancel an unpaid order — restore stock and redirect to Shop
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelPayment(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var order = await _ctx.Orders
                .Include(o => o.OrderDetails).ThenInclude(od => od.Book)
                .Include(o => o.Receipt)
                .FirstOrDefaultAsync(o => o.Id == id && o.CashierId == user!.Id);

            if (order == null) return NotFound();

            // Only allow cancellation if not yet paid
            if (order.PaymentStatus == PaymentStatus.Paid)
            {
                TempData["Error"] = "Cannot cancel an order that has already been paid.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Restore stock for each item
            foreach (var detail in order.OrderDetails)
            {
                var book = await _ctx.Books.FindAsync(detail.BookId);
                if (book != null)
                {
                    var before = book.Stock;
                    book.Stock += detail.Quantity;
                    _ctx.StockTransactions.Add(new StockTransaction
                    {
                        BookId = book.Id,
                        CategoryId = book.CategoryId,
                        Type = StockType.StockIn,
                        Quantity = detail.Quantity,
                        StockBefore = before,
                        StockAfter = book.Stock,
                        Reference = order.OrderNumber,
                        Notes = "Order Cancelled - Stock Restored",
                        UserId = user!.Id
                    });
                }
            }

            // Reverse loyalty points
            user!.LoyaltyPoints = Math.Max(0, user.LoyaltyPoints - Math.Floor(order.TotalAmount));
            await _userManager.UpdateAsync(user);

            // Mark order cancelled
            order.Status = OrderStatus.Cancelled;
            order.PaymentStatus = PaymentStatus.Unpaid;

            // Remove receipt if exists
            if (order.Receipt != null)
                _ctx.Receipts.Remove(order.Receipt);

            await _ctx.SaveChangesAsync();

            TempData["Info"] = $"Order {order.OrderNumber} has been cancelled. Stock has been restored.";
            return RedirectToAction("Index", "Shop");
        }
    }
}