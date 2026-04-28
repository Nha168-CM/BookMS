using System.ComponentModel.DataAnnotations;
using BookMS.Models;

namespace BookMS.ViewModels
{
    // Auth ViewModels
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email or Phone is required")]
        public string EmailOrPhone { get; set; } = string.Empty;
        [Required][DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required] public string FullName { get; set; } = string.Empty;
        // Email or Phone — at least one required (validated in controller)
        [EmailAddress] public string? Email { get; set; }
        [Phone] public string? Phone { get; set; }
        [Required][DataType(DataType.Password)][MinLength(6)] public string Password { get; set; } = string.Empty;
        [Compare("Password")] public string ConfirmPassword { get; set; } = string.Empty;
    }

    // Book ViewModels
    public class BookViewModel
    {
        public int Id { get; set; }
        [Required] public string Title { get; set; } = string.Empty;
        [Required] public string Author { get; set; } = string.Empty;
        [Required] public string ISBN { get; set; } = string.Empty;
        public string? Description { get; set; }
        [Required][Range(0, double.MaxValue)] public decimal Price { get; set; }
        [Required][Range(0, int.MaxValue)] public int Stock { get; set; }
        public string? ImageUrl { get; set; }
        [Required] public int CategoryId { get; set; }
        public IFormFile? ImageFile { get; set; }
        public List<Category> Categories { get; set; } = new();
    }

    // Order ViewModels
    public class OrderCreateViewModel
    {
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public decimal Discount { get; set; }
        public decimal AmountPaid { get; set; }
        public string? Notes { get; set; }
        public List<OrderItemViewModel> Items { get; set; } = new();
    }

    public class OrderItemViewModel
    {
        public int BookId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Discount { get; set; }
    }

    // Stock ViewModels
    public class StockTransactionViewModel
    {
        [Required] public int BookId { get; set; }
        [Required] public StockType Type { get; set; }
        [Required][Range(1, int.MaxValue)] public int Quantity { get; set; }
        public string? Reference { get; set; }
        public string? Notes { get; set; }
        public List<Book> Books { get; set; } = new();
    }

    // Dashboard ViewModel
    public class DashboardViewModel
    {
        public int TotalBooks { get; set; }
        public int TotalCategories { get; set; }
        public int TodayOrders { get; set; }
        public decimal TodaySales { get; set; }
        public decimal MonthlySales { get; set; }
        public int LowStockCount { get; set; }
        public List<Order> RecentOrders { get; set; } = new();
        public List<Book> LowStockBooks { get; set; } = new();
    }
}
