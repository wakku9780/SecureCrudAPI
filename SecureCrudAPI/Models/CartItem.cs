namespace SecureCrudAPI.Models
{
    public class CartItem
    {
        public int Id { get; set; }
        public int CartId { get; set; }  // Foreign Key for Cart
        public int ProductId { get; set; }  // Foreign Key for Product
        public int Quantity { get; set; }
        public decimal Price { get; set; }

        public Cart Cart { get; set; }
        public Product Product { get; set; }
    }
}
