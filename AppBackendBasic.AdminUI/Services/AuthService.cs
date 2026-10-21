namespace AppBackendBasic.AdminUI.Services;

public class AuthService
{
    private readonly ApiClient _apiClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ApiClient apiClient,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuthService> logger)
    {
        _apiClient = apiClient;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<LoginResult> LoginAsync(string email, string password)
    {
        try
        {
            var result = await _apiClient.PostAsync<LoginRequest,LoginResponse>(
                "/auth/login",
                new LoginRequest { Email = email, Password = password });

            if (result?.Success == true && result.AccessToken != null)
            {
                // Store tokens and user info in session
                _httpContextAccessor.HttpContext?.Session.SetString("JwtToken", result.AccessToken);
                _httpContextAccessor.HttpContext?.Session.SetString("RefreshToken", result.RefreshToken ?? string.Empty);
                _httpContextAccessor.HttpContext?.Session.SetString("UserEmail", email);
                _httpContextAccessor.HttpContext?.Session.SetString("TokenExpiry", DateTime.UtcNow.AddMinutes(55).ToString("o")); // 5 min buffer
                
                _logger.LogInformation("User {Email} logged in successfully", email);
                
                return new LoginResult { Success = true };
            }

            _logger.LogWarning("Login failed for user {Email}", email);
            return new LoginResult 
            { 
                Success = false, 
                ErrorMessage = result?.Message ?? "Login failed" 
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user {Email}", email);
            return new LoginResult 
            { 
                Success = false, 
                ErrorMessage = "An error occurred during login" 
            };
        }
    }

    public Task LogoutAsync()
    {
        _httpContextAccessor.HttpContext?.Session.Clear();
        _logger.LogInformation("User logged out");
        return Task.CompletedTask;
    }

    public bool IsAuthenticated()
    {
        var token = _httpContextAccessor.HttpContext?.Session.GetString("JwtToken");
        return !string.IsNullOrEmpty(token);
    }

    public string? GetCurrentUserEmail()
    {
        return _httpContextAccessor.HttpContext?.Session.GetString("UserEmail");
    }

    public async Task<bool> RefreshTokenAsync()
    {
        try
        {
            var refreshToken = _httpContextAccessor.HttpContext?.Session.GetString("RefreshToken");
            if (string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogWarning("No refresh token found in session");
                return false;
            }

            var result = await _apiClient.PostAsync<RefreshTokenRequest, LoginResponse>(
                "/auth/refresh-token",
                new RefreshTokenRequest { RefreshToken = refreshToken });

            if (result?.Success == true && result.AccessToken != null)
            {
                // Update tokens in session
                _httpContextAccessor.HttpContext?.Session.SetString("JwtToken", result.AccessToken);
                _httpContextAccessor.HttpContext?.Session.SetString("RefreshToken", result.RefreshToken ?? refreshToken);
                _httpContextAccessor.HttpContext?.Session.SetString("TokenExpiry", DateTime.UtcNow.AddMinutes(55).ToString("o"));
                
                _logger.LogInformation("Token refreshed successfully");
                return true;
            }

            _logger.LogWarning("Token refresh failed");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return false;
        }
    }

    public bool IsTokenExpiringSoon()
    {
        var expiryString = _httpContextAccessor.HttpContext?.Session.GetString("TokenExpiry");
        if (string.IsNullOrEmpty(expiryString))
            return true;

        if (DateTime.TryParse(expiryString, out var expiry))
        {
            // Check if token expires in next 5 minutes
            return expiry <= DateTime.UtcNow.AddMinutes(5);
        }

        return true;
    }
}

// Request/Response Models
public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public bool Success { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public string? Message { get; set; }
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class LoginResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
