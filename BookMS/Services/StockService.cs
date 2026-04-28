using BookMS.Data;
using BookMS.Models;
using BookMS.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BookMS.Services
{
    public interface IStockService
    {
        Task<List<StockTransaction>> GetAllAsync(int? bookId = null, int? categoryId = null);
        Task<StockTransaction> AddTransactionAsync(StockTransactionViewModel vm, string userId);
    }

    public class StockService : IStockService
    {
        private readonly ApplicationDbContext _ctx;
        public StockService(ApplicationDbContext ctx) => _ctx = ctx;

        public async Task<List<StockTransaction>> GetAllAsync(int? bookId = null, int? categoryId = null)
        {
            var query = _ctx.StockTransactions
                .Include(s => s.Book).Include(s => s.Category).Include(s => s.User)
                .AsQueryable();
            if (bookId.HasValue) query = query.Where(s => s.BookId == bookId);
            if (categoryId.HasValue) query = query.Where(s => s.CategoryId == categoryId);
            return await query.OrderByDescending(s => s.TransactionDate).ToListAsync();
        }

        public async Task<StockTransaction> AddTransactionAsync(StockTransactionViewModel vm, string userId)
        {
            var book = await _ctx.Books.FindAsync(vm.BookId)
                ?? throw new Exception("Book not found");

            var stockBefore = book.Stock;
            if (vm.Type == StockType.StockIn)
                book.Stock += vm.Quantity;
            else if (vm.Type == StockType.StockOut)
            {
                if (book.Stock < vm.Quantity) throw new Exception("Insufficient stock");
                book.Stock -= vm.Quantity;
            }
            else
                book.Stock = vm.Quantity;

            var transaction = new StockTransaction
            {
                BookId = vm.BookId,
                CategoryId = book.CategoryId,
                Type = vm.Type,
                Quantity = vm.Quantity,
                StockBefore = stockBefore,
                StockAfter = book.Stock,
                Reference = vm.Reference,
                Notes = vm.Notes,
                UserId = userId
            };

            _ctx.StockTransactions.Add(transaction);
            await _ctx.SaveChangesAsync();
            return transaction;
        }
    }
}
