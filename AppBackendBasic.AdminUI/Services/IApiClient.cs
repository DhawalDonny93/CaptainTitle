namespace AppBackendBasic.AdminUI.Services;

/// <summary>
/// Interface for API client operations
/// Follows Interface Segregation Principle (ISP) from SOLID
/// </summary>
public interface IApiClient
{
    /// <summary>
    /// Executes a GET request to the specified endpoint
    /// </summary>
    /// <typeparam name="T">The type to deserialize the response to</typeparam>
    /// <param name="endpoint">The API endpoint (will be prefixed with /api if not present)</param>
    /// <returns>The deserialized response or null</returns>
    Task<T?> GetAsync<T>(string endpoint);

    /// <summary>
    /// Executes a POST request to the specified endpoint with data
    /// </summary>
    /// <typeparam name="TRequest">The type of the request data</typeparam>
    /// <typeparam name="TResponse">The type to deserialize the response to</typeparam>
    /// <param name="endpoint">The API endpoint (will be prefixed with /api if not present)</param>
    /// <param name="data">The data to send in the request body</param>
    /// <returns>The deserialized response or null</returns>
    Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data);

    /// <summary>
    /// Executes a PUT request to the specified endpoint with data
    /// </summary>
    /// <typeparam name="TRequest">The type of the request data</typeparam>
    /// <typeparam name="TResponse">The type to deserialize the response to</typeparam>
    /// <param name="endpoint">The API endpoint (will be prefixed with /api if not present)</param>
    /// <param name="data">The data to send in the request body</param>
    /// <returns>The deserialized response or null</returns>
    Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest data);

    /// <summary>
    /// Executes a DELETE request to the specified endpoint
    /// </summary>
    /// <param name="endpoint">The API endpoint (will be prefixed with /api if not present)</param>
    /// <returns>True if the request was successful, false otherwise</returns>
    Task<bool> DeleteAsync(string endpoint);
}
