using BookMS.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BookMS.Data
{
    public class ApplicationDbContext : IdentityDbContext<AppUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Book> Books { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<StockTransaction> StockTransactions { get; set; }
        public DbSet<Receipt> Receipts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<CustomerAddress> CustomerAddresses { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<Book>(e => { e.Property(b => b.Price).HasPrecision(18, 2); });
            builder.Entity<AppUser>(e => { e.Property(u => u.LoyaltyPoints).HasPrecision(18, 2); });
            builder.Entity<Order>(e => {
                e.Property(o => o.SubTotal).HasPrecision(18, 2);
                e.Property(o => o.Discount).HasPrecision(18, 2);
                e.Property(o => o.TotalAmount).HasPrecision(18, 2);
                e.Property(o => o.AmountPaid).HasPrecision(18, 2);
                e.Property(o => o.Change).HasPrecision(18, 2);
            });
            builder.Entity<OrderDetail>(e => {
                e.Property(od => od.UnitPrice).HasPrecision(18, 2);
                e.Property(od => od.Discount).HasPrecision(18, 2);
                e.Ignore(od => od.SubTotal);
            });
            builder.Entity<CartItem>(e => {
                e.HasIndex(c => new { c.CustomerId, c.BookId }).IsUnique();
            });
            builder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Fiction",     Description = "Fiction Books" },
                new Category { Id = 2, Name = "Non-Fiction", Description = "Non-Fiction Books" },
                new Category { Id = 3, Name = "Technology",  Description = "Tech & Programming" },
                new Category { Id = 4, Name = "Science",     Description = "Science Books" },
                new Category { Id = 5, Name = "History",     Description = "History Books" }
            );
        }
    }
}
