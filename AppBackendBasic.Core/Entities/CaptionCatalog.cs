using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackendBasic.Core.Entities;

/// <summary>
/// Admin-managed caption template (old hardcoded THEMES in captions.py + `templates` in ThemePanel.jsx).
/// Flat templates use BaseColor/AccentColor/AccentFont; tiered templates use TiersJson
/// ({"support":{font,color,size},"feature":{...},"key":{...}}).
/// </summary>
public class CaptionTheme
{
    public int Id { get; set; }
    [Required, MaxLength(50)] public string Key { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
    [MaxLength(9)] public string? BaseColor { get; set; }
    [Required, MaxLength(9)] public string AccentColor { get; set; } = "#FFFFFF";
    [MaxLength(100)] public string? AccentFont { get; set; }
    [Column(TypeName = "jsonb")] public string? TiersJson { get; set; }
    /// <summary>Editor-only card preview styling: {stroke,shadow,glow,highlight}.</summary>
    [Column(TypeName = "jsonb")] public string? CardJson { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Selectable primary caption font (old FONTS dict).</summary>
public class CaptionFont
{
    public int Id { get; set; }
    [Required, MaxLength(50)] public string Key { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string Label { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string Family { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}

/// <summary>Style variant overlay (old VARIANTS dict + `variants` in ThemePanel.jsx).</summary>
public class CaptionVariant
{
    public int Id { get; set; }
    [Required, MaxLength(50)] public string Key { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string Label { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string FontFamily { get; set; } = string.Empty;
    public int Border { get; set; }
    /// <summary>Null = use the word's own colour.</summary>
    [MaxLength(9)] public string? OutlineColor { get; set; }
    public int Shadow { get; set; }
    public int Blur { get; set; }
    [Required, MaxLength(9)] public string AccentColor { get; set; } = "#EAB308";
    /// <summary>CSS text-shadow used by the editor preview only.</summary>
    [MaxLength(300)] public string? CssTextShadow { get; set; }
    /// <summary>CSS -webkit-text-stroke used by the editor preview only.</summary>
    [MaxLength(50)] public string? CssStroke { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}

/// <summary>SFX marker type + synthesized tone parameters (old SFX_TONE_PARAMS and sfx.js SFX_TYPES).</summary>
public class CaptionSfxType
{
    public int Id { get; set; }
    [Required, MaxLength(50)] public string Key { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string Label { get; set; } = string.Empty;
    [MaxLength(50)] public string Icon { get; set; } = "volume";
    [MaxLength(9)] public string Color { get; set; } = "#FFFFFF";
    /// <summary>Web Audio preview shape: sweep | drop | blip | chime | click.</summary>
    [MaxLength(20)] public string PreviewKind { get; set; } = "blip";
    public double Frequency { get; set; }
    public double SweepTo { get; set; }
    public double DurationSeconds { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}

/// <summary>Sticker glyph offered in the Elements panel (old STICKERS array).</summary>
public class CaptionSticker
{
    public int Id { get; set; }
    [Required, MaxLength(20)] public string Glyph { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}

/// <summary>
/// Purchasable pass/plan (old hardcoded CATALOG + usage plans).
/// Kind "payment" grants OneoffCredits, "subscription" grants BasicCredits.
/// </summary>
public class CaptionPlan
{
    public int Id { get; set; }
    [Required, MaxLength(50)] public string Key { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(20)] public string Kind { get; set; } = "payment";
    [Column(TypeName = "decimal(10,2)")] public decimal Price { get; set; }
    [Required, MaxLength(3)] public string Currency { get; set; } = "INR";
    public int Videos { get; set; }
    public int MaxSeconds { get; set; }
    /// <summary>Cap on purchases per user (old: 3 for the single pass). Null = unlimited.</summary>
    public int? MaxPurchasesPerUser { get; set; }
    [MaxLength(100)] public string? StripeLookupKey { get; set; }
    [MaxLength(200)] public string? Description { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}

/// <summary>Admin-managed filler word list for the AI tools panel (old FILLER_WORDS).</summary>
public class CaptionFillerWord
{
    public int Id { get; set; }
    [Required, MaxLength(50)] public string Word { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public static class CaptionSettingKeys
{
    public const string FreeVideos = "captions.free_videos";
    public const string FreeMaxSeconds = "captions.free_max_seconds";
    public const string AbsoluteMaxSeconds = "captions.absolute_max_seconds";
    public const string MaxUploadMb = "captions.max_upload_mb";
    public const string UnlimitedTesting = "captions.unlimited_testing";
    public const string UnlimitedTestingMaxSeconds = "captions.unlimited_testing_max_seconds";
    public const string AllowedExtensions = "captions.allowed_extensions";
    public const string DefaultTheme = "captions.default_theme";
    public const string HinglishModelJobKey = "CaptionHinglishConversion";
    public const string EmphasisJobKey = "CaptionEmphasisAssignment";
}
