using System.Security.Claims;
using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppBackendBasic.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LlmProvidersController : ControllerBase
    {
        private readonly ILlmProviderService _llmProviderService;
        private readonly ILogger<LlmProvidersController> _logger;

        public LlmProvidersController(
            ILlmProviderService llmProviderService,
            ILogger<LlmProvidersController> logger)
        {
            _llmProviderService = llmProviderService;
            _logger = logger;
        }

        /// <summary>
        /// Get list of available LLM providers
        /// </summary>
        [HttpGet("providers")]
        public async Task<IActionResult> GetProviders()
        {
            try
            {
                var providers = await _llmProviderService.GetAvailableProvidersAsync();
                return Ok(new { providers });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available providers");
                return StatusCode(500, new { message = "An error occurred while retrieving providers." });
            }
        }

        /// <summary>
        /// Get LLM cost tracking and analytics
        /// </summary>
        [HttpGet("cost-tracking")]
        public async Task<IActionResult> GetCostTracking(
            [FromQuery] int? projectId = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var userId = GetUserId();
            if (userId == 0)
            {
                return Unauthorized(new { message = "Invalid user ID in token." });
            }

            try
            {
                var costData = await _llmProviderService.GetCostTrackingAsync(
                    userId, 
                    projectId, 
                    startDate, 
                    endDate);

                return Ok(costData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cost tracking for user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while retrieving cost data." });
            }
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }
}
