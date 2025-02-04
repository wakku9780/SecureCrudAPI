using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Razorpay.Api;
using SecureCrudAPI.Models;
using SecureCrudAPI.Services;

namespace SecureCrudAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly UserDbContext _context;
        private readonly EmailService _emailService;
        private readonly IConfiguration _configuration;

        public CartController(UserDbContext context, EmailService emailService, IConfiguration configuration)
        {
            _context = context;
            _emailService = emailService;
            _configuration = configuration;

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

        // 🟢 Add to Cart
        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartDto request)
        {
            var userId = GetUserId();
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart { UserId = userId };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            var product = await _context.Products.FindAsync(request.ProductId);
            if (product == null)
                return NotFound(new { Message = "Product not found" });

            var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == request.ProductId);
            if (cartItem != null)
            {
                cartItem.Quantity += request.Quantity;
                cartItem.Price = product.Price * cartItem.Quantity;
            }
            else
            {
                cartItem = new CartItem
                {
                    CartId = cart.Id,
                    ProductId = request.ProductId,
                    Quantity = request.Quantity,
                    Price = product.Price * request.Quantity
                };
                cart.CartItems.Add(cartItem);
            }

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Product added to cart" });
        }

        // 🔵 Get Cart Items
        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var userId = GetUserId();
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
                return NotFound(new { Message = "Cart not found" });

            var response = new CartResponseDto
            {
                CartId = cart.Id,
                Items = cart.CartItems.Select(ci => new CartItemDto
                {
                    ProductId = ci.ProductId,
                    ProductName = ci.Product.Name,
                    Quantity = ci.Quantity,
                    Price = ci.Price
                }).ToList(),
                TotalPrice = cart.CartItems.Sum(ci => ci.Price)
            };

            return Ok(response);
        }

        // 🟠 Checkout
        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout()
        {
            var userId = GetUserId();
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.CartItems.Any())
                return BadRequest(new { Message = "Cart is empty" });

            decimal totalAmount = cart.CartItems.Sum(ci => ci.Price);

            // TODO: Payment integration logic yaha add karein

            // ✅ Checkout complete, cart empty karein
            _context.CartItems.RemoveRange(cart.CartItems);
            await _context.SaveChangesAsync(); 

            return Ok(new { Message = "Checkout successful", TotalAmount = totalAmount });
        }




        [HttpPost("place-order")]
        public async Task<IActionResult> PlaceOrder([FromBody] PaymentVerificationRequest request)
        {
            var userId = GetUserId();
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.CartItems.Any())
                return BadRequest(new { Message = "Cart is empty" });

            // ✅ 1. Verify Payment Status
            string key = _configuration["Razorpay:KeyId"];
            string secret = _configuration["Razorpay:KeySecret"];
            RazorpayClient client = new RazorpayClient(key, secret);

            Payment payment = client.Payment.Fetch(request.PaymentId);
            if (payment["status"].ToString() != "captured")
            {
                return BadRequest(new { Message = "Payment verification failed!" });
            }

            decimal totalAmount = cart.CartItems.Sum(ci => ci.Price);

            // ✅ 2. Create Order After Payment
            var order = new SecureCrudAPI.Models.Order
            {
                UserId = userId,
                TotalAmount = totalAmount,
                OrderStatus = "Pending",
                OrderDate = DateTime.UtcNow
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // ✅ 3. Send Confirmation Email
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                string emailBody = $"<h3>Congratulations {user.Username}!</h3><p>Your order has been placed successfully.</p><p>Total Amount: {totalAmount:C}</p>";
                await _emailService.SendEmailAsync(user.Email, "Order Confirmation", emailBody);
            }

            return Ok(new { Message = "Order placed successfully!", OrderId = order.Id });
        }

        public class PaymentVerificationRequest
        {
            public string PaymentId { get; set; }
        }





        //[HttpPost("place-order")]

        //public async Task<IActionResult> PlaceOrder()
        //{
        //    var userId = GetUserId();
        //    var cart = await _context.Carts
        //        .Include(c => c.CartItems)
        //        .ThenInclude(ci => ci.Product)
        //        .FirstOrDefaultAsync(c => c.UserId == userId);

        //    if (cart == null || !cart.CartItems.Any())
        //        return BadRequest(new { Message = "Cart is empty" });

        //    decimal totalAmount = cart.CartItems.Sum(ci => ci.Price);

        //    var order = new Order
        //    {
        //        UserId = userId,
        //        TotalAmount = totalAmount,
        //        OrderDate = DateTime.UtcNow,
        //        OrderStatus = "Pending"  // ✅ Fix: Default Order Status Set Karo
        //    };
        //    _context.Orders.Add(order);
        //    await _context.SaveChangesAsync();

        //    // ✅ Email Confirmation
        //    var user = await _context.Users.FindAsync(userId);
        //    if (user != null)
        //    {
        //        string emailBody = $"<h3>Congratulations {user.Username}!</h3><p>Your order has been successfully placed.</p><p>Total Amount: {totalAmount:C}</p>";
        //        await _emailService.SendEmailAsync(user.Email, "Order Confirmation", emailBody);
        //    }

        //    return Ok(new { Message = "Order placed successfully!", TotalAmount = totalAmount });
        //}



        [HttpPut("confirm-order/{orderId}")]
        public async Task<IActionResult> ConfirmOrder(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                return NotFound(new { Message = "Order not found" });

            if (order.OrderStatus != "Pending")
                return BadRequest(new { Message = "Order is not in pending state" });

            // ✅ OrderStatus Update Karo
            order.OrderStatus = "Confirmed";
            await _context.SaveChangesAsync();

            // ✅ Email Send Karo (Order Confirmation)
            var user = await _context.Users.FindAsync(order.UserId);
            if (user != null)
            {
                string emailBody = $"<h3>Dear {user.Username},</h3><p>Your order #{order.Id} has been <b>Confirmed</b>!</p>";
                await _emailService.SendEmailAsync(user.Email, "Order Confirmed", emailBody);
            }

            return Ok(new { Message = "Order confirmed successfully!" });
        }



        [HttpPost("create-payment")]
        public IActionResult CreatePayment()
        {
            try
            {
                string key = _configuration["Razorpay:KeyId"];
                string secret = _configuration["Razorpay:KeySecret"];
                RazorpayClient client = new RazorpayClient(key, secret);

                // 🔹 1. Payment Order Data Set Karo
                Dictionary<string, object> options = new Dictionary<string, object>
        {
            { "amount", 50000 }, // Amount in paisa (₹500 = 50000 paisa)
            { "currency", "INR" },
            { "receipt", "order_rcptid_11" },  // Random receipt ID
            { "payment_capture", 1 }  // Auto capture payment
        };

                // 🔹 2. Razorpay Order Create Karo
                Razorpay.Api.Order order = client.Order.Create(options);

                // 🔹 3. Response Return Karo
                return Ok(new
                {
                    orderId = order["id"].ToString(),
                    receipt = order["receipt"].ToString()
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = "Error while creating payment order", Error = ex.Message });
            }
        }






    }

}
