using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackendBasic.Core.Entities;

/// <summary>
/// One uploaded video and its caption session (old Mongo `projects` document).
/// Named CaptionProject because <see cref="Project"/> already exists for the legacy SEO module.
/// Words / elements / SFX markers are stored as jsonb exactly like the old embedded arrays.
/// </summary>
public class CaptionProject
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public virtual User? User { get; set; }

    [Required, MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    public double Duration { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public long SizeBytes { get; set; }

    [Required, MaxLength(50)]
    public string Theme { get; set; } = "street_glow";

    [Required, MaxLength(50)]
    public string FontId { get; set; } = "poppins";

    public int? WordsPerCaption { get; set; }
    public int CaptionLines { get; set; } = 1;

    [Required, MaxLength(10)]
    public string CaptionPosition { get; set; } = "bottom";

    public int AudioVolume { get; set; } = 100;
    public bool AudioMuted { get; set; }
    public int Brightness { get; set; }
    public int Contrast { get; set; } = 100;
    public int Saturation { get; set; } = 100;

    [Required, MaxLength(10)]
    public string RenderQuality { get; set; } = "high";

    /// <summary>Legacy style-variant id. The old app removed variants (replaced by the granular styling below); kept for old rows, ignored by the export.</summary>
    [MaxLength(50)]
    public string? Variant { get; set; }

    /// <summary>roman = word.text (Hinglish), native = word.original (Devanagari from the ASR).</summary>
    [Required, MaxLength(10)]
    public string ScriptMode { get; set; } = "roman";

    // Project-wide typography (old ProjectEdit outline/shadow/glow/background/bold/italic fields).
    public bool OutlineEnabled { get; set; }
    public double OutlineWidth { get; set; } = 2;
    [Required, MaxLength(9)] public string OutlineColor { get; set; } = "#000000";
    public bool ShadowEnabled { get; set; } = true;
    public double ShadowAngle { get; set; }
    public double ShadowDistance { get; set; } = 2;
    public double ShadowBlur { get; set; } = 2;
    public double ShadowOpacity { get; set; } = 0.5;
    [Required, MaxLength(9)] public string ShadowColor { get; set; } = "#000000";
    public bool GlowEnabled { get; set; }
    /// <summary>Null = use each word's own colour.</summary>
    [MaxLength(9)] public string? GlowColor { get; set; }
    public double GlowBlur { get; set; } = 8;
    public double GlowOpacity { get; set; } = 0.6;
    public bool BackgroundEnabled { get; set; }
    [Required, MaxLength(9)] public string BackgroundColor { get; set; } = "#000000";
    public double BackgroundOpacity { get; set; } = 0.6;
    public bool Bold { get; set; } = true;
    public bool Italic { get; set; }

    [Column(TypeName = "jsonb")]
    public string WordsJson { get; set; } = "[]";

    [Column(TypeName = "jsonb")]
    public string ElementsJson { get; set; } = "[]";

    [Column(TypeName = "jsonb")]
    public string SfxMarkersJson { get; set; } = "[]";

    public string SourceTranscript { get; set; } = string.Empty;

    [Required, MaxLength(10)]
    public string SpeechLanguage { get; set; } = "auto";

    [MaxLength(60)]
    public string? TranscriptionMode { get; set; }

    [Column(TypeName = "jsonb")]
    public string? TranscriptionQualityJson { get; set; }

    [Required, MaxLength(500)]
    public string SourcePath { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? PreviewPath { get; set; }

    [MaxLength(100)]
    public string? PreviewContentType { get; set; }

    [MaxLength(100)]
    public string? ContentType { get; set; }

    /// <summary>testing | free | oneoff | basic - which credit bucket this run will consume.</summary>
    [MaxLength(20)]
    public string? CreditType { get; set; }

    [MaxLength(500)]
    public string? RenderPath { get; set; }

    /// <summary>queued | processing | ready | failed | rendering | rendered</summary>
    [Required, MaxLength(20)]
    public string Status { get; set; } = CaptionProjectStatus.Queued;

    public int Progress { get; set; } = 5;

    [MaxLength(100)]
    public string ProcessingLabel { get; set; } = "Upload complete";

    [MaxLength(500)]
    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public static class CaptionProjectStatus
{
    public const string Queued = "queued";
    public const string Processing = "processing";
    public const string Ready = "ready";
    public const string Failed = "failed";
    public const string Rendering = "rendering";
    public const string Rendered = "rendered";
}
