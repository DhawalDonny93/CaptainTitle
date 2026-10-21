namespace AppBackendBasic.Core.Interfaces;

/// <summary>
/// Service for encrypting and decrypting sensitive data at rest
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypt a plain text string
    /// </summary>
    string Encrypt(string plainText);
    
    /// <summary>
    /// Decrypt an encrypted string
    /// </summary>
    string Decrypt(string cipherText);
    
    /// <summary>
    /// Check if a string is encrypted
    /// </summary>
    bool IsEncrypted(string value);
}
