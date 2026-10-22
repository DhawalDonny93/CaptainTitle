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
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.StatusCode == 402 ? "PAYMENT_REQUIRED" : null });
        }
    }
}
