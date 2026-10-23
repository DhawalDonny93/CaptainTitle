using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace AppBackendBasic.Services.Captions;

public sealed record CaptionPaymentStatus(string SessionId, string Status, string PaymentStatus);

public interface ICaptionBillingService
{
    Task<(List<CaptionPlanSummaryDto> Items, bool Available, CaptionPaymentProvidersDto Providers)> GetCatalogAsync(CancellationToken ct = default);
    Task<CaptionCheckoutResponse> CreateCheckoutAsync(int userId, CaptionCheckoutRequest request, CancellationToken ct = default);
    Task<CaptionPaymentStatus> VerifyRazorpayAsync(int userId, RazorpayVerifyRequest request, CancellationToken ct = default);
    Task<CaptionPaymentStatus> GetStatusAsync(int userId, string sessionId, CancellationToken ct = default);
    Task HandleStripeWebhookAsync(string payload, string signature, CancellationToken ct = default);
    Task HandleRazorpayWebhookAsync(string payload, string signature, CancellationToken ct = default);
}

/// <summary>
/// Payments for caption passes. Razorpay is used for INR (India), Stripe for other currencies - a plan's currency picks the provider
/// unless the request names one. Prices and plan ids are server-owned (CaptionPlans), the client can never choose an amount.
/// Credits are granted exactly once, only after payment is confirmed: signed client callback, signed webhook, or a status poll
/// that asks the provider directly.
/// </summary>
public class CaptionBillingService : ICaptionBillingService
{
    private const string RazorpayApi = "https://api.razorpay.com/v1";

    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;
    private readonly CaptionOptions _options;
    private readonly HttpClient _http;
    private readonly ICaptionCatalogService _catalog;
    private readonly ICaptionEntitlementService _entitlement;
    private readonly ILogger<CaptionBillingService> _logger;

    public CaptionBillingService(ApplicationDbContext db, IConfiguration config, IOptions<CaptionOptions> options, HttpClient http,
        ICaptionCatalogService catalog, ICaptionEntitlementService entitlement, ILogger<CaptionBillingService> logger)
    {
        _db = db; _config = config; _options = options.Value; _http = http; _catalog = catalog; _entitlement = entitlement; _logger = logger;
        _http.Timeout = TimeSpan.FromSeconds(30);
    }

    // ---------- configuration ----------
    private static string? Real(string? v) => string.IsNullOrWhiteSpace(v) || v.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase) ? null : v;
    private string? StripeKey => Real(_config["Stripe:SecretKey"]);
    private string? RzpKeyId => Real(_config["Razorpay:KeyId"]);
    private string? RzpSecret => Real(_config["Razorpay:KeySecret"]);
    private string? RzpWebhookSecret => Real(_config["Razorpay:WebhookSecret"]);
    private bool RazorpayOn => RzpKeyId != null && RzpSecret != null;
    private RequestOptions StripeOpts => new() { ApiKey = StripeKey };

    public async Task<(List<CaptionPlanSummaryDto> Items, bool Available, CaptionPaymentProvidersDto Providers)> GetCatalogAsync(CancellationToken ct = default)
    {
        var plans = await _catalog.GetActivePlansAsync(ct);
        var providers = new CaptionPaymentProvidersDto { Stripe = StripeKey != null, Razorpay = RazorpayOn, RazorpayKeyId = RazorpayOn ? RzpKeyId : null };
        return (plans.Select(CaptionEntitlementService.ToSummary).ToList(), providers.Stripe || providers.Razorpay, providers);
    }

    private bool IsAllowedOrigin(string origin)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https")) return false;
        var normalized = uri.GetLeftPart(UriPartial.Authority);
        var configured = _config.GetSection("Captions:ReturnOrigins").Get<string[]>() ?? _options.ReturnOrigins;
        return configured.Any(a => string.Equals(a.TrimEnd('/'), normalized, StringComparison.OrdinalIgnoreCase));
    }

    private static string ChooseProvider(CaptionPlan plan, string? requested, bool razorpayOn, bool stripeOn)
    {
        var r = requested?.Trim().ToLowerInvariant();
        if (r is "razorpay" or "stripe") return r;
        if (string.Equals(plan.Currency, "INR", StringComparison.OrdinalIgnoreCase)) return razorpayOn || !stripeOn ? "razorpay" : "stripe";
        return stripeOn || !razorpayOn ? "stripe" : "razorpay";
    }

    // ---------- checkout ----------
    public async Task<CaptionCheckoutResponse> CreateCheckoutAsync(int userId, CaptionCheckoutRequest request, CancellationToken ct = default)
    {
        var plan = (await _catalog.GetActivePlansAsync(ct)).FirstOrDefault(p => p.Key == request.PackageId)
                   ?? throw new CaptionServiceException(400, "Unknown package");
        if (!IsAllowedOrigin(request.OriginUrl)) throw new CaptionServiceException(400, "Return URL is not an allowed origin");
        if (await _entitlement.HasReachedPurchaseCapAsync(userId, plan, ct))
            throw new CaptionServiceException(409, $"Single-video passes can be purchased up to {plan.MaxPurchasesPerUser} times; then Basic is required.");

        var provider = ChooseProvider(plan, request.Provider, RazorpayOn, StripeKey != null);
        return provider == "razorpay" ? await CreateRazorpayAsync(userId, plan, ct) : await CreateStripeAsync(userId, plan, request, ct);
    }

    private async Task<CaptionCheckoutResponse> CreateStripeAsync(int userId, CaptionPlan plan, CaptionCheckoutRequest request, CancellationToken ct)
    {
        if (StripeKey == null) throw new CaptionServiceException(503, "Card payments are not available right now");
        if (string.IsNullOrWhiteSpace(plan.StripeLookupKey)) throw new CaptionServiceException(503, "Payment catalog is not ready");

        var prices = await new PriceService().ListAsync(new PriceListOptions { LookupKeys = new List<string> { plan.StripeLookupKey }, Active = true, Limit = 1 }, StripeOpts, ct);
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
        }, StripeOpts, ct);

        _db.CaptionPaymentTransactions.Add(new CaptionPaymentTransaction { UserId = userId, SessionId = session.Id, PackageId = plan.Key, Provider = "stripe" });
        await _db.SaveChangesAsync(ct);
        return new CaptionCheckoutResponse { Provider = "stripe", CheckoutUrl = session.Url, SessionId = session.Id };
    }

    private HttpRequestMessage RzpRequest(HttpMethod method, string path, object? body = null)
    {
        var req = new HttpRequestMessage(method, RazorpayApi + path);
        req.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{RzpKeyId}:{RzpSecret}")));
        if (body != null) req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        return req;
    }

    private async Task<JsonElement> RzpSendAsync(HttpRequestMessage req, CancellationToken ct)
    {
        using var res = await _http.SendAsync(req, ct);
        var text = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode)
        {
            _logger.LogError("Razorpay {Method} {Url} failed ({Status}): {Body}", req.Method, req.RequestUri, (int)res.StatusCode, text);
            throw new CaptionServiceException(502, "The payment provider rejected the request. Please try again.");
        }
        return JsonDocument.Parse(text).RootElement.Clone();
    }

    /// <summary>Razorpay receipts are limited to 40 characters.</summary>
    private static string Receipt(int userId)
    {
        var r = $"ct_{userId}_{Guid.NewGuid():N}";
        return r.Length <= 40 ? r : r[..40];
    }

    private async Task<CaptionCheckoutResponse> CreateRazorpayAsync(int userId, CaptionPlan plan, CancellationToken ct)
    {
        if (!RazorpayOn) throw new CaptionServiceException(503, "Payments are not available right now");
        var user = await _db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => new { u.Email, u.FirstName }).FirstOrDefaultAsync(ct);
        var notes = new Dictionary<string, string> { ["user_id"] = userId.ToString(), ["package_id"] = plan.Key };
        var dto = new RazorpayCheckoutDto { KeyId = RzpKeyId!, Currency = plan.Currency.ToUpperInvariant(), Description = plan.Name + (plan.Description != null ? " - " + plan.Description : ""),
            PrefillEmail = user?.Email, PrefillName = user?.FirstName };
        string id;
        if (plan.Kind == "subscription")
        {
            if (string.IsNullOrWhiteSpace(plan.RazorpayPlanId)) throw new CaptionServiceException(503, "This plan is not available for Razorpay yet");
            var sub = await RzpSendAsync(RzpRequest(HttpMethod.Post, "/subscriptions", new
            {
                plan_id = plan.RazorpayPlanId, total_count = Math.Max(1, _options.RazorpaySubscriptionCycles), quantity = 1, customer_notify = 1, notes,
            }), ct);
            id = sub.GetProperty("id").GetString()!;
            dto.SubscriptionId = id;
        }
        else
        {
            dto.Amount = (long)Math.Round(plan.Price * 100m, MidpointRounding.AwayFromZero);
            var order = await RzpSendAsync(RzpRequest(HttpMethod.Post, "/orders", new
            {
                amount = dto.Amount, currency = dto.Currency, receipt = Receipt(userId), notes,
            }), ct);
            id = order.GetProperty("id").GetString()!;
            dto.OrderId = id;
        }
        _db.CaptionPaymentTransactions.Add(new CaptionPaymentTransaction { UserId = userId, SessionId = id, PackageId = plan.Key, Provider = "razorpay" });
        await _db.SaveChangesAsync(ct);
        return new CaptionCheckoutResponse { Provider = "razorpay", SessionId = id, Razorpay = dto };
    }

    // ---------- confirmation ----------
    private static string Hmac(string secret, string data) =>
        Convert.ToHexString(new HMACSHA256(Encoding.UTF8.GetBytes(secret)).ComputeHash(Encoding.UTF8.GetBytes(data))).ToLowerInvariant();

    private static bool SignatureMatches(string expected, string? provided) =>
        provided != null && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(provided.Trim().ToLowerInvariant()));

    /// <summary>Marks the transaction paid (once) and grants the credits. Returns without effect when already granted.</summary>
    private async Task GrantAsync(string sessionId, int userId, string packageId, string? paymentId, CancellationToken ct)
    {
        var marked = await _db.CaptionPaymentTransactions
            .Where(t => t.SessionId == sessionId && t.PaymentStatus != "paid")
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.Status, "completed").SetProperty(t => t.PaymentStatus, "paid")
                .SetProperty(t => t.ProviderPaymentId, paymentId).SetProperty(t => t.UpdatedAt, DateTime.UtcNow), ct);
        if (marked == 0) return; // already granted - idempotent

        var plan = await _db.CaptionPlans.AsNoTracking().FirstOrDefaultAsync(p => p.Key == packageId, ct);
        if (plan == null) { _logger.LogError("Paid session {Session} references unknown plan {Plan}", sessionId, packageId); return; }
        if (!await _entitlement.GrantAsync(userId, plan, ct))
            _logger.LogError("Session {Session} was paid but user {User} already hit the purchase cap for {Plan}; manual refund required", sessionId, userId, packageId);
    }

    public async Task<CaptionPaymentStatus> VerifyRazorpayAsync(int userId, RazorpayVerifyRequest request, CancellationToken ct = default)
    {
        if (!RazorpayOn) throw new CaptionServiceException(503, "Payments are not available right now");
        var isSubscription = !string.IsNullOrWhiteSpace(request.SubscriptionId);
        var providerId = isSubscription ? request.SubscriptionId! : request.OrderId ?? throw new CaptionServiceException(400, "order or subscription id is required");
        // Razorpay signs order_id|payment_id for orders and payment_id|subscription_id for subscriptions.
        var signed = isSubscription ? $"{request.PaymentId}|{providerId}" : $"{providerId}|{request.PaymentId}";
        if (!SignatureMatches(Hmac(RzpSecret!, signed), request.Signature)) throw new CaptionServiceException(400, "Payment signature is invalid");

        var tx = await _db.CaptionPaymentTransactions.AsNoTracking().FirstOrDefaultAsync(t => t.SessionId == providerId && t.UserId == userId && t.Provider == "razorpay", ct)
                 ?? throw new CaptionServiceException(404, "Transaction not found");
        await GrantAsync(providerId, userId, tx.PackageId, request.PaymentId, ct);
        var after = await _db.CaptionPaymentTransactions.AsNoTracking().FirstAsync(t => t.SessionId == providerId, ct);
        return new CaptionPaymentStatus(providerId, after.Status, after.PaymentStatus);
    }

    public async Task<CaptionPaymentStatus> GetStatusAsync(int userId, string sessionId, CancellationToken ct = default)
    {
        // Scoped to the caller (the original let any signed-in user poll any session id).
        var record = await _db.CaptionPaymentTransactions.AsNoTracking().FirstOrDefaultAsync(t => t.SessionId == sessionId && t.UserId == userId, ct)
                     ?? throw new CaptionServiceException(404, "Transaction not found");
        if (record.PaymentStatus != "paid")
        {
            if (record.Provider == "stripe" && StripeKey != null)
            {
                var session = await new SessionService().GetAsync(sessionId, null, StripeOpts, ct);
                if (session.PaymentStatus == "paid" || session.Status == "complete") await GrantAsync(sessionId, record.UserId, record.PackageId, null, ct);
            }
            else if (record.Provider == "razorpay" && RazorpayOn)
            {
                bool paid = false;
                if (sessionId.StartsWith("order_"))
                    paid = (await RzpSendAsync(RzpRequest(HttpMethod.Get, $"/orders/{sessionId}"), ct)).GetProperty("status").GetString() == "paid";
                else if (sessionId.StartsWith("sub_"))
                {
                    var sub = await RzpSendAsync(RzpRequest(HttpMethod.Get, $"/subscriptions/{sessionId}"), ct);
                    paid = sub.TryGetProperty("paid_count", out var pc) && pc.ValueKind == JsonValueKind.Number && pc.GetInt32() >= 1;
                }
                if (paid) await GrantAsync(sessionId, record.UserId, record.PackageId, null, ct);
            }
            record = await _db.CaptionPaymentTransactions.AsNoTracking().FirstAsync(t => t.SessionId == sessionId, ct);
        }
        return new CaptionPaymentStatus(sessionId, record.Status, record.PaymentStatus);
    }

    public async Task HandleStripeWebhookAsync(string payload, string signature, CancellationToken ct = default)
    {
        var secret = Real(_config["Captions:StripeWebhookSecret"]) ?? Real(_config["Stripe:WebhookSecret"]);
        if (secret == null) throw new CaptionServiceException(503, "Stripe webhook is not configured");
        Event stripeEvent;
        try { stripeEvent = EventUtility.ConstructEvent(payload, signature, secret, throwOnApiVersionMismatch: false); }
        catch (Exception) { throw new CaptionServiceException(400, "Invalid Stripe signature"); }

        if (stripeEvent.Type == "checkout.session.completed" && stripeEvent.Data.Object is Session session
            && session.Metadata != null && session.Metadata.TryGetValue("user_id", out var uid) && int.TryParse(uid, out var userId)
            && session.Metadata.TryGetValue("package_id", out var pkg))
        {
            await GrantAsync(session.Id, userId, pkg, null, ct);
        }
    }

    public async Task HandleRazorpayWebhookAsync(string payload, string signature, CancellationToken ct = default)
    {
        var secret = RzpWebhookSecret ?? throw new CaptionServiceException(503, "Razorpay webhook is not configured");
        if (!SignatureMatches(Hmac(secret, payload), signature)) throw new CaptionServiceException(400, "Invalid Razorpay signature");

        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;
        var ev = root.TryGetProperty("event", out var e) ? e.GetString() : null;
        var body = root.TryGetProperty("payload", out var pl) ? pl : default;
        string? providerId = null, paymentId = null;
        switch (ev)
        {
            case "order.paid":
                providerId = body.GetProperty("order").GetProperty("entity").GetProperty("id").GetString();
                if (body.TryGetProperty("payment", out var op)) paymentId = op.GetProperty("entity").GetProperty("id").GetString();
                break;
            case "payment.captured":
                var pe = body.GetProperty("payment").GetProperty("entity");
                providerId = pe.TryGetProperty("order_id", out var oid) && oid.ValueKind == JsonValueKind.String ? oid.GetString() : null;
                paymentId = pe.GetProperty("id").GetString();
                break;
            case "subscription.charged":
            case "subscription.activated":
                providerId = body.GetProperty("subscription").GetProperty("entity").GetProperty("id").GetString();
                if (body.TryGetProperty("payment", out var sp)) paymentId = sp.GetProperty("entity").GetProperty("id").GetString();
                break;
        }
        if (providerId == null) return; // event we do not act on

        // Trust our own transaction row (not the payload notes) for who gets what.
        var tx = await _db.CaptionPaymentTransactions.AsNoTracking().FirstOrDefaultAsync(t => t.SessionId == providerId && t.Provider == "razorpay", ct);
        if (tx == null) { _logger.LogWarning("Razorpay webhook {Event} for unknown id {Id}", ev, providerId); return; }
        // A subscription is only granted on its FIRST successful charge (renewal credits are a later feature).
        await GrantAsync(providerId, tx.UserId, tx.PackageId, paymentId, ct);
    }
}
