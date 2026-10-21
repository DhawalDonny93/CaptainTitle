using System.Security.Claims;
using AppBackendBasic.Core.Interfaces;
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
    public class CitationsController : ControllerBase
    {
        private readonly ICitationTrackingService _citationTrackingService;
        private readonly IUserTeamMappingRepository _userTeamMappingRepository;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CitationsController> _logger;

        public CitationsController(
            ICitationTrackingService citationTrackingService,
            IUserTeamMappingRepository userTeamMappingRepository,
            ApplicationDbContext context,
            ILogger<CitationsController> logger)
        {
            _citationTrackingService = citationTrackingService;
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
        /// Get all citations for a project
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCitations(
            [FromQuery] int projectId,
            [FromQuery] string? platform = null,
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
                var citations = await _citationTrackingService.GetCitationsAsync(projectId, platform, startDate, endDate);
                return Ok(citations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting citations for project {ProjectId}", projectId);
                return StatusCode(500, new { message = "An error occurred while retrieving citations." });
            }
        }

        /// <summary>
        /// Get citation by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCitation(int id)
        {
            var (userId, teamId) = await GetUserAndTeamAsync();
            if (teamId == 0)
                return Forbid();

            try
            {
                var citation = await _citationTrackingService.GetCitationByIdAsync(id);

                if (citation == null)
                {
                    return NotFound(new { message = "Citation not found." });
                }

                // Verify citation belongs to team
                if (citation.TeamId != teamId)
                    return Forbid();

                return Ok(citation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting citation {CitationId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the citation." });
            }
        }

        /// <summary>
        /// Get citation statistics for a project
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetCitationStats(
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
                var stats = await _citationTrackingService.GetCitationStatsAsync(projectId, startDate, endDate);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting citation stats for project {ProjectId}", projectId);
                return StatusCode(500, new { message = "An error occurred while retrieving citation statistics." });
            }
        }

        /// <summary>
        /// Get brand mentions for a project
        /// </summary>
        [HttpGet("mentions")]
        public async Task<IActionResult> GetBrandMentions(
            [FromQuery] int projectId,
            [FromQuery] string? platform = null,
            [FromQuery] bool? isMentioned = null,
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
                var mentions = await _citationTrackingService.GetBrandMentionsAsync(projectId, platform, isMentioned, startDate, endDate);
                return Ok(mentions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting brand mentions for project {ProjectId}", projectId);
                return StatusCode(500, new { message = "An error occurred while retrieving brand mentions." });
            }
        }

        /// <summary>
        /// Get brand mention statistics for a project
        /// </summary>
        [HttpGet("mentions/stats")]
        public async Task<IActionResult> GetBrandMentionStats(
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
                var stats = await _citationTrackingService.GetBrandMentionStatsAsync(projectId, startDate, endDate);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting brand mention stats for project {ProjectId}", projectId);
                return StatusCode(500, new { message = "An error occurred while retrieving brand mention statistics." });
            }
        }

        /// <summary>
        /// Get top cited domains for a project
        /// </summary>
        [HttpGet("top-domains")]
        public async Task<IActionResult> GetTopCitedDomains(
            [FromQuery] int projectId,
            [FromQuery] int limit = 10,
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
                var topDomains = await _citationTrackingService.GetTopCitedDomainsAsync(projectId, limit, startDate, endDate);
                return Ok(topDomains);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top cited domains for project {ProjectId}", projectId);
                return StatusCode(500, new { message = "An error occurred while retrieving top cited domains." });
            }
        }

        /// <summary>
        /// Manually trigger daily aggregation
        /// </summary>
        [HttpPost("aggregate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AggregateCitations(
            [FromQuery] int projectId,
            [FromQuery] DateTime? date = null)
        {
            var (userId, teamId) = await GetUserAndTeamAsync();
            if (teamId == 0)
                return Forbid();

            if (!await IsProjectInTeamAsync(projectId, teamId))
                return Forbid();

            try
            {
                var aggregationDate = date ?? DateTime.UtcNow.Date.AddDays(-1);
                await _citationTrackingService.AggregateDailyCitationsAsync(projectId, aggregationDate);

                return Ok(new { message = "Citations aggregated successfully.", date = aggregationDate });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error aggregating citations for project {ProjectId}", projectId);
                return StatusCode(500, new { message = "An error occurred while aggregating citations." });
            }
        }

        /// <summary>
        /// Get sources analytics for a project
        /// </summary>
        [HttpGet("sources/analytics")]
        public async Task<IActionResult> GetSourcesAnalytics(
            [FromQuery] int projectId,
            [FromQuery] string? platform = null,
            [FromQuery] string? topic = null,
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
                var analytics = await _citationTrackingService.GetSourcesAnalyticsAsync(projectId, platform, topic, startDate, endDate);
                return Ok(analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sources analytics for project {ProjectId}", projectId);
                return StatusCode(500, new { message = "An error occurred while retrieving sources analytics." });
            }
        }

        /// <summary>
        /// Get mentions analytics for a project
        /// </summary>
        [HttpGet("mentions/analytics")]
        public async Task<IActionResult> GetMentionsAnalytics(
            [FromQuery] int projectId,
            [FromQuery] string? platform = null,
            [FromQuery] string? topic = null,
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
                var analytics = await _citationTrackingService.GetMentionsAnalyticsAsync(projectId, platform, topic, startDate, endDate);
                return Ok(analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting mentions analytics for project {ProjectId}", projectId);
                return StatusCode(500, new { message = "An error occurred while retrieving mentions analytics." });
            }
        }

        /// <summary>
        /// Get detailed citation data for a specific domain
        /// </summary>
        [HttpGet("domain-details")]
        public async Task<IActionResult> GetCitationDetailsByDomain(
            [FromQuery] int projectId,
            [FromQuery] string domain,
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
                if (string.IsNullOrEmpty(domain))
                {
                    return BadRequest(new { message = "Domain parameter is required." });
                }

                var details = await _citationTrackingService.GetCitationDetailsByDomainAsync(projectId, domain, startDate, endDate);
                return Ok(details);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting citation details for domain {Domain} in project {ProjectId}", domain, projectId);
                return StatusCode(500, new { message = "An error occurred while retrieving citation details." });
            }
        }
    }
}
