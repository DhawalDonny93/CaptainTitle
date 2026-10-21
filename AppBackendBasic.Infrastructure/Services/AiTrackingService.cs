using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;
using AppBackendBasic.Core.Entities;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Infrastructure.Services
{
    public class AiTrackingService : IAiTrackingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AiTrackingService> _logger;

        public AiTrackingService(ApplicationDbContext context, ILogger<AiTrackingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task TrackAllActivePromptsAsync()
        {
            _logger.LogInformation("Starting AI Tracking job at {Time}", DateTime.UtcNow);

            var activePrompts = await _context.PromptTrackings
                .Include(p => p.Project)
                .Where(p => p.IsActive)
                .ToListAsync();

            if (!activePrompts.Any())
            {
                _logger.LogInformation("No active prompts found to track.");
                return;
            }

            foreach (var prompt in activePrompts)
            {
                try
                {
                    // In a real implementation, this would call external API (SerpApi/Perplexity)
                    // For MVP, we simulate a check
                    _logger.LogInformation("Tracking prompt: {PromptText} for Project: {ProjectName}", prompt.PromptText, prompt.Project?.Name);

                    var random = new Random();
                    var score = random.Next(0, 101);
                    var sentiment = score > 70 ? "Positive" : score > 40 ? "Neutral" : "Negative";

                    var result = new AiTrackingResult
                    {
                        PromptId = prompt.Id,
                        AiEngine = "ChatGPT (Simulated)",
                        ResponseText = "Simulated response containing brand mention...",
                        BrandMentioned = score > 30, // Randomize based on score
                        MentionPosition = score > 30 ? random.Next(1, 6) : null, // Position 1-5 if mentioned
                        VisibilityScore = score,
                        Sentiment = sentiment,
                        SentimentScore = score / 100.0,
                        ExecutedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.AiTrackingResults.Add(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error tracking prompt {Id}", prompt.Id);
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("AI Tracking job completed. Tracked {Count} prompts.", activePrompts.Count);
        }
    }
}
