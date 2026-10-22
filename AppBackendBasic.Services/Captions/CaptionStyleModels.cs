namespace AppBackendBasic.Services.Captions;

public sealed record TierStyle(string Font, string CssFont, string Color, int Size);

/// <summary>Resolved render style for a template (from the admin-managed CaptionTheme table).</summary>
public sealed record ThemeStyle(string Key, string? BaseColor, string AccentColor, string? AccentFont,
    IReadOnlyDictionary<string, TierStyle>? Tiers)
{
    /// <summary>Colour/size baked into a word when it is first generated or when the theme is switched.</summary>
    public (string Color, int Size) BakeFor(string? emphasis, bool important)
    {
        if (Tiers != null)
        {
            var tier = emphasis != null && Tiers.TryGetValue(emphasis, out var t) ? t : Tiers["support"];
            return (tier.Color, tier.Size);
        }
        return important ? (AccentColor, 62) : (BaseColor ?? "#FFFFFF", 52);
    }
}

public sealed record FontStyle(string Key, string Family);

public sealed record VariantStyle(string Key, string Font, int Border, string? Outline, int Shadow, int Blur, string Accent);

/// <summary>Everything the ASS writer needs to resolve ids to concrete styles.</summary>
public sealed class CaptionStyleSet
{
    public required ThemeStyle DefaultTheme { get; init; }
    public required IReadOnlyDictionary<string, ThemeStyle> Themes { get; init; }
    public required IReadOnlyDictionary<string, FontStyle> Fonts { get; init; }
    public required IReadOnlyDictionary<string, VariantStyle> Variants { get; init; }
    public string FallbackFontKey { get; init; } = "poppins";

    public ThemeStyle Theme(string key) => Themes.TryGetValue(key, out var t) ? t : DefaultTheme;

    public FontStyle Font(string? key)
    {
        if (key != null && Fonts.TryGetValue(key, out var f)) return f;
        if (Fonts.TryGetValue(FallbackFontKey, out var fb)) return fb;
        return new FontStyle(FallbackFontKey, "Poppins");
    }

    public VariantStyle? Variant(string? key) => key != null && Variants.TryGetValue(key, out var v) ? v : null;
}

public sealed record SfxTone(string Key, double Frequency, double SweepTo, double Duration);
