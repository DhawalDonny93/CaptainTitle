using System.Text.Json;
using System.Text.RegularExpressions;
using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;
using AppBackendBasic.Infrastructure.Parsers;
using AppBackendBasic.Infrastructure.Services.LlmProviders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Infrastructure.Services
{
    public class LlmProviderService : ILlmProviderService
    {
        private readonly ApplicationDbContext _context;
        private readonly LlmProviderFactory _providerFactory;
        private readonly ILlmJobConfigService _jobConfigService;
        private readonly ILogger<LlmProviderService> _logger;
        private readonly CitationParser _citationParser;

        public LlmProviderService(
            ApplicationDbContext context,
            LlmProviderFactory providerFactory,
            ILlmJobConfigService jobConfigService,
            ILogger<LlmProviderService> logger,
            ILoggerFactory loggerFactory)
        {
            _context = context;
            _providerFactory = providerFactory;
            _jobConfigService = jobConfigService;
            _logger = logger;
            _citationParser = new CitationParser(loggerFactory.CreateLogger<CitationParser>());
        }

        public async Task<LlmResponseDto> SendRequestAsync(LlmRequestDto request)
        {
            try
            {
                // If the request has a RequestType (job key), resolve provider+model from admin config
                if (!string.IsNullOrEmpty(request.RequestType))
                {
                    var resolved = await _jobConfigService.GetJobConfigAsync(
                        request.RequestType,
                        defaultProvider: request.Provider,
                        defaultModel: request.Model,
                        defaultMaxTokens: request.MaxTokens,
                        defaultTemperature: request.Temperature);

                    request.Provider = resolved.Provider;
                    request.Model = resolved.Model;
                    request.MaxTokens = resolved.MaxTokens;
                    request.Temperature = resolved.Temperature;
                }

                // Get provider
                var provider = _providerFactory.GetProvider(request.Provider);
                
                // Send request
                var response = await provider.SendRequestAsync(request);
                
                // Log request to database
                await LogRequestAsync(request, response);
                
                // Update cost tracking if successful
                if (response.Success && response.Cost.HasValue)
                {
                    await UpdateCostTrackingAsync(request, response);
                }
                
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendRequestAsync");
                return new LlmResponseDto
                {
                    Success = false,
                    ErrorMessage = $"Service error: {ex.Message}"
                };
            }
        }

        public async Task<ContentGenerationResponse> GenerateContentAsync(ContentGenerationRequest request)
        {
            try
            {
                var provider = _providerFactory.GetProvider(request.Provider);
                var llmResponse = await provider.GenerateContentAsync(request);

                if (!llmResponse.Success)
                {
                    return new ContentGenerationResponse
                    {
                        Success = false,
                        ErrorMessage = llmResponse.ErrorMessage
                    };
                }

                // Parse the JSON response
                var contentResponse = new ContentGenerationResponse
                {
                    Success = true,
                    Cost = llmResponse.Cost
                };

                if (llmResponse.ResponseJson != null)
                {
                    var jsonElement = JsonSerializer.SerializeToElement(llmResponse.ResponseJson);
                    
                    if (jsonElement.TryGetProperty("title", out var title))
                        contentResponse.Title = title.GetString();
                    
                    if (jsonElement.TryGetProperty("metaDescription", out var meta))
                        contentResponse.MetaDescription = meta.GetString();
                    
                    if (jsonElement.TryGetProperty("content", out var content))
                        contentResponse.Content = content.GetString();
                    
                    if (jsonElement.TryGetProperty("suggestedHeadings", out var headings))
                    {
                        contentResponse.SuggestedHeadings = new List<string>();
                        foreach (var heading in headings.EnumerateArray())
                        {
                            contentResponse.SuggestedHeadings.Add(heading.GetString() ?? "");
                        }
                    }
                    
                    if (jsonElement.TryGetProperty("keywordDensity", out var density))
                    {
                        contentResponse.KeywordDensity = new Dictionary<string, double>();
                        foreach (var prop in density.EnumerateObject())
                        {
                            contentResponse.KeywordDensity[prop.Name] = prop.Value.GetDouble();
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(llmResponse.ResponseText))
                {
                    // Fallback: use response text as content
                    contentResponse.Content = llmResponse.ResponseText;
                }

                // Calculate word count
                if (!string.IsNullOrEmpty(contentResponse.Content))
                {
                    contentResponse.WordCount = contentResponse.Content.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
                }

                return contentResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GenerateContentAsync");
                return new ContentGenerationResponse
                {
                    Success = false,
                    ErrorMessage = $"Content generation failed: {ex.Message}"
                };
            }
        }

        public async Task<PromptTestResponse> TestPromptAsync(PromptTestRequest request)
        {
            var response = new PromptTestResponse
            {
                Success = true,
                Results = new List<PlatformTestResult>()
            };

            decimal totalCost = 0;
            var visibilityScores = new List<double>();

            foreach (var platform in request.Platforms)
            {
                try
                {
                    var platformResult = await TestPromptOnPlatformAsync(request, platform);
                    response.Results.Add(platformResult);
                    visibilityScores.Add(platformResult.VisibilityScore);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error testing prompt on platform {Platform}", platform);
                    response.Results.Add(new PlatformTestResult
                    {
                        Platform = platform,
                        BrandMentioned = false,
                        VisibilityScore = 0
                    });
                }
            }

            response.AverageVisibilityScore = visibilityScores.Any() ? visibilityScores.Average() : 0;
            response.TotalCost = totalCost;

            return response;
        }

        private async Task<PlatformTestResult> TestPromptOnPlatformAsync(PromptTestRequest request, string platform)
        {
            var providerName = platform.ToLower() switch
            {
                "chatgpt" => "OpenAI",
                "claude" => "Anthropic",
                "gemini" => "Google",
                "perplexity" => "Perplexity",
                "searchgpt" => "GoogleSearch",
                "googlesearch" => "GoogleSearch",
                _ => "OpenAI"
            };

            var llmRequest = new LlmRequestDto
            {
                ProjectId = request.ProjectId,
                Provider = providerName,
                Model = platform,
                RequestType = request.RequestType ?? "PromptTest", // Use JobKey from request or default to "PromptTest"
                PromptText = request.PromptText,
                SystemPrompt = "Respond naturally as you would to a user query. Include relevant citations and source URLs in your response when providing factual information or recommendations. Format URLs as clickable links.",
                Temperature = 0.7m,
                MaxTokens = 1000
            };

            var llmResponse = await SendRequestAsync(llmRequest);

            var result = new PlatformTestResult
            {
                Platform = platform,
                ResponseText = llmResponse.ResponseText ?? ""
            };

            if (!llmResponse.Success || string.IsNullOrEmpty(llmResponse.ResponseText))
            {
                return result;
            }

            // Analyze brand mentions
            var responseText = llmResponse.ResponseText.ToLower();
            var brandName = request.BrandName.ToLower();

            result.BrandMentioned = responseText.Contains(brandName);

            if (result.BrandMentioned)
            {
                // Find mention position (1-based index)
                var sentences = llmResponse.ResponseText.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < sentences.Length; i++)
                {
                    if (sentences[i].ToLower().Contains(brandName))
                    {
                        result.MentionPosition = i + 1;
                        result.MentionContext = sentences[i].Trim();
                        break;
                    }
                }

                // Calculate visibility score based on position
                result.VisibilityScore = CalculateVisibilityScore(result.MentionPosition ?? 999);

                // Determine mention type
                if (request.BrandDomain != null && responseText.Contains(request.BrandDomain.ToLower()))
                {
                    result.MentionType = "Direct";
                }
                else if (request.ProductNames?.Any(p => responseText.Contains(p.ToLower())) == true)
                {
                    result.MentionType = "Indirect";
                }
                else
                {
                    result.MentionType = "Comparison";
                }

                // Analyze sentiment
                var sentimentResult = await AnalyzeSentimentAsync(new SentimentAnalysisRequest
                {
                    Text = result.MentionContext ?? llmResponse.ResponseText
                });

                if (sentimentResult.Success)
                {
                    result.Sentiment = sentimentResult.OverallSentiment;
                    result.SentimentScore = sentimentResult.SentimentScore;
                }
            }

            // Extract citations
            result.CitationsFound = ExtractUrls(llmResponse.ResponseText);

            return result;
        }

        public async Task<CitationAnalysisResponse> AnalyzeCitationsAsync(CitationAnalysisRequest request)
        {
            var response = new CitationAnalysisResponse
            {
                Success = true,
                Citations = new List<DetectedCitation>()
            };

            try
            {
                var urls = ExtractUrls(request.ResponseText);
                int position = 1;

                foreach (var url in urls)
                {
                    var citation = new DetectedCitation
                    {
                        Url = url,
                        Position = position++
                    };

                    // Parse URL
                    try
                    {
                        var uri = new Uri(url);
                        citation.Domain = uri.Host.Replace("www.", "");
                        citation.PagePath = uri.PathAndQuery;
                    }
                    catch
                    {
                        citation.Domain = url;
                    }

                    // Determine citation type
                    if (request.ResponseText.Contains($"[{url}]") || request.ResponseText.Contains($"({url})"))
                    {
                        citation.CitationType = "Inline";
                    }
                    else if (request.ResponseText.Contains($"<a href=\"{url}\"") || request.ResponseText.Contains($"[link]({url})"))
                    {
                        citation.CitationType = "DirectLink";
                    }
                    else
                    {
                        citation.CitationType = "Reference";
                    }

                    // Extract context (100 chars before and after)
                    var urlIndex = request.ResponseText.IndexOf(url);
                    if (urlIndex >= 0)
                    {
                        var start = Math.Max(0, urlIndex - 100);
                        var length = Math.Min(request.ResponseText.Length - start, 200 + url.Length);
                        citation.Context = request.ResponseText.Substring(start, length);
                    }

                    response.Citations.Add(citation);
                }

                response.TotalCitations = response.Citations.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AnalyzeCitationsAsync");
                response.Success = false;
                response.ErrorMessage = $"Citation analysis failed: {ex.Message}";
            }

            return response;
        }

        public async Task<SentimentAnalysisResponse> AnalyzeSentimentAsync(SentimentAnalysisRequest request)
        {
            try
            {
                var systemPrompt = @"Analyze the sentiment of the following text and provide a detailed sentiment score.
Respond in JSON format:
{
  ""overallSentiment"": ""Positive"" | ""Neutral"" | ""Negative"",
  ""sentimentScore"": -1.0 to 1.0,
  ""confidence"": 0.0 to 1.0,
  ""emotions"": {
    ""joy"": 0.0 to 1.0,
    ""anger"": 0.0 to 1.0,
    ""sadness"": 0.0 to 1.0,
    ""fear"": 0.0 to 1.0,
    ""surprise"": 0.0 to 1.0
  },
  ""keyPhrases"": [""phrase1"", ""phrase2""],
  ""reasoning"": ""Brief explanation""
}";

                var llmRequest = new LlmRequestDto
                {
                    Provider = "Anthropic", // Claude is better for nuanced analysis
                    Model = "claude-3-sonnet-20240229",
                    RequestType = "SentimentAnalysis",
                    PromptText = $"Text: {request.Text}",
                    SystemPrompt = systemPrompt,
                    Temperature = 0.3m,
                    MaxTokens = 500
                };

                var llmResponse = await SendRequestAsync(llmRequest);

                if (!llmResponse.Success)
                {
                    return new SentimentAnalysisResponse
                    {
                        Success = false,
                        ErrorMessage = llmResponse.ErrorMessage
                    };
                }

                // Parse JSON response
                var jsonElement = JsonSerializer.Deserialize<JsonElement>(llmResponse.ResponseText ?? "{}");

                var response = new SentimentAnalysisResponse
                {
                    Success = true,
                    OverallSentiment = jsonElement.GetProperty("overallSentiment").GetString() ?? "Neutral",
                    SentimentScore = jsonElement.GetProperty("sentimentScore").GetDouble(),
                    Confidence = jsonElement.GetProperty("confidence").GetDouble(),
                    Reasoning = jsonElement.TryGetProperty("reasoning", out var reasoning) ? reasoning.GetString() : null
                };

                if (jsonElement.TryGetProperty("emotions", out var emotions))
                {
                    response.Emotions = new Dictionary<string, double>();
                    foreach (var emotion in emotions.EnumerateObject())
                    {
                        response.Emotions[emotion.Name] = emotion.Value.GetDouble();
                    }
                }

                if (jsonElement.TryGetProperty("keyPhrases", out var phrases))
                {
                    response.KeyPhrases = new List<string>();
                    foreach (var phrase in phrases.EnumerateArray())
                    {
                        response.KeyPhrases.Add(phrase.GetString() ?? "");
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AnalyzeSentimentAsync");
                return new SentimentAnalysisResponse
                {
                    Success = false,
                    ErrorMessage = $"Sentiment analysis failed: {ex.Message}"
                };
            }
        }

        public async Task<DraftReplyResponse> GenerateDraftReplyAsync(DraftReplyRequest request)
        {
            try
            {
                var systemPrompt = $@"You are a helpful community member responding to questions and discussions.
Your goal is to provide valuable, authentic responses that:
- Directly address the question or topic
- Provide actionable insights
- Naturally mention relevant products/services when appropriate
- Avoid being overly promotional
- Match the tone and style of {request.Platform}
- Follow community guidelines

Brand Voice: {request.BrandVoice ?? "Helpful and professional"}
Product/Service: {request.ProductDescription ?? ""}
Platform: {request.Platform}
Tone: Helpful, Professional, Casual";

                var userPrompt = $@"Original Post/Question:
{request.OriginalText}

Context: {request.Context ?? "None"}

Generate a helpful, authentic response that:
1. Addresses the specific question or problem
2. Provides valuable information
3. Mentions our product/service naturally if relevant
4. Includes a subtle call-to-action
5. Matches the platform's tone

Response length: 150-300 words
Format: Plain text";

                var llmRequest = new LlmRequestDto
                {
                    ProjectId = request.ProjectId,
                    Provider = "OpenAI",
                    Model = "gpt-4",
                    RequestType = "DraftReply",
                    PromptText = userPrompt,
                    SystemPrompt = systemPrompt,
                    Temperature = 0.8m,
                    MaxTokens = 500
                };

                var llmResponse = await SendRequestAsync(llmRequest);

                if (!llmResponse.Success)
                {
                    return new DraftReplyResponse
                    {
                        Success = false,
                        ErrorMessage = llmResponse.ErrorMessage
                    };
                }

                var draftReply = llmResponse.ResponseText ?? "";
                var wordCount = draftReply.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;

                // Check promotional language
                var promotionalKeywords = new[] { "buy", "purchase", "sale", "discount", "offer", "deal", "promo" };
                var promotionalCount = promotionalKeywords.Count(k => draftReply.ToLower().Contains(k));
                var totalWords = wordCount;
                var promotionalPercentage = totalWords > 0 ? (promotionalCount / (double)totalWords) * 100 : 0;

                return new DraftReplyResponse
                {
                    Success = true,
                    DraftReply = draftReply,
                    WordCount = wordCount,
                    IsPromotional = promotionalPercentage > 10,
                    PromotionalPercentage = promotionalPercentage,
                    Cost = llmResponse.Cost
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GenerateDraftReplyAsync");
                return new DraftReplyResponse
                {
                    Success = false,
                    ErrorMessage = $"Draft reply generation failed: {ex.Message}"
                };
            }
        }

        public async Task<List<string>> GetAvailableProvidersAsync()
        {
            return await Task.FromResult(_providerFactory.GetAvailableProviders());
        }

        public async Task<Dictionary<string, object>> GetCostTrackingAsync(int? userId, int? projectId, DateTime? startDate, DateTime? endDate)
        {
            var query = _context.LlmCostTrackings.AsQueryable();

            if (userId.HasValue)
                query = query.Where(c => c.UserId == userId.Value);

            if (projectId.HasValue)
                query = query.Where(c => c.ProjectId == projectId.Value);

            if (startDate.HasValue)
                query = query.Where(c => c.TrackingDate >= startDate.Value.Date);

            if (endDate.HasValue)
                query = query.Where(c => c.TrackingDate <= endDate.Value.Date);

            var costs = await query
                .Include(c => c.Provider)
                .ToListAsync();

            var result = new Dictionary<string, object>
            {
                ["totalCost"] = costs.Sum(c => c.TotalCost),
                ["totalRequests"] = costs.Sum(c => c.TotalRequests),
                ["totalInputTokens"] = costs.Sum(c => c.TotalInputTokens),
                ["totalOutputTokens"] = costs.Sum(c => c.TotalOutputTokens),
                ["byProvider"] = costs.GroupBy(c => c.Provider.Name)
                    .Select(g => new
                    {
                        provider = g.Key,
                        cost = g.Sum(c => c.TotalCost),
                        requests = g.Sum(c => c.TotalRequests)
                    }).ToList()
            };

            return result;
        }

        // Helper methods

        private async Task LogRequestAsync(LlmRequestDto request, LlmResponseDto response)
        {
            try
            {
                var provider = await _context.LlmProviders
                    .FirstOrDefaultAsync(p => p.Name == response.Provider);

                if (provider == null) return;

                var llmRequest = new LlmRequest
                {
                    UserId = request.UserId,
                    ProjectId = request.ProjectId,
                    ProviderId = provider.Id,
                    Model = response.Model,
                    RequestType = request.RequestType,
                    PromptText = request.PromptText,
                    SystemPrompt = request.SystemPrompt,
                    Temperature = request.Temperature,
                    MaxTokens = request.MaxTokens,
                    RequestTokens = response.RequestTokens,
                    ResponseTokens = response.ResponseTokens,
                    TotalTokens = response.TotalTokens,
                    Cost = response.Cost,
                    ResponseText = response.ResponseText,
                    ResponseJson = response.ResponseJson != null ? JsonSerializer.Serialize(response.ResponseJson) : null,
                    Status = response.Success ? "Success" : "Failed",
                    ErrorMessage = response.ErrorMessage,
                    LatencyMs = response.LatencyMs,
                    CreatedAt = DateTime.UtcNow
                };

                _context.LlmRequests.Add(llmRequest);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging LLM request");
            }
        }

        private async Task UpdateCostTrackingAsync(LlmRequestDto request, LlmResponseDto response)
        {
            try
            {
                var provider = await _context.LlmProviders
                    .FirstOrDefaultAsync(p => p.Name == response.Provider);

                if (provider == null) return;

                var today = DateTime.UtcNow.Date;
                var tracking = await _context.LlmCostTrackings
                    .FirstOrDefaultAsync(t =>
                        t.UserId == request.UserId &&
                        t.ProjectId == request.ProjectId &&
                        t.ProviderId == provider.Id &&
                        t.TrackingDate == today);

                if (tracking == null)
                {
                    tracking = new LlmCostTracking
                    {
                        UserId = request.UserId,
                        ProjectId = request.ProjectId,
                        ProviderId = provider.Id,
                        TrackingDate = today
                    };
                    _context.LlmCostTrackings.Add(tracking);
                }

                tracking.TotalRequests++;
                if (response.Success)
                    tracking.SuccessfulRequests++;
                else
                    tracking.FailedRequests++;

                tracking.TotalInputTokens += response.RequestTokens ?? 0;
                tracking.TotalOutputTokens += response.ResponseTokens ?? 0;
                tracking.TotalCost += response.Cost ?? 0;

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cost tracking");
            }
        }

        private List<string> ExtractUrls(string text)
        {
            var urls = new List<string>();
            var urlPattern = @"https?://[^\s<>""{}|\\^`\[\]]+";
            var matches = Regex.Matches(text, urlPattern);

            foreach (Match match in matches)
            {
                var url = match.Value.TrimEnd('.', ',', ')', ']', '}');
                if (!urls.Contains(url))
                {
                    urls.Add(url);
                }
            }

            return urls;
        }

        private double CalculateVisibilityScore(int position)
        {
            return position switch
            {
                <= 3 => 100,
                <= 6 => 75,
                <= 10 => 50,
                _ => 25
            };
        }

        // private string GetModelForPlatform(string platform)
        // {
        //     return platform.ToLower() switch
        //     {
        //         "chatgpt" => "gpt-4",
        //         "claude" => "claude-3-opus-20240229",
        //         "gemini" => "gemini-pro",
        //         "perplexity" => "gpt-4",
        //         _ => "gpt-4"
        //     };
        // }
    }
}
