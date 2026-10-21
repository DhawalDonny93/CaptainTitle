using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Services.LLM;

public class LlmService : ILlmService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LlmService> _logger;
    private readonly HttpClient _httpClient;
    private readonly ISubscriptionService _subscriptionService;

    public LlmService(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<LlmService> logger,
        HttpClient httpClient,
        ISubscriptionService subscriptionService)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
        _subscriptionService = subscriptionService;
        
        // Configure HttpClient for OpenAI
        var apiKey = _configuration["OpenAI:ApiKey"];
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", apiKey);
    }

    public async Task<LlmMessageResponse> SendMessageAsync(int userId, SendLlmMessageRequest request)
    {
        // Check rate limits
        var canProceed = await CheckRateLimitAsync(userId);
        if (!canProceed)
        {
            throw new InvalidOperationException("Rate limit exceeded. Please try again later.");
        }

        // Get prompt template
        var template = await _context.Set<LlmPromptTemplate>()
            .FirstOrDefaultAsync(t => t.Id == request.PromptTemplateId && t.IsActive);
        
        if (template == null)
        {
            throw new InvalidOperationException("Prompt template not found or inactive.");
        }

        // Validate image requirement
        if (template.RequiresImage && string.IsNullOrEmpty(request.ImageBase64) && string.IsNullOrEmpty(request.ImageUrl))
        {
            throw new InvalidOperationException("This prompt template requires an image.");
        }

        // Get or create conversation
        LlmConversation conversation;
        if (request.ConversationId.HasValue)
        {
            conversation = await _context.LlmConversations
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == request.ConversationId.Value && c.UserId == userId);
            
            if (conversation == null)
            {
                throw new InvalidOperationException("Conversation not found.");
            }
        }
        else
        {
            conversation = new LlmConversation
            {
                UserId = userId,
                Model = template.Model,
                Title = template.Name // Use template name as initial title
            };
            _context.LlmConversations.Add(conversation);
            await _context.SaveChangesAsync();
        }

        // Create user message (stores user context, not the actual prompt)
        var userMessage = new LlmMessage
        {
            ConversationId = conversation.Id,
            Role = "user",
            RawContent = request.UserContext,
            ParsedContent = request.UserContext ?? "Image analysis request",
            PromptTemplateId = template.Id,
            ImageBase64 = request.ImageBase64,
            ImageUrl = request.ImageUrl
        };
        _context.LlmMessages.Add(userMessage);

        // Build the actual prompt using template
        var actualPrompt = BuildPromptFromTemplate(template, request);

        // Prepare OpenAI request
        var openAIRequest = PrepareOpenAIRequest(template, actualPrompt, request);

        // Call OpenAI API
        var stopwatch = Stopwatch.StartNew();
        var openAIResponse = await CallOpenAIAsync(openAIRequest);
        stopwatch.Stop();

        // Get raw response
        var rawResponse = openAIResponse.Choices[0].Message.Content.ToString() ?? string.Empty;

        // Parse response according to template format
        var parsedResponse = ParseResponse(rawResponse, template);

        // Create assistant message
        var assistantMessage = new LlmMessage
        {
            ConversationId = conversation.Id,
            Role = "assistant",
            RawContent = rawResponse, // Store raw LLM response
            ParsedContent = JsonSerializer.Serialize(parsedResponse), // Store parsed response
            PromptTokens = openAIResponse.Usage.PromptTokens,
            CompletionTokens = openAIResponse.Usage.CompletionTokens,
            TotalTokens = openAIResponse.Usage.TotalTokens,
            FinishReason = openAIResponse.Choices[0].FinishReason,
            ResponseTimeMs = stopwatch.ElapsedMilliseconds
        };
        _context.LlmMessages.Add(assistantMessage);

        // Update conversation stats
        conversation.TotalMessages += 2; // user + assistant
        conversation.TotalTokensUsed += openAIResponse.Usage.TotalTokens;
        conversation.UpdatedAt = DateTime.UtcNow;

        // Update usage quota
        await UpdateUsageAsync(userId, openAIResponse.Usage.TotalTokens, !string.IsNullOrEmpty(request.ImageBase64) || !string.IsNullOrEmpty(request.ImageUrl));

        await _context.SaveChangesAsync();

        _logger.LogInformation("LLM message processed for user {UserId}, conversation {ConversationId}, tokens: {Tokens}", 
            userId, conversation.Id, openAIResponse.Usage.TotalTokens);

        return new LlmMessageResponse
        {
            MessageId = assistantMessage.Id,
            ConversationId = conversation.Id,
            Role = "assistant",
            ParsedResponse = parsedResponse, // Send parsed response to frontend
            RawContent = null, // Don't send raw response to frontend by default
            TotalTokens = openAIResponse.Usage.TotalTokens,
            CreatedAt = assistantMessage.CreatedAt,
            PromptTemplateName = template.Name
        };
    }

    private string BuildPromptFromTemplate(LlmPromptTemplate template, SendLlmMessageRequest request)
    {
        var prompt = template.UserPromptTemplate ?? "{userInput}";

        // Replace placeholders
        prompt = prompt.Replace("{userInput}", request.UserContext ?? "");
        prompt = prompt.Replace("{context}", request.UserContext ?? "");

        // Replace additional parameters
        if (request.AdditionalParams != null)
        {
            foreach (var param in request.AdditionalParams)
            {
                prompt = prompt.Replace($"{{{param.Key}}}", param.Value);
            }
        }

        return prompt;
    }

    private OpenAIRequest PrepareOpenAIRequest(LlmPromptTemplate template, string userPrompt, SendLlmMessageRequest request)
    {
        var openAIRequest = new OpenAIRequest
        {
            Model = template.Model,
            Temperature = template.Temperature,
            MaxTokens = template.MaxTokens,
            Messages = new List<OpenAIMessage>()
        };

        // Add system message with template's system prompt
        openAIRequest.Messages.Add(new OpenAIMessage
        {
            Role = "system",
            Content = template.SystemPrompt
        });

        // Add user message
        if (!string.IsNullOrEmpty(request.ImageBase64) || !string.IsNullOrEmpty(request.ImageUrl))
        {
            // Image message
            var contentParts = new List<OpenAIMessageContent>
            {
                new OpenAIMessageContent
                {
                    Type = "text",
                    Text = userPrompt
                }
            };

            if (!string.IsNullOrEmpty(request.ImageBase64))
            {
                contentParts.Add(new OpenAIMessageContent
                {
                    Type = "image_url",
                    ImageUrl = new OpenAIImageUrl
                    {
                        Url = $"data:image/jpeg;base64,{request.ImageBase64}"
                    }
                });
            }
            else if (!string.IsNullOrEmpty(request.ImageUrl))
            {
                contentParts.Add(new OpenAIMessageContent
                {
                    Type = "image_url",
                    ImageUrl = new OpenAIImageUrl
                    {
                        Url = request.ImageUrl
                    }
                });
            }

            openAIRequest.Messages.Add(new OpenAIMessage
            {
                Role = "user",
                Content = contentParts
            });
        }
        else
        {
            // Text-only message
            openAIRequest.Messages.Add(new OpenAIMessage
            {
                Role = "user",
                Content = userPrompt
            });
        }

        return openAIRequest;
    }

    private object ParseResponse(string rawResponse, LlmPromptTemplate template)
    {
        try
        {
            if (template.ResponseFormat == "json")
            {
                // Try to parse as JSON
                var jsonMatch = Regex.Match(rawResponse, @"\{.*\}", RegexOptions.Singleline);
                if (jsonMatch.Success)
                {
                    return JsonSerializer.Deserialize<object>(jsonMatch.Value) ?? new { content = rawResponse };
                }

                // If no JSON found, try to parse the entire response
                try
                {
                    return JsonSerializer.Deserialize<object>(rawResponse) ?? new { content = rawResponse };
                }
                catch
                {
                    // If parsing fails, wrap in object
                    return new { content = rawResponse };
                }
            }
            else if (template.ResponseFormat == "structured" && !string.IsNullOrEmpty(template.ResponseSchema))
            {
                // Parse according to schema
                // For now, just try to parse as JSON
                return JsonSerializer.Deserialize<object>(rawResponse) ?? new { content = rawResponse };
            }
            else
            {
                // Plain text
                return new { content = rawResponse };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse LLM response, returning as plain text");
            return new { content = rawResponse };
        }
    }

    private async Task<OpenAIResponse> CallOpenAIAsync(OpenAIRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("OpenAI API request failed: {StatusCode} - {Error}", response.StatusCode, error);
                throw new InvalidOperationException($"OpenAI API request failed: {response.StatusCode}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var openAIResponse = JsonSerializer.Deserialize<OpenAIResponse>(responseJson, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return openAIResponse ?? throw new InvalidOperationException("Failed to deserialize OpenAI response");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling OpenAI API");
            throw new InvalidOperationException("Failed to connect to OpenAI API", ex);
        }
    }

    // ... (rest of the methods remain similar: GetConversationAsync, GetUserConversationsAsync, etc.)
    // I'll include the key methods below

    public async Task<LlmConversationResponse> GetConversationAsync(int userId, int conversationId)
    {
        var conversation = await _context.LlmConversations
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId && !c.IsDeleted);

        if (conversation == null)
        {
            throw new InvalidOperationException("Conversation not found.");
        }

        return new LlmConversationResponse
        {
            Id = conversation.Id,
            Title = conversation.Title,
            Model = conversation.Model,
            TotalMessages = conversation.TotalMessages,
            TotalTokensUsed = conversation.TotalTokensUsed,
            CreatedAt = conversation.CreatedAt,
            UpdatedAt = conversation.UpdatedAt,
            Messages = conversation.Messages.OrderBy(m => m.CreatedAt).Select(m => new LlmMessageResponse
            {
                MessageId = m.Id,
                ConversationId = m.ConversationId,
                Role = m.Role,
                ParsedResponse = string.IsNullOrEmpty(m.ParsedContent) ? new { } : JsonSerializer.Deserialize<object>(m.ParsedContent) ?? new { },
                ImageUrl = m.ImageUrl,
                TotalTokens = m.TotalTokens,
                CreatedAt = m.CreatedAt,
                PromptTemplateName = "" // Can be populated from template if needed
            }).ToList()
        };
    }

    public async Task<List<LlmConversationResponse>> GetUserConversationsAsync(int userId, int page, int pageSize)
    {
        var conversations = await _context.LlmConversations
            .Where(c => c.UserId == userId && !c.IsDeleted)
            .OrderByDescending(c => c.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt).Take(5))
            .ToListAsync();

        return conversations.Select(c => new LlmConversationResponse
        {
            Id = c.Id,
            Title = c.Title,
            Model = c.Model,
            TotalMessages = c.TotalMessages,
            TotalTokensUsed = c.TotalTokensUsed,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt,
            Messages = c.Messages.Select(m => new LlmMessageResponse
            {
                MessageId = m.Id,
                ConversationId = m.ConversationId,
                Role = m.Role,
                ParsedResponse = string.IsNullOrEmpty(m.ParsedContent) ? new { } : JsonSerializer.Deserialize<object>(m.ParsedContent) ?? new { },
                ImageUrl = m.ImageUrl,
                TotalTokens = m.TotalTokens,
                CreatedAt = m.CreatedAt,
                PromptTemplateName = ""
            }).ToList()
        }).ToList();
    }

    public async Task<LlmConversationResponse> CreateConversationAsync(int userId, CreateConversationRequest request)
    {
        var conversation = new LlmConversation
        {
            UserId = userId,
            Title = request.Title,
            Model = request.Model
        };

        _context.LlmConversations.Add(conversation);
        await _context.SaveChangesAsync();

        return new LlmConversationResponse
        {
            Id = conversation.Id,
            Title = conversation.Title,
            Model = conversation.Model,
            TotalMessages = 0,
            TotalTokensUsed = 0,
            CreatedAt = conversation.CreatedAt,
            UpdatedAt = conversation.UpdatedAt,
            Messages = new List<LlmMessageResponse>()
        };
    }

    public async Task<bool> DeleteConversationAsync(int userId, int conversationId)
    {
        var conversation = await _context.LlmConversations
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId);

        if (conversation == null)
        {
            return false;
        }

        conversation.IsDeleted = true;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<LlmUsageResponse> GetUsageAsync(int userId)
    {
        var quota = await GetOrCreateQuotaAsync(userId);

        var subscription = await _subscriptionService.GetStatusAsync(userId);
        var tier = subscription.IsSubscribed ? "premium" : "free";

        return new LlmUsageResponse
        {
            RequestsUsed = quota.RequestsUsed,
            RequestsLimit = quota.RequestsLimit,
            RequestsRemaining = Math.Max(0, quota.RequestsLimit - quota.RequestsUsed),
            TokensUsed = quota.TokensUsed,
            TokensLimit = quota.TokensLimit,
            TokensRemaining = Math.Max(0, quota.TokensLimit - quota.TokensUsed),
            ImagesUsed = quota.ImagesUsed,
            ImagesLimit = quota.ImagesLimit,
            ImagesRemaining = Math.Max(0, quota.ImagesLimit - quota.ImagesUsed),
            PeriodStart = quota.PeriodStart,
            PeriodEnd = quota.PeriodEnd,
            SubscriptionTier = tier
        };
    }

    public async Task<bool> CheckRateLimitAsync(int userId)
    {
        var quota = await GetOrCreateQuotaAsync(userId);

        // Check if period has expired
        if (DateTime.UtcNow > quota.PeriodEnd)
        {
            // Reset quota for new period
            quota.PeriodStart = DateTime.UtcNow;
            quota.PeriodEnd = DateTime.UtcNow.AddMonths(1);
            quota.RequestsUsed = 0;
            quota.TokensUsed = 0;
            quota.ImagesUsed = 0;
            quota.ConsecutiveRequests = 0;
            quota.LastRequestAt = null;
            await _context.SaveChangesAsync();
        }

        // Check overall limits
        if (quota.RequestsUsed >= quota.RequestsLimit)
        {
            _logger.LogWarning("User {UserId} exceeded monthly request limit", userId);
            return false;
        }

        if (quota.TokensUsed >= quota.TokensLimit)
        {
            _logger.LogWarning("User {UserId} exceeded monthly token limit", userId);
            return false;
        }

        // Check time-based limits
        var now = DateTime.UtcNow;
        if (quota.LastRequestAt.HasValue)
        {
            var timeSinceLastRequest = now - quota.LastRequestAt.Value;

            // Per-minute check
            if (timeSinceLastRequest.TotalMinutes < 1 && quota.ConsecutiveRequests >= quota.RequestsPerMinute)
            {
                _logger.LogWarning("User {UserId} exceeded per-minute rate limit", userId);
                return false;
            }

            // Reset consecutive counter if more than a minute has passed
            if (timeSinceLastRequest.TotalMinutes >= 1)
            {
                quota.ConsecutiveRequests = 0;
            }
        }

        return true;
    }

    private async Task UpdateUsageAsync(int userId, int tokensUsed, bool hasImage)
    {
        var quota = await GetOrCreateQuotaAsync(userId);

        quota.RequestsUsed++;
        quota.TokensUsed += tokensUsed;
        if (hasImage)
        {
            quota.ImagesUsed++;
        }
        quota.LastRequestAt = DateTime.UtcNow;
        quota.ConsecutiveRequests++;
        quota.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    private async Task<LlmUsageQuota> GetOrCreateQuotaAsync(int userId)
    {
        var quota = await _context.Set<LlmUsageQuota>()
            .FirstOrDefaultAsync(q => q.UserId == userId && q.PeriodEnd > DateTime.UtcNow);

        if (quota == null)
        {
            // Get subscription status
            var subscription = await _subscriptionService.GetStatusAsync(userId);
            var isPremium = subscription.IsSubscribed;

            // Get quota limits from configuration
            var freeConfig = _configuration.GetSection("LlmQuotas:Free");
            var premiumConfig = _configuration.GetSection("LlmQuotas:Premium");

            quota = new LlmUsageQuota
            {
                UserId = userId,
                PeriodStart = DateTime.UtcNow,
                PeriodEnd = DateTime.UtcNow.AddMonths(1),
                RequestsLimit = isPremium ? premiumConfig.GetValue<int>("RequestsPerMonth") : freeConfig.GetValue<int>("RequestsPerMonth"),
                TokensLimit = isPremium ? premiumConfig.GetValue<int>("TokensPerMonth") : freeConfig.GetValue<int>("TokensPerMonth"),
                ImagesLimit = isPremium ? premiumConfig.GetValue<int>("ImagesPerMonth") : freeConfig.GetValue<int>("ImagesPerMonth"),
                RequestsPerMinute = isPremium ? premiumConfig.GetValue<int>("RequestsPerMinute") : freeConfig.GetValue<int>("RequestsPerMinute"),
                RequestsPerHour = isPremium ? premiumConfig.GetValue<int>("RequestsPerHour") : freeConfig.GetValue<int>("RequestsPerHour"),
                RequestsPerDay = isPremium ? premiumConfig.GetValue<int>("RequestsPerDay") : freeConfig.GetValue<int>("RequestsPerDay")
            };

            _context.Set<LlmUsageQuota>().Add(quota);
            await _context.SaveChangesAsync();
        }

        return quota;
    }

    private string GenerateTitleFromMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "New Conversation";
        }

        // Take first 50 characters
        var title = message.Length > 50 ? message.Substring(0, 50) + "..." : message;
        return title;
    }
}
