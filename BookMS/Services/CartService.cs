using BookMS.Data;
using BookMS.Models;
using Microsoft.EntityFrameworkCore;

namespace BookMS.Services
{
    public interface ICartService
    {
        Task<List<CartItem>> GetCartAsync(string userId);
        Task<int> GetCartCountAsync(string userId);
        Task AddOrUpdateAsync(string userId, int bookId, int qty);
        Task RemoveAsync(string userId, int bookId);
        Task ClearAsync(string userId);
    }

    public class CartService : ICartService
    {
        private readonly ApplicationDbContext _ctx;
        public CartService(ApplicationDbContext ctx) => _ctx = ctx;

        public async Task<List<CartItem>> GetCartAsync(string userId) =>
            await _ctx.CartItems
                .Include(c => c.Book).ThenInclude(b => b!.Category)
                .Where(c => c.CustomerId == userId)
                .OrderBy(c => c.AddedAt)
                .ToListAsync();

        public async Task<int> GetCartCountAsync(string userId) =>
            await _ctx.CartItems.Where(c => c.CustomerId == userId).SumAsync(c => c.Quantity);

        public async Task AddOrUpdateAsync(string userId, int bookId, int qty)
        {
            var item = await _ctx.CartItems
                .FirstOrDefaultAsync(c => c.CustomerId == userId && c.BookId == bookId);

            var book = await _ctx.Books.FindAsync(bookId)
                ?? throw new Exception("Book not found");

            if (item == null)
            {
                _ctx.CartItems.Add(new CartItem
                {
                    CustomerId = userId,
                    BookId     = bookId,
                    Quantity   = Math.Min(qty, book.Stock)
                });
            }
            else
            {
                item.Quantity = Math.Min(item.Quantity + qty, book.Stock);
            }
            await _ctx.SaveChangesAsync();
        }

        public async Task RemoveAsync(string userId, int bookId)
        {
            var item = await _ctx.CartItems
                .FirstOrDefaultAsync(c => c.CustomerId == userId && c.BookId == bookId);
            if (item != null) { _ctx.CartItems.Remove(item); await _ctx.SaveChangesAsync(); }
        }

        public async Task ClearAsync(string userId)
        {
            var items = _ctx.CartItems.Where(c => c.CustomerId == userId);
            _ctx.CartItems.RemoveRange(items);
            await _ctx.SaveChangesAsync();
        }
    }
}
