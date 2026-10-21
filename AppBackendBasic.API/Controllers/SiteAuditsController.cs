using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AppBackendBasic.Infrastructure.Data;
using AppBackendBasic.Infrastructure.Repositories;
using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;

namespace AppBackendBasic.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SiteAuditsController : ControllerBase
    {
        private readonly ISiteAuditService _siteAuditService;
        private readonly ISiteAuditorService _siteAuditorService;
        private readonly IUserTeamMappingRepository _userTeamMappingRepository;
        private readonly ILogger<SiteAuditsController> _logger;
        private readonly ApplicationDbContext _context;

        public SiteAuditsController(
            ISiteAuditService siteAuditService,
            ISiteAuditorService siteAuditorService,
            IUserTeamMappingRepository userTeamMappingRepository,
            ILogger<SiteAuditsController> logger,
            ApplicationDbContext context)
        {
            _siteAuditService = siteAuditService;
            _siteAuditorService = siteAuditorService;
            _userTeamMappingRepository = userTeamMappingRepository;
            _logger = logger;
            _context = context;
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

            // var isInTeam = await _userTeamMappingRepository.IsUserInTeamAsync(userId, teamId);
            // if (!isInTeam)
            // {
            //     _logger.LogWarning("User {UserId} is not in team {TeamId}", userId, teamId);
            //     return (0, 0);
            // }

            return (userId, teamId);
        }

        /// <summary>
        /// Verifies that a project belongs to the user's team
        /// </summary>
        private async Task<bool> IsProjectInTeamAsync(int projectId, int teamId)
        {
            return await _context.Projects.AnyAsync(p => p.Id == projectId && p.TeamId == teamId);
        }

        // GET: api/SiteAudits/project/5
        [HttpGet("project/{projectId}")]
        public async Task<ActionResult<IEnumerable<SiteAudit>>> GetByProject(int projectId)
        {
            var (userId, teamId) = await GetUserAndTeamAsync();
            if (teamId == 0)
                return Forbid();

            if (!await IsProjectInTeamAsync(projectId, teamId))
                return Forbid();

            return await _context.SiteAudits
                .Where(a => a.ProjectId == projectId && a.TeamId == teamId)
                .OrderByDescending(a => a.AuditDate)
                .ToListAsync();
        }

        // POST: api/SiteAudits/run/5
        [HttpPost("run")]
        public async Task<IActionResult> StartAudit([FromBody] StartAuditRequest request)
        {
            try
            {
                var (userId, teamId) = await GetUserAndTeamAsync();
                if (teamId == 0)
                    return Forbid();

                if (!await IsProjectInTeamAsync(request.ProjectId, teamId))
                    return Forbid();

                var auditRun = await _siteAuditorService.StartAuditAsync(request.ProjectId, request.Url, userId);

                // Update UserProfile with audit status to minimize future DB calls
                await UpdateUserProfileAuditStatusAsync(userId, request.ProjectId, true);

                return Ok(auditRun);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting audit for {Url}", request.Url);
                return StatusCode(500, new { message = "An error occurred while starting the audit." });
            }
        }

        /// <summary>
        /// Get audit run details by ID
        /// </summary>
        [HttpGet("runs/{id}")]
        public async Task<IActionResult> GetAuditRun(int id)
        {
            try
            {
                var (userId, teamId) = await GetUserAndTeamAsync();
                if (teamId == 0)
                    return Forbid();

                // Validate ownership
                var auditRun = await _context.SiteAuditRuns
                    .FirstOrDefaultAsync(r => r.Id == id && r.TeamId == teamId);

                if (auditRun == null)
                {
                    return Forbid();
                }

                return Ok(auditRun);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit run {Id}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the audit run." });
            }
        }

        /// <summary>
        /// Get all audit runs for a project
        /// </summary>
        [HttpGet("runs")]
        public async Task<IActionResult> GetAuditRuns([FromQuery] int projectId, [FromQuery] int limit = 10)
        {
            try
            {
                var (userId, teamId) = await GetUserAndTeamAsync();
                if (teamId == 0)
                    return Forbid();

                if (!await IsProjectInTeamAsync(projectId, teamId))
                    return Forbid();

                var auditRuns = await _context.SiteAuditRuns
                    .Where(r => r.ProjectId == projectId && r.TeamId == teamId)
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(limit)
                    .ToListAsync();

                return Ok(auditRuns);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit runs for project {ProjectId}", projectId);
                return StatusCode(500, new { message = "An error occurred while retrieving audit runs." });
            }
        }

        /// <summary>
        /// Get latest audit for a project
        /// </summary>
        [HttpGet("latest")]
        public async Task<IActionResult> GetLatestAudit([FromQuery] int projectId)
        {
            try
            {
                var auditRun = await _siteAuditorService.GetLatestAuditAsync(projectId);
                
                if (auditRun == null)
                {
                    return NotFound(new { message = "No audit found for this project." });
                }

                return Ok(auditRun);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting latest audit for project {ProjectId}", projectId);
                return StatusCode(500, new { message = "An error occurred while retrieving the latest audit." });
            }
        }

        /// <summary>
        /// Get audit summary with categorized results
        /// </summary>
        [HttpGet("summary/{runId}")]
        public async Task<IActionResult> GetAuditSummary(int runId)
        {
            try
            {
                var summary = await _siteAuditorService.GetAuditSummaryAsync(runId);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit summary for run {RunId}", runId);
                return StatusCode(500, new { message = "An error occurred while retrieving the audit summary." });
            }
        }

        /// <summary>
        /// Get audit trends over time
        /// </summary>
        [HttpGet("history")]
        public async Task<IActionResult> GetAuditHistory(
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

                var trends = await _siteAuditorService.GetAuditTrendsAsync(projectId, dateRange);
                return Ok(trends);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit history for project {ProjectId}", projectId);
                return StatusCode(500, new { message = "An error occurred while retrieving audit history." });
            }
        }

        /// <summary>
        /// Recheck a specific audit issue
        /// </summary>
        [HttpPost("{detailId}/recheck")]
        public async Task<IActionResult> RecheckIssue(int detailId)
        {
            try
            {
                var success = await _siteAuditorService.RecheckIssueAsync(detailId);
                
                if (!success)
                {
                    return NotFound(new { message = "Audit detail not found." });
                }

                return Ok(new { message = "Issue rechecked successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rechecking issue {DetailId}", detailId);
                return StatusCode(500, new { message = "An error occurred while rechecking the issue." });
            }
        }

        /// <summary>
        /// Mark an issue as manually fixed
        /// </summary>
        [HttpPost("{detailId}/mark-fixed")]
        public async Task<IActionResult> MarkIssueFixed(int detailId)
        {
            try
            {
                var success = await _siteAuditorService.MarkIssueFixedAsync(detailId);
                
                if (!success)
                {
                    return NotFound(new { message = "Audit detail not found." });
                }

                return Ok(new { message = "Issue marked as fixed." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking issue fixed {DetailId}", detailId);
                return StatusCode(500, new { message = "An error occurred while marking the issue as fixed." });
            }
        }

        /// <summary>
        /// Export audit report in specified format
        /// </summary>
        [HttpGet("export/{runId}")]
        public async Task<IActionResult> ExportAuditReport(int runId, [FromQuery] string format = "json")
        {
            try
            {
                var reportBytes = await _siteAuditorService.ExportAuditReportAsync(runId, format);
                var contentType = format.ToLower() == "json" ? "application/json" : "text/csv";
                var fileName = $"audit-report-{runId}.{format.ToLower()}";
                
                return File(reportBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting audit report for run {RunId}", runId);
                return StatusCode(500, new { message = "An error occurred while exporting the audit report." });
            }
        }

        /// <summary>
        /// Get audit issues for a specific audit run
        /// </summary>
        [HttpGet("runs/{runId}/issues")]
        public async Task<IActionResult> GetAuditIssues(int runId, [FromQuery] bool? isCompleted = null, [FromQuery] string? severity = null)
        {
            try
            {
                var (userId, teamId) = await GetUserAndTeamAsync();
                if (teamId == 0)
                    return Forbid();

                // Validate audit run ownership
                var auditRun = await _context.SiteAuditRuns
                    .FirstOrDefaultAsync(r => r.Id == runId && r.TeamId == teamId);

                if (auditRun == null)
                {
                    return Forbid();
                }
                
                var query = _context.SiteAuditIssues.Where(i => i.SiteAuditRunId == runId);

                if (isCompleted.HasValue)
                {
                    query = query.Where(i => i.IsCompleted == isCompleted.Value);
                }

                if (!string.IsNullOrEmpty(severity))
                {
                    query = query.Where(i => i.Severity == severity);
                }

                var issues = await query
                    .OrderByDescending(i => i.Priority)
                    .ThenBy(i => i.IsCompleted)
                    .ThenByDescending(i => i.CreatedAt)
                    .ToListAsync();

                return Ok(issues);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit issues for run {RunId}", runId);
                return StatusCode(500, new { message = "An error occurred while retrieving audit issues." });
            }
        }

        /// <summary>
        /// Get audit issues for a project (from latest audit)
        /// </summary>
        [HttpGet("project/{projectId}/issues")]
        public async Task<IActionResult> GetProjectAuditIssues(int projectId)
        {
            try
            {
                var (userId, teamId) = await GetUserAndTeamAsync();
                if (teamId == 0)
                    return Forbid();

                if (!await IsProjectInTeamAsync(projectId, teamId))
                    return Forbid();

                // Get the latest audit run for the project (filtered by TeamId)
                var latestRun = await _context.SiteAuditRuns
                    .Where(r => r.ProjectId == projectId && r.TeamId == teamId && r.Status == "Completed")
                    .OrderByDescending(r => r.CompletedAt)
                    .FirstOrDefaultAsync();

                if (latestRun == null)
                {
                    return Ok(new List<object>()); // Return empty list if no completed audits
                }

                var issues = await _context.SiteAuditIssues
                    .Where(i => i.SiteAuditRunId == latestRun.Id && i.TeamId == teamId)
                    .OrderByDescending(i => i.Priority)
                    .ThenBy(i => i.IsCompleted)
                    .ThenByDescending(i => i.CreatedAt)
                    .ToListAsync();

                return Ok(issues);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit issues for project {ProjectId}", projectId);
                return Ok(new List<object>()); // Return empty list on error to prevent frontend crash
            }
        }

        /// <summary>
        /// Mark an audit issue as completed
        /// </summary>
        [HttpPost("issues/{issueId}/complete")]
        public async Task<IActionResult> MarkIssueCompleted(int issueId)
        {
            try
            {
                var (userId, teamId) = await GetUserAndTeamAsync();
                if (teamId == 0)
                    return Forbid();

                var issue = await _context.SiteAuditIssues
                    .FirstOrDefaultAsync(i => i.Id == issueId && i.TeamId == teamId);

                if (issue == null)
                {
                    return Forbid();
                }

                issue.IsCompleted = true;
                issue.CompletedAt = DateTime.UtcNow;
                issue.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Issue marked as completed.", issue });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking issue completed {IssueId}", issueId);
                return StatusCode(500, new { message = "An error occurred while marking the issue as completed." });
            }
        }

        /// <summary>
        /// Mark an audit issue as incomplete
        /// </summary>
        [HttpPost("issues/{issueId}/incomplete")]
        public async Task<IActionResult> MarkIssueIncomplete(int issueId)
        {
            try
            {
                var (userId, teamId) = await GetUserAndTeamAsync();
                if (teamId == 0)
                    return Forbid();

                var issue = await _context.SiteAuditIssues
                    .FirstOrDefaultAsync(i => i.Id == issueId && i.TeamId == teamId);

                if (issue == null)
                {
                    return Forbid();
                }

                issue.IsCompleted = false;
                issue.CompletedAt = null;
                issue.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Issue marked as incomplete.", issue });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking issue incomplete {IssueId}", issueId);
                return StatusCode(500, new { message = "An error occurred while marking the issue as incomplete." });
            }
        }

        /// <summary>
        /// Create audit issues (typically called by LLM service after analysis)
        /// </summary>
        [HttpPost("runs/{runId}/issues")]
        public async Task<IActionResult> CreateAuditIssues(int runId, [FromBody] List<CreateSiteAuditIssueDto> issues)
        {
            try
            {
                var (userId, teamId) = await GetUserAndTeamAsync();
                if (teamId == 0)
                    return Forbid();

                // Validate audit run ownership
                var auditRun = await _context.SiteAuditRuns
                    .FirstOrDefaultAsync(r => r.Id == runId && r.TeamId == teamId);

                if (auditRun == null)
                {
                    return Forbid();
                }

                var newIssues = issues.Select(dto => new SiteAuditIssue
                {
                    SiteAuditRunId = runId,
                    TeamId = teamId,
                    Title = dto.Title,
                    Description = dto.Description,
                    Severity = dto.Severity,
                    Category = dto.Category,
                    Type = dto.Type,
                    AffectedPages = dto.AffectedPages,
                    Recommendation = dto.Recommendation,
                    AutoFixAvailable = dto.AutoFixAvailable,
                    Priority = dto.Priority,
                    EstimatedImpactScore = dto.EstimatedImpactScore,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }).ToList();

                _context.SiteAuditIssues.AddRange(newIssues);
                await _context.SaveChangesAsync();

                return Ok(new { message = $"Created {newIssues.Count} audit issues.", issues = newIssues });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating audit issues for run {RunId}", runId);
                return StatusCode(500, new { message = "An error occurred while creating audit issues." });
            }
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userIdClaim))
            {
                _logger.LogWarning("NameIdentifier claim not found. Available claims: {Claims}", 
                    string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}")));
                return 0;
            }
            
            if (!int.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("Failed to parse userId from claim value: {ClaimValue}", userIdClaim);
                return 0;
            }
            
            return userId;
        }
        
        private async Task<bool> ValidateProjectOwnership(int projectId)
        {
            var (userId, teamId) = await GetUserAndTeamAsync();
            if (teamId == 0) return false;

            return await _context.Projects
                .AnyAsync(p => p.Id == projectId && p.TeamId == teamId);
        }
        
        private async Task UpdateUserProfileAuditStatusAsync(int userId, int projectId, bool hasAudit)
        {
            try
            {
                var userProfile = await _context.UserProfiles
                    .FirstOrDefaultAsync(p => p.UserId == userId);
                
                if (userProfile == null)
                {
                    // Create profile if it doesn't exist
                    userProfile = new UserProfile
                    {
                        UserId = userId,
                        ExtendedProperties = "{}"
                    };
                    _context.UserProfiles.Add(userProfile);
                }
                
                // Parse existing ExtendedProperties
                var extendedProps = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(userProfile.ExtendedProperties) 
                    ?? new Dictionary<string, object>();
                
                // Update or add siteAuditStatus
                if (!extendedProps.ContainsKey("siteAuditStatus"))
                {
                    extendedProps["siteAuditStatus"] = new Dictionary<string, bool>();
                }
                
                var auditStatus = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, bool>>(
                    System.Text.Json.JsonSerializer.Serialize(extendedProps["siteAuditStatus"])) 
                    ?? new Dictionary<string, bool>();
                
                auditStatus[projectId.ToString()] = hasAudit;
                extendedProps["siteAuditStatus"] = auditStatus;
                
                // Save back to ExtendedProperties
                userProfile.ExtendedProperties = System.Text.Json.JsonSerializer.Serialize(extendedProps);
                userProfile.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile audit status for user {UserId}, project {ProjectId}", userId, projectId);
                // Don't fail the request if profile update fails
            }
        }
    }

    public class CreateSiteAuditIssueDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = "Medium";
        public string Category { get; set; } = "Technical";
        public string Type { get; set; } = string.Empty;
        public int AffectedPages { get; set; }
        public string? Recommendation { get; set; }
        public bool AutoFixAvailable { get; set; }
        public int Priority { get; set; } = 3;
        public int EstimatedImpactScore { get; set; }
    }
}
