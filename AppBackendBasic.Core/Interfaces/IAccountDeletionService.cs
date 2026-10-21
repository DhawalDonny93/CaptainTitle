namespace AppBackendBasic.Core.Interfaces;

/// <summary>
/// Service for handling account deletion (GDPR - Right to be Forgotten)
/// </summary>
public interface IAccountDeletionService
{
    /// <summary>
    /// Request account deletion with a grace period
    /// </summary>
    Task<DeletionResult> RequestDeletionAsync(int userId, string password, string? reason = null);
    
    /// <summary>
    /// Cancel a pending deletion request
    /// </summary>
    Task<bool> CancelDeletionAsync(int userId);
    
    /// <summary>
    /// Process all scheduled deletions (run by background job)
    /// </summary>
    Task ProcessScheduledDeletionsAsync();
    
    /// <summary>
    /// Permanently delete user data (after grace period)
    /// </summary>
    Task PermanentlyDeleteUserDataAsync(int userId);
    
    /// <summary>
    /// Check if user has a pending deletion
    /// </summary>
    Task<DeletionStatus?> GetDeletionStatusAsync(int userId);
}

public class DeletionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime? ScheduledDeletionDate { get; set; }
    public int GracePeriodDays { get; set; }
}

public class DeletionStatus
{
    public bool IsPending { get; set; }
    public DateTime? ScheduledDeletionDate { get; set; }
    public int DaysRemaining { get; set; }
    public string? Reason { get; set; }
}
