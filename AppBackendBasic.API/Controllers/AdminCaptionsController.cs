using System.Text.RegularExpressions;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Infrastructure.Data;
using AppBackendBasic.Services.Admin;
using AppBackendBasic.Services.Captions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppBackendBasic.API.Controllers;

/// <summary>
/// Admin controls for the Captain Title module: everything that was hardcoded in the old app
/// (templates, fonts, variants, SFX tones, stickers, filler words, plans/prices, limits) plus
/// project/usage support tools. Consumed by AdminUI pages under /Captions.
/// </summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/captions")]
public class AdminCaptionsController : ControllerBase
{
    private static readonly Regex Hex = new(@"^#[0-9A-Fa-f]{3,8}$", RegexOptions.Compiled);
    private static readonly Regex KeyRx = new(@"^[a-z0-9_]{1,50}$", RegexOptions.Compiled);

    private readonly ApplicationDbContext _db;
    private readonly ICaptionCatalogService _catalog;
    private readonly SystemSettingsService _settings;
    private readonly ICaptionJobQueue _queue;
    private readonly ICaptionEntitlementService _entitlement;
    private readonly ILogger<AdminCaptionsController> _logger;

    public AdminCaptionsController(ApplicationDbContext db, ICaptionCatalogService catalog, SystemSettingsService settings,
 ICaptionJobQueue queue, ICaptionEntitlementService entitlement, ILogger<AdminCaptionsController> logger)
    {
        _db = db; _catalog = catalog; _settings = settings; _queue = queue; _entitlement = entitlement; _logger = logger;
    }

    // ---------- environment health ----------
    /// <summary>Is this server able to run the caption pipeline? (ffmpeg with libx264 + libass, payment/provider keys present)</summary>
    [HttpGet("health")]
    public async Task<IActionResult> Health([FromServices] IFfmpegService ffmpeg, [FromServices] IConfiguration config,
        [FromServices] Microsoft.Extensions.Options.IOptions<CaptionOptions> options)
    {
        static bool Real(string? v) => !string.IsNullOrWhiteSpace(v) && !v.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase);
        var o = options.Value;
        var fonts = o.ResolveFontsDir();
        return Ok(new
        {
            ffmpeg = await ffmpeg.CheckAsync(),
            fontsDirectory = new { path = fonts, exists = Directory.Exists(fonts), files = Directory.Exists(fonts) ? Directory.GetFiles(fonts, "*.ttf").Length : 0 },
            storageRoot = Path.GetFullPath(o.StorageRoot),
            maxConcurrentJobs = o.MaxConcurrentJobs,
            ffmpegThreads = o.FfmpegThreads,
            sarvamConfigured = Real(o.SarvamApiKey),
            razorpayConfigured = Real(config["Razorpay:KeyId"]) && Real(config["Razorpay:KeySecret"]),
            razorpayWebhookConfigured = Real(config["Razorpay:WebhookSecret"]),
            stripeConfigured = Real(config["Stripe:SecretKey"]),
        });
    }

    // ---------- overview ----------
    [HttpGet("overview")]
    public async Task<IActionResult> Overview()
    {
        var since = DateTime.UtcNow.AddDays(-7);
        return Ok(new
        {
            projects = await _db.CaptionProjects.CountAsync(),
            projectsLast7Days = await _db.CaptionProjects.CountAsync(p => p.CreatedAt >= since),
            byStatus = await _db.CaptionProjects.GroupBy(p => p.Status).Select(g => new { status = g.Key, count = g.Count() }).ToListAsync(),
            failed = await _db.CaptionProjects.CountAsync(p => p.Status == CaptionProjectStatus.Failed),
            paidTransactions = await _db.CaptionPaymentTransactions.CountAsync(t => t.PaymentStatus == "paid"),
            activeSubscribers = await _db.CaptionUsages.CountAsync(u => u.SubscriptionActive),
            usersWithUsage = await _db.CaptionUsages.CountAsync(),
        });
    }

    // ---------- generic catalog helpers ----------
    private async Task<IActionResult> ListAsync<T>(IQueryable<T> q) where T : class => Ok(await q.AsNoTracking().ToListAsync());

    private async Task<IActionResult> SaveAsync<T>(DbSet<T> set, T incoming, int? id, string? error) where T : class
    {
        if (error != null) return BadRequest(new { message = error });
        if (id == null) set.Add(incoming);
        else
        {
            var existing = await set.FindAsync(id.Value);
            if (existing == null) return NotFound();
            typeof(T).GetProperty("Id")!.SetValue(incoming, id.Value);
            _db.Entry(existing).CurrentValues.SetValues(incoming);
        }
        try { await _db.SaveChangesAsync(); }
        catch (DbUpdateException ex) { _logger.LogWarning(ex, "Caption catalog save failed"); return Conflict(new { message = "Save failed - is the key/value already in use?" }); }
        _catalog.Invalidate();
        return Ok(incoming);
    }

    private async Task<IActionResult> DeleteAsync<T>(DbSet<T> set, int id, Func<T, Task<string?>>? inUse = null) where T : class
    {
        var entity = await set.FindAsync(id);
        if (entity == null) return NotFound();
        var block = inUse == null ? null : await inUse(entity);
        if (block != null) return Conflict(new { message = block });
        set.Remove(entity);
        await _db.SaveChangesAsync();
        _catalog.Invalidate();
        return NoContent();
    }

    private static string? ValidateKey(string key) => KeyRx.IsMatch(key ?? "") ? null : "Key must be 1-50 chars of a-z, 0-9 or underscore";
    private static string? ValidateHex(string? color, bool required) =>
        string.IsNullOrEmpty(color) ? (required ? "Colour is required" : null) : Hex.IsMatch(color) ? null : $"'{color}' is not a hex colour like #FF3B30";

    // ---------- themes ----------
    [HttpGet("themes")] public Task<IActionResult> Themes() => ListAsync(_db.CaptionThemes.OrderBy(t => t.SortOrder));
    [HttpPost("themes")] public Task<IActionResult> CreateTheme(CaptionTheme t) => SaveAsync(_db.CaptionThemes, t, null, ValidateTheme(t));
    [HttpPut("themes/{id:int}")] public Task<IActionResult> UpdateTheme(int id, CaptionTheme t) => SaveAsync(_db.CaptionThemes, t, id, ValidateTheme(t));
    [HttpDelete("themes/{id:int}")]
    public Task<IActionResult> DeleteTheme(int id) => DeleteAsync(_db.CaptionThemes, id, async t =>
        await _db.CaptionProjects.AnyAsync(p => p.Theme == t.Key) ? "Template is used by existing projects - deactivate it instead." : null);

    private static string? ValidateTheme(CaptionTheme t)
    {
        var err = ValidateKey(t.Key) ?? ValidateHex(t.AccentColor, true) ?? ValidateHex(t.BaseColor, false);
        if (err != null) return err;
        if (string.IsNullOrWhiteSpace(t.TiersJson)) { t.TiersJson = null; if (t.BaseColor == null) return "Flat templates need a base colour (or define tiers)"; }
        else
        {
            try
            {
                var tiers = CaptionStyleFactory.ParseTiers(t.TiersJson);
                foreach (var x in tiers!.Values)
                {
                    if (ValidateHex(x.Color, true) is { } e) return e;
                    if (x.Size is < 24 or > 120) return "Tier size must be 24-120";
                    if (string.IsNullOrWhiteSpace(x.Font) || string.IsNullOrWhiteSpace(x.CssFont)) return "Tier font and css_font are required";
                }
            }
            catch (Exception ex) { return "Invalid TiersJson: " + ex.Message; }
        }
        if (string.IsNullOrWhiteSpace(t.CardJson)) t.CardJson = null;
        else try { System.Text.Json.JsonDocument.Parse(t.CardJson); } catch { return "CardJson is not valid JSON"; }
        t.UpdatedAt = DateTime.UtcNow;
        return null;
    }

    // ---------- fonts ----------
    [HttpGet("fonts")] public Task<IActionResult> Fonts() => ListAsync(_db.CaptionFonts.OrderBy(t => t.SortOrder));
    [HttpPost("fonts")] public Task<IActionResult> CreateFont(CaptionFont f) => SaveAsync(_db.CaptionFonts, f, null, ValidateKey(f.Key));
    [HttpPut("fonts/{id:int}")] public Task<IActionResult> UpdateFont(int id, CaptionFont f) => SaveAsync(_db.CaptionFonts, f, id, ValidateKey(f.Key));
    [HttpDelete("fonts/{id:int}")]
    public Task<IActionResult> DeleteFont(int id) => DeleteAsync(_db.CaptionFonts, id, async f =>
        await _db.CaptionProjects.AnyAsync(p => p.FontId == f.Key) ? "Font is used by existing projects - deactivate it instead." : null);

    // ---------- variants ----------
    [HttpGet("variants")] public Task<IActionResult> Variants() => ListAsync(_db.CaptionVariants.OrderBy(t => t.SortOrder));
    [HttpPost("variants")] public Task<IActionResult> CreateVariant(CaptionVariant v) => SaveAsync(_db.CaptionVariants, v, null, ValidateVariant(v));
    [HttpPut("variants/{id:int}")] public Task<IActionResult> UpdateVariant(int id, CaptionVariant v) => SaveAsync(_db.CaptionVariants, v, id, ValidateVariant(v));
    [HttpDelete("variants/{id:int}")]
    public Task<IActionResult> DeleteVariant(int id) => DeleteAsync(_db.CaptionVariants, id, async v =>
        await _db.CaptionProjects.AnyAsync(p => p.Variant == v.Key) ? "Variant is used by existing projects - deactivate it instead." : null);
    private static string? ValidateVariant(CaptionVariant v) => ValidateKey(v.Key) ?? ValidateHex(v.AccentColor, true) ?? ValidateHex(v.OutlineColor, false);

    // ---------- SFX ----------
    [HttpGet("sfx")] public Task<IActionResult> Sfx() => ListAsync(_db.CaptionSfxTypes.OrderBy(t => t.SortOrder));
    [HttpPost("sfx")] public Task<IActionResult> CreateSfx(CaptionSfxType s) => SaveAsync(_db.CaptionSfxTypes, s, null, ValidateSfx(s));
    [HttpPut("sfx/{id:int}")] public Task<IActionResult> UpdateSfx(int id, CaptionSfxType s) => SaveAsync(_db.CaptionSfxTypes, s, id, ValidateSfx(s));
    [HttpDelete("sfx/{id:int}")] public Task<IActionResult> DeleteSfx(int id) => DeleteAsync(_db.CaptionSfxTypes, id);
    private static string? ValidateSfx(CaptionSfxType s) =>
        ValidateKey(s.Key) ?? ValidateHex(s.Color, true)
        ?? (s.DurationSeconds is <= 0 or > 3 ? "Duration must be between 0 and 3 seconds" : null)
        ?? (s.Frequency <= 0 || s.SweepTo <= 0 ? "Frequencies must be positive" : null)
        ?? (new[] { "sweep", "drop", "blip", "chime", "click" }.Contains(s.PreviewKind) ? null : "PreviewKind must be sweep, drop, blip, chime or click");

    // ---------- stickers / fillers ----------
    [HttpGet("stickers")] public Task<IActionResult> Stickers() => ListAsync(_db.CaptionStickers.OrderBy(t => t.SortOrder));
    [HttpPost("stickers")] public Task<IActionResult> CreateSticker(CaptionSticker s) => SaveAsync(_db.CaptionStickers, s, null, string.IsNullOrWhiteSpace(s.Glyph) ? "Glyph is required" : null);
    [HttpPut("stickers/{id:int}")] public Task<IActionResult> UpdateSticker(int id, CaptionSticker s) => SaveAsync(_db.CaptionStickers, s, id, string.IsNullOrWhiteSpace(s.Glyph) ? "Glyph is required" : null);
    [HttpDelete("stickers/{id:int}")] public Task<IActionResult> DeleteSticker(int id) => DeleteAsync(_db.CaptionStickers, id);

    [HttpGet("filler-words")] public Task<IActionResult> Fillers() => ListAsync(_db.CaptionFillerWords.OrderBy(t => t.Word));
    [HttpPost("filler-words")] public Task<IActionResult> CreateFiller(CaptionFillerWord w) { w.Word = (w.Word ?? "").Trim().ToLowerInvariant(); return SaveAsync(_db.CaptionFillerWords, w, null, w.Word.Length == 0 ? "Word is required" : null); }
    [HttpPut("filler-words/{id:int}")] public Task<IActionResult> UpdateFiller(int id, CaptionFillerWord w) { w.Word = (w.Word ?? "").Trim().ToLowerInvariant(); return SaveAsync(_db.CaptionFillerWords, w, id, w.Word.Length == 0 ? "Word is required" : null); }
    [HttpDelete("filler-words/{id:int}")] public Task<IActionResult> DeleteFiller(int id) => DeleteAsync(_db.CaptionFillerWords, id);

    // ---------- plans ----------
    [HttpGet("plans")] public Task<IActionResult> Plans() => ListAsync(_db.CaptionPlans.OrderBy(t => t.SortOrder));
    [HttpPost("plans")] public Task<IActionResult> CreatePlan(CaptionPlan p) => SaveAsync(_db.CaptionPlans, p, null, ValidatePlan(p));
    [HttpPut("plans/{id:int}")] public Task<IActionResult> UpdatePlan(int id, CaptionPlan p) => SaveAsync(_db.CaptionPlans, p, id, ValidatePlan(p));
    [HttpDelete("plans/{id:int}")]
    public Task<IActionResult> DeletePlan(int id) => DeleteAsync(_db.CaptionPlans, id, async p =>
        await _db.CaptionPaymentTransactions.AnyAsync(t => t.PackageId == p.Key) ? "Plan has payment history - deactivate it instead." : null);
    private static string? ValidatePlan(CaptionPlan p) =>
        ValidateKey(p.Key)
        ?? (p.Kind is "payment" or "subscription" ? null : "Kind must be 'payment' or 'subscription'")
        ?? (p.Price < 0 ? "Price cannot be negative" : null) ?? (p.Videos < 1 ? "Videos must be at least 1" : null)
        ?? (p.MaxSeconds < 1 ? "MaxSeconds must be at least 1" : null) ?? (p.Currency?.Length == 3 ? null : "Currency must be a 3-letter code");

    // ---------- settings (captions.* in SystemSettings) ----------
    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings() => Ok(await _settings.GetSettingsByCategoryAsync("captions"));

    [HttpPut("settings/{key}")]
    public async Task<IActionResult> PutSetting(string key, [FromBody] SettingValue body)
    {
        if (!key.StartsWith("captions.")) return BadRequest(new { message = "Only captions.* settings are editable here" });
        var existing = await _settings.GetSettingAsync(key);
        if (existing == null) return NotFound();
        if (existing.Type == "int" && !int.TryParse(body.Value, out var n)) return BadRequest(new { message = "Value must be an integer" });
        if (existing.Type == "bool" && !bool.TryParse(body.Value, out _)) return BadRequest(new { message = "Value must be true or false" });
        var saved = await _settings.UpsertSettingAsync(key, body.Value ?? "", existing.Type, existing.Category, existing.Description, existing.IsPublic);
        _catalog.Invalidate();
        return Ok(saved);
    }
    public record SettingValue(string? Value);

    // ---------- support tools ----------
    [HttpGet("projects")]
    public async Task<IActionResult> Projects([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 25)
    {
        pageSize = Math.Clamp(pageSize, 1, 100); page = Math.Max(1, page);
        var q = _db.CaptionProjects.AsNoTracking().Where(p => status == null || p.Status == status);
        var total = await q.CountAsync();
        var items = await q.OrderByDescending(p => p.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new { p.Id, p.UserId, UserEmail = p.User!.Email, p.Name, p.Duration, p.Theme, p.Status, p.Progress, p.ProcessingLabel, p.ErrorMessage, p.CreditType, p.CreatedAt })
            .ToListAsync();
        return Ok(new { total, page, pageSize, items });
    }

    [HttpPost("projects/{id:int}/requeue")]
    public async Task<IActionResult> Requeue(int id)
    {
        var p = await _db.CaptionProjects.FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();
        p.Status = CaptionProjectStatus.Queued; p.Progress = 5; p.ProcessingLabel = "Re-queued by admin"; p.ErrorMessage = null; p.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        // No credit is consumed again for an admin re-run.
        return Ok(new { queued = _queue.Enqueue(id, consumeCredit: false) });
    }

    [HttpGet("usage/{userId:int}")]
    public async Task<IActionResult> Usage(int userId) => Ok(await _entitlement.GetUsageDtoAsync(userId));

    [HttpPost("usage/{userId:int}/grant")]
    public async Task<IActionResult> Grant(int userId, [FromBody] GrantRequest body)
    {
        if (!await _db.Users.AnyAsync(u => u.Id == userId)) return NotFound();
        await _entitlement.GetOrCreateUsageAsync(userId);
        var now = DateTime.UtcNow;
        await _db.CaptionUsages.Where(u => u.UserId == userId).ExecuteUpdateAsync(s => s
            .SetProperty(u => u.FreeRemaining, u => u.FreeRemaining + body.Free < 0 ? 0 : u.FreeRemaining + body.Free)
            .SetProperty(u => u.OneoffCredits, u => u.OneoffCredits + body.Oneoff < 0 ? 0 : u.OneoffCredits + body.Oneoff)
            .SetProperty(u => u.BasicCredits, u => u.BasicCredits + body.Basic < 0 ? 0 : u.BasicCredits + body.Basic)
            .SetProperty(u => u.UpdatedAt, now));
        _logger.LogInformation("Admin adjusted caption credits for user {UserId}: free {F}, oneoff {O}, basic {B}", userId, body.Free, body.Oneoff, body.Basic);
        return Ok(await _entitlement.GetUsageDtoAsync(userId));
    }
    public record GrantRequest(int Free, int Oneoff, int Basic);

}
