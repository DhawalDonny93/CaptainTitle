using AppBackendBasic.Core.Interfaces;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AppBackendBasic.Infrastructure.Converters;

/// <summary>
/// EF Core value converter for automatic encryption/decryption of string properties
/// </summary>
public class EncryptedStringConverter : ValueConverter<string?, string?>
{
    public EncryptedStringConverter(IEncryptionService encryptionService)
        : base(
            v => encryptionService.Encrypt(v ?? string.Empty),
            v => encryptionService.Decrypt(v ?? string.Empty))
    {
    }
}
