using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Services.Captions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppBackendBasic.API.Controllers;

/// <summary>Stripe Checkout for caption passes. Separate from the template's SEO subscription endpoints.</summary>
[ApiController]
[Route("api/captions/payments")]
public class CaptionPaymentsController : CaptionsControllerBase
{
    private readonly ICaptionBillingService _billing;

    public CaptionPaymentsController(ICaptionBillingService billing) { _billing = billing; }

    [HttpGet("catalog")]
    [AllowAnonymous]
    public Task<IActionResult> Catalog() => RunAsync(async () =>
    {
        var (items, available, providers) = await _billing.GetCatalogAsync(HttpContext.RequestAborted);
        return Ok(new { items, available, providers });
    });

    /// <summary>Starts a payment. Razorpay (INR/India): returns the order for Checkout.js. Stripe (other currencies): returns a hosted checkout URL.</summary>
    [HttpPost("checkout")]
    [Authorize]
    public Task<IActionResult> Checkout([FromBody] CaptionCheckoutRequest request) => RunAsync(async () =>
        Ok(await _billing.CreateCheckoutAsync(CurrentUserId, request, HttpContext.RequestAborted)));

    /// <summary>Razorpay Checkout.js success callback: verifies the signature server-side and grants the credits.</summary>
    [HttpPost("razorpay/verify")]
    [Authorize]
    public Task<IActionResult> VerifyRazorpay([FromBody] RazorpayVerifyRequest request) => RunAsync(async () =>
    {
        var s = await _billing.VerifyRazorpayAsync(CurrentUserId, request, HttpContext.RequestAborted);
        return Ok(new { session_id = s.SessionId, status = s.Status, payment_status = s.PaymentStatus });
    });

    [HttpGet("status/{sessionId}")]
    [Authorize]
    public Task<IActionResult> Status(string sessionId) => RunAsync(async () =>
    {
        var s = await _billing.GetStatusAsync(CurrentUserId, sessionId, HttpContext.RequestAborted);
        return Ok(new { session_id = s.SessionId, status = s.Status, payment_status = s.PaymentStatus });
    });

    [HttpPost("stripe/webhook")]
    [AllowAnonymous]
    public Task<IActionResult> StripeWebhook() => RunAsync(async () =>
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync();
        await _billing.HandleStripeWebhookAsync(payload, Request.Headers["Stripe-Signature"].ToString(), HttpContext.RequestAborted);
        return Ok(new { status = "ok" });
    });

    /// <summary>Razorpay dashboard webhook (events order.paid, payment.captured, subscription.charged). Signature in X-Razorpay-Signature.</summary>
    [HttpPost("razorpay/webhook")]
    [AllowAnonymous]
    public Task<IActionResult> RazorpayWebhook() => RunAsync(async () =>
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync();
        await _billing.HandleRazorpayWebhookAsync(payload, Request.Headers["X-Razorpay-Signature"].ToString(), HttpContext.RequestAborted);
        return Ok(new { status = "ok" });
    });
}
