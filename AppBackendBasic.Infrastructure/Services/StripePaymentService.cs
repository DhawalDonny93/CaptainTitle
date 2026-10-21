using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;

namespace AppBackendBasic.Infrastructure.Services
{
    public class StripePaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<StripePaymentService> _logger;

        public StripePaymentService(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<StripePaymentService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;

            // Initialize Stripe
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        }

        public async Task<CreateCheckoutSessionResponse> CreateCheckoutSessionAsync(int userId, CreateCheckoutSessionRequest request)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    throw new Exception("User not found");

                // Get or create Stripe customer
                var customerId = await GetOrCreateStripeCustomerAsync(user);

                var options = new SessionCreateOptions
                {
                    Customer = customerId,
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            Price = request.PriceId,
                            Quantity = 1,
                        },
                    },
                    Mode = "subscription",
                    SuccessUrl = request.SuccessUrl,
                    CancelUrl = request.CancelUrl,
                    ClientReferenceId = userId.ToString(),
                    Metadata = new Dictionary<string, string>
                    {
                        { "user_id", userId.ToString() },
                        { "project_id", request.ProjectId.ToString() }
                    }
                };

                var service = new SessionService();
                var session = await service.CreateAsync(options);

                return new CreateCheckoutSessionResponse
                {
                    SessionId = session.Id,
                    CheckoutUrl = session.Url
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating checkout session for user {UserId}", userId);
                throw;
            }
        }

        public async Task<CreatePaymentIntentResponse> CreatePaymentIntentAsync(int userId, CreatePaymentIntentRequest request)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    throw new Exception("User not found");

                var customerId = await GetOrCreateStripeCustomerAsync(user);

                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(request.Amount * 100), // Convert to cents
                    Currency = request.Currency,
                    Customer = customerId,
                    Description = request.Description,
                    Metadata = new Dictionary<string, string>
                    {
                        { "user_id", userId.ToString() },
                        { "project_id", request.ProjectId.ToString() }
                    }
                };

                var service = new PaymentIntentService();
                var paymentIntent = await service.CreateAsync(options);

                return new CreatePaymentIntentResponse
                {
                    ClientSecret = paymentIntent.ClientSecret,
                    PaymentIntentId = paymentIntent.Id
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment intent for user {UserId}", userId);
                throw;
            }
        }

        public async Task<CustomerPortalResponse> CreateCustomerPortalSessionAsync(int userId, CustomerPortalRequest request)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    throw new Exception("User not found");

                var customerId = await GetOrCreateStripeCustomerAsync(user);

                var options = new Stripe.BillingPortal.SessionCreateOptions
                {
                    Customer = customerId,
                    ReturnUrl = request.ReturnUrl,
                };

                var service = new Stripe.BillingPortal.SessionService();
                var session = await service.CreateAsync(options);

                return new CustomerPortalResponse
                {
                    PortalUrl = session.Url
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer portal session for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> HandleWebhookAsync(string payload, string signature)
        {
            try
            {
                var webhookSecret = _configuration["Stripe:WebhookSecret"];
                var stripeEvent = EventUtility.ConstructEvent(payload, signature, webhookSecret);

                _logger.LogInformation("Stripe webhook received: {EventType}", stripeEvent.Type);

                switch (stripeEvent.Type)
                {
                    case "checkout.session.completed":
                        await HandleCheckoutSessionCompletedAsync(stripeEvent);
                        break;

                    case "customer.subscription.created":
                    case "customer.subscription.updated":
                        await HandleSubscriptionUpdatedAsync(stripeEvent);
                        break;

                    case "customer.subscription.deleted":
                        await HandleSubscriptionDeletedAsync(stripeEvent);
                        break;

                    case "invoice.payment_succeeded":
                        await HandleInvoicePaymentSucceededAsync(stripeEvent);
                        break;

                    case "invoice.payment_failed":
                        await HandleInvoicePaymentFailedAsync(stripeEvent);
                        break;

                    default:
                        _logger.LogInformation("Unhandled webhook event type: {EventType}", stripeEvent.Type);
                        break;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling Stripe webhook");
                return false;
            }
        }

        public async Task<List<PricingPlan>> GetPricingPlansAsync()
        {
            // This should be configured in appsettings.json or database
            // For now, returning hardcoded plans
            return await Task.FromResult(new List<PricingPlan>
            {
                new PricingPlan
                {
                    Id = "starter",
                    Name = "Starter",
                    Description = "Perfect for individuals and small projects",
                    Price = 0,
                    Currency = "usd",
                    Interval = "month",
                    StripePriceId = "", // Free plan, no Stripe price
                    Features = new[]
                    {
                        "5 Projects",
                        "Basic AI Visibility Tracking",
                        "100 Prompts/month",
                        "Community Support"
                    },
                    IsPopular = false
                },
                new PricingPlan
                {
                    Id = "pro",
                    Name = "Pro",
                    Description = "For growing businesses and agencies",
                    Price = 49,
                    Currency = "usd",
                    Interval = "month",
                    StripePriceId = _configuration["Stripe:ProMonthlyPriceId"] ?? "",
                    Features = new[]
                    {
                        "Unlimited Projects",
                        "Advanced AI Visibility Tracking",
                        "Unlimited Prompts",
                        "Site Auditing",
                        "Opportunity Discovery",
                        "Priority Support",
                        "API Access"
                    },
                    IsPopular = true
                },
                new PricingPlan
                {
                    Id = "enterprise",
                    Name = "Enterprise",
                    Description = "For large organizations with custom needs",
                    Price = 299,
                    Currency = "usd",
                    Interval = "month",
                    StripePriceId = _configuration["Stripe:EnterpriseMonthlyPriceId"] ?? "",
                    Features = new[]
                    {
                        "Everything in Pro",
                        "White-label Options",
                        "Custom Integrations",
                        "Dedicated Account Manager",
                        "SLA Guarantee",
                        "Custom Contracts"
                    },
                    IsPopular = false
                }
            });
        }

        private async Task<string> GetOrCreateStripeCustomerAsync(User user)
        {
            // Check if user already has a Stripe customer ID
            if (!string.IsNullOrEmpty(user.StripeCustomerId))
                return user.StripeCustomerId;

            // Create new Stripe customer
            var options = new CustomerCreateOptions
            {
                Email = user.Email,
                Name = $"{user.FirstName} {user.LastName}",
                Metadata = new Dictionary<string, string>
                {
                    { "user_id", user.Id.ToString() }
                }
            };

            var service = new CustomerService();
            var customer = await service.CreateAsync(options);

            // Save customer ID to user
            user.StripeCustomerId = customer.Id;
            await _context.SaveChangesAsync();

            return customer.Id;
        }

        private async Task HandleCheckoutSessionCompletedAsync(Event stripeEvent)
        {
            var session = stripeEvent.Data.Object as Session;
            if (session == null) return;

            var userId = int.Parse(session.ClientReferenceId ?? session.Metadata["user_id"]);
            var projectId = int.Parse(session.Metadata["project_id"]);

            _logger.LogInformation("Checkout session completed for user {UserId}, project {ProjectId}", userId, projectId);

            // Subscription will be handled by subscription.created event
        }

        private async Task HandleSubscriptionUpdatedAsync(Event stripeEvent)
        {
            var subscription = stripeEvent.Data.Object as Stripe.Subscription;
            if (subscription == null) return;

            // TODO: Fix Stripe Subscription property access - need to check Stripe.NET SDK v50 documentation
            // Properties like CurrentPeriodEnd, Created may have different names or types
            // Action: Verify Stripe.NET SDK v50 API, map subscription.CurrentPeriodEnd/CreatedDate correctly, update Subscriptions table with accurate dates and status
            _logger.LogInformation("Subscription updated webhook received for subscription {SubscriptionId}", subscription.Id);
            
            // Placeholder implementation - needs proper Stripe SDK property mapping
            await Task.CompletedTask;
        }

        private async Task HandleSubscriptionDeletedAsync(Event stripeEvent)
        {
            var subscription = stripeEvent.Data.Object as Stripe.Subscription;
            if (subscription == null) return;

            var userId = int.Parse(subscription.Metadata["user_id"]);

            var existingSub = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Platform == "stripe");

            if (existingSub != null)
            {
                existingSub.Status = "cancelled";
                existingSub.AutoRenew = false;
                existingSub.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        private async Task HandleInvoicePaymentSucceededAsync(Event stripeEvent)
        {
            var invoice = stripeEvent.Data.Object as Invoice;
            if (invoice == null) return;

            _logger.LogInformation("Invoice payment succeeded: {InvoiceId}", invoice.Id);

            // Create transaction record
            var transaction = new SubscriptionTransaction
            {
                SubscriptionId = 0, // Will be updated
                TransactionType = "payment",
                Amount = (decimal)invoice.AmountPaid / 100,
                Currency = invoice.Currency,
                Status = "completed",
                TransactionDate = invoice.Created,
                StripeInvoiceId = invoice.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.SubscriptionTransactions.Add(transaction);
            await _context.SaveChangesAsync();
        }

        private async Task HandleInvoicePaymentFailedAsync(Event stripeEvent)
        {
            var invoice = stripeEvent.Data.Object as Invoice;
            if (invoice == null) return;

            _logger.LogWarning("Invoice payment failed: {InvoiceId}", invoice.Id);

            // Create failed transaction record
            var transaction = new SubscriptionTransaction
            {
                SubscriptionId = 0,
                TransactionType = "payment",
                Amount = (decimal)invoice.AmountDue / 100,
                Currency = invoice.Currency,
                Status = "failed",
                TransactionDate = invoice.Created,
                StripeInvoiceId = invoice.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.SubscriptionTransactions.Add(transaction);
            await _context.SaveChangesAsync();
        }
    }
}
