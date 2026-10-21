using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppBackendBasic.API.Controllers
{
    [ApiController]
    [Route("api/visibility")]
    [Authorize]
    public class VisibilityDashboardController : ControllerBase
    {
        private readonly IVisibilityDashboardService _dashboardService;
        private readonly ILogger<VisibilityDashboardController> _logger;

        public VisibilityDashboardController(
            IVisibilityDashboardService dashboardService,
            ILogger<VisibilityDashboardController> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        /// <summary>
        /// Get dashboard overview with latest metrics
        /// </summary>
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard([FromQuery] int projectId)
        {
            try
            {
                var dashboard = await _dashboardService.GetDashboardOverviewAsync(projectId);
                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard for project {ProjectId}", projectId);
                return StatusCode(500, new { message = "An error occurred while retrieving the dashboard." });
            }
        }

        /// <summary>
        /// Get metrics for a date range
        /// </summary>
        [HttpGet("metrics")]
        public async Task<IActionResult> GetMetrics(
            [FromQuery] int projectId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string period = "daily")
        {
            try
            {
                var dateRange = new DateRangeDto
                {
                    StartDate = startDate ?? DateTime.UtcNow.AddDays(-30),
                    EndDate = endDate ?? DateTime.UtcNow
                };

                var metrics = await _dashboardService.GetMetricsAsync(projectId, dateRange, period);
                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting metrics for project {ProjectId}", projectId);
                return StatusCode(500, new { message = "An error occurred while retrieving metrics." });
            }
        }

        /// <summary>
        /// Get trend data
        /// </summary>
        [HttpGet("trends")]
        public async Task<IActionResult> GetTrends(
            [FromQuery] int projectId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var dateRange = new DateRangeDto
                {
                    StartDate = startDate ?? DateTime.UtcNow.AddDays(-30),
                    EndDate = endDate ?? DateTime.UtcNow
                };

                var trends = await _dashboardService.GetTrendsAsync(projectId, dateRange);
                return Ok(trends);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting trends for project {ProjectId}", projectId);
                return StatusCode(500, new { message = "An error occurred while retrieving trends." });
            }
        }

        /// <summary>
        /// Get competitive analysis
        /// </summary>
        [HttpGet("competitive")]
        public async Task<IActionResult> GetCompetitiveAnalysis(
            [FromQuery] int projectId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var dateRange = new DateRangeDto
                {
                    StartDate = startDate ?? DateTime.UtcNow.AddDays(-30),
                    EndDate = endDate ?? DateTime.UtcNow
                };

                var analysis = await _dashboardService.GetCompetitiveAnalysisAsync(projectId, dateRange);
                return Ok(analysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting competitive analysis for project {ProjectId}", projectId);
                return StatusCode(500, new { message = "An error occurred while retrieving competitive analysis." });
            }
        }

        /// <summary>
        /// Get visibility goals
        /// </summary>
        [HttpGet("goals")]
        public async Task<IActionResult> GetGoals([FromQuery] int projectId)
        {
            try
            {
                var goals = await _dashboardService.GetGoalsAsync(projectId);
                return Ok(goals);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting goals for project {ProjectId}", projectId);
                return StatusCode(500, new { message = "An error occurred while retrieving goals." });
            }
        }

        /// <summary>
        /// Create a new visibility goal
        /// </summary>
        [HttpPost("goals")]
        public async Task<IActionResult> CreateGoal([FromBody] CreateGoalDto dto)
        {
            try
            {
                var goal = await _dashboardService.CreateGoalAsync(dto);
                return Ok(goal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating goal");
                return StatusCode(500, new { message = "An error occurred while creating the goal." });
            }
        }

        /// <summary>
        /// Update a visibility goal
        /// </summary>
        [HttpPut("goals/{id}")]
        public async Task<IActionResult> UpdateGoal(int id, [FromBody] UpdateGoalDto dto)
        {
            try
            {
                var goal = await _dashboardService.UpdateGoalAsync(id, dto);
                if (goal == null)
                {
                    return NotFound(new { message = "Goal not found." });
                }

                return Ok(goal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating goal {GoalId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the goal." });
            }
        }

        /// <summary>
        /// Delete a visibility goal
        /// </summary>
        [HttpDelete("goals/{id}")]
        public async Task<IActionResult> DeleteGoal(int id)
        {
            try
            {
                var success = await _dashboardService.DeleteGoalAsync(id);
                if (!success)
                {
                    return NotFound(new { message = "Goal not found." });
                }

                return Ok(new { message = "Goal deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting goal {GoalId}", id);
                return StatusCode(500, new { message = "An error occurred while deleting the goal." });
            }
        }

        /// <summary>
        /// Get visibility alerts
        /// </summary>
        [HttpGet("alerts")]
        public async Task<IActionResult> GetAlerts(
            [FromQuery] int projectId,
            [FromQuery] bool unreadOnly = false)
        {
            try
            {
                var alerts = await _dashboardService.GetAlertsAsync(projectId, unreadOnly);
                return Ok(alerts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting alerts for project {ProjectId}", projectId);
                return StatusCode(500, new { message = "An error occurred while retrieving alerts." });
            }
        }

        /// <summary>
        /// Mark alert as read
        /// </summary>
        [HttpPut("alerts/{id}/read")]
        public async Task<IActionResult> MarkAlertRead(int id)
        {
            try
            {
                var success = await _dashboardService.MarkAlertReadAsync(id);
                if (!success)
                {
                    return NotFound(new { message = "Alert not found." });
                }

                return Ok(new { message = "Alert marked as read." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking alert {AlertId} as read", id);
                return StatusCode(500, new { message = "An error occurred while marking the alert as read." });
            }
        }

        /// <summary>
        /// Dismiss alert
        /// </summary>
        [HttpPut("alerts/{id}/dismiss")]
        public async Task<IActionResult> DismissAlert(int id)
        {
            try
            {
                var success = await _dashboardService.DismissAlertAsync(id);
                if (!success)
                {
                    return NotFound(new { message = "Alert not found." });
                }

                return Ok(new { message = "Alert dismissed." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dismissing alert {AlertId}", id);
                return StatusCode(500, new { message = "An error occurred while dismissing the alert." });
            }
        }
    }
}
