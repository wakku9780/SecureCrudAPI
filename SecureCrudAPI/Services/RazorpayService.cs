using Razorpay.Api;

namespace SecureCrudAPI.Services
{
    public class RazorpayService
    {
        private readonly string _keyId;
        private readonly string _keySecret;

        public RazorpayService(IConfiguration configuration)
        {
            _keyId = configuration["Razorpay:KeyId"];
            _keySecret = configuration["Razorpay:KeySecret"];
        }

        public Order CreateOrder(decimal amount, string currency = "INR")
        {
            var client = new RazorpayClient(_keyId, _keySecret);

            Dictionary<string, object> options = new Dictionary<string, object>
            {
                { "amount", (int)(amount * 100) }, // Razorpay amount in paise
                { "currency", currency },
                { "payment_capture", 1 } // Auto-capture payment
            };

            Order order = client.Order.Create(options);
            return order;
        }
    }

    //public class RazorpayService
    //{
    //    private readonly string _key = "rzp_test_ueTNKZ90gFcHKr";
    //    private readonly string _secret = "TluI5thGioKKVGY2GC9qG520";

    //    public Order CreateOrder(decimal amount, string currency = "INR")
    //    {
    //        var razorpayClient = new RazorpayClient(_key, _secret);

    //        var options = new Dictionary<string, object>
    //        {
    //            { "amount", (int)(amount * 100) }, // Amount in paise
    //            { "currency", currency },
    //            { "payment_capture", 1 }
    //        };

    //        Order order = razorpayClient.Order.Create(options);
    //        return order;
    //    }
    //}

}
