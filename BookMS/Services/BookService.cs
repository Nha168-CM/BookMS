using BookMS.Data;
using BookMS.Models;
using Microsoft.EntityFrameworkCore;

namespace BookMS.Services
{
    public interface IBookService
    {
        Task<List<Book>> GetAllAsync(string? search = null, int? categoryId = null);
        Task<Book?> GetByIdAsync(int id);
        Task<Book> CreateAsync(Book book);
        Task<Book> UpdateAsync(Book book);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }

    public class BookService : IBookService
    {
        private readonly ApplicationDbContext _ctx;
        public BookService(ApplicationDbContext ctx) => _ctx = ctx;

        public async Task<List<Book>> GetAllAsync(string? search = null, int? categoryId = null)
        {
            var query = _ctx.Books.Include(b => b.Category).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(b => b.Title.Contains(search) || b.Author.Contains(search) || b.ISBN.Contains(search));
            if (categoryId.HasValue)
                query = query.Where(b => b.CategoryId == categoryId);
            return await query.OrderByDescending(b => b.CreatedAt).ToListAsync();
        }

        public async Task<Book?> GetByIdAsync(int id) =>
            await _ctx.Books.Include(b => b.Category).FirstOrDefaultAsync(b => b.Id == id);

        public async Task<Book> CreateAsync(Book book)
        {
            _ctx.Books.Add(book);
            await _ctx.SaveChangesAsync();
            return book;
        }

        public async Task<Book> UpdateAsync(Book book)
        {
            book.UpdatedAt = DateTime.Now;
            _ctx.Books.Update(book);
            await _ctx.SaveChangesAsync();
            return book;
        }

        public async Task DeleteAsync(int id)
        {
            var book = await _ctx.Books.FindAsync(id);
            if (book != null) { _ctx.Books.Remove(book); await _ctx.SaveChangesAsync(); }
        }

        public async Task<bool> ExistsAsync(int id) => await _ctx.Books.AnyAsync(b => b.Id == id);
    }
}
