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

    public static ThemeStyle ToStyle(CaptionTheme t)
    {
        var (upper, flow, active) = ParseCardFlags(t.CardJson);
        return new(t.Key, t.BaseColor, t.AccentColor, t.AccentFont, ParseTiers(t.TiersJson), upper, flow, active);
    }

    private static readonly System.Text.RegularExpressions.Regex HexColor = new("^#[0-9A-Fa-f]{6}$");

    /// <summary>Optional render flags in CardJson (uppercase, flow, active_color). Anything missing or malformed means "off", never an error.</summary>
    public static (bool Uppercase, bool Flow, string? ActiveColor) ParseCardFlags(string? cardJson)
    {
        if (string.IsNullOrWhiteSpace(cardJson)) return (false, false, null);
        try
        {
            using var doc = JsonDocument.Parse(cardJson);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return (false, false, null);
            bool Flag(string name) => root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.True;
            string? active = root.TryGetProperty("active_color", out var a) && a.ValueKind == JsonValueKind.String && HexColor.IsMatch(a.GetString()!) ? a.GetString() : null;
            return (Flag("uppercase"), Flag("flow"), active);
        }
        catch (JsonException) { return (false, false, null); }
    }

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
        Build(CaptionSeedData.Themes(), CaptionSeedData.Fonts(), CaptionSeedData.Variants(), FixedTemplates.StreetGlow);
}
