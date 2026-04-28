using Microsoft.AspNetCore.Identity;

namespace BookMS.Models
{
    public class AppUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string? ProfileImage { get; set; }   // path: /uploads/profiles/xxx.jpg
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;

        // Customer-specific fields
        public string? Address { get; set; }
        public string? City { get; set; }
        public decimal LoyaltyPoints { get; set; } = 0;

        // Navigation
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public ICollection<CustomerAddress> Addresses { get; set; } = new List<CustomerAddress>();
    }
}
