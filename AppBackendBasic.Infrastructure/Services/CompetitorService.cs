using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace AppBackendBasic.Infrastructure.Services
{
    public class CompetitorService : ICompetitorService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CompetitorService> _logger;
        private readonly HttpClient _httpClient;

        public CompetitorService(
            ApplicationDbContext context,
            ILogger<CompetitorService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        public async Task<List<Competitor>> GetByProjectIdAsync(int projectId)
        {
            return await _context.Competitors
                .Where(c => c.ProjectId == projectId && c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<Competitor> AddCompetitorAsync(int projectId, string domain)
        {
            // Normalize domain
            domain = NormalizeDomain(domain);
            
            // Extract name from domain
            var name = ExtractNameFromDomain(domain);

            // Check if competitor already exists (Direct type)
            var existing = await _context.Competitors
                .FirstOrDefaultAsync(c => c.ProjectId == projectId && c.Domain == domain && c.CompetitorType == "Direct" && c.IsActive);

            if (existing != null)
            {
                return existing;
            }

            // Try to fetch logo
            var logoUrl = await FetchLogoFromWebsiteAsync(domain);

            var competitor = new Competitor
            {
                ProjectId = projectId,
                Name = name,
                Domain = domain,
                LogoUrl = logoUrl,
                CompetitorType = "Direct", // Manually added competitors are Direct
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Competitors.Add(competitor);
            await _context.SaveChangesAsync();

            return competitor;
        }

        public async Task<Competitor> FindOrCreateSerpCompetitorAsync(int projectId, string domain, string? name = null)
        {
            // Normalize domain
            domain = NormalizeDomain(domain);
            
            // Extract name from domain if not provided
            name ??= ExtractNameFromDomain(domain);

            // Check if SERP competitor already exists
            var existing = await _context.Competitors
                .FirstOrDefaultAsync(c => c.ProjectId == projectId && c.Domain == domain && c.CompetitorType == "SERP" && c.IsActive);

            if (existing != null)
            {
                return existing;
            }

            // Try to fetch logo
            var logoUrl = await FetchLogoFromWebsiteAsync(domain);

            var competitor = new Competitor
            {
                ProjectId = projectId,
                Name = name,
                Domain = domain,
                LogoUrl = logoUrl,
                CompetitorType = "SERP", // Found during prompt tracking
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Competitors.Add(competitor);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created SERP competitor {Name} ({Domain}) for project {ProjectId}", name, domain, projectId);

            return competitor;
        }

        public async Task DeleteCompetitorAsync(int competitorId)
        {
            var competitor = await _context.Competitors.FindAsync(competitorId);
            if (competitor != null)
            {
                competitor.IsActive = false;
                competitor.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<string?> FetchLogoFromWebsiteAsync(string domain)
        {
            try
            {
                domain = NormalizeDomain(domain);
                var baseUrl = $"https://{domain}";

                // Try multiple logo sources
                var logoSources = new[]
                {
                    // Google Favicon Service (most reliable)
                    $"https://www.google.com/s2/favicons?domain={domain}&sz=128",
                    // Clearbit Logo API
                    $"https://logo.clearbit.com/{domain}",
                    // DuckDuckGo Icons
                    $"https://icons.duckduckgo.com/ip3/{domain}.ico"
                };

                // Try Google's favicon service first (most reliable)
                return logoSources[0];
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not fetch logo for domain {Domain}", domain);
                return null;
            }
        }

        private string NormalizeDomain(string input)
        {
            // Remove protocol
            input = Regex.Replace(input, @"^https?://", "", RegexOptions.IgnoreCase);
            // Remove www.
            input = Regex.Replace(input, @"^www\.", "", RegexOptions.IgnoreCase);
            // Remove trailing slash and path
            input = input.Split('/')[0];
            // Remove port if present
            input = input.Split(':')[0];
            
            return input.ToLowerInvariant().Trim();
        }

        private string ExtractNameFromDomain(string domain)
        {
            // Remove TLD
            var parts = domain.Split('.');
            if (parts.Length > 1)
            {
                var name = parts[0];
                // Capitalize first letter
                return char.ToUpperInvariant(name[0]) + name.Substring(1);
            }
            return domain;
        }
    }
}
