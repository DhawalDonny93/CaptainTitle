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
    public class PromptsController : ControllerBase
    {
        private readonly IPromptTrackingService _promptTrackingService;
        private readonly IUserTeamMappingRepository _userTeamMappingRepository;
        private readonly IWebSubscriptionService _subscriptionService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PromptsController> _logger;

        public PromptsController(
            IPromptTrackingService promptTrackingService,
            IUserTeamMappingRepository userTeamMappingRepository,
            IWebSubscriptionService subscriptionService,
            ApplicationDbContext context,
            ILogger<PromptsController> logger)
        {
            _promptTrackingService = promptTrackingService;
            _userTeamMappingRepository = userTeamMappingRepository;
            _subscriptionService = subscriptionService;
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
        /// Get all prompts for a project
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPrompts(
            [FromQuery] int projectId,
            [FromQuery] string? status = null,
            [FromQuery] string? topic = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] bool? isCompetitor = null)
        {
            try
            {
                var prompts = await _promptTrackingService.GetPromptsAsync(projectId, status, topic, isActive, isCompetitor);
                return Ok(prompts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting prompts for project {ProjectId}", projectId);
                return StatusCode(500, new { message = "An error occurred while retrieving prompts." });
            }
        }

        /// <summary>
        /// Get a specific prompt by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPrompt(int id)
        {
            try
            {
                var prompt = await _promptTrackingService.GetPromptByIdAsync(id);
                
                if (prompt == null)
                {
                    return NotFound(new { message = "Prompt not found." });
                }

                return Ok(prompt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting prompt {PromptId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the prompt." });
            }
        }

        /// <summary>
        /// Create a new prompt
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreatePrompt([FromBody] CreatePromptRequest request)
        {
            var (userId, teamId) = await GetUserAndTeamAsync();
            if (teamId == 0)
            {
                return Forbid();
            }

            // Enforce subscription prompt limit per project
            try
            {
                var plan = await _subscriptionService.GetUserSubscriptionPlanAsync(userId);
                if (plan != null && plan.MaxPromptsPerProject != -1)
                {
                    var currentCount = await _context.PromptTrackings
                        .CountAsync(p => p.ProjectId == request.ProjectId && p.IsActive && p.TeamId == teamId);

                    if (currentCount >= plan.MaxPromptsPerProject)
                    {
                        return StatusCode(402, new
                        {
                            message = $"You have reached the maximum of {plan.MaxPromptsPerProject} prompt(s) per project on your {plan.DisplayName} plan. Please upgrade to track more prompts.",
                            code = "QUOTA_EXCEEDED",
                            limit = plan.MaxPromptsPerProject,
                            used = currentCount
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not check prompt quota for user {UserId}, proceeding", userId);
            }

            try
            {
                var prompt = await _promptTrackingService.CreatePromptAsync(
                    request.ProjectId,
                    teamId,
                    userId,
                    request.PromptText,
                    request.Topic,
                    request.Platforms ?? new List<string> { "ChatGPT", "Claude", "Gemini" },
                    request.IsCompetitor);

                return CreatedAtAction(nameof(GetPrompt), new { id = prompt.Id }, prompt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating prompt for project {ProjectId}", request.ProjectId);
                return StatusCode(500, new { message = "An error occurred while creating the prompt." });
            }
        }

        /// <summary>
        /// Update an existing prompt
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePrompt(int id, [FromBody] UpdatePromptRequest request)
        {
            var (userId, teamId) = await GetUserAndTeamAsync();
            if (teamId == 0)
                return Forbid();

            try
            {
                var prompt = await _promptTrackingService.UpdatePromptAsync(
                    id,
                    teamId,
                    request.PromptText,
                    request.Topic,
                    request.Status);

                return Ok(prompt);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating prompt {PromptId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the prompt." });
            }
        }

        /// <summary>
        /// Delete a prompt
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePrompt(int id)
        {
            var (userId, teamId) = await GetUserAndTeamAsync();
            if (teamId == 0)
                return Forbid();

            try
            {
                var result = await _promptTrackingService.DeletePromptAsync(id, teamId);

                if (!result)
                {
                    return NotFound(new { message = "Prompt not found." });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting prompt {PromptId}", id);
                return StatusCode(500, new { message = "An error occurred while deleting the prompt." });
            }
        }

        /// <summary>
        /// Search prompts
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> SearchPrompts(
            [FromQuery] int projectId,
            [FromQuery] string searchTerm)
        {
            try
            {
                var prompts = await _promptTrackingService.SearchPromptsAsync(projectId, searchTerm);
                return Ok(prompts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching prompts for project {ProjectId}", projectId);
                return StatusCode(500, new { message = "An error occurred while searching prompts." });
            }
        }

        /// <summary>
        /// Bulk update prompt status
        /// </summary>
        [HttpPost("bulk-action")]
        public async Task<IActionResult> BulkAction([FromBody] BulkActionRequest request)
        {
            try
            {
                bool result = false;

                if (request.Action == "updateStatus")
                {
                    result = await _promptTrackingService.BulkUpdateStatusAsync(request.PromptIds, request.Status ?? "Active");
                }
                else if (request.Action == "delete")
                {
                    result = await _promptTrackingService.BulkDeleteAsync(request.PromptIds);
                }
                else
                {
                    return BadRequest(new { message = "Invalid action. Use 'updateStatus' or 'delete'." });
                }

                return Ok(new { success = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing bulk action");
                return StatusCode(500, new { message = "An error occurred while performing the bulk action." });
            }
        }

        /// <summary>
        /// Manually trigger visibility check for a prompt
        /// </summary>
        [HttpPost("{id}/check-visibility")]
        public async Task<IActionResult> CheckVisibility(int id)
        {
            var (userId, teamId) = await GetUserAndTeamAsync();
            if (teamId == 0)
                return Forbid();

            try
            {
                // Enqueue background job for visibility check
                var jobId = Hangfire.BackgroundJob.Enqueue<IPromptTrackingService>(
                    service => service.CheckPromptVisibilityAsync(teamId, id));

                _logger.LogInformation("Enqueued visibility check job {JobId} for prompt {PromptId}", jobId, id);

                return Ok(new
                {
                    message = "Visibility check initiated successfully.",
                    jobId = jobId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking visibility for prompt {PromptId}", id);
                return StatusCode(500, new { message = "An error occurred while checking visibility." });
            }
        }

        /// <summary>
        /// Get prompt research data
        /// </summary>
        [HttpGet("research")]
        public async Task<IActionResult> GetResearchData(
            [FromQuery] string? topic = null,
            [FromQuery] int limit = 100)
        {
            try
            {
                var researchData = await _promptTrackingService.GetPromptResearchDataAsync(topic, limit);
                
                // Group by topic for frontend display
                var grouped = researchData
                    .GroupBy(p => p.Topic ?? "Uncategorized")
                    .Select(g => new
                    {
                        topic = g.Key,
                        promptCount = g.Count(),
                        prompts = g.Select(p => new
                        {
                            id = p.Id,
                            text = p.PromptText,
                            estimatedVolume = p.EstimatedVolume,
                            difficulty = p.Difficulty,
                            relevanceScore = p.RelevanceScore,
                            discoveredAt = p.DiscoveredAt
                        }).ToList()
                    })
                    .ToList();

                return Ok(grouped);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting research data");
                return StatusCode(500, new { message = "An error occurred while retrieving research data." });
            }
        }

    }

    // Request DTOs
    public class CreatePromptRequest
    {
        public int ProjectId { get; set; }
        public string PromptText { get; set; } = string.Empty;
        public string? Topic { get; set; }
        public List<string>? Platforms { get; set; }
        public bool? IsCompetitor { get; set; }
    }

    public class UpdatePromptRequest
    {
        public string PromptText { get; set; } = string.Empty;
        public string? Topic { get; set; }
        public string Status { get; set; } = "Active";
    }

    public class BulkActionRequest
    {
        public string Action { get; set; } = string.Empty; // updateStatus, delete
        public List<int> PromptIds { get; set; } = new();
        public string? Status { get; set; }
    }
}
