using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppBackendBasic.Core.Interfaces;
using System.Security.Claims;
using Stripe;
using Stripe.Checkout;

namespace AppBackendBasic.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubscriptionController : ControllerBase
{
    private readonly IWebSubscriptionService _subscriptionService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SubscriptionController> _logger;

    public SubscriptionController(
        IWebSubscriptionService subscriptionService,
        IConfiguration configuration,
        ILogger<SubscriptionController> logger)
    {
        _subscriptionService = subscriptionService;
        _configuration = configuration;
        _logger = logger;
        
        StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
    }

    private int GetUserIdFromClaims()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                         ?? User.FindFirst("sub")?.Value 
                         ?? User.FindFirst("userId")?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        
        return userId;
    }

    [HttpGet("plans")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPlans()
    {
        try
        {
            var plans = await _subscriptionService.GetAllPlansAsync();
            return Ok(plans);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching subscription plans");
            return StatusCode(500, new { message = "Error fetching plans" });
        }
    }

    [HttpGet("status")]
    [Authorize]
    public async Task<IActionResult> GetStatus()
    {
        try
        {
            var userId = GetUserIdFromClaims();
            var subscription = await _subscriptionService.GetActiveSubscriptionAsync(userId);

            if (subscription == null)
            {
                return Ok(new
                {
                    hasActiveSubscription = false,
                    requiresSubscription = true
                });
            }

            var quotas = await _subscriptionService.GetAllQuotasAsync(userId);

            return Ok(new
            {
                hasActiveSubscription = true,
                requiresSubscription = false,
                planName = subscription.SubscriptionPlan.DisplayName,
                planId = subscription.SubscriptionPlanId,
                status = subscription.Status,
                startDate = subscription.StartDate,
                endDate = subscription.EndDate,
                billingCycle = subscription.BillingCycle,
                quotas = quotas.Values.Select(q => new
                {
                    type = q.QuotaType,
                    limit = q.Limit,
                    used = q.Used,
                    remaining = q.Remaining
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching subscription status");
            return StatusCode(500, new { message = "Error fetching subscription status" });
        }
    }

    [HttpPost("assign-free")]
    [Authorize]
    public async Task<IActionResult> AssignFreePlan()
    {
        try
        {
            var userId = GetUserIdFromClaims();

            if (await _subscriptionService.HasActiveSubscriptionAsync(userId))
            {
                return Ok(new { success = true,message = "User already has an active subscription" });
            }

            var userSubscription = await _subscriptionService.AssignFreePlanAsync(userId);

            return Ok(new
            {
                success = true,
                message = "Free plan assigned successfully",
                subscriptionId = userSubscription.Id
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Error assigning free plan");
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning free plan");
            return StatusCode(500, new { message = "Error assigning free plan" });
        }
    }

    [HttpPost("create-checkout")]
    [Authorize]
    public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateCheckoutRequest request)
    {
        try
        {
            var userId = GetUserIdFromClaims();

            if (await _subscriptionService.HasActiveSubscriptionAsync(userId))
            {
                return BadRequest(new { message = "User already has an active subscription" });
            }

            var plan = await _subscriptionService.GetPlanByIdAsync(request.PlanId);
            if (plan == null)
            {
                return NotFound(new { message = "Plan not found" });
            }

            if (plan.MonthlyPrice == 0)
            {
                return BadRequest(new { message = "Use assign-free endpoint for free plans" });
            }

            var price = request.BillingCycle == "Yearly" ? plan.YearlyPrice : plan.MonthlyPrice;

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = plan.Currency.ToLower(),
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = plan.DisplayName,
                                Description = plan.Description,
                            },
                            UnitAmount = (long)(price * 100),
                            Recurring = new SessionLineItemPriceDataRecurringOptions
                            {
                                Interval = request.BillingCycle == "Yearly" ? "year" : "month"
                            }
                        },
                        Quantity = 1,
                    },
                },
                Mode = "subscription",
                SuccessUrl = $"{_configuration["AppUrl"]}/payment-success?session_id={{CHECKOUT_SESSION_ID}}",
                CancelUrl = $"{_configuration["AppUrl"]}/pricing",
                ClientReferenceId = userId.ToString(),
                Metadata = new Dictionary<string, string>
                {
                    { "planId", request.PlanId.ToString() },
                    { "userId", userId.ToString() },
                    { "billingCycle", request.BillingCycle }
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            _logger.LogInformation("Stripe checkout session created for user {UserId}, plan {PlanId}", userId, request.PlanId);

            return Ok(new
            {
                sessionId = session.Id,
                url = session.Url
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Stripe checkout session");
            return StatusCode(500, new { message = "Error creating checkout session" });
        }
    }

    [HttpPost("webhooks/stripe")]
    [AllowAnonymous]
    public async Task<IActionResult> StripeWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                _configuration["Stripe:WebhookSecret"]
            );

            _logger.LogInformation("Stripe webhook received: {EventType}", stripeEvent.Type);

            if (stripeEvent.Type == "checkout.session.completed")
            {
                var session = stripeEvent.Data.Object as Session;
                
                if (session?.ClientReferenceId == null || session?.Metadata == null)
                {
                    _logger.LogWarning("Stripe session missing required data");
                    return Ok();
                }

                var userId = int.Parse(session.ClientReferenceId);
                var planId = int.Parse(session.Metadata["planId"]);
                var billingCycle = session.Metadata["billingCycle"];

                // Check if user already has subscription
                if (await _subscriptionService.HasActiveSubscriptionAsync(userId))
                {
                    _logger.LogWarning("User {UserId} already has active subscription", userId);
                    return Ok();
                }

                // Assign paid plan via service
                await _subscriptionService.AssignPaidPlanAsync(
                    userId, 
                    planId, 
                    billingCycle,
                    session.SubscriptionId ?? string.Empty,
                    session.CustomerId ?? string.Empty
                );

                _logger.LogInformation("Subscription assigned to user {UserId} via Stripe webhook", userId);
            }

            return Ok();
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe webhook error");
            return BadRequest();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe webhook");
            return StatusCode(500);
        }
    }

}

public class CreateCheckoutRequest
{
    public int PlanId { get; set; }
    public string BillingCycle { get; set; } = "Monthly";
}
