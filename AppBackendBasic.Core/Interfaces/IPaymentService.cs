using AppBackendBasic.Core.DTOs;

namespace AppBackendBasic.Core.Interfaces
{
    public interface IPaymentService
    {
        Task<CreateCheckoutSessionResponse> CreateCheckoutSessionAsync(int userId, CreateCheckoutSessionRequest request);
        Task<CreatePaymentIntentResponse> CreatePaymentIntentAsync(int userId, CreatePaymentIntentRequest request);
        Task<CustomerPortalResponse> CreateCustomerPortalSessionAsync(int userId, CustomerPortalRequest request);
        Task<bool> HandleWebhookAsync(string payload, string signature);
        Task<List<PricingPlan>> GetPricingPlansAsync();
    }
}
