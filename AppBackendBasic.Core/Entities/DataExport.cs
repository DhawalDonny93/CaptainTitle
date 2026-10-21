using System.ComponentModel.DataAnnotations;

namespace AppBackendBasic.Core.Entities;

/// <summary>
/// Tracks user data export requests (GDPR)
/// </summary>
public class DataExport
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    
    [MaxLength(100)]
    public string ExportId { get; set; } = string.Empty;
    
    public bool IsReady { get; set; } = false;
    
    [MaxLength(500)]
    public string? FilePath { get; set; }
    
    public long? FileSizeBytes { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime ExpiresAt { get; set; }
    
    public bool IsDownloaded { get; set; } = false;
    
    public DateTime? DownloadedAt { get; set; }
    
    // Navigation property
    public virtual User User { get; set; } = null!;
}
