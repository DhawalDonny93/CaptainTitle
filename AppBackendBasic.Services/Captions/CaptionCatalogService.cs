using System.Text.Json;
using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Infrastructure.Data;
using AppBackendBasic.Services.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace AppBackendBasic.Services.Captions;

public interface ICaptionCatalogService
{
    Task<CaptionCatalogDto> GetCatalogAsync(CancellationToken ct = default);
    Task<CaptionStyleSet> GetStyleSetAsync(CancellationToken ct = default);
    Task<CaptionLimitsDto> GetLimitsAsync(CancellationToken ct = default);
    Task<List<CaptionPlan>> GetActivePlansAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SfxTone>> GetSfxTonesAsync(CancellationToken ct = default);
    void Invalidate();
}

/// <summary>Reads the admin-managed caption catalog (templates, fonts, variants, SFX, stickers, fillers, limits, plans).</summary>
public class CaptionCatalogService : ICaptionCatalogService
{
    private const string CacheKey = "captions:catalog:v1";
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(30);
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    private sealed record Snapshot(CaptionCatalogDto Catalog, CaptionStyleSet Styles, List<CaptionPlan> Plans, List<SfxTone> Tones);

    private readonly ApplicationDbContext _db;
    private readonly SystemSettingsService _settings;
    private readonly IMemoryCache _cache;

    public CaptionCatalogService(ApplicationDbContext db, SystemSettingsService settings, IMemoryCache cache)
    {
        _db = db; _settings = settings; _cache = cache;
    }

    public void Invalidate() => _cache.Remove(CacheKey);

    public async Task<CaptionLimitsDto> GetLimitsAsync(CancellationToken ct = default)
    {
        var ext = await _settings.GetSettingValueAsync(CaptionSettingKeys.AllowedExtensions) ?? ".mp4,.mov";
        return new CaptionLimitsDto
        {
            AbsoluteMaxSeconds = await _settings.GetIntSettingAsync(CaptionSettingKeys.AbsoluteMaxSeconds, 120),
            MaxUploadMb = await _settings.GetIntSettingAsync(CaptionSettingKeys.MaxUploadMb, 120),
            FreeVideos = await _settings.GetIntSettingAsync(CaptionSettingKeys.FreeVideos, 1),
            FreeMaxSeconds = await _settings.GetIntSettingAsync(CaptionSettingKeys.FreeMaxSeconds, 45),
            UnlimitedTesting = await _settings.GetBoolSettingAsync(CaptionSettingKeys.UnlimitedTesting, false),
            AllowedExtensions = ext.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(e => e.StartsWith('.') ? e.ToLowerInvariant() : "." + e.ToLowerInvariant()).ToList(),
        };
    }

    private async Task<Snapshot> LoadAsync(CancellationToken ct)
    {
        if (_cache.TryGetValue(CacheKey, out Snapshot? cached) && cached != null) return cached;

        var themes = await _db.CaptionThemes.AsNoTracking().Where(t => t.IsActive).OrderBy(t => t.SortOrder).ThenBy(t => t.Id).ToListAsync(ct);
        var fonts = await _db.CaptionFonts.AsNoTracking().Where(t => t.IsActive).OrderBy(t => t.SortOrder).ToListAsync(ct);
        var variants = await _db.CaptionVariants.AsNoTracking().Where(t => t.IsActive).OrderBy(t => t.SortOrder).ToListAsync(ct);
        var sfx = await _db.CaptionSfxTypes.AsNoTracking().Where(t => t.IsActive).OrderBy(t => t.SortOrder).ToListAsync(ct);
        var stickers = await _db.CaptionStickers.AsNoTracking().Where(t => t.IsActive).OrderBy(t => t.SortOrder).ToListAsync(ct);
        var fillers = await _db.CaptionFillerWords.AsNoTracking().Where(t => t.IsActive).OrderBy(t => t.Word).ToListAsync(ct);
        var plans = await _db.CaptionPlans.AsNoTracking().Where(t => t.IsActive).OrderBy(t => t.SortOrder).ToListAsync(ct);
        var defaultTheme = await _settings.GetSettingValueAsync(CaptionSettingKeys.DefaultTheme) ?? "viral";
        if (themes.Count == 0) throw new InvalidOperationException("No caption templates are configured. Run Database_Migrations/21_captain_title_captions.sql (includes the seed data).");
        if (themes.All(t => t.Key != defaultTheme)) defaultTheme = themes[0].Key;

        var catalog = new CaptionCatalogDto
        {
            DefaultTheme = defaultTheme,
            Limits = await GetLimitsAsync(ct),
            Themes = themes.Select(t => new CaptionThemeDto
            {
                Id = t.Key, Name = t.Name, BaseColor = t.BaseColor, Color = t.AccentColor, AccentFont = t.AccentFont,
                Tiers = CaptionStyleFactory.ParseTiers(t.TiersJson)?.ToDictionary(k => k.Key,
                    k => new CaptionTierDto { Font = k.Value.Font, CssFont = k.Value.CssFont, Color = k.Value.Color, Size = k.Value.Size }),
                Card = string.IsNullOrWhiteSpace(t.CardJson) ? new CaptionCardDto() : JsonSerializer.Deserialize<CaptionCardDto>(t.CardJson, Json) ?? new CaptionCardDto(),
            }).ToList(),
            Fonts = fonts.Select(f => new CaptionFontDto { Id = f.Key, Name = f.Label, Family = f.Family }).ToList(),
            Variants = variants.Select(v => new CaptionVariantDto
            {
                Id = v.Key, Label = v.Label, FontFamily = v.FontFamily, TextShadow = v.CssTextShadow, Stroke = v.CssStroke, Accent = v.AccentColor,
            }).ToList(),
            SfxTypes = sfx.Select(s => new CaptionSfxTypeDto
            {
                Id = s.Key, Label = s.Label, Icon = s.Icon, Color = s.Color, Kind = s.PreviewKind, Freq = s.Frequency, SweepTo = s.SweepTo, Duration = s.DurationSeconds,
            }).ToList(),
            Stickers = stickers.Select(s => s.Glyph).ToList(),
            FillerWords = fillers.Select(f => f.Word).ToList(),
        };
        var snapshot = new Snapshot(catalog, CaptionStyleFactory.Build(themes, fonts, variants, defaultTheme), plans,
            sfx.Select(s => new SfxTone(s.Key, s.Frequency, s.SweepTo, s.DurationSeconds)).ToList());
        _cache.Set(CacheKey, snapshot, Ttl);
        return snapshot;
    }

    public async Task<CaptionCatalogDto> GetCatalogAsync(CancellationToken ct = default) => (await LoadAsync(ct)).Catalog;
    public async Task<CaptionStyleSet> GetStyleSetAsync(CancellationToken ct = default) => (await LoadAsync(ct)).Styles;
    public async Task<List<CaptionPlan>> GetActivePlansAsync(CancellationToken ct = default) => (await LoadAsync(ct)).Plans;
    public async Task<IReadOnlyList<SfxTone>> GetSfxTonesAsync(CancellationToken ct = default) => (await LoadAsync(ct)).Tones;
}
