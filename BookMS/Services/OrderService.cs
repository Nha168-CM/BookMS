using BookMS.Data;
using BookMS.Models;
using BookMS.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BookMS.Services
{
    public interface IOrderService
    {
        Task<List<Order>> GetAllAsync();
        Task<Order?> GetByIdAsync(int id);
        Task<Order> CreateAsync(OrderCreateViewModel vm, string cashierId);
        Task<bool> CancelAsync(int id);
        Task<List<Order>> GetTodayOrdersAsync();
    }

    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _ctx;
        public OrderService(ApplicationDbContext ctx) => _ctx = ctx;

        public async Task<List<Order>> GetAllAsync() =>
            await _ctx.Orders.Include(o => o.OrderDetails).ThenInclude(od => od.Book)
                .Include(o => o.Cashier).OrderByDescending(o => o.OrderDate).ToListAsync();

        public async Task<Order?> GetByIdAsync(int id) =>
            await _ctx.Orders.Include(o => o.OrderDetails).ThenInclude(od => od.Book)
                .Include(o => o.Cashier).Include(o => o.Receipt)
                .FirstOrDefaultAsync(o => o.Id == id);

        public async Task<Order> CreateAsync(OrderCreateViewModel vm, string cashierId)
        {
            var order = new Order
            {
                OrderNumber = $"ORD-{DateTime.Now:yyyyMMddHHmmss}",
                CustomerName = vm.CustomerName,
                CustomerPhone = vm.CustomerPhone,
                PaymentMethod = vm.PaymentMethod,
                Discount = vm.Discount,
                AmountPaid = vm.AmountPaid,
                Notes = vm.Notes,
                CashierId = cashierId,
                Status = OrderStatus.Completed,
                PaymentStatus = PaymentStatus.Paid
            };

            foreach (var item in vm.Items)
            {
                var book = await _ctx.Books.FindAsync(item.BookId);
                if (book == null) continue;
                order.OrderDetails.Add(new OrderDetail
                {
                    BookId = item.BookId,
                    Quantity = item.Quantity,
                    UnitPrice = book.Price,
                    Discount = item.Discount
                });
                // Auto reduce stock
                var stockBefore = book.Stock;
                book.Stock -= item.Quantity;
                _ctx.StockTransactions.Add(new StockTransaction
                {
                    BookId = book.Id,
                    CategoryId = book.CategoryId,
                    Type = StockType.StockOut,
                    Quantity = item.Quantity,
                    StockBefore = stockBefore,
                    StockAfter = book.Stock,
                    Reference = order.OrderNumber,
                    Notes = "Sale",
                    UserId = cashierId
                });
            }

            order.SubTotal = order.OrderDetails.Sum(od => od.UnitPrice * od.Quantity);
            order.TotalAmount = order.SubTotal - order.Discount;
            order.Change = order.AmountPaid - order.TotalAmount;

            // Create Receipt
            order.Receipt = new Receipt
            {
                ReceiptNumber = $"RCP-{DateTime.Now:yyyyMMddHHmmss}",
                IssuedBy = cashierId,
                StoreName = "Book Store",
                StoreAddress = "Phnom Penh, Cambodia",
                StorePhone = "+855 12 345 678"
            };

            _ctx.Orders.Add(order);
            await _ctx.SaveChangesAsync();
            return order;
        }

        public async Task<bool> CancelAsync(int id)
        {
            var order = await _ctx.Orders.Include(o => o.OrderDetails).ThenInclude(od => od.Book)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order == null || order.Status == OrderStatus.Cancelled) return false;
            order.Status = OrderStatus.Cancelled;
            order.PaymentStatus = PaymentStatus.Refunded;
            // Restore stock
            foreach (var detail in order.OrderDetails)
            {
                if (detail.Book != null)
                {
                    var before = detail.Book.Stock;
                    detail.Book.Stock += detail.Quantity;
                    _ctx.StockTransactions.Add(new StockTransaction
                    {
                        BookId = detail.BookId,
                        CategoryId = detail.Book.CategoryId,
                        Type = StockType.StockIn,
                        Quantity = detail.Quantity,
                        StockBefore = before,
                        StockAfter = detail.Book.Stock,
                        Reference = order.OrderNumber,
                        Notes = "Order Cancelled - Stock Restored"
                    });
                }
            }
            await _ctx.SaveChangesAsync();
            return true;
        }

        public async Task<List<Order>> GetTodayOrdersAsync()
        {
            var today = DateTime.Today;
            return await _ctx.Orders.Where(o => o.OrderDate >= today && o.Status == OrderStatus.Completed)
                .Include(o => o.OrderDetails).ToListAsync();
        }
    }
}
