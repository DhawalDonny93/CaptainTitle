using System.Security.Claims;
using AppBackendBasic.Services.Captions;
using Microsoft.AspNetCore.Mvc;

namespace AppBackendBasic.API.Controllers;

/// <summary>Shared helpers for the Captain Title controllers.</summary>
public abstract class CaptionsControllerBase : ControllerBase
{
    protected int CurrentUserId =>
        int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;

    /// <summary>Runs an action and converts <see cref="CaptionServiceException"/> into the matching HTTP error body.</summary>
    protected async Task<IActionResult> RunAsync(Func<Task<IActionResult>> action)
    {
        try { return await action(); }
        catch (CaptionServiceException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.Code ?? (ex.StatusCode == 402 ? "PAYMENT_REQUIRED" : null) });
        }
        // Without this an unexpected failure (database down, ffmpeg missing...) reaches the browser as an empty 500 with nothing useful to show.
        catch (Exception ex) when (!HttpContext.RequestAborted.IsCancellationRequested)
        {
            HttpContext.RequestServices.GetRequiredService<ILogger<CaptionsControllerBase>>()
                .LogError(ex, "Unhandled caption API error on {Method} {Path}", Request.Method, Request.Path);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = CaptionErrors.ServerIssue });
        }
    }
}
