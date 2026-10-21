namespace AppBackendBasic.Core.DTOs;

public class AuthResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public UserDto User { get; set; } = null!;
    public int ExpiresIn { get; set; }
    public bool RequiresEmailVerification { get; set; }
    public string? EmailVerificationSessionId { get; set; }
    public bool RequiresSubscription { get; set; }
    
    // Account Lockout
    public bool IsLockedOut { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public string? Message { get; set; }
}

public class UserDto
{
    public int Id { get; set; }
    public string? Email { get; set; }
    public string? UserName { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public string Provider { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
    public bool PhoneVerified { get; set; }
    public bool IsActive { get; set; }
    public bool IsSubscribed { get; set; }
    public bool OnboardingCompleted { get; set; }
    public bool IsFirstLogin { get; set; }
    public DateTime CreatedAt { get; set; }
    public UserProfilePreferencesDto? Profile { get; set; }
}

public class UserProfilePreferencesDto
{
    public string Language { get; set; } = "en";
    public string Theme { get; set; } = "light";
    public string Timezone { get; set; } = "UTC";
    public string ExtendedProperties { get; set; } = "{}";
}
