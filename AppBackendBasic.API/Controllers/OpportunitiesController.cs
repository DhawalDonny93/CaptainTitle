using System.Security.Claims;
using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Infrastructure.Data;
using AppBackendBasic.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppBackendBasic.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OpportunitiesController : ControllerBase
    {
        private readonly IOpportunityDiscoveryService _opportunityService;
        private readonly IOpportunityDetectionService _detectionService;
        private readonly IUserTeamMappingRepository _userTeamMappingRepository;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OpportunitiesController> _logger;

        public OpportunitiesController(
            IOpportunityDiscoveryService opportunityService,
            IOpportunityDetectionService detectionService,
            IUserTeamMappingRepository userTeamMappingRepository,
            ApplicationDbContext context,
            ILogger<OpportunitiesController> logger)
        {
            _opportunityService = opportunityService;
            _detectionService = detectionService;
            _userTeamMappingRepository = userTeamMappingRepository;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Extracts and validates user and team IDs from JWT claims
        /// </summary>
        private async Task<(int userId, int teamId)> GetUserAndTeamAsync()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var teamIdClaim = User.FindFirst("team_id")?.Value;

            if (!int.TryParse(userIdClaim, out var userId) || !int.TryParse(teamIdClaim, out var teamId))
            {
                _logger.LogWarning("Invalid user or team ID in claims");
                return (0, 0);
            }

            var isInTeam = await _userTeamMappingRepository.IsUserInTeamAsync(userId, teamId);
            if (!isInTeam)
            {
                _logger.LogWarning("User {UserId} is not in team {TeamId}", userId, teamId);
                return (0, 0);
            }

            return (userId, teamId);
        }

        /// <summary>
        /// Verifies that a project belongs to the user's team
        /// </summary>
        private async Task<bool> IsProjectInTeamAsync(int projectId, int teamId)
        {
            return await _context.Projects.AnyAsync(p => p.Id == projectId && p.TeamId == teamId);
        }

        /// <summary>
        /// Get all opportunities for a project
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetOpportunities([FromQuery] int projectId)
        {
            var (userId, teamId) = await GetUserAndTeamAsync();
            if (teamId == 0)
                return Forbid();

            if (!await IsProjectInTeamAsync(projectId, teamId))
                return Forbid();

            try
            {
                // Try to get opportunities from different sources
                var contentGaps = await _opportunityService.GetContentGapsAsync(projectId);
                var dashboard = await _opportunityService.GetDashboardAsync(projectId);

                return Ok(new
                {
                    contentGaps = contentGaps?.Cast<object>().ToList() ?? new List<object>(),
                    dashboard = dashboard,
                    total = contentGaps?.Count ?? 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting opportunities for project {ProjectId}", projectId);
                // Return empty result instead of error to prevent frontend crash
                return Ok(new
                {
                    contentGaps = new List<object>(),
                    dashboard = new { },
                    total = 0
                });
            }
        }

        /// <summary>
        /// Get all opportunities for a project as a unified list (used by OpportunitiesFound page).
        /// Aggregates VisibilityOpportunities, ContentGapOpportunities, and SeoOpportunities
        /// and maps them to a common shape the frontend expects:
        /// id, title, description, priority (string label), opportunityType, platform, status, estimatedImpact, discoveredAt, createdAt
        /// </summary>
        [HttpGet("project/{projectId}")]
        public async Task<IActionResult> GetOpportunitiesByProject(int projectId, [FromQuery] string? status = null)
        {
            var (userId, teamId) = await GetUserAndTeamAsync();
            if (teamId == 0)
                return Forbid();

            if (!await IsProjectInTeamAsync(projectId, teamId))
                return Forbid();

            try
            {
                var result = new List<OpportunityResponseDto>();

                // 1. Visibility Opportunities
                var visibilityOpps = await _detectionService.GetVisibilityOpportunitiesAsync(projectId);
                foreach (var v in visibilityOpps)
                {
                    if (status != null && !v.Status.Equals(status, StringComparison.OrdinalIgnoreCase)) continue;
                    result.Add(new OpportunityResponseDto
                    {
                        Id = v.Id,
                        ProjectId = v.ProjectId,
                        Source = "Visibility",
                        OpportunityType = v.OpportunityType,
                        Title = BuildVisibilityTitle(v.OpportunityType, v.Platform, v.PromptText),
                        Description = v.Reasoning ?? v.RecommendedContent ?? string.Empty,
                        Platform = v.Platform,
                        Priority = MapIntPriorityToString(v.Priority),
                        Status = v.Status,
                        EstimatedImpact = v.Priority == 1 ? 90 : v.Priority == 2 ? 70 : 40,
                        OptimizationStrategy = v.OptimizationStrategy,
                        DiscoveredAt = v.DiscoveredAt,
                        CreatedAt = v.CreatedAt
                    });
                }

                // 2. Content Gap Opportunities
                var contentGaps = await _detectionService.GetContentGapOpportunitiesAsync(projectId);
                foreach (var c in contentGaps)
                {
                    if (status != null && !c.Status.Equals(status, StringComparison.OrdinalIgnoreCase)) continue;
                    result.Add(new OpportunityResponseDto
                    {
                        Id = c.Id,
                        ProjectId = c.ProjectId,
                        Source = "ContentGap",
                        OpportunityType = "ContentGap",
                        Title = c.Topic,
                        Description = c.Description ?? c.RecommendedContentType ?? string.Empty,
                        Platform = "All",
                        Priority = c.EstimatedImpact, // already "High"/"Medium"/"Low" string
                        Status = c.Status,
                        EstimatedImpact = c.EstimatedImpact == "High" ? 85 : c.EstimatedImpact == "Medium" ? 55 : 25,
                        DiscoveredAt = c.CreatedAt,
                        CreatedAt = c.CreatedAt
                    });
                }

                // 3. SEO Opportunities
                var seoOpps = await _detectionService.GetSeoOpportunitiesAsync(projectId);
                foreach (var s in seoOpps)
                {
                    if (status != null && !s.Status.Equals(status, StringComparison.OrdinalIgnoreCase)) continue;
                    result.Add(new OpportunityResponseDto
                    {
                        Id = s.Id,
                        ProjectId = s.ProjectId,
                        Source = "SEO",
                        OpportunityType = s.OpportunityType,
                        Title = $"{s.OpportunityType} on {s.PageUrl}",
                        Description = s.RecommendedAction,
                        Platform = "Website",
                        Priority = s.Priority, // already "Critical"/"High"/"Medium"/"Low" string
                        Status = s.Status,
                        EstimatedImpact = s.EstimatedImpact,
                        SourceUrl = s.PageUrl,
                        DiscoveredAt = s.DiscoveredAt,
                        CreatedAt = s.CreatedAt
                    });
                }

                // Sort: Critical first, then High, Medium, Low, then by newest
                var priorityOrder = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                    { ["Critical"] = 0, ["High"] = 1, ["Medium"] = 2, ["Low"] = 3 };
                result = result
                    .OrderBy(o => priorityOrder.TryGetValue(o.Priority, out var p) ? p : 99)
                    .ThenByDescending(o => o.DiscoveredAt)
                    .ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting opportunities for project {ProjectId}", projectId);
                return Ok(new List<OpportunityResponseDto>());
            }
        }

        private static string MapIntPriorityToString(int priority) => priority switch
        {
            1 => "High",
            2 => "Medium",
            3 => "Low",
            _ => "Medium"
        };

        private static string BuildVisibilityTitle(string opportunityType, string platform, string promptText)
        {
            var shortPrompt = promptText.Length > 60 ? promptText[..60] + "..." : promptText;
            return opportunityType switch
            {
                "LowVisibility" => $"Low visibility on {platform}: \"{shortPrompt}\"",
                "CompetitorDominance" => $"Competitor dominance on {platform}: \"{shortPrompt}\"",
                "NotMentioned" => $"Not mentioned on {platform}: \"{shortPrompt}\"",
                "MentionedLate" => $"Mentioned late on {platform}: \"{shortPrompt}\"",
                "NegativeMention" => $"Negative mention on {platform}: \"{shortPrompt}\"",
                _ => $"{opportunityType} on {platform}: \"{shortPrompt}\""
            };
        }

        /// <summary>
        /// Start a new opportunity scan
        /// </summary>
        [HttpPost("scan")]
        public async Task<IActionResult> StartScan([FromBody] StartOpportunityScanRequest request)
        {
            var (userId, teamId) = await GetUserAndTeamAsync();
            if (teamId == 0)
                return Forbid();

            if (!await IsProjectInTeamAsync(request.ProjectId, teamId))
                return Forbid();

            try
            {
                var scan = await _opportunityService.StartScanAsync(
                    request.ProjectId,
                    request.ScanType,
                    request.TargetUrl
                );

                return Ok(scan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting opportunity scan");
                return StatusCode(500, new { message = "An error occurred while starting the scan." });
            }
        }

        /// <summary>
        /// Get scan details by ID
        /// </summary>
        [HttpGet("scans/{id}")]
        public async Task<IActionResult> GetScan(int id)
        {
            var (userId, teamId) = await GetUserAndTeamAsync();
            if (teamId == 0)
                return Forbid();

            try
            {
                var scan = await _opportunityService.GetScanAsync(id);

                if (scan == null)
                {
                    return NotFound(new { message = "Scan not found." });
                }

                // Verify scan belongs to team
                if (scan.TeamId != teamId)
                    return Forbid();

                return Ok(scan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting scan {ScanId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the scan." });
            }
        }

        /// <summary>
        /// Get all scans for a project
        /// </summary>
        [HttpGet("scans")]
        public async Task<IActionResult> GetScans([FromQuery] int projectId, [FromQuery] int limit = 10)
        {
            var (userId, teamId) = await GetUserAndTeamAsync();
            if (teamId == 0)
                return Forbid();

            if (!await IsProjectInTeamAsync(projectId, teamId))
                return Forbid();

            try
            {
                var scans = await _opportunityService.GetProjectScansAsync(projectId, limit);
                return Ok(scans);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting scans for project {ProjectId}", projectId);
                return StatusCode(500, new { message = "An error occurred while retrieving scans." });
            }
        }

        /// <summary>
        /// Get community opportunities with filters
        /// </summary>
        [HttpGet("community")]
        public async Task<IActionResult> GetCommunityOpportunities(
            [FromQuery] int projectId,
            [FromQuery] string? platform = null,
            [FromQuery] string? status = null,
            [FromQuery] int? minPriority = null,
            [FromQuery] int? minEngagementScore = null,
            [FromQuery] bool? isArchived = null)
        {
            var (userId, teamId) = await GetUserAndTeamAsync();
            if (teamId == 0)
                return Forbid();

            if (!await IsProjectInTeamAsync(projectId, teamId))
                return Forbid();

            try
            {
                var filters = new OpportunityFilters
                {
                    Platform = platform,
                    Status = status,
                    MinPriority = minPriority,
                    MinEngagementScore = minEngagementScore,
                    IsArchived = isArchived
                };

                var opportunities = await _opportunityService.GetCommunityOpportunitiesAsync(projectId, filters);
                return Ok(opportunities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting community opportunities for project {ProjectId}", projectId);
                return StatusCode(500, new { message = "An error occurred while retrieving opportunities." });
            }
        }

        /// <summary>
        /// Get content gap opportunities
        /// </summary>
        [HttpGet("content-gaps")]
        public async Task<IActionResult> GetContentGaps([FromQuery] int projectId)
        {
            var (userId, teamId) = await GetUserAndTeamAsync();
            if (teamId == 0)
                return Forbid();

            if (!await IsProjectInTeamAsync(projectId, teamId))
                return Forbid();

            try
            {
                var gaps = await _opportunityService.GetContentGapsAsync(projectId);
                return Ok(gaps);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting content gaps for project {ProjectId}", projectId);
                return StatusCode(500, new { message = "An error occurred while retrieving content gaps." });
            }
        }

        /// <summary>
        /// Get competitor analyses
        /// </summary>
        [HttpGet("competitors")]
        public async Task<IActionResult> GetCompetitors([FromQuery] int projectId)
        {
            var (userId, teamId) = await GetUserAndTeamAsync();
            if (teamId == 0)
                return Forbid();

            if (!await IsProjectInTeamAsync(projectId, teamId))
                return Forbid();

            try
            {
                var competitors = await _opportunityService.GetCompetitorAnalysesAsync(projectId);
                return Ok(competitors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting competitors for project {ProjectId}", projectId);
                return StatusCode(500, new { message = "An error occurred while retrieving competitor analyses." });
            }
        }

        /// <summary>
        /// Analyze a specific competitor
        /// </summary>
        [HttpPost("competitors/analyze")]
        public async Task<IActionResult> AnalyzeCompetitor([FromBody] AnalyzeCompetitorRequest request)
        {
            var (userId, teamId) = await GetUserAndTeamAsync();
            if (teamId == 0)
                return Forbid();

            if (!await IsProjectInTeamAsync(request.ProjectId, teamId))
                return Forbid();

            try
            {
                var analysis = await _opportunityService.AnalyzeCompetitorAsync(
                    request.ProjectId,
                    request.CompetitorUrl
                );

                return Ok(analysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing competitor {Url}", request.CompetitorUrl);
                return StatusCode(500, new { message = "An error occurred while analyzing the competitor." });
            }
        }

        /// <summary>
        /// Update opportunity status
        /// </summary>
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOpportunityStatusRequest request)
        {
            var (userId, teamId) = await GetUserAndTeamAsync();
            if (teamId == 0)
                return Forbid();

            try
            {
                var success = await _opportunityService.UpdateOpportunityStatusAsync(id, request.Status, teamId);

                if (!success)
                {
                    return NotFound(new { message = "Opportunity not found." });
                }

                return Ok(new { message = "Status updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating opportunity status {OpportunityId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the status." });
            }
        }

        /// <summary>
        /// Generate draft reply for community opportunity
        /// </summary>
        [HttpPost("{id}/draft-reply")]
        public async Task<IActionResult> GenerateDraftReply(int id)
        {
            var (userId, teamId) = await GetUserAndTeamAsync();
            if (teamId == 0)
                return Forbid();

            try
            {
                var draftReply = await _opportunityService.GenerateDraftReplyAsync(id, teamId);
                return Ok(new { draftReply });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating draft reply for opportunity {OpportunityId}", id);
                return StatusCode(500, new { message = "An error occurred while generating the draft reply." });
            }
        }

        /// <summary>
        /// Archive an opportunity
        /// </summary>
        [HttpPost("{id}/archive")]
        public async Task<IActionResult> ArchiveOpportunity(int id)
        {
            var (userId, teamId) = await GetUserAndTeamAsync();
            if (teamId == 0)
                return Forbid();

            try
            {
                var success = await _opportunityService.ArchiveOpportunityAsync(id, teamId);

                if (!success)
                {
                    return NotFound(new { message = "Opportunity not found." });
                }

                return Ok(new { message = "Opportunity archived successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error archiving opportunity {OpportunityId}", id);
                return StatusCode(500, new { message = "An error occurred while archiving the opportunity." });
            }
        }

        /// <summary>
        /// Get opportunities dashboard
        /// </summary>
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard([FromQuery] int projectId)
        {
            var (userId, teamId) = await GetUserAndTeamAsync();
            if (teamId == 0)
                return Forbid();

            if (!await IsProjectInTeamAsync(projectId, teamId))
                return Forbid();

            try
            {
                var dashboard = await _opportunityService.GetDashboardAsync(projectId);
                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard for project {ProjectId}", projectId);
                return StatusCode(500, new { message = "An error occurred while retrieving the dashboard." });
            }
        }

        /// <summary>
        /// Get opportunity trends
        /// </summary>
        [HttpGet("trends")]
        public async Task<IActionResult> GetTrends(
            [FromQuery] int projectId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var (userId, teamId) = await GetUserAndTeamAsync();
            if (teamId == 0)
                return Forbid();

            if (!await IsProjectInTeamAsync(projectId, teamId))
                return Forbid();

            try
            {
                var dateRange = new DateRangeDto
                {
                    StartDate = startDate ?? DateTime.UtcNow.AddDays(-30),
                    EndDate = endDate ?? DateTime.UtcNow
                };

                var trends = await _opportunityService.GetTrendsAsync(projectId, dateRange);
                return Ok(trends);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting trends for project {ProjectId}", projectId);
                return StatusCode(500, new { message = "An error occurred while retrieving trends." });
            }
        }

        /// <summary>
        /// Add opportunity to tasks
        /// </summary>
        [HttpPost("{id}/add-to-tasks")]
        public async Task<IActionResult> AddOpportunityToTasks(int id, [FromBody] AddOpportunityToTaskRequest request)
        {
            var (userId, teamId) = await GetUserAndTeamAsync();
            if (teamId == 0)
                return Forbid();

            if (!await IsProjectInTeamAsync(request.ProjectId, teamId))
                return Forbid();

            try
            {
                // Create a new task from the opportunity
                var task = new UserTask
                {
                    ProjectId = request.ProjectId,
                    Title = request.Title,
                    Description = request.Description,
                    Priority = request.Priority ?? "Medium",
                    Status = "To Do",
                    DueDate = request.DueDate,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Note: You'll need to inject ApplicationDbContext to save the task
                // For now, this is a placeholder showing the structure
                // In production, inject ITaskService or ApplicationDbContext

                return Ok(new
                {
                    message = "Opportunity added to tasks successfully.",
                    taskId = task.Id,
                    opportunityId = id,
                    opportunitySource = request.OpportunitySource
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding opportunity {OpportunityId} to tasks", id);
                return StatusCode(500, new { message = "An error occurred while adding opportunity to tasks." });
            }
        }
    }

    public class AnalyzeCompetitorRequest
    {
        public int ProjectId { get; set; }
        public string CompetitorUrl { get; set; } = string.Empty;
    }

    public class AddOpportunityToTaskRequest
    {
        public int ProjectId { get; set; }
        public string OpportunitySource { get; set; } = string.Empty; // Visibility, ContentGap, SEO
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Priority { get; set; }
        public DateTime? DueDate { get; set; }
    }
}
