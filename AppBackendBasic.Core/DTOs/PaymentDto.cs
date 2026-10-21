using System;

namespace AppBackendBasic.Core.DTOs
{
    public class CreateCheckoutSessionRequest
    {
        public string PriceId { get; set; } = string.Empty;
        public int ProjectId { get; set; }
        public string SuccessUrl { get; set; } = string.Empty;
        public string CancelUrl { get; set; } = string.Empty;
    }

    public class CreateCheckoutSessionResponse
    {
        public string SessionId { get; set; } = string.Empty;
        public string CheckoutUrl { get; set; } = string.Empty;
    }

    public class CreatePaymentIntentRequest
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "usd";
        public int ProjectId { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class CreatePaymentIntentResponse
    {
        public string ClientSecret { get; set; } = string.Empty;
        public string PaymentIntentId { get; set; } = string.Empty;
    }

    public class StripeWebhookEvent
    {
        public string Type { get; set; } = string.Empty;
        public object Data { get; set; } = null!;
    }

    public class CustomerPortalRequest
    {
        public string ReturnUrl { get; set; } = string.Empty;
    }

    public class CustomerPortalResponse
    {
        public string PortalUrl { get; set; } = string.Empty;
    }

    public class PricingPlan
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Currency { get; set; } = "usd";
        public string Interval { get; set; } = "month"; // month, year
        public string StripePriceId { get; set; } = string.Empty;
        public string[] Features { get; set; } = Array.Empty<string>();
        public bool IsPopular { get; set; }
    }
}
