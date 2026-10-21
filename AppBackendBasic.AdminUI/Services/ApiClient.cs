using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace AppBackendBasic.AdminUI.Services;

/// <summary>
/// HTTP API client for making authenticated requests to the backend API
/// Implements Dependency Inversion Principle (DIP) by depending on abstractions (IApiClient)
/// Follows Single Responsibility Principle (SRP) - handles only API communication
/// </summary>
public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiClient> _logger;

    public ApiClient(
        HttpClient httpClient,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        ILogger<ApiClient> logger)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
        _logger = logger;

        var baseUrl = configuration["ApiSettings:BaseUrl"];
        _httpClient.BaseAddress = new Uri(baseUrl!);
        _httpClient.Timeout = TimeSpan.FromSeconds(configuration.GetValue<int>("ApiSettings:Timeout"));
    }

    /// <summary>
    /// Ensures a valid JWT token is available, refreshing if necessary
    /// </summary>
    /// <returns>The valid JWT token or null if unavailable</returns>
    private async Task<string?> EnsureValidTokenAsync()
    {
        var token = _httpContextAccessor.HttpContext?.Session.GetString("JwtToken");
        _logger.LogInformation("EnsureValidTokenAsync: Token from session length: {Length}", token?.Length ?? 0);
        
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("EnsureValidTokenAsync: No token found in session");
            return null;
        }

        // Check if token is expiring soon
        var expiryString = _httpContextAccessor.HttpContext?.Session.GetString("TokenExpiry");
        if (!string.IsNullOrEmpty(expiryString) && DateTime.TryParse(expiryString, out var expiry))
        {
            if (expiry <= DateTime.UtcNow.AddMinutes(5))
            {
                // Token expiring soon, refresh it
                _logger.LogInformation("Token expiring soon, attempting refresh");
                var refreshed = await RefreshTokenAsync();
                if (refreshed)
                {
                    // Get the new token from session
                    token = _httpContextAccessor.HttpContext?.Session.GetString("JwtToken");
                }
                else
                {
                    return null;
                }
            }
        }

        return token;
    }

    /// <summary>
    /// Sets the Authorization header with the provided JWT token
    /// </summary>
    /// <param name="token">The JWT token to set in the header</param>
    private void SetAuthHeader(string token)
    {
        // Clear any existing authorization header first
        _httpClient.DefaultRequestHeaders.Authorization = null;
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", token);
        
        _logger.LogInformation("Auth header set. Token length: {Length}, First 20 chars: {TokenStart}", 
            token?.Length ?? 0, token?.Substring(0, Math.Min(20, token?.Length ?? 0)));
    }

    /// <summary>
    /// Attempts to refresh the JWT token using the refresh token
    /// </summary>
    /// <returns>True if refresh was successful, false otherwise</returns>
    private async Task<bool> RefreshTokenAsync()
    {
        try
        {
            var refreshToken = _httpContextAccessor.HttpContext?.Session.GetString("RefreshToken");
            if (string.IsNullOrEmpty(refreshToken))
                return false;

            var json = JsonConvert.SerializeObject(new { refreshToken });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/api/auth/refresh-token", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<RefreshTokenResponse>(responseContent);
                
                if (result?.AccessToken != null)
                {
                    // Update session with new tokens
                    _httpContextAccessor.HttpContext?.Session.SetString("JwtToken", result.AccessToken);
                    if (!string.IsNullOrEmpty(result.RefreshToken))
                        _httpContextAccessor.HttpContext?.Session.SetString("RefreshToken", result.RefreshToken);
                    _httpContextAccessor.HttpContext?.Session.SetString("TokenExpiry", DateTime.UtcNow.AddMinutes(55).ToString("o"));
                    
                    SetAuthHeader(result.AccessToken);
                    _logger.LogInformation("Token refreshed successfully");
                    return true;
                }
            }
            
            _logger.LogWarning("Token refresh failed");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return false;
        }
    }


    /// <summary>
    /// Generic method to execute HTTP requests with authentication and error handling
    /// </summary>
    private async Task<TResponse?> ExecuteRequestAsync<TResponse>(
        Func<string, Task<HttpResponseMessage>> httpRequest,
        string endpoint,
        string httpMethod,
        bool requiresAuth = true)
    {
        try
        {
            var normalizedEndpoint = NormalizeEndpoint(endpoint);
            
            // Handle authentication if required
            if (requiresAuth)
            {
                await SetAuthenticationHeaderAsync(normalizedEndpoint, httpMethod);
            }
            
            // Execute the request with the normalized endpoint
            var response = await httpRequest(normalizedEndpoint);
            
            return await HandleResponseAsync<TResponse>(response, normalizedEndpoint, httpMethod);
        }
        catch (UnauthorizedAccessException)
        {
            throw; // Re-throw to allow proper handling
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {HttpMethod} request to {Endpoint}", httpMethod, endpoint);
            throw;
        }
    }

    /// <summary>
    /// Normalizes the endpoint by ensuring it starts with /api
    /// </summary>
    private string NormalizeEndpoint(string endpoint)
    {
        return endpoint.StartsWith("/api") ? endpoint : "/api" + endpoint;
    }

    /// <summary>
    /// Sets authentication header if token is available and valid
    /// </summary>
    private async Task SetAuthenticationHeaderAsync(string endpoint, string httpMethod)
    {
        // Skip token check for auth endpoints
        if (endpoint.Contains("/auth/"))
            return;

        var token = await EnsureValidTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("No valid token available for {HttpMethod} request to {Endpoint}", httpMethod, endpoint);
            throw new UnauthorizedAccessException("Authentication required");
        }
        
        SetAuthHeader(token);
    }

    /// <summary>
    /// Handles HTTP response, checks status code, and deserializes content
    /// </summary>
    private async Task<TResponse?> HandleResponseAsync<TResponse>(
        HttpResponseMessage response,
        string endpoint,
        string httpMethod)
    {
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning(
                "{HttpMethod} request to {Endpoint} failed with status {StatusCode}: {Error}",
                httpMethod, endpoint, response.StatusCode, errorContent);
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException("Unauthorized access");
                
            throw new HttpRequestException($"Request failed with status {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<TResponse>(content);
    }

    /// <summary>
    /// Creates JSON content from request data
    /// </summary>
    private StringContent CreateJsonContent<T>(T data)
    {
        var json = JsonConvert.SerializeObject(data);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    public async Task<T?> GetAsync<T>(string endpoint)
    {
        return await ExecuteRequestAsync<T>(
            (normalizedEndpoint) => _httpClient.GetAsync(normalizedEndpoint),
            endpoint,
            "GET");
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data)
    {
        return await ExecuteRequestAsync<TResponse>(
            (normalizedEndpoint) => _httpClient.PostAsync(normalizedEndpoint, CreateJsonContent(data)),
            endpoint,
            "POST");
    }

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest data)
    {
        return await ExecuteRequestAsync<TResponse>(
            (normalizedEndpoint) => _httpClient.PutAsync(normalizedEndpoint, CreateJsonContent(data)),
            endpoint,
            "PUT");
    }

    public async Task<bool> DeleteAsync(string endpoint)
    {
        try
        {
            var normalizedEndpoint = NormalizeEndpoint(endpoint);
            await SetAuthenticationHeaderAsync(normalizedEndpoint, "DELETE");
            
            var response = await _httpClient.DeleteAsync(normalizedEndpoint);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning(
                    "DELETE request to {Endpoint} failed with status {StatusCode}: {Error}",
                    normalizedEndpoint, response.StatusCode, errorContent);
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    throw new UnauthorizedAccessException("Unauthorized access");
            }
            
            return response.IsSuccessStatusCode;
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DELETE request to {Endpoint}", endpoint);
            throw;
        }
    }
}

// Response model for token refresh
public class RefreshTokenResponse
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
}
