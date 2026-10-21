namespace AppBackendBasic.Core.DTOs;

/// <summary>
/// Request to verify age
/// </summary>
public class VerifyAgeRequest
{
    public DateTime DateOfBirth { get; set; }
}

/// <summary>
/// Request for parental consent
/// </summary>
public class ParentalConsentRequest
{
    public string ParentEmail { get; set; } = string.Empty;
    public string? ParentName { get; set; }
    public DateTime ChildDateOfBirth { get; set; }
}

/// <summary>
/// Request to accept a policy
/// </summary>
public class AcceptPolicyRequest
{
    public int PolicyId { get; set; }
    public string? DeviceId { get; set; }
    public string? IpAddress { get; set; }
}

/// <summary>
/// Request to update unsubscribe preferences
/// </summary>
public class UnsubscribePreferencesRequest
{
    public bool? DisableMarketing { get; set; }
    public bool? DisableUpdates { get; set; }
    public bool? DisableReminders { get; set; }
    public bool? DisableAll { get; set; }
    public bool? DisableAllEmails { get; set; }
}
