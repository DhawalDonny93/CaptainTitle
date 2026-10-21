using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppBackendBasic.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PromptResearchController : ControllerBase
    {
        private readonly IPromptResearchService _promptResearchService;
        private readonly ILogger<PromptResearchController> _logger;

        public PromptResearchController(
            IPromptResearchService promptResearchService,
            ILogger<PromptResearchController> logger)
        {
            _promptResearchService = promptResearchService;
            _logger = logger;
        }

        /// <summary>
        /// Generate prompt research suggestions for a topic
        /// </summary>
        [HttpPost("generate")]
        public async Task<IActionResult> GeneratePromptResearch([FromBody] GeneratePromptResearchRequest request)
        {
            try
            {
                var prompts = await _promptResearchService.GeneratePromptResearchAsync(
                    request.ProjectId,
                    request.Topic,
                    request.Industry,
                    request.Audience,
                    null, // businessType
                    request.Count
                );

                return Ok(new { success = true, count = prompts.Count, data = prompts });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating prompt research");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Get prompt research data for a project with optional topic filter
        /// </summary>
        [HttpGet("project/{projectId}")]
        public async Task<IActionResult> GetPromptResearch(
            int projectId,
            [FromQuery] string? topic = null)
        {
            try
            {
                var prompts = await _promptResearchService.GetPromptResearchAsync(projectId, topic);
                return Ok(new { success = true, count = prompts.Count, data = prompts });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving prompt research");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Categorize prompts for a project
        /// </summary>
        [HttpGet("project/{projectId}/categorize")]
        public async Task<IActionResult> CategorizePrompts(int projectId)
        {
            try
            {
                var categorized = await _promptResearchService.CategorizePromptsAsync(projectId);
                return Ok(new { success = true, data = categorized });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error categorizing prompts for project {ProjectId}", projectId);
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Update prompt research status
        /// </summary>
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdatePromptResearchStatusRequest request)
        {
            try
            {
                var success = await _promptResearchService.UpdatePromptStatusAsync(id, request.Status);
                if (!success)
                    return NotFound(new { success = false, error = "Prompt research not found" });

                return Ok(new { success = true, message = "Status updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating prompt research status {Id}", id);
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Analyze prompt relevance for a project
        /// </summary>
        [HttpPost("project/{projectId}/analyze")]
        public async Task<IActionResult> AnalyzeRelevance(int projectId, [FromBody] AnalyzeRelevanceRequest request)
        {
            try
            {
                var result = await _promptResearchService.AnalyzePromptRelevanceAsync(
                    projectId,
                    request.PromptText
                );

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing prompt relevance for project {ProjectId}", projectId);
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Delete prompt research entry
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePromptResearch(int id)
        {
            try
            {
                var success = await _promptResearchService.DeletePromptResearchAsync(id);
                if (!success)
                    return NotFound(new { success = false, error = "Prompt research not found" });

                return Ok(new { success = true, message = "Prompt research deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting prompt research {Id}", id);
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }
    }
}
