namespace AppBackendBasic.AdminUI.Pages.Captions;

public enum FieldType { Text, Number, Bool, Color, Json, Select }

public record CatalogField(string Name, string Label, FieldType Type = FieldType.Text, bool Required = false,
    string? Help = null, string[]? Options = null, bool InTable = true);

public record CatalogKind(string Slug, string Title, string Description, string ApiPath, CatalogField[] Fields);

/// <summary>
/// Describes every admin-managed caption catalog so one generic page (Catalog.cshtml) can list/edit/delete them.
/// Property names match the camelCase JSON of the entities in AppBackendBasic.Core/Entities/CaptionCatalog.cs.
/// </summary>
public static class CaptionCatalogSchema
{
    private static readonly CatalogField Active = new("isActive", "Active", FieldType.Bool);
    private static readonly CatalogField Sort = new("sortOrder", "Order", FieldType.Number);

    public static readonly CatalogKind[] Kinds =
    {
        new("themes", "Caption templates", "Colours, accent font and tiers for each template. Tiered templates define support/feature/key in Tiers JSON.", "/admin/captions/themes", new[]
        {
            new CatalogField("key", "Key", Required: true, Help: "Stable id stored on projects (a-z, 0-9, _). Do not rename once in use."),
            new CatalogField("name", "Name", Required: true),
            new CatalogField("baseColor", "Base colour", FieldType.Color, Help: "Flat templates only"),
            new CatalogField("accentColor", "Accent colour", FieldType.Color, true),
            new CatalogField("accentFont", "Accent font", Help: "Font family name bundled in Assets/captions/fonts (flat templates)"),
            new CatalogField("tiersJson", "Tiers JSON", FieldType.Json, Help: "{\"support\":{\"font\":\"Poppins\",\"css_font\":\"'Poppins', sans-serif\",\"color\":\"#FFFFFF\",\"size\":40},\"feature\":{...},\"key\":{...}}", InTable: false),
            new CatalogField("cardJson", "Card preview JSON", FieldType.Json, Help: "{\"stroke\":null,\"shadow\":\"0 2px 6px rgba(0,0,0,.6)\",\"glow\":null,\"highlight\":false}", InTable: false),
            Sort, Active,
        }),
        new("fonts", "Caption fonts", "Selectable primary fonts. Family must match a font file in Assets/captions/fonts (or a system font on the render server).", "/admin/captions/fonts", new[]
        {
            new CatalogField("key", "Key", Required: true), new CatalogField("label", "Label", Required: true),
            new CatalogField("family", "Font family", Required: true), Sort, Active,
        }),
        new("variants", "Style variants", "Outline/shadow/blur overlays (Rock, Glow, Swiss, Clean ...).", "/admin/captions/variants", new[]
        {
            new CatalogField("key", "Key", Required: true), new CatalogField("label", "Label", Required: true),
            new CatalogField("fontFamily", "Font family", Required: true),
            new CatalogField("border", "Outline width", FieldType.Number), new CatalogField("outlineColor", "Outline colour", FieldType.Color, Help: "Empty = the word's own colour"),
            new CatalogField("shadow", "Shadow", FieldType.Number), new CatalogField("blur", "Blur", FieldType.Number),
            new CatalogField("accentColor", "Emphasis accent", FieldType.Color, true),
            new CatalogField("cssTextShadow", "Preview text-shadow (CSS)", InTable: false), new CatalogField("cssStroke", "Preview stroke (CSS)", InTable: false),
            Sort, Active,
        }),
        new("sfx", "Sound effects", "Synthesized placeholder tones used by the editor preview AND the export (single source of truth).", "/admin/captions/sfx", new[]
        {
            new CatalogField("key", "Key", Required: true), new CatalogField("label", "Label", Required: true),
            new CatalogField("icon", "Icon", FieldType.Select, Options: new[] { "wind", "speaker", "balloon", "money", "camera", "volume" }),
            new CatalogField("color", "Colour", FieldType.Color), new CatalogField("previewKind", "Preview shape", FieldType.Select, Options: new[] { "sweep", "drop", "blip", "chime", "click" }),
            new CatalogField("frequency", "Start Hz", FieldType.Number, true), new CatalogField("sweepTo", "End Hz", FieldType.Number, true),
            new CatalogField("durationSeconds", "Duration (s)", FieldType.Number, true), Sort, Active,
        }),
        new("stickers", "Stickers", "Glyphs offered in the Elements panel. Must be renderable by the bundled/system font fallback.", "/admin/captions/stickers", new[]
        {
            new CatalogField("glyph", "Glyph", Required: true), Sort, Active,
        }),
        new("filler-words", "Filler words", "Words the AI-tools panel offers to remove.", "/admin/captions/filler-words", new[]
        {
            new CatalogField("word", "Word", Required: true), Active,
        }),
        new("plans", "Plans & prices", "Passes and subscriptions shown in the paywall. INR plans are paid with Razorpay (one-off passes need no extra setup; subscriptions need a Razorpay plan id); other currencies use Stripe (the Stripe price must carry the lookup key).", "/admin/captions/plans", new[]
        {
            new CatalogField("key", "Key", Required: true, Help: "e.g. single_45, basic_monthly"), new CatalogField("name", "Name", Required: true),
            new CatalogField("kind", "Kind", FieldType.Select, true, "payment = one-off credits, subscription = monthly credits", new[] { "payment", "subscription" }),
            new CatalogField("price", "Price", FieldType.Number, true), new CatalogField("currency", "Currency", Required: true),
            new CatalogField("videos", "Videos", FieldType.Number, true), new CatalogField("maxSeconds", "Max seconds / video", FieldType.Number, true),
            new CatalogField("maxPurchasesPerUser", "Max purchases / user", FieldType.Number, Help: "Empty = unlimited"),
            new CatalogField("stripeLookupKey", "Stripe lookup key", Help: "Used for non-INR plans (Stripe)"),
            new CatalogField("razorpayPlanId", "Razorpay plan id", Help: "plan_xxx from the Razorpay dashboard - only needed for subscription plans paid in INR"),
            new CatalogField("description", "Description", InTable: false),
            new CatalogField("isFeatured", "Featured", FieldType.Bool), Sort, Active,
        }),
    };

    public static CatalogKind? Find(string slug) => Kinds.FirstOrDefault(k => k.Slug == slug);
}
