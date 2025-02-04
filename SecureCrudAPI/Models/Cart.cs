namespace SecureCrudAPI.Models
{
    public class Cart
    {
        public int Id { get; set; }
        public int UserId { get; set; }  // Foreign Key for User
        public List<CartItem> CartItems { get; set; } = new List<CartItem>();
    }
}
