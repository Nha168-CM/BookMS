namespace BookMS.Models
{
    public static class AppRoles
    {
        public const string SuperAdmin = "SuperAdmin";
        public const string Admin = "Admin";
        public const string Staff = "Staff";
        public const string Customer = "Customer";
    }

    public class CartItem
    {
        public int Id { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public AppUser? Customer { get; set; }
        public int BookId { get; set; }
        public Book? Book { get; set; }
        public int Quantity { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.Now;
    }

    public class CustomerAddress
    {
        public int Id { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public AppUser? Customer { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
    }
}
