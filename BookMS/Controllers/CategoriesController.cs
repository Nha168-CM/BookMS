using BookMS.Data;
using BookMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _ctx;
        public CategoriesController(ApplicationDbContext ctx) => _ctx = ctx;

        public async Task<IActionResult> Index()
            => View(await _ctx.Categories.Include(c => c.Books).ToListAsync());

        public IActionResult Create() => View();

        [HttpPost] [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category model)
        {
            if (!ModelState.IsValid) return View(model);
            _ctx.Categories.Add(model);
            await _ctx.SaveChangesAsync();
            TempData["Success"] = "Category created!";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var cat = await _ctx.Categories.FindAsync(id);
            if (cat == null) return NotFound();
            return View(cat);
        }

        [HttpPost] [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category model)
        {
            if (!ModelState.IsValid) return View(model);
            _ctx.Categories.Update(model);
            await _ctx.SaveChangesAsync();
            TempData["Success"] = "Category updated!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost] [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var cat = await _ctx.Categories.Include(c => c.Books).FirstOrDefaultAsync(c => c.Id == id);
            if (cat == null) return NotFound();
            if (cat.Books.Any())
            {
                TempData["Error"] = "Cannot delete category with books!";
                return RedirectToAction(nameof(Index));
            }
            _ctx.Categories.Remove(cat);
            await _ctx.SaveChangesAsync();
            TempData["Success"] = "Category deleted!";
            return RedirectToAction(nameof(Index));
        }
    }
}
