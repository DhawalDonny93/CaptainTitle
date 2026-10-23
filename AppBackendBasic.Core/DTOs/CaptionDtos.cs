using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AppBackendBasic.Core.DTOs;

// NOTE: The caption API intentionally keeps the snake_case wire format of the original
// Captain Title API (font_size, pos_x, new_line, ...). The ported editor depends on these
// names and the nested word/element documents are persisted as jsonb in the same shape.

public class CaptionWordDto
{
    [JsonPropertyName("index")] public int Index { get; set; }
    [JsonPropertyName("text"), Required, StringLength(80, MinimumLength = 1)] public string Text { get; set; } = string.Empty;
    [JsonPropertyName("original")] public string Original { get; set; } = string.Empty;
    [JsonPropertyName("start")] public double Start { get; set; }
    [JsonPropertyName("end")] public double End { get; set; }
    [JsonPropertyName("important")] public bool Important { get; set; }
    [JsonPropertyName("emphasis")] public string Emphasis { get; set; } = "support";
    [JsonPropertyName("uncertain")] public bool Uncertain { get; set; }
    [JsonPropertyName("color")] public string Color { get; set; } = "#FFFFFF";
    // Bounds mirror the editor's Scale slider ([16,120]); they must match or "Apply to All" fails validation for every word at once.
    [JsonPropertyName("font_size"), Range(16, 120)] public int FontSize { get; set; } = 66;
    [JsonPropertyName("pos_x"), Range(0, 100)] public double? PosX { get; set; }
    [JsonPropertyName("pos_y"), Range(0, 100)] public double? PosY { get; set; }
    [JsonPropertyName("new_line")] public bool? NewLine { get; set; }
    [JsonPropertyName("box_width"), Range(0, 100)] public double? BoxWidth { get; set; }
    [JsonPropertyName("box_height"), Range(0, 100)] public double? BoxHeight { get; set; }
    /// <summary>Degrees (-45..45) the whole caption box is tilted around its centre. Null = none.</summary>
    [JsonPropertyName("rotation"), Range(-45, 45)] public double? Rotation { get; set; }
    /// <summary>Tri-state caption boundary: true = a new caption starts at this word (manual split), false = joins the previous caption (manual merge), null = auto grouping.</summary>
    [JsonPropertyName("new_caption")] public bool? NewCaption { get; set; }
    /// <summary>pulse | pop | scale | box | wobble. Editor-only; not rendered into the export.</summary>
    [JsonPropertyName("kinetic")] public string? Kinetic { get; set; }
    [JsonPropertyName("emphasize")] public bool Emphasize { get; set; }
}

public class CaptionElementDto
{
    [JsonPropertyName("id"), Required] public string Id { get; set; } = string.Empty;
    /// <summary>sticker | text</summary>
    [JsonPropertyName("kind")] public string Kind { get; set; } = "sticker";
    [JsonPropertyName("char"), Required, StringLength(60, MinimumLength = 1)] public string Char { get; set; } = string.Empty;
    [JsonPropertyName("font_id")] public string FontId { get; set; } = "poppins";
    [JsonPropertyName("color")] public string Color { get; set; } = "#FFFFFF";
    [JsonPropertyName("font_size"), Range(20, 280)] public int FontSize { get; set; } = 120;
    [JsonPropertyName("start"), Range(0, double.MaxValue)] public double Start { get; set; }
    [JsonPropertyName("end"), Range(0, double.MaxValue)] public double End { get; set; }
    [JsonPropertyName("pos_x"), Range(0, 100)] public double PosX { get; set; } = 50;
    [JsonPropertyName("pos_y"), Range(0, 100)] public double PosY { get; set; } = 30;
    /// <summary>front | behind - draw order relative to the caption text (plain z-staging, no subject matting).</summary>
    [JsonPropertyName("layer"), RegularExpression("^(front|behind)$")] public string Layer { get; set; } = "front";
}

public class CaptionSfxMarkerDto
{
    [JsonPropertyName("id"), Required] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("type"), Required] public string Type { get; set; } = "pop";
    [JsonPropertyName("time"), Range(0, double.MaxValue)] public double Time { get; set; }
    [JsonPropertyName("word_index")] public int? WordIndex { get; set; }
}

public class CaptionProjectEditDto
{
    [JsonPropertyName("theme"), Required] public string Theme { get; set; } = string.Empty;
    [JsonPropertyName("font_id")] public string FontId { get; set; } = "poppins";
    [JsonPropertyName("words_per_caption"), Range(1, 8)] public int? WordsPerCaption { get; set; }
    [JsonPropertyName("caption_lines"), Range(1, 2)] public int CaptionLines { get; set; } = 1;
    [JsonPropertyName("caption_position")] public string CaptionPosition { get; set; } = "bottom";
    [JsonPropertyName("audio_volume"), Range(0, 200)] public int AudioVolume { get; set; } = 100;
    [JsonPropertyName("audio_muted")] public bool AudioMuted { get; set; }
    [JsonPropertyName("brightness"), Range(-50, 50)] public int Brightness { get; set; }
    [JsonPropertyName("contrast"), Range(50, 200)] public int Contrast { get; set; } = 100;
    [JsonPropertyName("saturation"), Range(0, 200)] public int Saturation { get; set; } = 100;
    [JsonPropertyName("render_quality"), RegularExpression("^(high|draft)$")] public string RenderQuality { get; set; } = "high";
    [JsonPropertyName("words"), Required] public List<CaptionWordDto> Words { get; set; } = new();
    [JsonPropertyName("elements")] public List<CaptionElementDto> Elements { get; set; } = new();
    [JsonPropertyName("sfx_markers")] public List<CaptionSfxMarkerDto> SfxMarkers { get; set; } = new();
    [JsonPropertyName("variant")] public string? Variant { get; set; }
    // ---- project-wide typography (old ProjectEdit styling fields) ----
    [JsonPropertyName("script_mode"), RegularExpression("^(roman|native)$")] public string ScriptMode { get; set; } = "roman";
    [JsonPropertyName("outline_enabled")] public bool OutlineEnabled { get; set; }
    [JsonPropertyName("outline_width"), Range(0, 8)] public double OutlineWidth { get; set; } = 2;
    [JsonPropertyName("outline_color"), RegularExpression("^#[0-9A-Fa-f]{3,8}$")] public string OutlineColor { get; set; } = "#000000";
    [JsonPropertyName("shadow_enabled")] public bool ShadowEnabled { get; set; } = true;
    [JsonPropertyName("shadow_angle"), Range(0, 360)] public double ShadowAngle { get; set; }
    [JsonPropertyName("shadow_distance"), Range(0, 15)] public double ShadowDistance { get; set; } = 2;
    [JsonPropertyName("shadow_blur"), Range(0, 15)] public double ShadowBlur { get; set; } = 2;
    [JsonPropertyName("shadow_opacity"), Range(0, 1)] public double ShadowOpacity { get; set; } = 0.5;
    [JsonPropertyName("shadow_color"), RegularExpression("^#[0-9A-Fa-f]{3,8}$")] public string ShadowColor { get; set; } = "#000000";
    [JsonPropertyName("glow_enabled")] public bool GlowEnabled { get; set; }
    /// <summary>Null = each word's own colour.</summary>
    [JsonPropertyName("glow_color"), RegularExpression("^#[0-9A-Fa-f]{3,8}$")] public string? GlowColor { get; set; }
    [JsonPropertyName("glow_blur"), Range(0, 24)] public double GlowBlur { get; set; } = 8;
    [JsonPropertyName("glow_opacity"), Range(0, 1)] public double GlowOpacity { get; set; } = 0.6;
    [JsonPropertyName("background_enabled")] public bool BackgroundEnabled { get; set; }
    [JsonPropertyName("background_color"), RegularExpression("^#[0-9A-Fa-f]{3,8}$")] public string BackgroundColor { get; set; } = "#000000";
    [JsonPropertyName("background_opacity"), Range(0, 1)] public double BackgroundOpacity { get; set; } = 0.6;
    [JsonPropertyName("bold")] public bool Bold { get; set; } = true;
    [JsonPropertyName("italic")] public bool Italic { get; set; }
}

public class CaptionRetranscribeRequest
{
    [JsonPropertyName("speech_language")] public string SpeechLanguage { get; set; } = "auto";
}

/// <summary>Public view of a project: internal storage paths, owner and credit bucket are never exposed.</summary>
public class CaptionProjectDto
{
    [JsonPropertyName("project_id")] public int ProjectId { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("duration")] public double Duration { get; set; }
    [JsonPropertyName("width")] public int? Width { get; set; }
    [JsonPropertyName("height")] public int? Height { get; set; }
    [JsonPropertyName("size_bytes")] public long SizeBytes { get; set; }
    [JsonPropertyName("theme")] public string Theme { get; set; } = string.Empty;
    [JsonPropertyName("font_id")] public string FontId { get; set; } = string.Empty;
    [JsonPropertyName("words_per_caption")] public int? WordsPerCaption { get; set; }
    [JsonPropertyName("caption_lines")] public int CaptionLines { get; set; }
    [JsonPropertyName("caption_position")] public string CaptionPosition { get; set; } = "bottom";
    [JsonPropertyName("audio_volume")] public int AudioVolume { get; set; }
    [JsonPropertyName("audio_muted")] public bool AudioMuted { get; set; }
    [JsonPropertyName("brightness")] public int Brightness { get; set; }
    [JsonPropertyName("contrast")] public int Contrast { get; set; }
    [JsonPropertyName("saturation")] public int Saturation { get; set; }
    [JsonPropertyName("render_quality")] public string RenderQuality { get; set; } = "high";
    [JsonPropertyName("elements")] public List<CaptionElementDto> Elements { get; set; } = new();
    [JsonPropertyName("sfx_markers")] public List<CaptionSfxMarkerDto> SfxMarkers { get; set; } = new();
    [JsonPropertyName("variant")] public string? Variant { get; set; }
    // ---- project-wide typography (old ProjectEdit styling fields) ----
    [JsonPropertyName("script_mode"), RegularExpression("^(roman|native)$")] public string ScriptMode { get; set; } = "roman";
    [JsonPropertyName("outline_enabled")] public bool OutlineEnabled { get; set; }
    [JsonPropertyName("outline_width"), Range(0, 8)] public double OutlineWidth { get; set; } = 2;
    [JsonPropertyName("outline_color"), RegularExpression("^#[0-9A-Fa-f]{3,8}$")] public string OutlineColor { get; set; } = "#000000";
    [JsonPropertyName("shadow_enabled")] public bool ShadowEnabled { get; set; } = true;
    [JsonPropertyName("shadow_angle"), Range(0, 360)] public double ShadowAngle { get; set; }
    [JsonPropertyName("shadow_distance"), Range(0, 15)] public double ShadowDistance { get; set; } = 2;
    [JsonPropertyName("shadow_blur"), Range(0, 15)] public double ShadowBlur { get; set; } = 2;
    [JsonPropertyName("shadow_opacity"), Range(0, 1)] public double ShadowOpacity { get; set; } = 0.5;
    [JsonPropertyName("shadow_color"), RegularExpression("^#[0-9A-Fa-f]{3,8}$")] public string ShadowColor { get; set; } = "#000000";
    [JsonPropertyName("glow_enabled")] public bool GlowEnabled { get; set; }
    /// <summary>Null = each word's own colour.</summary>
    [JsonPropertyName("glow_color"), RegularExpression("^#[0-9A-Fa-f]{3,8}$")] public string? GlowColor { get; set; }
    [JsonPropertyName("glow_blur"), Range(0, 24)] public double GlowBlur { get; set; } = 8;
    [JsonPropertyName("glow_opacity"), Range(0, 1)] public double GlowOpacity { get; set; } = 0.6;
    [JsonPropertyName("background_enabled")] public bool BackgroundEnabled { get; set; }
    [JsonPropertyName("background_color"), RegularExpression("^#[0-9A-Fa-f]{3,8}$")] public string BackgroundColor { get; set; } = "#000000";
    [JsonPropertyName("background_opacity"), Range(0, 1)] public double BackgroundOpacity { get; set; } = 0.6;
    [JsonPropertyName("bold")] public bool Bold { get; set; } = true;
    [JsonPropertyName("italic")] public bool Italic { get; set; }
    [JsonPropertyName("words")] public List<CaptionWordDto> Words { get; set; } = new();
    [JsonPropertyName("source_transcript")] public string SourceTranscript { get; set; } = string.Empty;
    [JsonPropertyName("speech_language")] public string SpeechLanguage { get; set; } = "auto";
    [JsonPropertyName("transcription_mode")] public string? TranscriptionMode { get; set; }
    [JsonPropertyName("transcription_quality")] public JsonElement? TranscriptionQuality { get; set; }
    [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;
    [JsonPropertyName("progress")] public int Progress { get; set; }
    [JsonPropertyName("processing_label")] public string ProcessingLabel { get; set; } = string.Empty;
    [JsonPropertyName("error_message")] public string? ErrorMessage { get; set; }
    [JsonPropertyName("created_at")] public DateTime CreatedAt { get; set; }
    [JsonPropertyName("updated_at")] public DateTime? UpdatedAt { get; set; }
}

public class CaptionUsageDto
{
    [JsonPropertyName("free_remaining")] public int FreeRemaining { get; set; }
    [JsonPropertyName("oneoff_credits")] public int OneoffCredits { get; set; }
    [JsonPropertyName("oneoff_purchase_count")] public int OneoffPurchaseCount { get; set; }
    [JsonPropertyName("basic_credits")] public int BasicCredits { get; set; }
    [JsonPropertyName("subscription_active")] public bool SubscriptionActive { get; set; }
    [JsonPropertyName("videos_created")] public int VideosCreated { get; set; }
    [JsonPropertyName("unlimited_testing")] public bool UnlimitedTesting { get; set; }
    [JsonPropertyName("plans")] public List<CaptionPlanSummaryDto> Plans { get; set; } = new();
}

public class CaptionPlanSummaryDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("price")] public decimal Price { get; set; }
    [JsonPropertyName("currency")] public string Currency { get; set; } = "INR";
    [JsonPropertyName("kind")] public string Kind { get; set; } = "payment";
    [JsonPropertyName("videos")] public int Videos { get; set; }
    [JsonPropertyName("max_seconds")] public int MaxSeconds { get; set; }
    [JsonPropertyName("max_purchases")] public int? MaxPurchases { get; set; }
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("featured")] public bool Featured { get; set; }
}

public class CaptionCheckoutRequest
{
    [JsonPropertyName("package_id"), Required] public string PackageId { get; set; } = string.Empty;
    [JsonPropertyName("origin_url"), Required] public string OriginUrl { get; set; } = string.Empty;
    /// <summary>Optional: "razorpay" or "stripe". Default: Razorpay for INR plans (India), Stripe for other currencies.</summary>
    [JsonPropertyName("provider")] public string? Provider { get; set; }
}

public class CaptionCheckoutResponse
{
    [JsonPropertyName("provider")] public string Provider { get; set; } = "stripe";
    /// <summary>Stripe: hosted checkout page to redirect the browser to.</summary>
    [JsonPropertyName("checkout_url")] public string? CheckoutUrl { get; set; }
    /// <summary>Stripe session id / Razorpay order id / Razorpay subscription id.</summary>
    [JsonPropertyName("session_id")] public string SessionId { get; set; } = string.Empty;
    /// <summary>Razorpay: everything Checkout.js needs (the browser opens the payment modal itself).</summary>
    [JsonPropertyName("razorpay")] public RazorpayCheckoutDto? Razorpay { get; set; }
}

public class RazorpayCheckoutDto
{
    [JsonPropertyName("key_id")] public string KeyId { get; set; } = string.Empty;
    [JsonPropertyName("order_id")] public string? OrderId { get; set; }
    [JsonPropertyName("subscription_id")] public string? SubscriptionId { get; set; }
    /// <summary>Amount in the currency's smallest unit (paise).</summary>
    [JsonPropertyName("amount")] public long Amount { get; set; }
    [JsonPropertyName("currency")] public string Currency { get; set; } = "INR";
    [JsonPropertyName("name")] public string Name { get; set; } = "Captain Title";
    [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;
    [JsonPropertyName("prefill_email")] public string? PrefillEmail { get; set; }
    [JsonPropertyName("prefill_name")] public string? PrefillName { get; set; }
}

public class RazorpayVerifyRequest
{
    [JsonPropertyName("razorpay_order_id")] public string? OrderId { get; set; }
    [JsonPropertyName("razorpay_subscription_id")] public string? SubscriptionId { get; set; }
    [JsonPropertyName("razorpay_payment_id"), Required] public string PaymentId { get; set; } = string.Empty;
    [JsonPropertyName("razorpay_signature"), Required] public string Signature { get; set; } = string.Empty;
}

public class CaptionPaymentProvidersDto
{
    [JsonPropertyName("stripe")] public bool Stripe { get; set; }
    [JsonPropertyName("razorpay")] public bool Razorpay { get; set; }
    [JsonPropertyName("razorpay_key_id")] public string? RazorpayKeyId { get; set; }
}

/// <summary>Everything the editor needs that used to be hardcoded in the React bundle.</summary>
public class CaptionCatalogDto
{
    [JsonPropertyName("themes")] public List<CaptionThemeDto> Themes { get; set; } = new();
    [JsonPropertyName("fonts")] public List<CaptionFontDto> Fonts { get; set; } = new();
    [JsonPropertyName("variants")] public List<CaptionVariantDto> Variants { get; set; } = new();
    [JsonPropertyName("sfx_types")] public List<CaptionSfxTypeDto> SfxTypes { get; set; } = new();
    [JsonPropertyName("stickers")] public List<string> Stickers { get; set; } = new();
    [JsonPropertyName("filler_words")] public List<string> FillerWords { get; set; } = new();
    [JsonPropertyName("limits")] public CaptionLimitsDto Limits { get; set; } = new();
    [JsonPropertyName("default_theme")] public string DefaultTheme { get; set; } = "street_glow";
}

public class CaptionTierDto
{
    /// <summary>Family name used by the ASS export (must match a bundled font file).</summary>
    [JsonPropertyName("font")] public string Font { get; set; } = string.Empty;
    /// <summary>CSS font-family stack used by the editor preview.</summary>
    [JsonPropertyName("css_font")] public string CssFont { get; set; } = string.Empty;
    [JsonPropertyName("color")] public string Color { get; set; } = "#FFFFFF";
    [JsonPropertyName("size")] public int Size { get; set; } = 52;
}

public class CaptionThemeDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("base_color")] public string? BaseColor { get; set; }
    [JsonPropertyName("color")] public string Color { get; set; } = "#FFFFFF";
    [JsonPropertyName("accent_font")] public string? AccentFont { get; set; }
    [JsonPropertyName("tiers")] public Dictionary<string, CaptionTierDto>? Tiers { get; set; }
    [JsonPropertyName("card")] public CaptionCardDto Card { get; set; } = new();
}

public class CaptionCardDto
{
    [JsonPropertyName("stroke")] public string? Stroke { get; set; }
    [JsonPropertyName("shadow")] public string? Shadow { get; set; }
    [JsonPropertyName("glow")] public string? Glow { get; set; }
    [JsonPropertyName("highlight")] public bool Highlight { get; set; }
}

public class CaptionFontDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("family")] public string Family { get; set; } = string.Empty;
}

public class CaptionVariantDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("label")] public string Label { get; set; } = string.Empty;
    [JsonPropertyName("font_family")] public string FontFamily { get; set; } = string.Empty;
    [JsonPropertyName("text_shadow")] public string? TextShadow { get; set; }
    [JsonPropertyName("stroke")] public string? Stroke { get; set; }
    [JsonPropertyName("accent")] public string Accent { get; set; } = string.Empty;
}

public class CaptionSfxTypeDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("label")] public string Label { get; set; } = string.Empty;
    [JsonPropertyName("icon")] public string Icon { get; set; } = string.Empty;
    [JsonPropertyName("color")] public string Color { get; set; } = string.Empty;
    [JsonPropertyName("kind")] public string Kind { get; set; } = string.Empty;
    [JsonPropertyName("freq")] public double Freq { get; set; }
    [JsonPropertyName("sweep_to")] public double SweepTo { get; set; }
    [JsonPropertyName("duration")] public double Duration { get; set; }
}

public class CaptionLimitsDto
{
    [JsonPropertyName("absolute_max_seconds")] public int AbsoluteMaxSeconds { get; set; } = 120;
    [JsonPropertyName("max_upload_mb")] public int MaxUploadMb { get; set; } = 120;
    [JsonPropertyName("free_videos")] public int FreeVideos { get; set; } = 1;
    [JsonPropertyName("free_max_seconds")] public int FreeMaxSeconds { get; set; } = 45;
    [JsonPropertyName("unlimited_testing")] public bool UnlimitedTesting { get; set; }
    [JsonPropertyName("allowed_extensions")] public List<string> AllowedExtensions { get; set; } = new() { ".mp4", ".mov" };
}
