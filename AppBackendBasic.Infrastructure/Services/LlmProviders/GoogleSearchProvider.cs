using System.Diagnostics;
using System.Text;
using System.Text.Json;
using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Infrastructure.Services.LlmProviders
{
    /// <summary>
    /// Provider for Google AI Search Overview (Search Generative Experience - SGE)
    /// Uses Gemini API with grounding to simulate search-enhanced responses
    /// </summary>
    public class GoogleSearchProvider : ILlmProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILlmProviderConfigService _configService;
        private readonly ILogger<GoogleSearchProvider> _logger;

        public string ProviderName => "GoogleSearch";

        public GoogleSearchProvider(
            HttpClient httpClient, 
            ILlmProviderConfigService configService, 
            ILogger<GoogleSearchProvider> logger)
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
                // Get configuration from database - uses same API key as Google Gemini
                var apiKey = await _configService.GetConfigValueAsync("Google", "ApiKey");
                if (string.IsNullOrEmpty(apiKey))
                {
                    throw new InvalidOperationException($"Google API key not configured in database");
                }

                var apiEndpoint = await _configService.GetConfigValueAsync("Google", "Endpoint") 
                    ?? "https://generativelanguage.googleapis.com/v1beta";

                // Combine system prompt and user prompt
                var combinedPrompt = string.IsNullOrEmpty(request.SystemPrompt) 
                    ? request.PromptText 
                    : $"{request.SystemPrompt}\n\n{request.PromptText}";

                // Use Gemini with grounding for search-enhanced responses
                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[] { new { text = combinedPrompt } }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = (double)request.Temperature,
                        maxOutputTokens = request.MaxTokens,
                        topP = 0.95,
                        topK = 40
                    },
                    // Enable Google Search grounding for search-like responses
                    tools = new[]
                    {
                        new
                        {
                            googleSearchRetrieval = new
                            {
                                dynamicRetrievalConfig = new
                                {
                                    mode = "MODE_DYNAMIC",
                                    dynamicThreshold = 0.7
                                }
                            }
                        }
                    }
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var url = $"{apiEndpoint}/models/{request.Model}:generateContent?key={apiKey}";
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = httpContent
                };

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
                    _logger.LogError("Google Search API request failed: {StatusCode} - {Content}", httpResponse.StatusCode, responseContent);
                    return response;
                }

                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                // Extract response text
                if (jsonResponse.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    var firstCandidate = candidates[0];
                    if (firstCandidate.TryGetProperty("content", out var content))
                    {
                        if (content.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                        {
                            response.ResponseText = parts[0].GetProperty("text").GetString();
                        }
                    }

                    // Extract grounding metadata (search sources)
                    if (firstCandidate.TryGetProperty("groundingMetadata", out var groundingMetadata))
                    {
                        if (groundingMetadata.TryGetProperty("webSearchQueries", out var searchQueries))
                        {
                            _logger.LogInformation("Google Search used {Count} search queries", searchQueries.GetArrayLength());
                        }

                        if (groundingMetadata.TryGetProperty("groundingChunks", out var chunks))
                        {
                            var citationList = new List<string>();
                            foreach (var chunk in chunks.EnumerateArray())
                            {
                                if (chunk.TryGetProperty("web", out var web))
                                {
                                    if (web.TryGetProperty("uri", out var uri))
                                    {
                                        citationList.Add(uri.GetString() ?? "");
                                    }
                                }
                            }
                            _logger.LogInformation("Google Search returned {Count} grounding sources", citationList.Count);
                        }
                    }
                }

                // Extract token usage
                if (jsonResponse.TryGetProperty("usageMetadata", out var usage))
                {
                    response.RequestTokens = usage.GetProperty("promptTokenCount").GetInt32();
                    response.ResponseTokens = usage.GetProperty("candidatesTokenCount").GetInt32();
                    response.TotalTokens = usage.GetProperty("totalTokenCount").GetInt32();

                    // Calculate cost (same as Gemini)
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
                _logger.LogError(ex, "Google Search HTTP request failed");
                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                response.Success = false;
                response.ErrorMessage = $"Unexpected error: {ex.Message}";
                response.LatencyMs = (int)stopwatch.ElapsedMilliseconds;
                _logger.LogError(ex, "Google Search request processing failed");
                return response;
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var testRequest = new LlmRequestDto
                {
                    Model = "gemini-pro",
                    PromptText = "What is the weather today?",
                    MaxTokens = 50
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
            var systemPrompt = @"You are Google's AI Search Overview assistant.
Generate comprehensive, well-researched content with accurate citations from web sources.
Use search grounding to find relevant, up-to-date information.
Format responses as you would in Google Search results with clear, authoritative information.";

            var userPrompt = $@"Topic: {request.Topic}
Target Keywords: {string.Join(", ", request.Keywords)}
Target Prompts: {string.Join(", ", request.TargetPrompts)}
Citations to Include: {string.Join(", ", request.CitationsToInclude)}
Brand Voice: {request.BrandVoice ?? "Professional and informative"}
Target Audience: {request.TargetAudience ?? "General audience"}
Word Count: {request.MinWordCount}-{request.MaxWordCount} words
Additional Instructions: {request.AdditionalInstructions ?? "None"}

Generate a comprehensive article with search-grounded information and citations.";

            var llmRequest = new LlmRequestDto
            {
                UserId = null,
                ProjectId = request.ProjectId,
                Provider = ProviderName,
                Model = "gemini-pro",
                RequestType = "ContentGeneration",
                PromptText = userPrompt,
                SystemPrompt = systemPrompt,
                Temperature = 0.7m,
                MaxTokens = 2048
            };

            return await SendRequestAsync(llmRequest);
        }

        private (decimal inputCost, decimal outputCost) GetModelCosts(string model)
        {
            // Same pricing as Gemini since it uses the same API
            return model.ToLower() switch
            {
                "gemini-pro" => (0.00025m, 0.0005m),
                "gemini-pro-vision" => (0.00025m, 0.0005m),
                "gemini-1.5-pro" => (0.00125m, 0.00375m),
                "gemini-1.5-flash" => (0.000075m, 0.0003m),
                _ => (0.00025m, 0.0005m) // Default to Gemini Pro pricing
            };
        }
    }
}
