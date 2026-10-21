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
        /// Generate AI-powered content for an article
        /// </summary>
        [HttpPost("generate-content")]
        public async Task<IActionResult> GenerateContent([FromBody] ContentGenerationRequest request)
        {
            var userId = GetUserId();
            if (userId == 0)
            {
                return Unauthorized(new { message = "Invalid user ID in token." });
            }

            try
            {
                var response = await _llmProviderService.GenerateContentAsync(request);
                
                if (!response.Success)
                {
                    return BadRequest(new { message = response.ErrorMessage });
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating content for project {ProjectId}", request.ProjectId);
                return StatusCode(500, new { message = "An error occurred while generating content." });
            }
        }

        /// <summary>
        /// Test a prompt across multiple AI platforms
        /// </summary>
        [HttpPost("test-prompt")]
        public async Task<IActionResult> TestPrompt([FromBody] PromptTestRequest request)
        {
            var userId = GetUserId();
            if (userId == 0)
            {
                return Unauthorized(new { message = "Invalid user ID in token." });
            }

            try
            {
                var response = await _llmProviderService.TestPromptAsync(request);
                
                if (!response.Success)
                {
                    return BadRequest(new { message = response.ErrorMessage });
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing prompt for project {ProjectId}", request.ProjectId);
                return StatusCode(500, new { message = "An error occurred while testing the prompt." });
            }
        }

        /// <summary>
        /// Analyze citations in AI response text
        /// </summary>
        [HttpPost("analyze-citations")]
        public async Task<IActionResult> AnalyzeCitations([FromBody] CitationAnalysisRequest request)
        {
            var userId = GetUserId();
            if (userId == 0)
            {
                return Unauthorized(new { message = "Invalid user ID in token." });
            }

            try
            {
                var response = await _llmProviderService.AnalyzeCitationsAsync(request);
                
                if (!response.Success)
                {
                    return BadRequest(new { message = response.ErrorMessage });
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing citations for project {ProjectId}", request.ProjectId);
                return StatusCode(500, new { message = "An error occurred while analyzing citations." });
            }
        }

        /// <summary>
        /// Analyze sentiment of text
        /// </summary>
        [HttpPost("analyze-sentiment")]
        public async Task<IActionResult> AnalyzeSentiment([FromBody] SentimentAnalysisRequest request)
        {
            var userId = GetUserId();
            if (userId == 0)
            {
                return Unauthorized(new { message = "Invalid user ID in token." });
            }

            try
            {
                var response = await _llmProviderService.AnalyzeSentimentAsync(request);
                
                if (!response.Success)
                {
                    return BadRequest(new { message = response.ErrorMessage });
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing sentiment");
                return StatusCode(500, new { message = "An error occurred while analyzing sentiment." });
            }
        }

        /// <summary>
        /// Generate a draft reply for a community opportunity
        /// </summary>
        [HttpPost("generate-draft-reply")]
        public async Task<IActionResult> GenerateDraftReply([FromBody] DraftReplyRequest request)
        {
            var userId = GetUserId();
            if (userId == 0)
            {
                return Unauthorized(new { message = "Invalid user ID in token." });
            }

            try
            {
                var response = await _llmProviderService.GenerateDraftReplyAsync(request);
                
                if (!response.Success)
                {
                    return BadRequest(new { message = response.ErrorMessage });
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating draft reply for opportunity {OpportunityId}", request.OpportunityId);
                return StatusCode(500, new { message = "An error occurred while generating the draft reply." });
            }
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
