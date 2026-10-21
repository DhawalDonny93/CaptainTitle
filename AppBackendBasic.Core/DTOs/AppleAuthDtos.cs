using System.Text.Json;
using System.Text.Json.Serialization;

namespace AppBackendBasic.Core.DTOs;

/// <summary>
/// Decoded Apple identity token payload
/// </summary>
public class AppleIdTokenPayload
{
    [JsonPropertyName("iss")]
    public string Issuer { get; set; } = string.Empty;
    
    [JsonPropertyName("aud")]
    public string Audience { get; set; } = string.Empty;
    
    [JsonPropertyName("exp")]
    public long ExpirationTime { get; set; }
    
    [JsonPropertyName("iat")]
    public long IssuedAt { get; set; }
    
    [JsonPropertyName("sub")]
    public string Subject { get; set; } = string.Empty; // Apple User ID
    
    [JsonPropertyName("email")]
    public string? Email { get; set; }
    
    [JsonPropertyName("email_verified")]
    [JsonConverter(typeof(JsonStringBoolConverter))]
    public bool EmailVerified { get; set; }
    
    [JsonPropertyName("is_private_email")]
    [JsonConverter(typeof(JsonStringBoolConverter))]
    public bool IsPrivateEmail { get; set; }
    
    [JsonPropertyName("nonce_supported")]
    public bool? NonceSupported { get; set; }
}

/// <summary>
/// Custom JSON converter for string boolean values
/// </summary>
public class JsonStringBoolConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            return stringValue?.ToLower() == "true";
        }
        return reader.GetBoolean();
    }

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
    {
        writer.WriteBooleanValue(value);
    }
}

/// <summary>
/// Apple public key from Apple's public key endpoint
/// </summary>
public class ApplePublicKey
{
    [JsonPropertyName("kty")]
    public string KeyType { get; set; } = string.Empty;
    
    [JsonPropertyName("kid")]
    public string KeyId { get; set; } = string.Empty;
    
    [JsonPropertyName("use")]
    public string Use { get; set; } = string.Empty;
    
    [JsonPropertyName("alg")]
    public string Algorithm { get; set; } = string.Empty;
    
    [JsonPropertyName("n")]
    public string Modulus { get; set; } = string.Empty;
    
    [JsonPropertyName("e")]
    public string Exponent { get; set; } = string.Empty;
}

/// <summary>
/// Apple public keys response
/// </summary>
public class ApplePublicKeysResponse
{
    [JsonPropertyName("keys")]
    public List<ApplePublicKey> Keys { get; set; } = new();
}
