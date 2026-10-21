using System.Security.Claims;
using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppBackendBasic.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LlmController : ControllerBase
{
    private readonly ILlmService _llmService;
    private readonly ILogger<LlmController> _logger;

    public LlmController(ILlmService llmService, ILogger<LlmController> logger)
    {
        _llmService = llmService;
        _logger = logger;
    }

    /// <summary>
    /// Send a message to ChatGPT (with optional image)
    /// </summary>
    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] SendLlmMessageRequest request)
    {
        var userId = GetUserId();
        if (userId == 0)
        {
            return Unauthorized(new { message = "Invalid user ID in token." });
        }

        try
        {
            var response = await _llmService.SendMessageAsync(userId, request);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "LLM request failed for user {UserId}", userId);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in LLM request for user {UserId}", userId);
            return StatusCode(500, new { message = "An error occurred while processing your request." });
        }
    }

    /// <summary>
    /// Get a specific conversation with all messages
    /// </summary>
    [HttpGet("conversations/{conversationId}")]
    public async Task<IActionResult> GetConversation(int conversationId)
    {
        var userId = GetUserId();
        if (userId == 0)
        {
            return Unauthorized(new { message = "Invalid user ID in token." });
        }

        try
        {
            var conversation = await _llmService.GetConversationAsync(userId, conversationId);
            return Ok(conversation);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving conversation {ConversationId} for user {UserId}", conversationId, userId);
            return StatusCode(500, new { message = "An error occurred while retrieving the conversation." });
        }
    }

    /// <summary>
    /// Get all conversations for the current user
    /// </summary>
    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = GetUserId();
        if (userId == 0)
        {
            return Unauthorized(new { message = "Invalid user ID in token." });
        }

        try
        {
            var conversations = await _llmService.GetUserConversationsAsync(userId, page, pageSize);
            return Ok(new { conversations, page, pageSize, total = conversations.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving conversations for user {UserId}", userId);
            return StatusCode(500, new { message = "An error occurred while retrieving conversations." });
        }
    }

    /// <summary>
    /// Create a new conversation
    /// </summary>
    [HttpPost("conversations")]
    public async Task<IActionResult> CreateConversation([FromBody] CreateConversationRequest request)
    {
        var userId = GetUserId();
        if (userId == 0)
        {
            return Unauthorized(new { message = "Invalid user ID in token." });
        }

        try
        {
            var conversation = await _llmService.CreateConversationAsync(userId, request);
            return CreatedAtAction(nameof(GetConversation), new { conversationId = conversation.Id }, conversation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating conversation for user {UserId}", userId);
            return StatusCode(500, new { message = "An error occurred while creating the conversation." });
        }
    }

    /// <summary>
    /// Delete a conversation
    /// </summary>
    [HttpDelete("conversations/{conversationId}")]
    public async Task<IActionResult> DeleteConversation(int conversationId)
    {
        var userId = GetUserId();
        if (userId == 0)
        {
            return Unauthorized(new { message = "Invalid user ID in token." });
        }

        try
        {
            var deleted = await _llmService.DeleteConversationAsync(userId, conversationId);
            if (!deleted)
            {
                return NotFound(new { message = "Conversation not found." });
            }

            return Ok(new { message = "Conversation deleted successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting conversation {ConversationId} for user {UserId}", conversationId, userId);
            return StatusCode(500, new { message = "An error occurred while deleting the conversation." });
        }
    }

    /// <summary>
    /// Get current usage and quota information
    /// </summary>
    [HttpGet("usage")]
    public async Task<IActionResult> GetUsage()
    {
        var userId = GetUserId();
        if (userId == 0)
        {
            return Unauthorized(new { message = "Invalid user ID in token." });
        }

        try
        {
            var usage = await _llmService.GetUsageAsync(userId);
            return Ok(usage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving usage for user {UserId}", userId);
            return StatusCode(500, new { message = "An error occurred while retrieving usage information." });
        }
    }

    /// <summary>
    /// Check if user can make a request (rate limit check)
    /// </summary>
    [HttpGet("can-request")]
    public async Task<IActionResult> CanRequest()
    {
        var userId = GetUserId();
        if (userId == 0)
        {
            return Unauthorized(new { message = "Invalid user ID in token." });
        }

        try
        {
            var canRequest = await _llmService.CheckRateLimitAsync(userId);
            return Ok(new { canRequest });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking rate limit for user {UserId}", userId);
            return StatusCode(500, new { message = "An error occurred while checking rate limits." });
        }
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }
}
