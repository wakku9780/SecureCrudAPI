using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureCrudAPI.Models;

namespace SecureCrudAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {

        private readonly UserDbContext _context;

        public AdminController(UserDbContext context)
        {
            _context = context;
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


        [HttpPut("update-order-status/{orderId}")]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, [FromBody] string newStatus)
        {
            var userId = GetUserId();

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null)
                return NotFound(new { Message = "Order not found" });

            order.OrderStatus = newStatus;

            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Order status updated successfully" });
        }

    }
}
