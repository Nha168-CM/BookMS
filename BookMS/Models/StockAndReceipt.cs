namespace BookMS.Models
{
    public enum StockType { StockIn, StockOut, Adjustment }

    public class StockTransaction
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public Book? Book { get; set; }
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        public StockType Type { get; set; }
        public int Quantity { get; set; }
        public int StockBefore { get; set; }
        public int StockAfter { get; set; }
        public string? Reference { get; set; }
        public string? Notes { get; set; }
        public DateTime TransactionDate { get; set; } = DateTime.Now;
        public string? UserId { get; set; }
        public AppUser? User { get; set; }
    }

    public class Receipt
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public Order? Order { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; } = DateTime.Now;
        public string? IssuedBy { get; set; }
        public string? StoreName { get; set; } = "Book Store";
        public string? StoreAddress { get; set; }
        public string? StorePhone { get; set; }
    }
}
