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
    public class StockController : Controller
    {
        private readonly IStockService _stockService;
        private readonly ApplicationDbContext _ctx;
        private readonly UserManager<AppUser> _userManager;

        public StockController(IStockService stockService, ApplicationDbContext ctx, UserManager<AppUser> userManager)
        {
            _stockService = stockService; _ctx = ctx; _userManager = userManager;
        }

        public async Task<IActionResult> Index(int? bookId, int? categoryId)
        {
            var transactions = await _stockService.GetAllAsync(bookId, categoryId);
            ViewBag.Books = await _ctx.Books.ToListAsync();
            ViewBag.Categories = await _ctx.Categories.ToListAsync();
            return View(transactions);
        }

        public async Task<IActionResult> Create()
        {
            var vm = new StockTransactionViewModel { Books = await _ctx.Books.Include(b => b.Category).ToListAsync() };
            return View(vm);
        }

        [HttpPost] [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StockTransactionViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Books = await _ctx.Books.Include(b => b.Category).ToListAsync();
                return View(vm);
            }
            try
            {
                var user = await _userManager.GetUserAsync(User);
                await _stockService.AddTransactionAsync(vm, user!.Id);
                TempData["Success"] = "Stock transaction recorded!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                vm.Books = await _ctx.Books.Include(b => b.Category).ToListAsync();
                return View(vm);
            }
        }
    }
}
