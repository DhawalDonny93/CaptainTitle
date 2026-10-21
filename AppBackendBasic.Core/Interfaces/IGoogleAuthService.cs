namespace AppBackendBasic.Core.Interfaces;

public class GoogleTokenInfo
{
    public string Sub { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Picture { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
}

public interface IGoogleAuthService
{
    Task<GoogleTokenInfo?> ValidateGoogleTokenAsync(string idToken);
}
