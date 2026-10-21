using System.Diagnostics;
using System.Text;
using System.Text.Json;
using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Infrastructure.Services.LlmProviders
{
    public class AnthropicProvider : ILlmProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILlmProviderConfigService _configService;
        private readonly ILogger<AnthropicProvider> _logger;

        public string ProviderName => "Anthropic";

        public AnthropicProvider(
            HttpClient httpClient, 
            ILlmProviderConfigService configService, 
            ILogger<AnthropicProvider> logger)
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
                    ?? "https://api.anthropic.com/v1";

                var requestBody = new
                {
                    model = request.Model,
                    max_tokens = request.MaxTokens,
                    messages = new[]
                    {
                        new { role = "user", content = request.PromptText }
                    },
                    system = request.SystemPrompt ?? "You are a helpful assistant.",
                    temperature = (double)request.Temperature
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{apiEndpoint}/messages")
                {
                    Content = httpContent
                };
                httpRequest.Headers.Add("x-api-key", apiKey);
                httpRequest.Headers.Add("anthropic-version", "2023-06-01");

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
                    _logger.LogError("Anthropic API request failed: {StatusCode} - {Content}", httpResponse.StatusCode, responseContent);
                    return response;
                }

                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                // Extract response text from content array
                var contentArray = jsonResponse.GetProperty("content");
                if (contentArray.GetArrayLength() > 0)
                {
                    response.ResponseText = contentArray[0].GetProperty("text").GetString();
                }

                // Extract token usage
                if (jsonResponse.TryGetProperty("usage", out var usage))
                {
                    response.RequestTokens = usage.GetProperty("input_tokens").GetInt32();
                    response.ResponseTokens = usage.GetProperty("output_tokens").GetInt32();
                    response.TotalTokens = (response.RequestTokens ?? 0) + (response.ResponseTokens ?? 0);

                    // Calculate cost
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
                _logger.LogError(ex, "Anthropic HTTP request failed");
                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                response.Success = false;
                response.ErrorMessage = $"Unexpected error: {ex.Message}";
                response.LatencyMs = (int)stopwatch.ElapsedMilliseconds;
                _logger.LogError(ex, "Anthropic request processing failed");
                return response;
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var testRequest = new LlmRequestDto
                {
                    Model = "claude-3-haiku-20240307",
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
Generate high-quality, engaging content following the user's requirements.";

            var userPrompt = $@"Topic: {request.Topic}
Target Keywords: {string.Join(", ", request.Keywords)}
Target Prompts: {string.Join(", ", request.TargetPrompts)}
Citations to Include: {string.Join(", ", request.CitationsToInclude)}
Brand Voice: {request.BrandVoice ?? "Professional and informative"}
Target Audience: {request.TargetAudience ?? "General audience"}
Word Count: {request.MinWordCount}-{request.MaxWordCount} words
Additional Instructions: {request.AdditionalInstructions ?? "None"}

Generate a comprehensive article with title, meta description, and well-structured content.";

            var llmRequest = new LlmRequestDto
            {
                UserId = null,
                ProjectId = request.ProjectId,
                Provider = ProviderName,
                Model = "claude-3-opus-20240229",
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
                "claude-3-opus-20240229" => (0.015m, 0.075m),
                "claude-3-sonnet-20240229" => (0.003m, 0.015m),
                "claude-3-haiku-20240307" => (0.00025m, 0.00125m),
                _ => (0.015m, 0.075m) // Default to Opus pricing
            };
        }
    }
}
