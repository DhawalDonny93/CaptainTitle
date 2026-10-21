using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Services.Notifications;

/// <summary>
/// Service for generating and validating unsubscribe tokens
/// </summary>
public class UnsubscribeTokenService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<UnsubscribeTokenService> _logger;
    private readonly string _secretKey;

    public UnsubscribeTokenService(
        IConfiguration configuration,
        ILogger<UnsubscribeTokenService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _secretKey = _configuration["Encryption:Key"] ?? throw new ArgumentNullException("Encryption:Key");
    }

    /// <summary>
    /// Generate an unsubscribe token for a user
    /// </summary>
    public string GenerateUnsubscribeToken(int userId, string email)
    {
        try
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var data = $"{userId}|{email}|{timestamp}";
            
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            var signature = Convert.ToBase64String(hash);
            
            // Combine data and signature
            var token = $"{Convert.ToBase64String(Encoding.UTF8.GetBytes(data))}:{signature}";
            
            return Uri.EscapeDataString(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating unsubscribe token for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Validate and parse an unsubscribe token
    /// </summary>
    public (bool IsValid, int UserId, string Email) ValidateUnsubscribeToken(string token)
    {
        try
        {
            token = Uri.UnescapeDataString(token);
            var parts = token.Split(':');
            
            if (parts.Length != 2)
            {
                _logger.LogWarning("Invalid token format");
                return (false, 0, string.Empty);
            }

            var dataBase64 = parts[0];
            var signatureBase64 = parts[1];
            
            var data = Encoding.UTF8.GetString(Convert.FromBase64String(dataBase64));
            var dataParts = data.Split('|');
            
            if (dataParts.Length != 3)
            {
                _logger.LogWarning("Invalid data format in token");
                return (false, 0, string.Empty);
            }

            var userId = int.Parse(dataParts[0]);
            var email = dataParts[1];
            var timestamp = long.Parse(dataParts[2]);
            
            // Verify signature
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            var expectedSignature = Convert.ToBase64String(hash);
            
            if (signatureBase64 != expectedSignature)
            {
                _logger.LogWarning("Invalid signature for token");
                return (false, 0, string.Empty);
            }

            // Check if token is expired (30 days)
            var tokenDate = DateTimeOffset.FromUnixTimeSeconds(timestamp);
            if (DateTimeOffset.UtcNow - tokenDate > TimeSpan.FromDays(30))
            {
                _logger.LogWarning("Expired token for user {UserId}", userId);
                return (false, 0, string.Empty);
            }

            return (true, userId, email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating unsubscribe token");
            return (false, 0, string.Empty);
        }
    }
}
