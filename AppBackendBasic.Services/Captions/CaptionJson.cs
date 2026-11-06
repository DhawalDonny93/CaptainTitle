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
    public const string RevisionConflict = "REVISION_CONFLICT";

    public int StatusCode { get; }
    /// <summary>Optional machine-readable code the editor can branch on.</summary>
    public string? Code { get; }
    public CaptionServiceException(int statusCode, string message, string? code = null) : base(message) { StatusCode = statusCode; Code = code; }
}

/// <summary>The video itself can't be captioned (not a server problem); its message is safe to show to the user.</summary>
public sealed class CaptionVideoUnusableException : Exception
{
    public CaptionVideoUnusableException(string message) : base(message) { }
}

/// <summary>What users are told when something fails. The cause (bad API key, provider outage, ffmpeg...) goes to the log, never to the user.</summary>
public static class CaptionErrors
{
    public const string ServerIssue = "Something went wrong on our server, so this could not be completed. Please try again later, and contact the admin if it keeps happening.";

    /// <summary>Only failures known to come from the video blame the video; anything else is a server issue.</summary>
    public static string JobFailureMessage(Exception ex) => ex switch
    {
        NoTrustworthyTranscriptException => "No trustworthy transcript was found. Try clearer speech or less background music.",
        CaptionVideoUnusableException => ex.Message,
        _ => ServerIssue,
    };
}
