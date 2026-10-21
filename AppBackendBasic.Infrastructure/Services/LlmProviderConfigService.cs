using System.Security.Cryptography;
using System.Text;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Infrastructure.Services
{
    public class LlmProviderConfigService : ILlmProviderConfigService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LlmProviderConfigService> _logger;
        private readonly string _encryptionKey;

        public LlmProviderConfigService(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<LlmProviderConfigService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            
            // Get encryption key from configuration
            _encryptionKey = _configuration["EncryptionKey"] 
                ?? throw new InvalidOperationException("EncryptionKey not configured");
        }

        public async Task<string?> GetConfigValueAsync(string providerName, string configKey)
        {
            var provider = await GetProviderByNameAsync(providerName);
            if (provider == null)
            {
                _logger.LogWarning("Provider {ProviderName} not found", providerName);
                return null;
            }

            return await GetConfigValueAsync(provider.Id, configKey);
        }

        public async Task<string?> GetConfigValueAsync(int providerId, string configKey)
        {
            var config = await _context.LlmProviderConfigs
                .FirstOrDefaultAsync(c => c.ProviderId == providerId && c.ConfigKey == configKey);

            if (config == null)
            {
                return null;
            }

            // Decrypt if encrypted
            if (config.IsEncrypted && !string.IsNullOrEmpty(config.ConfigValue))
            {
                try
                {
                    return Decrypt(config.ConfigValue);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error decrypting config value for key {ConfigKey}", configKey);
                    return null;
                }
            }

            return config.ConfigValue;
        }

        public async Task<Dictionary<string, string>> GetAllConfigsAsync(string providerName)
        {
            var provider = await GetProviderByNameAsync(providerName);
            if (provider == null)
            {
                _logger.LogWarning("Provider {ProviderName} not found", providerName);
                return new Dictionary<string, string>();
            }

            return await GetAllConfigsAsync(provider.Id);
        }

        public async Task<Dictionary<string, string>> GetAllConfigsAsync(int providerId)
        {
            var configs = await _context.LlmProviderConfigs
                .Where(c => c.ProviderId == providerId)
                .ToListAsync();

            var result = new Dictionary<string, string>();

            foreach (var config in configs)
            {
                if (string.IsNullOrEmpty(config.ConfigValue))
                    continue;

                // Decrypt if encrypted
                var value = config.IsEncrypted 
                    ? Decrypt(config.ConfigValue) 
                    : config.ConfigValue;

                if (!string.IsNullOrEmpty(value))
                {
                    result[config.ConfigKey] = value;
                }
            }

            return result;
        }

        public async Task SetConfigValueAsync(string providerName, string configKey, string configValue, bool isEncrypted = false)
        {
            var provider = await GetProviderByNameAsync(providerName);
            if (provider == null)
            {
                throw new InvalidOperationException($"Provider {providerName} not found");
            }

            await SetConfigValueAsync(provider.Id, configKey, configValue, isEncrypted);
        }

        public async Task SetConfigValueAsync(int providerId, string configKey, string configValue, bool isEncrypted = false)
        {
            var config = await _context.LlmProviderConfigs
                .FirstOrDefaultAsync(c => c.ProviderId == providerId && c.ConfigKey == configKey);

            var valueToStore = isEncrypted ? Encrypt(configValue) : configValue;

            if (config == null)
            {
                config = new LlmProviderConfig
                {
                    ProviderId = providerId,
                    ConfigKey = configKey,
                    ConfigValue = valueToStore,
                    IsEncrypted = isEncrypted,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.LlmProviderConfigs.Add(config);
            }
            else
            {
                config.ConfigValue = valueToStore;
                config.IsEncrypted = isEncrypted;
                config.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<LlmProvider?> GetProviderByNameAsync(string providerName)
        {
            return await _context.LlmProviders
                .Include(p => p.Configs)
                .FirstOrDefaultAsync(p => p.Name.ToLower() == providerName.ToLower());
        }

        public async Task<LlmProvider?> GetProviderByIdAsync(int providerId)
        {
            return await _context.LlmProviders
                .Include(p => p.Configs)
                .FirstOrDefaultAsync(p => p.Id == providerId);
        }

        public async Task<List<LlmProvider>> GetActiveProvidersAsync()
        {
            return await _context.LlmProviders
                .Include(p => p.Configs)
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.Priority)
                .ToListAsync();
        }

        public async Task<bool> IsProviderConfiguredAsync(string providerName)
        {
            var apiKey = await GetConfigValueAsync(providerName, "ApiKey");
            return !string.IsNullOrEmpty(apiKey);
        }

        // Encryption/Decryption helpers

        private string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            using var aes = Aes.Create();
            aes.Key = DeriveKeyFromPassword(_encryptionKey);
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var msEncrypt = new MemoryStream();
            
            // Prepend IV to encrypted data
            msEncrypt.Write(aes.IV, 0, aes.IV.Length);
            
            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            using (var swEncrypt = new StreamWriter(csEncrypt))
            {
                swEncrypt.Write(plainText);
            }

            return Convert.ToBase64String(msEncrypt.ToArray());
        }

        private string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            var fullCipher = Convert.FromBase64String(cipherText);

            using var aes = Aes.Create();
            aes.Key = DeriveKeyFromPassword(_encryptionKey);

            // Extract IV from the beginning
            var iv = new byte[aes.IV.Length];
            var cipher = new byte[fullCipher.Length - iv.Length];

            Array.Copy(fullCipher, iv, iv.Length);
            Array.Copy(fullCipher, iv.Length, cipher, 0, cipher.Length);

            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var msDecrypt = new MemoryStream(cipher);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);

            return srDecrypt.ReadToEnd();
        }

        private byte[] DeriveKeyFromPassword(string password)
        {
            // Use PBKDF2 to derive a 256-bit key from the password
            using var deriveBytes = new Rfc2898DeriveBytes(
                password,
                Encoding.UTF8.GetBytes("AISEO_LLM_SALT_2026"), // Static salt for consistency
                10000,
                HashAlgorithmName.SHA256);

            return deriveBytes.GetBytes(32); // 256 bits
        }
    }
}
