﻿using Microsoft.EntityFrameworkCore;

namespace SecureCrudAPI.Models
{

    public class UserDbContext : DbContext
    {
        public UserDbContext(DbContextOptions<UserDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }

        public DbSet<Cart> Carts { get; set; }

        public DbSet<CartItem> CartItems { get; set; }

        public DbSet<Wishlist> Wishlists { get; set; }

        public DbSet<Order> Orders { get; set; }  // Add this line
        public DbSet<OrderItem> OrderItems { get; set; }



    }
}
