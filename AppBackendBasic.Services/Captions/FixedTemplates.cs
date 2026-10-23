namespace AppBackendBasic.Services.Captions;

/// <summary>
/// The two fixed, fully-designed "motion graphic" templates of the current old app (captions.py THEMES + the STREET_GLOW_* / DOODLE_* constants).
/// Unlike the older skinnable templates these own their complete look AND bespoke motion (hand-matched between the ASS export in
/// <see cref="AssSubtitleWriter"/> and the editor preview in CaptionStudio.tsx), so the numbers live in code, not in the CaptionThemes table.
/// The table rows only make the templates selectable (name, order, active flag, card preview).
/// </summary>
public static class FixedTemplates
{
    public const string StreetGlow = "street_glow";
    public const string DoodleMarker = "doodle_marker";

    /// <summary>Starting font size baked into a word (old _apply_theme_defaults).</summary>
    public const int DefaultWordSize = 66;

    public static bool IsFixed(string? themeKey) => themeKey == StreetGlow || themeKey == DoodleMarker;

    // ---- Street Glow: LINE-based (headline + subline), project px at the 1080x1920 canvas ----
    public const string StreetGlowLine1Font = "Anton";
    public const string StreetGlowLine1Color = "#FF2E2E";
    public const int StreetGlowLine1Size = 72;
    public const string StreetGlowLine2Font = "Playfair Display SemiBold";
    public const string StreetGlowLine2Color = "#FFFFFF";
    public const int StreetGlowLine2Size = 44;
    public const int StreetGlowGapPx = -8;
    public const double StreetGlowGlowBlur = 10;
    public const double StreetGlowShadowBlur = 3;
    public const double StreetGlowGlowOpacity = 0.85;
    public const double StreetGlowShadowOpacity = 0.55;

    // ---- Doodle Marker: WORD-based; one uniform size, decorations per tier ----
    public const string DoodleFont = "Caveat";
    public const int DoodleFontSize = 56;
    public const string DoodlePillColor = "#FACC15";
    public const string DoodleLoopColor = "#EF4444";
    /// <summary>Same multiplier calibrated for the generic \N engine; only used to ESTIMATE where later rows sit for the decorations.</summary>
    public const double DoodleNativeLineHeight = 1.47;
    /// <summary>The pill/loop draw-on is a handful of discrete steps (libass \clip + \p did not render reliably).</summary>
    public const int DoodleRevealSteps = 6;
}
