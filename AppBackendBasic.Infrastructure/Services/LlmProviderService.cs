using System.Text.Json;
using System.Text.RegularExpressions;
using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;
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
