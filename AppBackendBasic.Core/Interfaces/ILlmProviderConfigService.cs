using AppBackendBasic.Core.Entities;

namespace AppBackendBasic.Core.Interfaces
{
    public interface ILlmProviderConfigService
    {
        /// <summary>
        /// Get configuration value for a provider by config key
        /// </summary>
        Task<string?> GetConfigValueAsync(string providerName, string configKey);
        
        /// <summary>
        /// Get configuration value for a provider by ID and config key
        /// </summary>
        Task<string?> GetConfigValueAsync(int providerId, string configKey);
        
        /// <summary>
        /// Get all configurations for a provider
        /// </summary>
        Task<Dictionary<string, string>> GetAllConfigsAsync(string providerName);
        
        /// <summary>
        /// Get all configurations for a provider by ID
        /// </summary>
        Task<Dictionary<string, string>> GetAllConfigsAsync(int providerId);
        
        /// <summary>
        /// Set configuration value (encrypts if needed)
        /// </summary>
        Task SetConfigValueAsync(string providerName, string configKey, string configValue, bool isEncrypted = false);
        
        /// <summary>
        /// Set configuration value by provider ID (encrypts if needed)
        /// </summary>
        Task SetConfigValueAsync(int providerId, string configKey, string configValue, bool isEncrypted = false);
        
        /// <summary>
        /// Get provider by name
        /// </summary>
        Task<LlmProvider?> GetProviderByNameAsync(string providerName);
        
        /// <summary>
        /// Get provider by ID
        /// </summary>
        Task<LlmProvider?> GetProviderByIdAsync(int providerId);
        
        /// <summary>
        /// Get all active providers
        /// </summary>
        Task<List<LlmProvider>> GetActiveProvidersAsync();
        
        /// <summary>
        /// Test if provider is configured (has API key)
        /// </summary>
        Task<bool> IsProviderConfiguredAsync(string providerName);
    }
}
