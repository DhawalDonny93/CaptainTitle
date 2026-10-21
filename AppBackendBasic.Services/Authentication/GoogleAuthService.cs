using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AppBackendBasic.Core.Interfaces;

namespace AppBackendBasic.Services.Authentication;

public class GoogleAuthService : IGoogleAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GoogleAuthService> _logger;
    private readonly string _googleClientId;

    public GoogleAuthService(HttpClient httpClient, ILogger<GoogleAuthService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _googleClientId = configuration["Google:ClientId"] ?? throw new ArgumentNullException("Google:ClientId");
    }

    public async Task<GoogleTokenInfo?> ValidateGoogleTokenAsync(string idToken)
    {
        try
        {
            var response = await _httpClient.GetAsync($"https://oauth2.googleapis.com/tokeninfo?id_token={idToken}");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Google token validation failed with status: {StatusCode}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var tokenInfo = JsonSerializer.Deserialize<JsonElement>(content);

            // Verify the token is for our application
            if (!tokenInfo.TryGetProperty("aud", out var audience) || audience.GetString() != _googleClientId)
            {
                _logger.LogWarning("Google token validation failed: Invalid audience");
                return null;
            }

            // Extract user information
            var googleTokenInfo = new GoogleTokenInfo
            {
                Sub = tokenInfo.TryGetProperty("sub", out var sub) ? sub.GetString() ?? string.Empty : string.Empty,
                Email = tokenInfo.TryGetProperty("email", out var email) ? email.GetString() ?? string.Empty : string.Empty,
                Name = tokenInfo.TryGetProperty("name", out var name) ? name.GetString() ?? string.Empty : string.Empty,
                Picture = tokenInfo.TryGetProperty("picture", out var picture) ? picture.GetString() ?? string.Empty : string.Empty,
                EmailVerified = ParseEmailVerified(tokenInfo)
            };

            _logger.LogInformation("Google token validation successful for user: {Email}", googleTokenInfo.Email);
            return googleTokenInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Google token");
            return null;
        }
    }

    private bool ParseEmailVerified(JsonElement tokenInfo)
    {
        if (!tokenInfo.TryGetProperty("email_verified", out var emailVerified))
        {
            return false;
        }

        // Handle both boolean and string representations
        if (emailVerified.ValueKind == JsonValueKind.True)
        {
            return true;
        }
        else if (emailVerified.ValueKind == JsonValueKind.False)
        {
            return false;
        }
        else if (emailVerified.ValueKind == JsonValueKind.String)
        {
            var stringValue = emailVerified.GetString();
            return stringValue?.ToLower() == "true";
        }

        return false;
    }
}
