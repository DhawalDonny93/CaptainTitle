using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Services.Captions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

namespace AppBackendBasic.API.Controllers;

/// <summary>
/// Captain Title caption editor API. Route/shape parity with the old FastAPI `/api/*` endpoints
/// (usage, projects/process, projects/{id}, srt, retranscribe, ai/emphasis, render, media, download),
/// moved under /api/captions and authenticated with the template's JWT bearer tokens.
/// </summary>
[ApiController]
[Route("api/captions")]
public class CaptionsController : CaptionsControllerBase
{
    private readonly ICaptionProjectService _projects;
    private readonly ICaptionCatalogService _catalog;
    private readonly ICaptionEntitlementService _entitlement;
    private readonly ICaptionMediaTokenService _mediaTokens;

    public CaptionsController(ICaptionProjectService projects, ICaptionCatalogService catalog,
        ICaptionEntitlementService entitlement, ICaptionMediaTokenService mediaTokens)
    {
        _projects = projects; _catalog = catalog; _entitlement = entitlement; _mediaTokens = mediaTokens;
    }

    /// <summary>Templates, fonts, variants, SFX, stickers, filler words and limits (all admin-managed).</summary>
    [HttpGet("catalog")]
    [AllowAnonymous]
    public Task<IActionResult> Catalog() => RunAsync(async () => Ok(await _catalog.GetCatalogAsync(HttpContext.RequestAborted)));

    [HttpGet("usage")]
    [Authorize]
    public Task<IActionResult> Usage() => RunAsync(async () => Ok(await _entitlement.GetUsageDtoAsync(CurrentUserId, HttpContext.RequestAborted)));

    [HttpPost("projects/process")]
    [Authorize]
    [DisableRequestSizeLimit]
    [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
    public Task<IActionResult> Process(IFormFile file, [FromForm] string theme = "street_glow", [FromForm(Name = "font_id")] string fontId = "poppins",
        [FromForm(Name = "speech_language")] string speechLanguage = "auto") => RunAsync(async () =>
    {
        if (file == null || file.Length == 0) return BadRequest(new { message = "Video file is required" });
        await using var stream = file.OpenReadStream();
        var project = await _projects.CreateFromUploadAsync(CurrentUserId, stream, file.FileName, file.ContentType, file.Length,
            theme, fontId, speechLanguage, HttpContext.RequestAborted);
        return Ok(project);
    });

    /// <summary>The signed-in user's projects, most recently edited first (cursor paged, no word documents).</summary>
    [HttpGet("projects")]
    [Authorize]
    public Task<IActionResult> List([FromQuery] string? cursor, [FromQuery] int limit = 12) => RunAsync(async () =>
        Ok(await _projects.ListAsync(CurrentUserId, cursor, limit, HttpContext.RequestAborted)));

    [HttpGet("projects/{id:int}")]
    [Authorize]
    public Task<IActionResult> Get(int id) => RunAsync(async () => Ok(await _projects.GetAsync(CurrentUserId, id, HttpContext.RequestAborted)));

    [HttpPut("projects/{id:int}")]
    [Authorize]
    public Task<IActionResult> Save(int id, [FromBody] CaptionProjectEditDto edit) => RunAsync(async () =>
        Ok(await _projects.SaveAsync(CurrentUserId, id, edit, HttpContext.RequestAborted)));

    /// <summary>Removes the project, its restore points and its stored files. Credits are not refunded.</summary>
    [HttpDelete("projects/{id:int}")]
    [Authorize]
    public Task<IActionResult> Delete(int id) => RunAsync(async () =>
    {
        await _projects.DeleteAsync(CurrentUserId, id, HttpContext.RequestAborted);
        return NoContent();
    });

    /// <summary>Saves where the user left off (playhead, open tabs). Never touches the project content or its revision.</summary>
    [HttpPut("projects/{id:int}/editor-state")]
    [Authorize]
    public Task<IActionResult> SaveEditorState(int id, [FromBody] CaptionEditorStateDto state) => RunAsync(async () =>
    {
        await _projects.SaveEditorStateAsync(CurrentUserId, id, state, HttpContext.RequestAborted);
        return NoContent();
    });

    [HttpGet("projects/{id:int}/versions")]
    [Authorize]
    public Task<IActionResult> Versions(int id) => RunAsync(async () =>
        Ok(await _projects.ListVersionsAsync(CurrentUserId, id, HttpContext.RequestAborted)));

    [HttpPost("projects/{id:int}/versions/{versionId:int}/restore")]
    [Authorize]
    public Task<IActionResult> RestoreVersion(int id, int versionId) => RunAsync(async () =>
        Ok(await _projects.RestoreVersionAsync(CurrentUserId, id, versionId, HttpContext.RequestAborted)));

    [HttpGet("projects/{id:int}/srt")]
    [Authorize]
    public Task<IActionResult> Srt(int id) => RunAsync(async () =>
    {
        var srt = await _projects.ExportSrtAsync(CurrentUserId, id, HttpContext.RequestAborted);
        return File(System.Text.Encoding.UTF8.GetBytes(srt), "application/x-subrip", "captain-title.srt");
    });

    [HttpPost("projects/{id:int}/retranscribe")]
    [Authorize]
    public Task<IActionResult> Retranscribe(int id, [FromBody] CaptionRetranscribeRequest request) => RunAsync(async () =>
        Ok(await _projects.RetranscribeAsync(CurrentUserId, id, request.SpeechLanguage, HttpContext.RequestAborted)));

    [HttpPost("projects/{id:int}/ai/emphasis")]
    [Authorize]
    public Task<IActionResult> Emphasis(int id) => RunAsync(async () =>
        Ok(await _projects.ReassignEmphasisAsync(CurrentUserId, id, HttpContext.RequestAborted)));

    [HttpPost("projects/{id:int}/render")]
    [Authorize]
    public Task<IActionResult> Render(int id) => RunAsync(async () =>
    {
        // The export runs as a background job (ffmpeg can take minutes); the editor polls GET projects/{id} until the status
        // is "rendered" (download via /download) or back to "ready" with error_message.
        var project = await _projects.RequestRenderAsync(CurrentUserId, id, HttpContext.RequestAborted);
        return Accepted(project);
    });

    [HttpGet("projects/{id:int}/download")]
    [Authorize]
    public Task<IActionResult> Download(int id) => RunAsync(async () =>
    {
        var stream = await _projects.OpenRenderAsync(CurrentUserId, id, HttpContext.RequestAborted);
        return File(stream, "video/mp4", "captain-title.mp4");
    });

    /// <summary>Short-lived signed URL the &lt;video&gt; element can use (it cannot send a bearer header).</summary>
    [HttpPost("projects/{id:int}/media-url")]
    [Authorize]
    public Task<IActionResult> MediaUrl(int id) => RunAsync(async () =>
    {
        await _projects.EnsureOwnedAsync(CurrentUserId, id, HttpContext.RequestAborted);
        var token = _mediaTokens.Create(id, CurrentUserId);
        return Ok(new { url = $"/api/captions/media/{id}?t={Uri.EscapeDataString(token)}" });
    });

    /// <summary>Byte-range capable media stream (preview WebM when available, else the source upload).</summary>
    [HttpGet("media/{id:int}")]
    [AllowAnonymous]
    public Task<IActionResult> Media(int id, [FromQuery(Name = "t")] string? token) => RunAsync(async () =>
    {
        var claims = string.IsNullOrEmpty(token) ? null : _mediaTokens.Validate(token);
        if (claims == null || claims.Value.ProjectId != id) return Unauthorized(new { message = "Media link expired" });
        var (stream, contentType) = await _projects.OpenMediaAsync(claims.Value.UserId, id, HttpContext.RequestAborted);
        Response.Headers["Cache-Control"] = "private, max-age=3600";
        return File(stream, contentType, enableRangeProcessing: true);
    });

    /// <summary>Library thumbnail; signed like the media URL because an &lt;img&gt; cannot send a bearer header either.</summary>
    [HttpGet("thumbnail/{id:int}")]
    [AllowAnonymous]
    public Task<IActionResult> Thumbnail(int id, [FromQuery(Name = "t")] string? token) => RunAsync(async () =>
    {
        var claims = string.IsNullOrEmpty(token) ? null : _mediaTokens.Validate(token);
        if (claims == null || claims.Value.ProjectId != id) return Unauthorized(new { message = "Media link expired" });
        var stream = await _projects.OpenThumbnailAsync(claims.Value.UserId, id, HttpContext.RequestAborted);
        Response.Headers["Cache-Control"] = "private, max-age=3600";
        return File(stream, "image/jpeg");
    });
}
