using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Http;

namespace AppBackendBasic.Services.Notifications;

/// <summary>
/// Service for sending push notifications via Expo Push Notification service
/// </summary>
public class ExpoPushNotificationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExpoPushNotificationService> _logger;
    private const string ExpoApiUrl = "https://exp.host/--/api/v2/push/send";

    public ExpoPushNotificationService(
        IHttpClientFactory httpClientFactory,
        ILogger<ExpoPushNotificationService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("ExpoClient");
        _logger = logger;
    }

    /// <summary>
    /// Send push notification via Expo
    /// </summary>
    public async Task<ExpoPushResult> SendNotificationAsync(
        string expoToken,
        string title,
        string body,
        Dictionary<string, string>? data = null,
        string? sound = "default",
        int? badge = null,
        string? channelId = null,
        int? priority = null)
    {
        try
        {
            // Validate Expo token format
            if (!IsValidExpoToken(expoToken))
            {
                _logger.LogWarning("Invalid Expo token format: {Token}", expoToken);
                return new ExpoPushResult
                {
                    Success = false,
                    ErrorMessage = "Invalid Expo token format"
                };
            }

            var message = new ExpoMessage
            {
                To = expoToken,
                Title = title,
                Body = body,
                Data = data,
                Sound = sound,
                Badge = badge,
                ChannelId = channelId,
                Priority = priority switch
                {
                    1 => "high",
                    -1 => "low",
                    _ => "default"
                }
            };

            var response = await _httpClient.PostAsJsonAsync(ExpoApiUrl, message);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ExpoApiResponse>(responseContent);
                
                if (result?.Data != null && result.Data.Count > 0)
                {
                    var ticketData = result.Data[0];
                    
                    if (ticketData.Status == "ok")
                    {
                        _logger.LogInformation("Expo notification sent successfully. Ticket ID: {TicketId}", ticketData.Id);
                        return new ExpoPushResult
                        {
                            Success = true,
                            TicketId = ticketData.Id
                        };
                    }
                    else if (ticketData.Status == "error")
                    {
                        _logger.LogWarning("Expo notification failed. Error: {Error}, Message: {Message}", 
                            ticketData.Details?.Error, ticketData.Message);
                        
                        return new ExpoPushResult
                        {
                            Success = false,
                            ErrorMessage = ticketData.Message,
                            ErrorCode = ticketData.Details?.Error,
                            ShouldDeactivateToken = ShouldDeactivateToken(ticketData.Details?.Error)
                        };
                    }
                }
            }

            _logger.LogError("Expo API returned error: {StatusCode} - {Response}", 
                response.StatusCode, responseContent);
            
            return new ExpoPushResult
            {
                Success = false,
                ErrorMessage = $"Expo API error: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending Expo notification");
            return new ExpoPushResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Send notifications to multiple Expo tokens
    /// </summary>
    public async Task<List<ExpoPushResult>> SendBatchNotificationsAsync(
        List<string> expoTokens,
        string title,
        string body,
        Dictionary<string, string>? data = null,
        string? sound = "default",
        int? badge = null)
    {
        var results = new List<ExpoPushResult>();

        try
        {
            // Expo supports batch sending up to 100 messages
            var batches = expoTokens.Chunk(100);

            foreach (var batch in batches)
            {
                var messages = batch.Select(token => new ExpoMessage
                {
                    To = token,
                    Title = title,
                    Body = body,
                    Data = data,
                    Sound = sound,
                    Badge = badge
                }).ToList();

                var response = await _httpClient.PostAsJsonAsync(ExpoApiUrl, messages);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ExpoApiResponse>(responseContent);
                    
                    if (result?.Data != null)
                    {
                        foreach (var ticketData in result.Data)
                        {
                            results.Add(new ExpoPushResult
                            {
                                Success = ticketData.Status == "ok",
                                TicketId = ticketData.Id,
                                ErrorMessage = ticketData.Message,
                                ErrorCode = ticketData.Details?.Error,
                                ShouldDeactivateToken = ShouldDeactivateToken(ticketData.Details?.Error)
                            });
                        }
                    }
                }
                else
                {
                    _logger.LogError("Expo batch API error: {StatusCode}", response.StatusCode);
                    results.AddRange(batch.Select(_ => new ExpoPushResult
                    {
                        Success = false,
                        ErrorMessage = $"Expo API error: {response.StatusCode}"
                    }));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending Expo batch notifications");
        }

        return results;
    }

    private bool IsValidExpoToken(string token)
    {
        // Expo tokens start with ExponentPushToken[...] or ExpoPushToken[...]
        return token.StartsWith("ExponentPushToken[") || 
               token.StartsWith("ExpoPushToken[");
    }

    private bool ShouldDeactivateToken(string? errorCode)
    {
        // Deactivate token for these error codes
        return errorCode switch
        {
            "DeviceNotRegistered" => true,
            "InvalidCredentials" => true,
            "MessageTooBig" => false,
            "MessageRateExceeded" => false,
            _ => false
        };
    }
}

// DTOs for Expo API
public class ExpoMessage
{
    [JsonPropertyName("to")]
    public string To { get; set; } = string.Empty;
    
    [JsonPropertyName("title")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Title { get; set; }
    
    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;
    
    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Data { get; set; }
    
    [JsonPropertyName("sound")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Sound { get; set; }
    
    [JsonPropertyName("badge")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Badge { get; set; }
    
    [JsonPropertyName("channelId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ChannelId { get; set; }
    
    [JsonPropertyName("priority")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Priority { get; set; }
}

public class ExpoApiResponse
{
    [JsonPropertyName("data")]
    public List<ExpoTicketData>? Data { get; set; }
}

public class ExpoTicketData
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    
    [JsonPropertyName("message")]
    public string? Message { get; set; }
    
    [JsonPropertyName("details")]
    public ExpoErrorDetails? Details { get; set; }
}

public class ExpoErrorDetails
{
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

public class ExpoPushResult
{
    public bool Success { get; set; }
    public string? TicketId { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public bool ShouldDeactivateToken { get; set; }
}
