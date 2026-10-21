using AppBackendBasic.Core.Entities;

namespace AppBackendBasic.Core.Interfaces;

/// <summary>
/// Service for managing privacy policies and terms of service
/// </summary>
public interface IPolicyService
{
    /// <summary>
    /// Get the current active policy by type
    /// </summary>
    Task<PolicyDocument?> GetCurrentPolicyAsync(string type);
    
    /// <summary>
    /// Get a specific policy version
    /// </summary>
    Task<PolicyDocument?> GetPolicyByVersionAsync(string type, string version);
    
    /// <summary>
    /// Record user acceptance of a policy
    /// </summary>
    Task<bool> AcceptPolicyAsync(int userId, int policyId, string ipAddress, string? userAgent = null, string? deviceId = null);
    
    /// <summary>
    /// Check if user has accepted the current policy
    /// </summary>
    Task<bool> HasAcceptedCurrentPolicyAsync(int userId, string type);
    
    /// <summary>
    /// Get user's policy acceptance history
    /// </summary>
    Task<List<UserPolicyAcceptance>> GetUserAcceptanceHistoryAsync(int userId);
    
    /// <summary>
    /// Check if user needs to accept updated policies
    /// </summary>
    Task<PolicyUpdateStatus> CheckPolicyUpdatesAsync(int userId);
}

public class PolicyUpdateStatus
{
    public bool RequiresAcceptance { get; set; }
    public List<PolicyDocument> PendingPolicies { get; set; } = new();
}
