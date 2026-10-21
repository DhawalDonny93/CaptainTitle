using AppBackendBasic.Core.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Services.Security;

public class EncryptionService : IEncryptionService
{
    private readonly IDataProtector _protector;
    private readonly ILogger<EncryptionService> _logger;
    private const string EncryptionPrefix = "ENC:";

    public EncryptionService(IDataProtectionProvider dataProtectionProvider, ILogger<EncryptionService> logger)
    {
        _protector = dataProtectionProvider.CreateProtector("SensitiveDataProtection");
        _logger = logger;
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        try
        {
            // Check if already encrypted
            if (IsEncrypted(plainText))
                return plainText;

            var encrypted = _protector.Protect(plainText);
            return EncryptionPrefix + encrypted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encrypting data");
            throw;
        }
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return cipherText;

        try
        {
            // Check if encrypted
            if (!IsEncrypted(cipherText))
                return cipherText;

            var encryptedValue = cipherText.Substring(EncryptionPrefix.Length);
            return _protector.Unprotect(encryptedValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrypting data");
            throw;
        }
    }

    public bool IsEncrypted(string value)
    {
        return !string.IsNullOrEmpty(value) && value.StartsWith(EncryptionPrefix);
    }
}
