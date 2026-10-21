using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Infrastructure.Services
{
    public class AutoBlogService : IAutoBlogService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AutoBlogService> _logger;

        public AutoBlogService(ApplicationDbContext context, ILogger<AutoBlogService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task PublishScheduledArticlesAsync()
        {
            _logger.LogInformation("Starting AutoBlog publishing job at {Time}", DateTime.UtcNow);

            var articlesToPublish = await _context.ContentArticles
                .Where(a => a.Status == "Scheduled" && a.ScheduledPublishAt <= DateTime.UtcNow)
                .ToListAsync();

            if (!articlesToPublish.Any())
            {
                _logger.LogInformation("No scheduled articles found to publish.");
                return;
            }

            foreach (var article in articlesToPublish)
            {
                try 
                {
                    article.Status = "Published";
                    article.UpdatedAt = DateTime.UtcNow;
                    _logger.LogInformation("Publishing article: {Title} (ID: {Id})", article.Title, article.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error publishing article {Id}", article.Id);
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("AutoBlog job completed. Published {Count} articles.", articlesToPublish.Count);
        }
    }
}
