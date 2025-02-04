namespace SecureCrudAPI.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }


        // Adding an ImageUrl to store the Cloudinary image URL
        public string? ImageUrl { get; set; }

        public string? Category { get; set; }  // 👈 Yeh field exist karni chahiye

        // 🔹 CartItems Navigation Property Add Karo
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    }

}
