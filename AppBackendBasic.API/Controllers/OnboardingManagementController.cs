using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Services.Onboarding;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppBackendBasic.API.Controllers;

[ApiController]
[Route("api/admin/onboarding")]
[Authorize(Roles = "Admin")] // Restrict to admin users only
public class OnboardingManagementController : ControllerBase
{
    private readonly OnboardingManagementService _managementService;
    private readonly ILogger<OnboardingManagementController> _logger;

    public OnboardingManagementController(
        OnboardingManagementService managementService,
        ILogger<OnboardingManagementController> logger)
    {
        _managementService = managementService;
        _logger = logger;
    }

    #region Onboarding Steps

    /// <summary>
    /// Get all onboarding steps
    /// </summary>
    [HttpGet("steps")]
    public async Task<IActionResult> GetAllSteps([FromQuery] bool includeInactive = false)
    {
        try
        {
            var steps = await _managementService.GetAllStepsAsync(includeInactive);
            return Ok(new { success = true, data = steps });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting onboarding steps");
            return StatusCode(500, new { success = false, message = "Error retrieving onboarding steps" });
        }
    }

    /// <summary>
    /// Get onboarding step by ID
    /// </summary>
    [HttpGet("steps/{stepId}")]
    public async Task<IActionResult> GetStepById(int stepId, [FromQuery] bool includeInactive = false)
    {
        try
        {
            var step = await _managementService.GetStepByIdAsync(stepId, includeInactive);
            
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
    /// Create a new onboarding step
    /// </summary>
    [HttpPost("steps")]
    public async Task<IActionResult> CreateStep([FromBody] CreateOnboardingStepDto dto)
    {
        try
        {
            var step = await _managementService.CreateStepAsync(dto);
            return CreatedAtAction(nameof(GetStepById), new { stepId = step.Id }, 
                new { success = true, data = step, message = "Onboarding step created successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating onboarding step");
            return StatusCode(500, new { success = false, message = "Error creating onboarding step" });
        }
    }

    /// <summary>
    /// Update an existing onboarding step
    /// </summary>
    [HttpPut("steps/{stepId}")]
    public async Task<IActionResult> UpdateStep(int stepId, [FromBody] UpdateOnboardingStepDto dto)
    {
        try
        {
            if (stepId != dto.Id)
            {
                return BadRequest(new { success = false, message = "Step ID mismatch" });
            }

            var step = await _managementService.UpdateStepAsync(dto);
            return Ok(new { success = true, data = step, message = "Onboarding step updated successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating onboarding step {StepId}", stepId);
            return StatusCode(500, new { success = false, message = "Error updating onboarding step" });
        }
    }

    /// <summary>
    /// Delete an onboarding step (soft delete)
    /// </summary>
    [HttpDelete("steps/{stepId}")]
    public async Task<IActionResult> DeleteStep(int stepId)
    {
        try
        {
            var result = await _managementService.DeleteStepAsync(stepId);
            
            if (!result)
            {
                return NotFound(new { success = false, message = "Onboarding step not found" });
            }

            return Ok(new { success = true, message = "Onboarding step deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting onboarding step {StepId}", stepId);
            return StatusCode(500, new { success = false, message = "Error deleting onboarding step" });
        }
    }

    /// <summary>
    /// Reorder onboarding steps
    /// </summary>
    [HttpPost("steps/reorder")]
    public async Task<IActionResult> ReorderSteps([FromBody] ReorderStepsDto dto)
    {
        try
        {
            var result = await _managementService.ReorderStepsAsync(dto);
            return Ok(new { success = true, message = "Onboarding steps reordered successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering onboarding steps");
            return StatusCode(500, new { success = false, message = "Error reordering onboarding steps" });
        }
    }

    #endregion

    #region Onboarding Questions

    /// <summary>
    /// Get questions for a specific step
    /// </summary>
    [HttpGet("steps/{stepId}/questions")]
    public async Task<IActionResult> GetQuestionsByStepId(int stepId, [FromQuery] bool includeInactive = false)
    {
        try
        {
            var questions = await _managementService.GetQuestionsByStepIdAsync(stepId, includeInactive);
            return Ok(new { success = true, data = questions });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting questions for step {StepId}", stepId);
            return StatusCode(500, new { success = false, message = "Error retrieving questions" });
        }
    }

    /// <summary>
    /// Get question by ID
    /// </summary>
    [HttpGet("questions/{questionId}")]
    public async Task<IActionResult> GetQuestionById(int questionId, [FromQuery] bool includeInactive = false)
    {
        try
        {
            var question = await _managementService.GetQuestionByIdAsync(questionId, includeInactive);
            
            if (question == null)
            {
                return NotFound(new { success = false, message = "Question not found" });
            }

            return Ok(new { success = true, data = question });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting question {QuestionId}", questionId);
            return StatusCode(500, new { success = false, message = "Error retrieving question" });
        }
    }

    /// <summary>
    /// Create a new onboarding question
    /// </summary>
    [HttpPost("questions")]
    public async Task<IActionResult> CreateQuestion([FromBody] CreateOnboardingQuestionDto dto)
    {
        try
        {
            var question = await _managementService.CreateQuestionAsync(dto);
            return CreatedAtAction(nameof(GetQuestionById), new { questionId = question.Id }, 
                new { success = true, data = question, message = "Question created successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating question");
            return StatusCode(500, new { success = false, message = "Error creating question" });
        }
    }

    /// <summary>
    /// Update an existing onboarding question
    /// </summary>
    [HttpPut("questions/{questionId}")]
    public async Task<IActionResult> UpdateQuestion(int questionId, [FromBody] UpdateOnboardingQuestionDto dto)
    {
        try
        {
            if (questionId != dto.Id)
            {
                return BadRequest(new { success = false, message = "Question ID mismatch" });
            }

            var question = await _managementService.UpdateQuestionAsync(dto);
            return Ok(new { success = true, data = question, message = "Question updated successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating question {QuestionId}", questionId);
            return StatusCode(500, new { success = false, message = "Error updating question" });
        }
    }

    /// <summary>
    /// Delete an onboarding question (soft delete)
    /// </summary>
    [HttpDelete("questions/{questionId}")]
    public async Task<IActionResult> DeleteQuestion(int questionId)
    {
        try
        {
            var result = await _managementService.DeleteQuestionAsync(questionId);
            
            if (!result)
            {
                return NotFound(new { success = false, message = "Question not found" });
            }

            return Ok(new { success = true, message = "Question deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting question {QuestionId}", questionId);
            return StatusCode(500, new { success = false, message = "Error deleting question" });
        }
    }

    /// <summary>
    /// Reorder questions within a step
    /// </summary>
    [HttpPost("steps/{stepId}/questions/reorder")]
    public async Task<IActionResult> ReorderQuestions(int stepId, [FromBody] ReorderQuestionsDto dto)
    {
        try
        {
            if (stepId != dto.StepId)
            {
                return BadRequest(new { success = false, message = "Step ID mismatch" });
            }

            var result = await _managementService.ReorderQuestionsAsync(dto);
            return Ok(new { success = true, message = "Questions reordered successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering questions for step {StepId}", stepId);
            return StatusCode(500, new { success = false, message = "Error reordering questions" });
        }
    }

    #endregion

    #region Bulk Operations

    /// <summary>
    /// Create a step with questions in one operation
    /// </summary>
    [HttpPost("steps/with-questions")]
    public async Task<IActionResult> CreateStepWithQuestions([FromBody] CreateStepWithQuestionsDto dto)
    {
        try
        {
            var step = await _managementService.CreateStepWithQuestionsAsync(dto);
            return CreatedAtAction(nameof(GetStepById), new { stepId = step.Id }, 
                new { success = true, data = step, message = "Step with questions created successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating step with questions");
            return StatusCode(500, new { success = false, message = "Error creating step with questions" });
        }
    }

    #endregion

    #region Statistics

    /// <summary>
    /// Get onboarding statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        try
        {
            var stats = await _managementService.GetStatisticsAsync();
            return Ok(new { success = true, data = stats });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting onboarding statistics");
            return StatusCode(500, new { success = false, message = "Error retrieving statistics" });
        }
    }

    #endregion
}
