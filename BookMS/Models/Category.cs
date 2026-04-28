// Models/Category.cs
namespace BookMS.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public ICollection<Book> Books { get; set; } = new List<Book>();
    }
}
