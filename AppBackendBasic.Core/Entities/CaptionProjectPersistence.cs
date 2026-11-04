using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackendBasic.Core.Entities;

/// <summary>
/// Where the user left off in the studio (playhead, open tabs), one row per project. Its own table because it changes far more
/// often than the content: writing it never rewrites the CaptionProjects row or bumps the revision.
/// </summary>
public class CaptionProjectEditorState
{
    public int ProjectId { get; set; }

    /// <summary>A serialized CaptionEditorStateDto.</summary>
    [Column(TypeName = "jsonb")]
    public string StateJson { get; set; } = "{}";

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>A restore point: the whole editable document as it was before autosave, re-listen, AI emphasis or a restore replaced it.</summary>
public class CaptionProjectVersion
{
    public int Id { get; set; }
    public int ProjectId { get; set; }

    /// <summary>CaptionProject.Revision when the document was captured.</summary>
    public int ProjectRevision { get; set; }

    /// <summary>One of <see cref="CaptionVersionReason"/>.</summary>
    [Required, MaxLength(30)]
    public string Reason { get; set; } = CaptionVersionReason.Autosave;

    /// <summary>A serialized CaptionProjectEditDto.</summary>
    [Column(TypeName = "jsonb")]
    public string DocumentJson { get; set; } = "{}";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public static class CaptionVersionReason
{
    public const string Autosave = "autosave";
    public const string BeforeRelisten = "before_relisten";
    public const string BeforeAiEmphasis = "before_ai_emphasis";
    public const string BeforeRestore = "before_restore";
}
