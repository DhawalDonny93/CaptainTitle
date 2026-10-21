namespace AppBackendBasic.Core.DTOs;

/// <summary>
/// Request to delete user account
/// </summary>
public class RequestDeletionDto
{
    public string Password { get; set; } = string.Empty;
    public string? Reason { get; set; }
}

/// <summary>
/// Request to recover deleted account
/// </summary>
public class RecoverAccountRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Request to update user preferences
/// </summary>
public class UpdateUserPreferencesRequest
{
    public string? Language { get; set; }
    public string? Theme { get; set; }
    public string? Timezone { get; set; }
    public string? CustomPreferences { get; set; }
}

/// <summary>
/// Request to update privacy settings
/// </summary>
public class UpdatePrivacySettingsRequest
{
    public string? ProfileVisibility { get; set; }
    public bool? ShowEmail { get; set; }
    public bool? ShowPhone { get; set; }
    public bool? AllowDataCollection { get; set; }
}
