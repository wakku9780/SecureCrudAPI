using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SecureCrudAPI.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SecureCrudAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize] // Sirf logged-in users use kar sakein
    public class WishlistController : ControllerBase
    {
        private readonly UserDbContext _context;
        private readonly IConfiguration _configuration;

        public WishlistController(UserDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // 🟢 Add to Wishlist
        [HttpPost("add")]
        public async Task<IActionResult> AddToWishlist([FromBody] int productId)
        {
            Console.WriteLine($"AddToWishlist Called - Product ID: {productId}");

            var userId = GetUserId();
            Console.WriteLine($"User ID: {userId}");

            var existingItem = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

            if (existingItem != null)
                return BadRequest(new { Message = "Product already in wishlist" });

            var wishlistItem = new Wishlist { UserId = userId, ProductId = productId };
            _context.Wishlists.Add(wishlistItem);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Product added to wishlist" });
        }


        // 🔴 Remove from Wishlist
        [HttpDelete("remove/{productId}")]
        public async Task<IActionResult> RemoveFromWishlist(int productId)
        {
            var userId = GetUserId();

            var wishlistItem = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

            if (wishlistItem == null)
                return NotFound(new { Message = "Product not found in wishlist" });

            _context.Wishlists.Remove(wishlistItem);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Product removed from wishlist" });
        }

        // 🟢 Get User Wishlist
        [HttpGet]
        public async Task<IActionResult> GetWishlist()
        {
            var userId = GetUserId();
            var wishlist = await _context.Wishlists
                .Where(w => w.UserId == userId)
                .Include(w => w.Product)
                .Select(w => new
                {
                    w.Product.Id,
                    w.Product.Name,
                    w.Product.Price,
                    w.Product.ImageUrl
                })
                .ToListAsync();

            return Ok(wishlist);
        }

        // 🛠 Helper function to get User ID from token
        //private int GetUserId()
        //{
        //    return int.Parse(User.Claims.First(c => c.Type == "UserId").Value);
        //}


        private int GetUserId()
        {
            var userIdClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (userIdClaim == null)
            {
                throw new UnauthorizedAccessException("User ID claim is missing.");
            }
            return int.Parse(userIdClaim.Value);
        }


        private string GenerateJwtToken(User user)
        {
            var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Username),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim(ClaimTypes.Role, user.Role),
        new Claim(ClaimTypes.Name, user.Username),
        new Claim("UserId", user.Id.ToString()) // ✅ Ensure this claim is present
    };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1), // ✅ Ensure expiration is future
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }
}
