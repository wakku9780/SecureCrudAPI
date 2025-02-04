namespace SecureCrudAPI.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int UserId { get; set; }  // User who placed the order
        public decimal TotalAmount { get; set; }
        public string OrderStatus { get; set; }  // Pending, Completed, Cancelled, etc.
        public DateTime OrderDate { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();  // Items in the order
    }
}
