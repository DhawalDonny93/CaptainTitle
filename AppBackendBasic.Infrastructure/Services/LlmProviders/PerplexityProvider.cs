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
    public class PerplexityProvider : ILlmProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILlmProviderConfigService _configService;
        private readonly ILogger<PerplexityProvider> _logger;

        public string ProviderName => "Perplexity";

        public PerplexityProvider(
            HttpClient httpClient, 
            ILlmProviderConfigService configService, 
            ILogger<PerplexityProvider> logger)
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
                    ?? "https://api.perplexity.ai";

                // Perplexity uses OpenAI-compatible API format
                var requestBody = new
                {
                    model = request.Model,
                    messages = new[]
                    {
                        new { role = "system", content = request.SystemPrompt ?? "You are a helpful assistant that provides accurate information with citations." },
                        new { role = "user", content = request.PromptText }
                    },
                    temperature = (double)request.Temperature,
                    max_tokens = request.MaxTokens,
                    // Perplexity-specific: Enable search and citations
                    return_citations = true,
                    return_images = false
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
                    _logger.LogError("Perplexity API request failed: {StatusCode} - {Content}", httpResponse.StatusCode, responseContent);
                    return response;
                }

                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                // Extract response text
                var messageContent = jsonResponse.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
                response.ResponseText = messageContent;

                // Extract citations if available (Perplexity-specific)
                if (jsonResponse.TryGetProperty("citations", out var citations))
                {
                    var citationList = new List<string>();
                    foreach (var citation in citations.EnumerateArray())
                    {
                        citationList.Add(citation.GetString() ?? "");
                    }
                    // Store citations in metadata or log them
                    _logger.LogInformation("Perplexity returned {Count} citations", citationList.Count);
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
                _logger.LogError(ex, "Perplexity HTTP request failed");
                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                response.Success = false;
                response.ErrorMessage = $"Unexpected error: {ex.Message}";
                response.LatencyMs = (int)stopwatch.ElapsedMilliseconds;
                _logger.LogError(ex, "Perplexity request processing failed");
                return response;
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var testRequest = new LlmRequestDto
                {
                    Model = "llama-3.1-sonar-small-128k-online",
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
Generate high-quality, engaging content with accurate citations and sources.
Use your search capabilities to find relevant, up-to-date information.";

            var userPrompt = $@"Topic: {request.Topic}
Target Keywords: {string.Join(", ", request.Keywords)}
Target Prompts: {string.Join(", ", request.TargetPrompts)}
Citations to Include: {string.Join(", ", request.CitationsToInclude)}
Brand Voice: {request.BrandVoice ?? "Professional and informative"}
Target Audience: {request.TargetAudience ?? "General audience"}
Word Count: {request.MinWordCount}-{request.MaxWordCount} words
Additional Instructions: {request.AdditionalInstructions ?? "None"}

Generate a comprehensive article with title, meta description, well-structured content, and include relevant citations from recent sources.";

            var llmRequest = new LlmRequestDto
            {
                UserId = null,
                ProjectId = request.ProjectId,
                Provider = ProviderName,
                Model = "llama-3.1-sonar-large-128k-online",
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
            // Perplexity pricing as of 2024
            return model.ToLower() switch
            {
                "llama-3.1-sonar-small-128k-online" => (0.0002m, 0.0002m),
                "llama-3.1-sonar-small-128k-chat" => (0.0002m, 0.0002m),
                "llama-3.1-sonar-large-128k-online" => (0.001m, 0.001m),
                "llama-3.1-sonar-large-128k-chat" => (0.001m, 0.001m),
                "llama-3.1-sonar-huge-128k-online" => (0.005m, 0.005m),
                _ => (0.001m, 0.001m) // Default to large model pricing
            };
        }
    }
}
