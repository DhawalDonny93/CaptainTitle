using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Interfaces;
using System.Security.Claims;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AppBackendBasic.API.Controllers;

[ApiController]
[Route("api/onboarding")]
[AllowAnonymous]
public class OnboardingController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IOnboardingService _onboardingService;
    private readonly ILlmProviderService _llmProviderService;
    private readonly IPromptTrackingService _promptTrackingService;
    private readonly ILogger<OnboardingController> _logger;

    public OnboardingController(
        ApplicationDbContext context,
        IOnboardingService onboardingService,
        ILlmProviderService llmProviderService,
        IPromptTrackingService promptTrackingService,
        ILogger<OnboardingController> logger)
    {
        _context = context;
        _onboardingService = onboardingService;
        _llmProviderService = llmProviderService;
        _promptTrackingService = promptTrackingService;
        _logger = logger;
    }

    /// <summary>
    /// Sync offline onboarding data from client (iOS app)
    /// </summary>
    [HttpPost("sync")]
    public async Task<IActionResult> SyncOnboardingData([FromBody] OnboardingSyncRequestDto request)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userInt))
            {
                return Unauthorized(new OnboardingSyncErrorDto
                {
                    Success = false,
                    Error = "Authentication required",
                    Message = "Please provide a valid access token"
                });
            }

            // Validate request
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new OnboardingSyncErrorDto
                {
                    Success = false,
                    Error = "Invalid onboarding data",
                    Message = "Request validation failed",
                    Details = new OnboardingSyncErrorDetailsDto
                    {
                        MissingFields = errors
                    }
                });
            }

            // Check if already synced (optional - service handles this)
            var existingSync = await _onboardingService.GetUserOnboardingSyncAsync(userInt);
            if (existingSync != null)
            {
                _logger.LogInformation("User {UserId} re-syncing onboarding data (previous sync: {SyncedAt})", 
                    userInt, existingSync.SyncedAt);
            }

            // Sync the data
            var response = await _onboardingService.SyncOnboardingDataAsync(userInt, request);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error during onboarding sync");
            return BadRequest(new OnboardingSyncErrorDto
            {
                Success = false,
                Error = "Invalid onboarding data",
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing onboarding data");
            return StatusCode(500, new OnboardingSyncErrorDto
            {
                Success = false,
                Error = "Internal server error",
                Message = "Failed to process onboarding data"
            });
        }
    }

    /// <summary>
    /// Get user's onboarding configuration
    /// </summary>
    [HttpGet("config")]
    [AllowAnonymous]
    public async Task<IActionResult> GetOnboardingConfiguration()
    {
        try
        {

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(userIdClaim, out var userInt);
        // Get onboarding progress
        var onboardingProgress = await _onboardingService.GetUserOnboardingProgressAsync(userInt);
        var nextStep = onboardingProgress.Steps.FirstOrDefault(s => !s.IsCompleted);
        
            // Get all onboarding steps with questions and media
            var allSteps = await _context.OnboardingSteps
                .Include(s => s.Questions.Where(q => q.IsActive))
                .Where(s => s.IsActive)
                .OrderBy(s => s.DisplayOrder)
                .AsNoTracking()
                .ToListAsync();
        
            var OnboardingConfig = new OnboardingSummaryDto
        {
            IsCompleted = onboardingProgress.IsOnboardingCompleted,
            TotalSteps = onboardingProgress.TotalSteps,
            CompletedSteps = onboardingProgress.CompletedSteps,
            RemainingSteps = onboardingProgress.TotalSteps - onboardingProgress.CompletedSteps,
            CompletionPercentage = onboardingProgress.CompletionPercentage,
            NextStepId = nextStep?.Id,
            NextStepName = nextStep?.StepName,
            Steps = allSteps.Select(s => new OnboardingStepConfigDto
            {
                Id = s.Id,
                StepKey = s.StepKey,
                StepName = s.StepName,
                Description = s.Description,
                StepType = s.StepType,
                MediaUrl = s.MediaUrl,
                MediaType = s.MediaType,
                ContentHtml = s.ContentHtml,
                DisplayOrder = s.DisplayOrder,
                IsRequired = s.IsRequired,
                Questions = s.Questions.OrderBy(q => q.DisplayOrder).Select(q => new OnboardingQuestionConfigDto
                {
                    Id = q.Id,
                    QuestionKey = q.QuestionKey,
                    QuestionText = q.QuestionText,
                    QuestionType = q.QuestionType,
                    ValidationRules = q.ValidationRules,
                    Options = q.Options,
                    Placeholder = q.Placeholder,
                    HelpText = q.HelpText,
                    IsRequired = q.IsRequired,
                    DisplayOrder = q.DisplayOrder
                }).ToList()
            }).ToList()
        };

            return Ok(new { success = true, data = OnboardingConfig });
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting onboarding progress");
            return StatusCode(500, new { success = false, message = "Error retrieving onboarding progress" });
        }
    }

    /// <summary>
    /// Get user's onboarding progress
    /// </summary>
    [HttpGet("progress")]
    public async Task<IActionResult> GetOnboardingProgress()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userInt))
            {
                return Unauthorized(new { success = false, message = "Authentication required" });
            }

            var progress = await _onboardingService.GetUserOnboardingProgressAsync(userInt);
            return Ok(new { success = true, data = progress });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting onboarding progress");
            return StatusCode(500, new { success = false, message = "Error retrieving onboarding progress" });
        }
    }

    /// <summary>
    /// Get specific onboarding step
    /// </summary>
    [AllowAnonymous]
    [HttpGet("step/{stepId}")]
    public async Task<IActionResult> GetOnboardingStep(int stepId)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userInt))
            {
                return Unauthorized(new { success = false, message = "Authentication required" });
            }

            var step = await _onboardingService.GetOnboardingStepAsync(userInt, stepId);
            if (step == null)
            {
                return NotFound(new { success = false, message = "Onboarding step not found" });
            }

            return Ok(new { success = true, data = step });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting onboarding step {StepId}", stepId);
            return StatusCode(500, new { success = false, message = "Error retrieving onboarding step" });
        }
    }

    /// <summary>
    /// Submit answers for onboarding step (legacy method - use sync for offline-first)
    /// </summary>
    [HttpPost("submit")]
    public async Task<IActionResult> SubmitOnboardingAnswers([FromBody] SubmitOnboardingAnswersDto request)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userInt))
            {
                return Unauthorized(new { success = false, message = "Authentication required" });
            }

            var result = await _onboardingService.SubmitOnboardingAnswersAsync(userInt, request);
            if (!result)
            {
                return BadRequest(new { success = false, message = "Failed to submit answers" });
            }

            return Ok(new { success = true, message = "Answers submitted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting onboarding answers");
            return StatusCode(500, new { success = false, message = "Error submitting answers" });
        }
    }

    /// <summary>
    /// Mark onboarding as completed (legacy method - use sync for offline-first)
    /// </summary>
    [HttpPost("complete")]
    public async Task<IActionResult> CompleteOnboarding()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userInt))
            {
                return Unauthorized(new { success = false, message = "Authentication required" });
            }

            var result = await _onboardingService.CompleteOnboardingAsync(userInt);
            if (!result)
            {
                return BadRequest(new { success = false, message = "Failed to complete onboarding" });
            }

            return Ok(new { success = true, message = "Onboarding completed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing onboarding");
            return StatusCode(500, new { success = false, message = "Error completing onboarding" });
        }
    }

    /// <summary>
    /// Generate 5 custom prompts for first-time users using ChatGPT
    /// </summary>
    [HttpPost("generate-prompts")]
    public async Task<IActionResult> GenerateOnboardingPrompts([FromBody] GenerateOnboardingPromptsRequest request)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { success = false, message = "Authentication required" });
            }

            // Get user info
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound(new { success = false, message = "User not found" });
            }

            // Get project info
            var project = await _context.Projects.FindAsync(request.ProjectId);
            if (project == null)
            {
                return NotFound(new { success = false, message = "Project not found" });
            }

            // Build system prompt for ChatGPT
            var systemPrompt = @"You are an AI assistant helping users create effective prompts for tracking their brand visibility across AI platforms like ChatGPT, Claude, and Gemini.

Generate 5 diverse, high-quality prompts that would help the user understand how AI platforms respond to queries related to their industry.

Return ONLY a valid JSON array with this exact structure:
[
  {
    ""promptText"": ""The actual question or prompt"",
    ""topic"": ""Brief topic category"",
    ""rationale"": ""Why this prompt is valuable for tracking"",
    ""suggestedPlatforms"": [""ChatGPT"", ""Claude"", ""Gemini""]
  }
]

Make prompts:
- Relevant to the user's industry
- Natural and conversational
- Likely to trigger brand mentions
- Diverse in topic coverage
- Actionable for visibility tracking";

            var userPrompt = $@"Generate 5 prompts for:
Company: {request.CompanyName ?? project.Name}
Industry: {request.Industry ?? "general"}
Target Audience: {request.TargetAudience ?? "general consumers"}

Focus on prompts that would naturally lead to mentions of products, services, or solutions in this industry.";

            // Call ChatGPT
            var llmRequest = new LlmRequestDto
            {
                ProjectId = request.ProjectId,
                Provider = "OpenAI",
                Model = "gpt-4",
                RequestType = "OnboardingPromptGeneration",
                SystemPrompt = systemPrompt,
                PromptText = userPrompt,
                Temperature = 0.8m,
                MaxTokens = 2000
            };

            var llmResponse = await _llmProviderService.SendRequestAsync(llmRequest);

            if (!llmResponse.Success || string.IsNullOrEmpty(llmResponse.ResponseText))
            {
                return StatusCode(500, new GenerateOnboardingPromptsResponse
                {
                    Success = false,
                    ErrorMessage = "Failed to generate prompts from LLM"
                });
            }

            // Parse JSON response
            List<GeneratedPrompt> prompts;
            try
            {
                // Clean response - remove markdown code blocks if present
                var cleanedResponse = llmResponse.ResponseText.Trim();
                if (cleanedResponse.StartsWith("```json"))
                {
                    cleanedResponse = cleanedResponse.Substring(7);
                }
                if (cleanedResponse.StartsWith("```"))
                {
                    cleanedResponse = cleanedResponse.Substring(3);
                }
                if (cleanedResponse.EndsWith("```"))
                {
                    cleanedResponse = cleanedResponse.Substring(0, cleanedResponse.Length - 3);
                }
                cleanedResponse = cleanedResponse.Trim();

                prompts = System.Text.Json.JsonSerializer.Deserialize<List<GeneratedPrompt>>(cleanedResponse) ?? new List<GeneratedPrompt>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse LLM response: {Response}", llmResponse.ResponseText);
                return StatusCode(500, new GenerateOnboardingPromptsResponse
                {
                    Success = false,
                    ErrorMessage = "Failed to parse generated prompts"
                });
            }

            return Ok(new GenerateOnboardingPromptsResponse
            {
                Success = true,
                Prompts = prompts
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating onboarding prompts");
            return StatusCode(500, new GenerateOnboardingPromptsResponse
            {
                Success = false,
                ErrorMessage = "Internal server error"
            });
        }
    }

    /// <summary>
    /// Save selected onboarding prompts and trigger visibility jobs
    /// </summary>
    [HttpPost("save-prompts")]
    public async Task<IActionResult> SaveOnboardingPrompts([FromBody] SaveOnboardingPromptsRequest request)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var teamIdClaim = User.FindFirst("team_id")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId) ||
                string.IsNullOrEmpty(teamIdClaim) || !int.TryParse(teamIdClaim, out var teamId))
            {
                return Unauthorized(new { success = false, message = "Authentication required" });
            }

            var promptIds = new List<int>();

            // Create each selected prompt
            foreach (var selectedPrompt in request.SelectedPrompts)
            {
                var prompt = await _promptTrackingService.CreatePromptAsync(
                    request.ProjectId,
                    teamId,
                    userId,
                    selectedPrompt.PromptText,
                    selectedPrompt.Topic,
                    selectedPrompt.Platforms,
                    isCompetitor: false
                );

                promptIds.Add(prompt.Id);

                // Trigger visibility check job for this prompt
                Hangfire.BackgroundJob.Enqueue<IPromptTrackingService>(
                    service => service.CheckPromptVisibilityAsync(userId, prompt.Id)
                );
            }

            // Mark user as no longer first login
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.IsFirstLogin = false;
                await _context.SaveChangesAsync();
            }

            return Ok(new SaveOnboardingPromptsResponse
            {
                Success = true,
                PromptsCreated = promptIds.Count,
                PromptIds = promptIds
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving onboarding prompts");
            return StatusCode(500, new SaveOnboardingPromptsResponse
            {
                Success = false,
                ErrorMessage = "Internal server error"
            });
        }
    }
}
