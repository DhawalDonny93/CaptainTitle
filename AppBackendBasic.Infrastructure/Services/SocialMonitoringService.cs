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
    public class SocialMonitoringService : ISocialMonitoringService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SocialMonitoringService> _logger;

        public SocialMonitoringService(ApplicationDbContext context, ILogger<SocialMonitoringService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task ScanOpportunitiesAsync()
        {
            _logger.LogInformation("Starting Social Monitoring job at {Time}", DateTime.UtcNow);

            var projects = await _context.Projects.ToListAsync();

            if (!projects.Any())
            {
                _logger.LogInformation("No projects found to scan.");
                return;
            }

            var random = new Random();

            foreach (var project in projects)
            {
                try
                {
                    // Mock finding a Reddit thread
                    if (random.Next(0, 100) > 50) // 50% chance to find an opportunity
                    {
                        var opportunity = new CommunityOpportunity
                        {
                            ProjectId = project.Id,
                            Platform = "Reddit",
                            Title = $"How does {project.Name} compare to competitors?",
                            Url = $"https://reddit.com/r/SaaS/comments/{Guid.NewGuid()}",
                            Summary = "User asking for recommendations in this niche.",
                            Sentiment = "Question",
                            Status = "New",
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.CommunityOpportunities.Add(opportunity);
                         _logger.LogInformation("Found new opportunity: {Title} for Project: {ProjectName}", opportunity.Title, project.Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error scanning for project {Id}", project.Id);
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Social Monitoring job completed.");
        }
    }
}
