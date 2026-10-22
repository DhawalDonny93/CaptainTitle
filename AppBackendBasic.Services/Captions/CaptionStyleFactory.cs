using System.Text.Json;
using AppBackendBasic.Core.Entities;

namespace AppBackendBasic.Services.Captions;

public static class CaptionStyleFactory
{
    private sealed class TierJson
    {
        public string font { get; set; } = "";
        public string css_font { get; set; } = "";
        public string color { get; set; } = "#FFFFFF";
        public int size { get; set; } = 52;
    }

    public static IReadOnlyDictionary<string, TierStyle>? ParseTiers(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        var raw = JsonSerializer.Deserialize<Dictionary<string, TierJson>>(json);
        if (raw == null || raw.Count == 0) return null;
        if (!raw.ContainsKey("support")) throw new InvalidOperationException("Tiered theme must define a 'support' tier");
        return raw.ToDictionary(k => k.Key, k => new TierStyle(k.Value.font, k.Value.css_font, k.Value.color, k.Value.size));
    }

    public static ThemeStyle ToStyle(CaptionTheme t) =>
        new(t.Key, t.BaseColor, t.AccentColor, t.AccentFont, ParseTiers(t.TiersJson));

    public static CaptionStyleSet Build(IEnumerable<CaptionTheme> themes, IEnumerable<CaptionFont> fonts,
        IEnumerable<CaptionVariant> variants, string defaultThemeKey)
    {
        var themeMap = themes.Select(ToStyle).ToDictionary(t => t.Key);
        var fallback = themeMap.TryGetValue(defaultThemeKey, out var d) ? d : themeMap.Values.First();
        return new CaptionStyleSet
        {
            DefaultTheme = fallback,
            Themes = themeMap,
            Fonts = fonts.ToDictionary(f => f.Key, f => new FontStyle(f.Key, f.Family)),
            Variants = variants.ToDictionary(v => v.Key,
                v => new VariantStyle(v.Key, v.FontFamily, v.Border, v.OutlineColor, v.Shadow, v.Blur, v.AccentColor)),
        };
    }

    public static CaptionStyleSet FromSeed() =>
        Build(CaptionSeedData.Themes(), CaptionSeedData.Fonts(), CaptionSeedData.Variants(), "viral");
}
