using BookMS.Data;
using BookMS.Models;
using BookMS.Services;
using BookMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BookMS.Controllers
{
    [Authorize(Roles = "Admin,Staff,SuperAdmin")]
    public class BooksController : Controller
    {
        private readonly IBookService _bookService;
        private readonly ApplicationDbContext _ctx;
        private readonly IWebHostEnvironment _env;

        public BooksController(IBookService bookService, ApplicationDbContext ctx, IWebHostEnvironment env)
        {
            _bookService = bookService; _ctx = ctx; _env = env;
        }

        public async Task<IActionResult> Index(string? search, int? categoryId)
        {
            var books = await _bookService.GetAllAsync(search, categoryId);
            ViewBag.Categories = new SelectList(await _ctx.Categories.ToListAsync(), "Id", "Name", categoryId);
            ViewBag.Search = search;
            ViewBag.CategoryId = categoryId;
            return View(books);
        }

        public async Task<IActionResult> Details(int id)
        {
            var book = await _bookService.GetByIdAsync(id);
            if (book == null) return NotFound();
            return View(book);
        }

        public async Task<IActionResult> Create()
        {
            var vm = new BookViewModel { Categories = await _ctx.Categories.ToListAsync() };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(10 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)]
        public async Task<IActionResult> Create(BookViewModel vm)
        {
            ModelState.Remove("ImageUrl");
            ModelState.Remove("CategoryId");

            // Resolve CategoryId from multi-select (first selected = primary)
            if (vm.CategoryIds != null && vm.CategoryIds.Count > 0)
                vm.CategoryId = vm.CategoryIds[0];

            if (vm.CategoryId == 0)
                ModelState.AddModelError("CategoryIds", "Please select at least one category.");

            if (!ModelState.IsValid)
            {
                vm.Categories = await _ctx.Categories.ToListAsync();
                return View(vm);
            }

            var book = new Book
            {
                Title = vm.Title,
                Author = vm.Author,
                ISBN = vm.ISBN,
                Description = vm.Description,
                Price = vm.Price,
                Stock = vm.Stock,
                CategoryId = vm.CategoryId
            };

            // Priority: File upload > URL input
            if (vm.ImageFile != null && vm.ImageFile.Length > 0)
                book.ImageUrl = await SaveImageAsync(vm.ImageFile);
            else if (!string.IsNullOrWhiteSpace(vm.ImageUrl))
                book.ImageUrl = vm.ImageUrl.Trim();

            await _bookService.CreateAsync(book);
            TempData["Success"] = "Book created successfully!";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var book = await _bookService.GetByIdAsync(id);
            if (book == null) return NotFound();
            var vm = new BookViewModel
            {
                Id = book.Id,
                Title = book.Title,
                Author = book.Author,
                ISBN = book.ISBN,
                Description = book.Description,
                Price = book.Price,
                Stock = book.Stock,
                ImageUrl = book.ImageUrl,
                CategoryId = book.CategoryId,
                CategoryIds = new List<int> { book.CategoryId },
                Categories = await _ctx.Categories.ToListAsync()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(10 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)]
        public async Task<IActionResult> Edit(int id, BookViewModel vm)
        {
            ModelState.Remove("ImageUrl");
            ModelState.Remove("CategoryId");

            if (vm.CategoryIds != null && vm.CategoryIds.Count > 0)
                vm.CategoryId = vm.CategoryIds[0];

            if (vm.CategoryId == 0)
                ModelState.AddModelError("CategoryIds", "Please select at least one category.");

            if (!ModelState.IsValid)
            {
                vm.Categories = await _ctx.Categories.ToListAsync();
                return View(vm);
            }

            var book = await _bookService.GetByIdAsync(id);
            if (book == null) return NotFound();

            book.Title = vm.Title;
            book.Author = vm.Author;
            book.ISBN = vm.ISBN;
            book.Description = vm.Description;
            book.Price = vm.Price;
            book.Stock = vm.Stock;
            book.CategoryId = vm.CategoryId;

            // Priority: new file upload > new URL > keep existing
            if (vm.ImageFile != null && vm.ImageFile.Length > 0)
                book.ImageUrl = await SaveImageAsync(vm.ImageFile);
            else if (!string.IsNullOrWhiteSpace(vm.ImageUrl))
                book.ImageUrl = vm.ImageUrl.Trim();

            await _bookService.UpdateAsync(book);
            TempData["Success"] = "Book updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _bookService.DeleteAsync(id);
            TempData["Success"] = "Book deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        private async Task<string> SaveImageAsync(IFormFile file)
        {
            var uploadsDir = Path.Combine(_env.WebRootPath, "images", "books");
            Directory.CreateDirectory(uploadsDir);
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsDir, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);
            return $"/images/books/{fileName}";
        }
    }
}