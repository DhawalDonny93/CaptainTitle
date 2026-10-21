namespace AppBackendBasic.Core.Interfaces;

/// <summary>
/// Service for exporting user data (GDPR - Right to Data Portability)
/// </summary>
public interface IDataExportService
{
    /// <summary>
    /// Request data export for a user
    /// </summary>
    Task<DataExportResult> RequestDataExportAsync(int userId);
    
    /// <summary>
    /// Get export status
    /// </summary>
    Task<DataExportStatus?> GetExportStatusAsync(int userId, string exportId);
    
    /// <summary>
    /// Download exported data
    /// </summary>
    Task<byte[]?> DownloadExportAsync(int userId, string exportId);
    
    /// <summary>
    /// Clean up expired exports (run by background job)
    /// </summary>
    Task CleanupExpiredExportsAsync();
}

public class DataExportResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ExportId { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class DataExportStatus
{
    public string ExportId { get; set; } = string.Empty;
    public bool IsReady { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public long? FileSizeBytes { get; set; }
}
