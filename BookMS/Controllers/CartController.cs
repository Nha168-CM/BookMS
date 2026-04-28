using BookMS.Models;
using BookMS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BookMS.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartService _cart;
        private readonly UserManager<AppUser> _userManager;

        public CartController(ICartService cart, UserManager<AppUser> userManager)
        { _cart = cart; _userManager = userManager; }

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var items = await _cart.GetCartAsync(user!.Id);
            ViewBag.Total = items.Sum(i => i.Book!.Price * i.Quantity);
            return View(items);
        }

        // POST Add — redirect to login if not authenticated, returnUrl = checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int bookId, int qty = 1)
        {
            if (!User.Identity?.IsAuthenticated ?? true)
                return RedirectToAction("Login", "Auth",
                    new { returnUrl = Url.Action("Checkout", "CustomerOrder") });

            if (!User.IsInRole("Customer"))
                return RedirectToAction("Login", "Auth");

            var user = await _userManager.GetUserAsync(User);
            try
            {
                await _cart.AddOrUpdateAsync(user!.Id, bookId, qty);
                TempData["Success"] = "Book added to cart!";
            }
            catch (Exception ex) { TempData["Error"] = ex.Message; }
            return RedirectToAction("Details", "Shop", new { id = bookId });
        }

        [Authorize(Roles = "Customer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int bookId, int qty)
        {
            var user = await _userManager.GetUserAsync(User);
            if (qty <= 0)
                await _cart.RemoveAsync(user!.Id, bookId);
            else
            {
                await _cart.RemoveAsync(user!.Id, bookId);
                await _cart.AddOrUpdateAsync(user!.Id, bookId, qty);
            }
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Customer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int bookId)
        {
            var user = await _userManager.GetUserAsync(User);
            await _cart.RemoveAsync(user!.Id, bookId);
            TempData["Success"] = "Item removed from cart.";
            return RedirectToAction(nameof(Index));
        }
    }
}
