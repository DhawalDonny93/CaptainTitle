using System.Text.Json;
using AppBackendBasic.Core.Entities;

namespace AppBackendBasic.Services.Captions;

/// <summary>
/// Factory defaults for the admin-managed caption catalog. Values are copied 1:1 from the old
/// Captain Title code (captions.py THEMES/FONTS/VARIANTS/SFX_TONE_PARAMS/CATALOG, ThemePanel.jsx,
/// ElementsPanel.jsx STICKERS, AiToolsPanel.jsx FILLER_WORDS, sfx.js). Seeded once; afterwards the
/// database (Admin UI) is the source of truth.
/// </summary>
public static class CaptionSeedData
{
    private static string Tiers(params (string Tier, string Font, string Css, string Color, int Size)[] t) =>
        JsonSerializer.Serialize(t.ToDictionary(x => x.Tier, x => new { font = x.Font, css_font = x.Css, color = x.Color, size = x.Size }));

    private static string Card(string? stroke, string? shadow, string? glow, bool highlight) =>
        JsonSerializer.Serialize(new { stroke, shadow, glow, highlight });

    public static List<CaptionTheme> Themes() => new()
    {
        // The two fixed templates of the current old app (migration 26). Their look and motion live in FixedTemplates / AssSubtitleWriter; these rows
        // only make them selectable (name, order, active flag, card preview). The nine templates below are the pre-sync catalog: migration 26 turns them
        // off (IsActive = FALSE) but keeps the rows, so existing projects/admin edits are not lost.
        new() { Key = "street_glow", Name = "Street Glow", BaseColor = "#FFFFFF", AccentColor = "#FF2E2E", AccentFont = "Anton", SortOrder = 0,
            CardJson = Card(null, "0 0 14px rgba(255,46,46,.95)", "0 0 28px rgba(255,46,46,.6)", false) },
        new() { Key = "doodle_marker", Name = "Doodle Marker", AccentColor = "#FACC15", SortOrder = 1,
            TiersJson = Tiers(("support", "Caveat", "'Caveat', cursive", "#FFFFFF", 56),
                              ("feature", "Caveat", "'Caveat', cursive", "#FFFFFF", 56),
                              ("key", "Caveat", "'Caveat', cursive", "#111111", 56)),
            CardJson = Card(null, null, null, true) },
        new() { Key = "viral", Name = "Viral", BaseColor = "#FFFFFF", AccentColor = "#FF3B30", AccentFont = "Anton", SortOrder = 1,
            CardJson = Card("1.5px #000", "0 3px 0 #000", null, false) },
        new() { Key = "cinematic", Name = "Cinematic", BaseColor = "#FFF7DB", AccentColor = "#FFB800", AccentFont = "Caveat", SortOrder = 2,
            CardJson = Card(null, "0 2px 10px rgba(0,0,0,.6)", "0 0 14px rgba(255,184,0,.65)", false) },
        new() { Key = "minimal", Name = "Minimal", BaseColor = "#FFFFFF", AccentColor = "#007AFF", SortOrder = 3,
            CardJson = Card(null, "0 1px 3px rgba(0,0,0,.5)", null, false) },
        new() { Key = "podcast", Name = "Podcast", BaseColor = "#FFFFFF", AccentColor = "#A855F7", SortOrder = 4,
            CardJson = Card(null, "0 2px 8px rgba(0,0,0,.55)", "0 0 10px rgba(168,85,247,.5)", false) },
        new() { Key = "meme_mix", Name = "Meme Mix", BaseColor = "#FFFFFF", AccentColor = "#FFEB3B", AccentFont = "Permanent Marker", SortOrder = 5,
            CardJson = Card("1.5px #000", "0 3px 0 #000", null, true) },
        new() { Key = "street_pop", Name = "Street Pop", BaseColor = "#FFFFFF", AccentColor = "#FF6EC7", AccentFont = "Caveat", SortOrder = 6,
            CardJson = Card("1px #fff", "0 2px 8px rgba(0,0,0,.6)", "0 0 12px rgba(255,110,199,.55)", false) },
        new() { Key = "editorial_pop", Name = "Editorial Pop", AccentColor = "#FFEB3B", SortOrder = 7,
            TiersJson = Tiers(("support", "Poppins", "'Poppins', sans-serif", "#FFFFFF", 40),
                              ("feature", "Playfair Display SemiBold", "'Playfair Display', serif", "#FFFFFF", 58),
                              ("key", "Anton", "'Anton', sans-serif", "#FFEB3B", 82)),
            CardJson = Card(null, "0 2px 6px rgba(0,0,0,.6)", null, true) },
        new() { Key = "clean_vogue", Name = "Clean Vogue", AccentColor = "#FF8FC7", SortOrder = 8,
            TiersJson = Tiers(("support", "Poppins", "'Poppins', sans-serif", "#E4E4E7", 38),
                              ("feature", "Playfair Display SemiBold", "'Playfair Display', serif", "#FF8FC7", 56),
                              ("key", "Bebas Neue", "'Bebas Neue', sans-serif", "#FFFFFF", 86)),
            CardJson = Card(null, "0 2px 6px rgba(0,0,0,.6)", "0 0 10px rgba(255,143,199,.5)", false) },
        new() { Key = "street_heat", Name = "Street Heat", AccentColor = "#FF4D00", SortOrder = 9,
            TiersJson = Tiers(("support", "Poppins", "'Poppins', sans-serif", "#FFFFFF", 40),
                              ("feature", "Permanent Marker", "'Permanent Marker', cursive", "#FFFFFF", 54),
                              ("key", "Anton", "'Anton', sans-serif", "#FF4D00", 84)),
            CardJson = Card("1px #000", "0 3px 0 #000", "0 0 12px rgba(255,77,0,.55)", true) },
    };

    public static List<CaptionFont> Fonts() => new()
    {
        new() { Key = "poppins", Label = "Poppins", Family = "Poppins", SortOrder = 1 },
        new() { Key = "anton", Label = "Anton", Family = "Anton", SortOrder = 2 },
        new() { Key = "bebas", Label = "Bebas Neue", Family = "Bebas Neue", SortOrder = 3 },
        new() { Key = "classic", Label = "Classic", Family = "DejaVu Sans", SortOrder = 4 },
        new() { Key = "archivo", Label = "Archivo Black", Family = "Archivo Black", SortOrder = 5 },
        new() { Key = "space_grotesk", Label = "Space Grotesk", Family = "Space Grotesk", SortOrder = 6 },
        new() { Key = "baloo2", Label = "Baloo 2", Family = "Baloo 2 ExtraBold", SortOrder = 7 },
        new() { Key = "hind", Label = "Hind", Family = "Hind", SortOrder = 8 },
        new() { Key = "mukta", Label = "Mukta", Family = "Mukta ExtraBold", SortOrder = 9 },
        new() { Key = "kalam", Label = "Kalam", Family = "Kalam", SortOrder = 10 },
    };

    public static List<CaptionVariant> Variants() => new()
    {
        new() { Key = "rock", Label = "Rock", FontFamily = "Anton", Border = 3, OutlineColor = "#000000", Shadow = 0, Blur = 0, AccentColor = "#FF4D00",
            CssTextShadow = "3px 3px 0 #000, -2px -2px 0 #000", CssStroke = "2px #000", SortOrder = 1 },
        new() { Key = "glow", Label = "Glow", FontFamily = "Outfit", Border = 3, OutlineColor = null, Shadow = 0, Blur = 4, AccentColor = "#22D3EE",
            CssTextShadow = "0 0 20px currentColor, 0 0 8px currentColor", SortOrder = 2 },
        new() { Key = "swiss", Label = "Swiss", FontFamily = "IBM Plex Sans", Border = 0, OutlineColor = null, Shadow = 0, Blur = 0, AccentColor = "#007AFF",
            CssTextShadow = "none", SortOrder = 3 },
        new() { Key = "clean", Label = "Clean", FontFamily = "Poppins", Border = 0, OutlineColor = "#000000", Shadow = 2, Blur = 1, AccentColor = "#EAB308",
            CssTextShadow = "0 2px 6px rgba(0,0,0,.55)", SortOrder = 4 },
    };

    public static List<CaptionSfxType> SfxTypes() => new()
    {
        new() { Key = "whoosh", Label = "Whoosh", Icon = "wind", Color = "#22d3ee", PreviewKind = "sweep", Frequency = 180, SweepTo = 540, DurationSeconds = 0.2, SortOrder = 1 },
        new() { Key = "bass_drop", Label = "Bass Drop", Icon = "speaker", Color = "#a78bfa", PreviewKind = "drop", Frequency = 90, SweepTo = 27, DurationSeconds = 0.4, SortOrder = 2 },
        new() { Key = "pop", Label = "Pop", Icon = "balloon", Color = "#ffeb3b", PreviewKind = "blip", Frequency = 880, SweepTo = 880, DurationSeconds = 0.2, SortOrder = 3 },
        new() { Key = "cash", Label = "Cash", Icon = "money", Color = "#5ee6a0", PreviewKind = "chime", Frequency = 660, SweepTo = 990, DurationSeconds = 0.2, SortOrder = 4 },
        new() { Key = "shutter", Label = "Shutter", Icon = "camera", Color = "#ff8fc7", PreviewKind = "click", Frequency = 1400, SweepTo = 1400, DurationSeconds = 0.08, SortOrder = 5 },
    };

    public static readonly string[] StickerGlyphs = { "★", "♥", "✓", "✗", "➤", "⚡", "☀", "☁", "♪", "✦", "❄", "⚑" };

    public static List<CaptionSticker> Stickers() =>
        StickerGlyphs.Select((g, i) => new CaptionSticker { Glyph = g, SortOrder = i + 1 }).ToList();

    public static readonly string[] FillerWordList =
    {
        "um", "umm", "uh", "uhh", "hmm", "like", "actually", "basically", "matlab", "yaar", "toh", "vo", "voh",
        "haan", "right", "well", "literally", "ok", "okay",
    };

    public static List<CaptionFillerWord> FillerWords() => FillerWordList.Select(w => new CaptionFillerWord { Word = w }).ToList();

    public static List<CaptionPlan> Plans() => new()
    {
        new() { Key = "single_45", Name = "Single video", Kind = "payment", Price = 69, Currency = "INR", Videos = 1, MaxSeconds = 45,
            MaxPurchasesPerUser = 3, StripeLookupKey = "captain_single_45_inr", Description = "1 video · up to 45 seconds", SortOrder = 1 },
        new() { Key = "basic_monthly", Name = "Basic", Kind = "subscription", Price = 399, Currency = "INR", Videos = 10, MaxSeconds = 60,
            StripeLookupKey = "captain_basic_monthly_inr", Description = "10 videos · up to 60 seconds each", IsFeatured = true, SortOrder = 2 },
    };

    public static List<(string Key, string Value, string Type, string Description)> Settings() => new()
    {
        (CaptionSettingKeys.FreeVideos, "1", "int", "Free captioned videos granted to every new account"),
        (CaptionSettingKeys.FreeMaxSeconds, "45", "int", "Maximum length (seconds) of a video that can use a free credit"),
        (CaptionSettingKeys.AbsoluteMaxSeconds, "120", "int", "Hard upper bound on uploaded video length (seconds)"),
        (CaptionSettingKeys.MaxUploadMb, "120", "int", "Maximum upload size in MB"),
        (CaptionSettingKeys.UnlimitedTesting, "false", "bool", "Process videos up to the testing max without spending credits (development only)"),
        (CaptionSettingKeys.UnlimitedTestingMaxSeconds, "120", "int", "Video length cap while unlimited testing is on"),
        (CaptionSettingKeys.AllowedExtensions, ".mp4,.mov", "string", "Comma-separated allowed upload extensions"),
        (CaptionSettingKeys.DefaultTheme, "viral", "string", "Template applied to freshly generated captions"),
        (CaptionSettingKeys.RenderHighPreset, "veryfast", "string", "x264 preset for 'High quality' exports (slower = smaller/better but much more CPU; the old app used 'slow')"),
        (CaptionSettingKeys.RenderHighCrf, "18", "int", "x264 CRF for 'High quality' exports (lower = better; the old app used 17)"),
        (CaptionSettingKeys.RenderDraftPreset, "ultrafast", "string", "x264 preset for 'Draft' exports (the old app used 'veryfast')"),
        (CaptionSettingKeys.RenderDraftCrf, "28", "int", "x264 CRF for 'Draft' exports (the old app used 26)"),
    };

    /// <summary>SFX job defaults mirroring the old hardcoded OpenAI settings (gpt-4o, temperature 0).</summary>
    public static IEnumerable<LlmJobConfig> LlmJobs() => new[]
    {
        new LlmJobConfig { JobKey = CaptionSettingKeys.HinglishModelJobKey, JobName = "Caption: Hinglish conversion", Description = "Converts recognised Hindi/mixed speech to Roman Hinglish and tags word emphasis", Provider = "OpenAI", Model = "gpt-4o", MaxTokens = 16000, Temperature = 0, Category = "Captions" },
        new LlmJobConfig { JobKey = CaptionSettingKeys.EmphasisJobKey, JobName = "Caption: Emphasis re-assignment", Description = "Re-tags Key/Feature/Support emphasis after manual text edits", Provider = "OpenAI", Model = "gpt-4o", MaxTokens = 16000, Temperature = 0, Category = "Captions" },
    };
}
