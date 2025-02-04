using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Razorpay.Api;
using SecureCrudAPI.Services;

namespace SecureCrudAPI.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public PaymentController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("create-order")]
        public IActionResult CreateOrder([FromBody] PaymentRequest request)
        {
            try
            {
                string key = _configuration["Razorpay:Key"];
                string secret = _configuration["Razorpay:Secret"];
                RazorpayClient client = new RazorpayClient(key, secret);

                Dictionary<string, object> options = new Dictionary<string, object>
                {
                    { "amount", request.Amount * 100 }, // Convert to paise
                    { "currency", "INR" },
                    { "payment_capture", 1 }
                };

                Order order = client.Order.Create(options);

                return Ok(new
                {
                    orderId = order["id"].ToString(),
                    amount = request.Amount,
                    currency = "INR"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = "Payment creation failed", Error = ex.Message });
            }
        }
    }

    public class PaymentRequest
    {
        public int Amount { get; set; }
    }

    //[Route("api/[controller]")]
    //[ApiController]
    //public class PaymentController : ControllerBase
    //{
    //    private readonly RazorpayService _razorpayService;

    //    public PaymentController(RazorpayService razorpayService)
    //    {
    //        _razorpayService = razorpayService;
    //    }

    //    [HttpPost("create-order")]
    //    public IActionResult CreateOrder([FromBody] decimal amount)
    //    {
    //        var order = _razorpayService.CreateOrder(amount);
    //        return Ok(new
    //        {
    //            orderId = order["id"].ToString(),
    //            amount = order["amount"],
    //            currency = order["currency"]
    //        });
    //    }
    //}

}
