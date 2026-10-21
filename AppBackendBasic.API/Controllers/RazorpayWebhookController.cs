using AppBackendBasic.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AppBackendBasic.API.Controllers
{
    [ApiController]
    [Route("api/webhooks/razorpay")]
    public class RazorpayWebhookController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<RazorpayWebhookController> _logger;

        public RazorpayWebhookController(
            IPaymentService paymentService,
            ILogger<RazorpayWebhookController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        /// <summary>
        /// Handle Razorpay webhook events
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> HandleWebhook()
        {
            try
            {
                using var reader = new StreamReader(Request.Body);
                var payload = await reader.ReadToEndAsync();

                // Get Razorpay signature from headers
                var signature = Request.Headers["X-Razorpay-Signature"].FirstOrDefault();

                if (string.IsNullOrEmpty(signature))
                {
                    _logger.LogWarning("Razorpay webhook received without signature");
                    return BadRequest(new { error = "Missing signature" });
                }

                var success = await _paymentService.HandleWebhookAsync(payload, signature);

                if (success)
                {
                    return Ok(new { received = true });
                }
                else
                {
                    return BadRequest(new { error = "Webhook processing failed" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Razorpay webhook");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}
