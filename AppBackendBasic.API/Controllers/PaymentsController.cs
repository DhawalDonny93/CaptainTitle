using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AppBackendBasic.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(
            IPaymentService paymentService,
            ILogger<PaymentsController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        /// <summary>
        /// Get available pricing plans
        /// </summary>
        [HttpGet("pricing-plans")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPricingPlans()
        {
            try
            {
                var plans = await _paymentService.GetPricingPlansAsync();
                return Ok(plans);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pricing plans");
                return StatusCode(500, new { message = "Error retrieving pricing plans" });
            }
        }

        /// <summary>
        /// Create a Stripe checkout session for subscription
        /// </summary>
        [HttpPost("create-checkout-session")]
        [Authorize]
        public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateCheckoutSessionRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Invalid user ID" });
                }

                var response = await _paymentService.CreateCheckoutSessionAsync(userId, request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating checkout session");
                return StatusCode(500, new { message = "Error creating checkout session" });
            }
        }

        /// <summary>
        /// Create a payment intent for one-time payment
        /// </summary>
        [HttpPost("create-payment-intent")]
        [Authorize]
        public async Task<IActionResult> CreatePaymentIntent([FromBody] CreatePaymentIntentRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Invalid user ID" });
                }

                var response = await _paymentService.CreatePaymentIntentAsync(userId, request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment intent");
                return StatusCode(500, new { message = "Error creating payment intent" });
            }
        }

        /// <summary>
        /// Create a customer portal session for managing subscription
        /// </summary>
        [HttpPost("customer-portal")]
        [Authorize]
        public async Task<IActionResult> CreateCustomerPortal([FromBody] CustomerPortalRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Invalid user ID" });
                }

                var response = await _paymentService.CreateCustomerPortalSessionAsync(userId, request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer portal session");
                return StatusCode(500, new { message = "Error creating customer portal session" });
            }
        }

        /// <summary>
        /// Stripe webhook endpoint
        /// </summary>
        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> StripeWebhook()
        {
            try
            {
                var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
                var signature = Request.Headers["Stripe-Signature"].ToString();

                var success = await _paymentService.HandleWebhookAsync(json, signature);

                if (success)
                    return Ok();
                else
                    return BadRequest();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Stripe webhook");
                return BadRequest();
            }
        }
    }
}
