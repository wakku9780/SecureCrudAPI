using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureCrudAPI.Models;
using SecureCrudAPI.Services;

namespace SecureCrudAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly UserDbContext _context;
        private readonly EmailService _emailService;

        public OrderController(UserDbContext context,EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        private int GetUserId()
        {
            var userIdClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (userIdClaim == null)
            {
                throw new UnauthorizedAccessException("User ID claim is missing.");
            }
            return int.Parse(userIdClaim.Value);
        }


        [HttpGet("track-order/{orderId}")]
        public async Task<IActionResult> TrackOrder(int orderId)
        {
            var userId = GetUserId();

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null)
                return NotFound(new { Message = "Order not found" });

            var response = new
            {
                OrderId = order.Id,
                OrderStatus = order.OrderStatus,
                OrderDate = order.OrderDate,
                TotalAmount = order.TotalAmount,
                Items = order.OrderItems.Select(oi => new
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.Product.Name,
                    Quantity = oi.Quantity,
                    Price = oi.Price
                }).ToList()
            };

            return Ok(response);
        }

        [HttpPost("place-order")]
        public async Task<IActionResult> PlaceOrder()
        {
            var userId = GetUserId();
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.CartItems.Any())
                return BadRequest(new { Message = "Cart is empty" });

            decimal totalAmount = cart.CartItems.Sum(ci => ci.Price);

            var order = new Order
            {
                UserId = userId,
                TotalAmount = totalAmount,
                OrderDate = DateTime.UtcNow
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // ✅ Email Confirmation
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                string emailBody = $"<h3>Congratulations {user.Username}!</h3><p>Your order has been successfully placed.</p><p>Total Amount: {totalAmount:C}</p>";
                await _emailService.SendEmailAsync(user.Email, "Order Confirmation", emailBody);
            }

            return Ok(new { Message = "Order placed successfully!", TotalAmount = totalAmount });
        }


    }
}
