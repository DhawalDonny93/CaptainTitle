using System.Text.Json;
using System.Text.Json.Serialization;
using AppBackendBasic.Core.DTOs;

namespace AppBackendBasic.Services.Captions;

/// <summary>Serializer settings for the jsonb documents stored on CaptionProject (nulls kept, snake_case via attributes).</summary>
public static class CaptionJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

    public static List<T> ParseList<T>(string? json) =>
        string.IsNullOrWhiteSpace(json) ? new List<T>() : JsonSerializer.Deserialize<List<T>>(json, Options) ?? new List<T>();
}

/// <summary>Maps an HTTP-meaningful failure out of the service layer (controllers translate it 1:1).</summary>
public sealed class CaptionServiceException : Exception
{
    public int StatusCode { get; }
    public CaptionServiceException(int statusCode, string message) : base(message) { StatusCode = statusCode; }
}
