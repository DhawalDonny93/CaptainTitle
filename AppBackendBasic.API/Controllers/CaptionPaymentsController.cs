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
        var (items, available) = await _billing.GetCatalogAsync(HttpContext.RequestAborted);
        return Ok(new { items, available });
    });

    [HttpPost("checkout")]
    [Authorize]
    public Task<IActionResult> Checkout([FromBody] CaptionCheckoutRequest request) => RunAsync(async () =>
    {
        var (url, sessionId) = await _billing.CreateCheckoutAsync(CurrentUserId, request, HttpContext.RequestAborted);
        return Ok(new { checkout_url = url, session_id = sessionId });
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
    public Task<IActionResult> Webhook() => RunAsync(async () =>
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync();
        await _billing.HandleWebhookAsync(payload, Request.Headers["Stripe-Signature"].ToString(), HttpContext.RequestAborted);
        return Ok(new { status = "ok" });
    });
}
