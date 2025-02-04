namespace SecureCrudAPI.Models
{
    public class Wishlist
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ProductId { get; set; }

        // Navigation Properties
        public User User { get; set; }
        public Product Product { get; set; }
    }
}
