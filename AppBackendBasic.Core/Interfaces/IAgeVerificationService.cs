namespace AppBackendBasic.Core.Interfaces;

/// <summary>
/// Service for age verification and COPPA compliance
/// </summary>
public interface IAgeVerificationService
{
    /// <summary>
    /// Verify user's age based on date of birth
    /// </summary>
    Task<AgeVerificationResult> VerifyAgeAsync(int userId, DateTime dateOfBirth);
    
    /// <summary>
    /// Request parental consent for users under 18
    /// </summary>
    Task<ParentalConsentResult> RequestParentalConsentAsync(int userId, string parentEmail, string? parentName = null);
    
    /// <summary>
    /// Grant parental consent using token
    /// </summary>
    Task<bool> GrantParentalConsentAsync(string consentToken, string ipAddress);
    
    /// <summary>
    /// Check if user meets age requirements
    /// </summary>
    Task<bool> MeetsAgeRequirementsAsync(int userId);
    
    /// <summary>
    /// Calculate age from date of birth
    /// </summary>
    int CalculateAge(DateTime dateOfBirth);
}

public class AgeVerificationResult
{
    public bool IsVerified { get; set; }
    public int Age { get; set; }
    public bool RequiresParentalConsent { get; set; }
    public bool IsBlocked { get; set; }
    public string? Message { get; set; }
}

public class ParentalConsentResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ConsentToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
