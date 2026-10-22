using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;

namespace AppBackendBasic.Services.Captions;

public sealed record CaptionPaymentStatus(string SessionId, string Status, string PaymentStatus);

public interface ICaptionBillingService
{
    Task<(List<CaptionPlanSummaryDto> Items, bool Available)> GetCatalogAsync(CancellationToken ct = default);
    Task<(string CheckoutUrl, string SessionId)> CreateCheckoutAsync(int userId, CaptionCheckoutRequest request, CancellationToken ct = default);
    Task<CaptionPaymentStatus> GetStatusAsync(int userId, string sessionId, CancellationToken ct = default);
    Task HandleWebhookAsync(string payload, string signature, CancellationToken ct = default);
}

/// <summary>
/// Stripe Checkout for caption passes (port of payments.py). Prices are looked up by the server-owned
/// lookup key stored on the admin-managed CaptionPlan, so the client can never choose an amount.
/// Credits are granted exactly once, only after payment is confirmed (status poll or signed webhook).
/// </summary>
public class CaptionBillingService : ICaptionBillingService
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;
    private readonly ICaptionCatalogService _catalog;
    private readonly ICaptionEntitlementService _entitlement;
    private readonly ILogger<CaptionBillingService> _logger;

    public CaptionBillingService(ApplicationDbContext db, IConfiguration config, ICaptionCatalogService catalog,
        ICaptionEntitlementService entitlement, ILogger<CaptionBillingService> logger)
    {
        _db = db; _config = config; _catalog = catalog; _entitlement = entitlement; _logger = logger;
    }

    private string? SecretKey => _config["Stripe:SecretKey"] is { Length: > 0 } k && !k.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase) ? k : null;
    private RequestOptions Opts => new() { ApiKey = SecretKey };

    public async Task<(List<CaptionPlanSummaryDto> Items, bool Available)> GetCatalogAsync(CancellationToken ct = default)
    {
        var plans = await _catalog.GetActivePlansAsync(ct);
        return (plans.Select(CaptionEntitlementService.ToSummary).ToList(), SecretKey != null);
    }

    private bool IsAllowedOrigin(string origin)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https")) return false;
        var normalized = uri.GetLeftPart(UriPartial.Authority);
        var allowed = _config.GetSection("Captions:ReturnOrigins").Get<string[]>() ?? new[] { "http://localhost:3000", "http://localhost:5173" };
        return allowed.Any(a => string.Equals(a.TrimEnd('/'), normalized, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<(string CheckoutUrl, string SessionId)> CreateCheckoutAsync(int userId, CaptionCheckoutRequest request, CancellationToken ct = default)
    {
        var plan = (await _catalog.GetActivePlansAsync(ct)).FirstOrDefault(p => p.Key == request.PackageId)
                   ?? throw new CaptionServiceException(400, "Unknown package");
        if (!IsAllowedOrigin(request.OriginUrl)) throw new CaptionServiceException(400, "Return URL is not an allowed origin");
        if (SecretKey == null) throw new CaptionServiceException(503, "Stripe checkout is unavailable for the configured business country");
        if (string.IsNullOrWhiteSpace(plan.StripeLookupKey)) throw new CaptionServiceException(503, "Payment catalog is not ready");
        if (await _entitlement.HasReachedPurchaseCapAsync(userId, plan, ct))
            throw new CaptionServiceException(409, $"Single-video passes can be purchased up to {plan.MaxPurchasesPerUser} times; then Basic is required.");

        var prices = await new PriceService().ListAsync(new PriceListOptions { LookupKeys = new List<string> { plan.StripeLookupKey }, Active = true, Limit = 1 }, Opts, ct);
        if (prices.Data.Count == 0) throw new CaptionServiceException(503, "Payment catalog is not ready");

        var origin = request.OriginUrl.TrimEnd('/');
        var session = await new SessionService().CreateAsync(new SessionCreateOptions
        {
            LineItems = new List<SessionLineItemOptions> { new() { Price = prices.Data[0].Id, Quantity = 1 } },
            Mode = plan.Kind == "subscription" ? "subscription" : "payment",
            SuccessUrl = $"{origin}/payment/success?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{origin}/payment/cancel",
            Metadata = new Dictionary<string, string> { ["user_id"] = userId.ToString(), ["package_id"] = plan.Key },
            AutomaticTax = new SessionAutomaticTaxOptions { Enabled = true },
            BillingAddressCollection = "required",
        }, Opts, ct);

        _db.CaptionPaymentTransactions.Add(new CaptionPaymentTransaction { UserId = userId, SessionId = session.Id, PackageId = plan.Key });
        await _db.SaveChangesAsync(ct);
        return (session.Url, session.Id);
    }

    private async Task GrantAsync(string sessionId, int userId, string packageId, CancellationToken ct)
    {
        var marked = await _db.CaptionPaymentTransactions
            .Where(t => t.SessionId == sessionId && t.PaymentStatus != "paid")
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.Status, "completed").SetProperty(t => t.PaymentStatus, "paid")
                .SetProperty(t => t.UpdatedAt, DateTime.UtcNow), ct);
        if (marked == 0) return; // already granted - idempotent

        var plan = await _db.CaptionPlans.AsNoTracking().FirstOrDefaultAsync(p => p.Key == packageId, ct);
        if (plan == null) { _logger.LogError("Paid session {Session} references unknown plan {Plan}", sessionId, packageId); return; }
        if (!await _entitlement.GrantAsync(userId, plan, ct))
            _logger.LogError("Session {Session} was paid but user {User} already hit the purchase cap for {Plan}; manual refund required", sessionId, userId, packageId);
    }

    public async Task<CaptionPaymentStatus> GetStatusAsync(int userId, string sessionId, CancellationToken ct = default)
    {
        // Scoped to the caller (the original let any signed-in user poll any session id).
        var record = await _db.CaptionPaymentTransactions.AsNoTracking().FirstOrDefaultAsync(t => t.SessionId == sessionId && t.UserId == userId, ct)
                     ?? throw new CaptionServiceException(404, "Transaction not found");
        if (record.PaymentStatus != "paid" && SecretKey != null)
        {
            var session = await new SessionService().GetAsync(sessionId, null, Opts, ct);
            if (session.PaymentStatus == "paid" || session.Status == "complete")
            {
                await GrantAsync(sessionId, record.UserId, record.PackageId, ct);
                record = await _db.CaptionPaymentTransactions.AsNoTracking().FirstAsync(t => t.SessionId == sessionId, ct);
            }
        }
        return new CaptionPaymentStatus(sessionId, record.Status, record.PaymentStatus);
    }

    public async Task HandleWebhookAsync(string payload, string signature, CancellationToken ct = default)
    {
        var secret = _config["Captions:StripeWebhookSecret"] is { Length: > 0 } s ? s : _config["Stripe:WebhookSecret"];
        if (string.IsNullOrWhiteSpace(secret) || secret.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase))
            throw new CaptionServiceException(503, "Stripe webhook is not configured");
        Event stripeEvent;
        try { stripeEvent = EventUtility.ConstructEvent(payload, signature, secret, throwOnApiVersionMismatch: false); }
        catch (Exception) { throw new CaptionServiceException(400, "Invalid Stripe signature"); }

        if (stripeEvent.Type == "checkout.session.completed" && stripeEvent.Data.Object is Session session
            && session.Metadata != null && session.Metadata.TryGetValue("user_id", out var uid) && int.TryParse(uid, out var userId)
            && session.Metadata.TryGetValue("package_id", out var pkg))
        {
            await GrantAsync(session.Id, userId, pkg, ct);
        }
    }
}
