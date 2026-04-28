using BookMS.Data;
using BookMS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookMS.Controllers
{
    // PUBLIC — anyone can browse, Customer needs login to add cart
    public class ShopController : Controller
    {
        private readonly ApplicationDbContext _ctx;
        public ShopController(ApplicationDbContext ctx) => _ctx = ctx;

        public async Task<IActionResult> Index(string? search, int? categoryId, string? sort, int page = 1)
        {
            var query = _ctx.Books.Include(b => b.Category).Where(b => b.Stock > 0).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(b => b.Title.Contains(search) || b.Author.Contains(search) || b.ISBN.Contains(search));

            if (categoryId.HasValue)
                query = query.Where(b => b.CategoryId == categoryId);

            query = sort switch
            {
                "price_asc"  => query.OrderBy(b => b.Price),
                "price_desc" => query.OrderByDescending(b => b.Price),
                "newest"     => query.OrderByDescending(b => b.CreatedAt),
                "title"      => query.OrderBy(b => b.Title),
                _            => query.OrderByDescending(b => b.CreatedAt)
            };

            int pageSize = 12;
            int total    = await query.CountAsync();
            var books    = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.Categories  = await _ctx.Categories.ToListAsync();
            ViewBag.Search      = search;
            ViewBag.CategoryId  = categoryId;
            ViewBag.Sort        = sort;
            ViewBag.Page        = page;
            ViewBag.TotalPages  = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.Total       = total;

            return View(books);
        }

        public async Task<IActionResult> Details(int id)
        {
            var book = await _ctx.Books
                .Include(b => b.Category)
                .FirstOrDefaultAsync(b => b.Id == id);
            if (book == null) return NotFound();

            // Related books same category
            var related = await _ctx.Books
                .Include(b => b.Category)
                .Where(b => b.CategoryId == book.CategoryId && b.Id != id && b.Stock > 0)
                .Take(4).ToListAsync();

            ViewBag.Related = related;
            return View(book);
        }
    }
}
