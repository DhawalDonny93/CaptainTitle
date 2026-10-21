using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Infrastructure.Services.LlmProviders
{
    public class OpenAIProvider : ILlmProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILlmProviderConfigService _configService;
        private readonly ILogger<OpenAIProvider> _logger;

        public string ProviderName => "OpenAI";

        public OpenAIProvider(
            HttpClient httpClient, 
            ILlmProviderConfigService configService, 
            ILogger<OpenAIProvider> logger)
        {
            _httpClient = httpClient;
            _configService = configService;
            _logger = logger;
        }

        public async Task<LlmResponseDto> SendRequestAsync(LlmRequestDto request)
        {
            var stopwatch = Stopwatch.StartNew();
            var response = new LlmResponseDto
            {
                Provider = ProviderName,
                Model = request.Model
            };

            try
            {
                // Get configuration from database
                var apiKey = await _configService.GetConfigValueAsync(ProviderName, "ApiKey");
                if (string.IsNullOrEmpty(apiKey))
                {
                    throw new InvalidOperationException($"{ProviderName} API key not configured in database");
                }

                var apiEndpoint = await _configService.GetConfigValueAsync(ProviderName, "Endpoint") 
                    ?? "https://api.openai.com/v1";

                var requestBody = new
                {
                    model = request.Model,
                    messages = new[]
                    {
                        new { role = "system", content = request.SystemPrompt ?? "You are a helpful assistant." },
                        new { role = "user", content = request.PromptText }
                    },
                    temperature = (double)request.Temperature,
                    max_completion_tokens = request.MaxTokens, // Changed from max_tokens to max_completion_tokens for newer OpenAI models
                    response_format = request.RequestType == "ContentGeneration" ? new { type = "json_object" } : null
                };

                var jsonContent = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{apiEndpoint}/chat/completions")
                {
                    Content = httpContent
                };
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                var httpResponse = await _httpClient.SendAsync(httpRequest);
                stopwatch.Stop();
                response.LatencyMs = (int)stopwatch.ElapsedMilliseconds;

                var responseContent = await httpResponse.Content.ReadAsStringAsync();

                if (httpResponse.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    response.Success = false;
                    response.ErrorMessage = "Rate limit exceeded. Please try again later.";
                    return response;
                }

                if (!httpResponse.IsSuccessStatusCode)
                {
                    response.Success = false;
                    response.ErrorMessage = $"API request failed with status {httpResponse.StatusCode}: {responseContent}";
                    _logger.LogError("OpenAI API request failed: {StatusCode} - {Content}", httpResponse.StatusCode, responseContent);
                    return response;
                }

                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                // Extract response text
                var messageContent = jsonResponse.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
                response.ResponseText = messageContent;

                // Try to parse as JSON if it's a structured response
                if (request.RequestType == "ContentGeneration" && !string.IsNullOrEmpty(messageContent))
                {
                    try
                    {
                        response.ResponseJson = JsonSerializer.Deserialize<object>(messageContent);
                    }
                    catch
                    {
                        // Not JSON, keep as text
                    }
                }

                // Extract token usage
                if (jsonResponse.TryGetProperty("usage", out var usage))
                {
                    response.RequestTokens = usage.GetProperty("prompt_tokens").GetInt32();
                    response.ResponseTokens = usage.GetProperty("completion_tokens").GetInt32();
                    response.TotalTokens = usage.GetProperty("total_tokens").GetInt32();

                    // Calculate cost based on model
                    var (inputCost, outputCost) = GetModelCosts(request.Model);
                    response.Cost = ((response.RequestTokens ?? 0) / 1000m * inputCost) + 
                                   ((response.ResponseTokens ?? 0) / 1000m * outputCost);
                }

                response.Success = true;
                return response;
            }
            catch (HttpRequestException ex)
            {
                stopwatch.Stop();
                response.Success = false;
                response.ErrorMessage = $"Network error: {ex.Message}";
                response.LatencyMs = (int)stopwatch.ElapsedMilliseconds;
                _logger.LogError(ex, "OpenAI HTTP request failed");
                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                response.Success = false;
                response.ErrorMessage = $"Unexpected error: {ex.Message}";
                response.LatencyMs = (int)stopwatch.ElapsedMilliseconds;
                _logger.LogError(ex, "OpenAI request processing failed");
                return response;
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var testRequest = new LlmRequestDto
                {
                    Model = "gpt-3.5-turbo",
                    PromptText = "Hello",
                    MaxTokens = 10
                };

                var response = await SendRequestAsync(testRequest);
                return response.Success;
            }
            catch
            {
                return false;
            }
        }

        public async Task<LlmResponseDto> GenerateContentAsync(ContentGenerationRequest request)
        {
            var systemPrompt = @"You are an expert content writer specializing in SEO-optimized articles.
Generate high-quality, engaging content that:
- Follows the specified topic and target keywords
- Includes natural citations to provided sources
- Maintains the specified brand voice and tone
- Is structured with clear headings and paragraphs
- Includes meta description and title suggestions

Respond in JSON format with the following structure:
{
  ""title"": ""Article title"",
  ""metaDescription"": ""SEO meta description (150-160 chars)"",
  ""content"": ""Full HTML content with proper formatting"",
  ""suggestedHeadings"": [""H2 heading 1"", ""H2 heading 2""],
  ""keywordDensity"": {""keyword1"": 2.5, ""keyword2"": 1.8}
}";

            var userPrompt = $@"Topic: {request.Topic}
Target Keywords: {string.Join(", ", request.Keywords)}
Target Prompts: {string.Join(", ", request.TargetPrompts)}
Citations to Include: {string.Join(", ", request.CitationsToInclude)}
Brand Voice: {request.BrandVoice ?? "Professional and informative"}
Target Audience: {request.TargetAudience ?? "General audience"}
Word Count: {request.MinWordCount}-{request.MaxWordCount} words
Additional Instructions: {request.AdditionalInstructions ?? "None"}

Generate a comprehensive article addressing this topic.";

            var llmRequest = new LlmRequestDto
            {
                UserId = null,
                ProjectId = request.ProjectId,
                Provider = request.Provider,
                Model = request.Provider.ToLower() == "openai" ? "gpt-4-turbo-preview" : "gpt-4",
                RequestType = "ContentGeneration",
                PromptText = userPrompt,
                SystemPrompt = systemPrompt,
                Temperature = 0.7m,
                MaxTokens = 4000
            };

            return await SendRequestAsync(llmRequest);
        }

        private (decimal inputCost, decimal outputCost) GetModelCosts(string model)
        {
            return model.ToLower() switch
            {
                "gpt-4" or "gpt-4-0613" => (0.03m, 0.06m),
                "gpt-4-turbo-preview" or "gpt-4-1106-preview" => (0.01m, 0.03m),
                "gpt-3.5-turbo" or "gpt-3.5-turbo-0125" => (0.0005m, 0.0015m),
                _ => (0.03m, 0.06m) // Default to GPT-4 pricing
            };
        }
    }
}
