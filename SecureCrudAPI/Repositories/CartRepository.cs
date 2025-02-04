using Microsoft.EntityFrameworkCore;
using SecureCrudAPI.Models;

namespace SecureCrudAPI.Repositories
{
    public class CartRepository
    {
        private readonly UserDbContext _context;

        public CartRepository(UserDbContext context)
        {
            _context = context;
        }

        public async Task<Cart> GetCartByUserId(int userId)
        {
            return await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);
        }

        public async Task<Cart> CreateCart(int userId)
        {
            var cart = new Cart { UserId = userId };
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();
            return cart;
        }

        public async Task AddCartItem(CartItem cartItem)
        {
            _context.CartItems.Add(cartItem);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateCart(Cart cart)
        {
            _context.Carts.Update(cart);
            await _context.SaveChangesAsync();
        }
    }

}
