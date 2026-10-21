using System.Linq;
using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Services.Onboarding;

/// <summary>
/// Service for managing onboarding steps and questions (Admin functionality)
/// </summary>
public class OnboardingManagementService
{
    private readonly IOnboardingRepository _repository;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OnboardingManagementService> _logger;

    public OnboardingManagementService(
        IOnboardingRepository repository,
        ApplicationDbContext context,
        ILogger<OnboardingManagementService> logger)
    {
        _repository = repository;
        _context = context;
        _logger = logger;
    }

    #region Onboarding Steps

    /// <summary>
    /// Get all onboarding steps (including inactive for admin)
    /// </summary>
    public async Task<List<OnboardingStepResponseDto>> GetAllStepsAsync(bool includeInactive = false)
    {
        try
        {
            var steps = includeInactive 
                ? await _repository.GetAllStepsIncludingInactiveAsync()
                : await _repository.GetAllStepsAsync();

            return steps.Select(MapToStepResponseDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all onboarding steps");
            throw;
        }
    }

    /// <summary>
    /// Get onboarding step by ID
    /// </summary>
    public async Task<OnboardingStepResponseDto?> GetStepByIdAsync(int stepId, bool includeInactive = false)
    {
        try
        {
            var step = includeInactive
                ? await _repository.GetStepByIdIncludingInactiveAsync(stepId)
                : await _repository.GetStepByIdAsync(stepId);

            return step != null ? MapToStepResponseDto(step) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting onboarding step {StepId}", stepId);
            throw;
        }
    }

    /// <summary>
    /// Create a new onboarding step
    /// </summary>
    public async Task<OnboardingStepResponseDto> CreateStepAsync(CreateOnboardingStepDto dto)
    {
        try
        {
            // Check if step key already exists
            var existing = await _repository.GetStepByKeyAsync(dto.StepKey);
            if (existing != null)
            {
                throw new InvalidOperationException($"Onboarding step with key '{dto.StepKey}' already exists");
            }

            var step = new OnboardingStep
            {
                StepKey = dto.StepKey,
                StepName = dto.StepName,
                Description = dto.Description,
                DisplayOrder = dto.DisplayOrder,
                IsRequired = dto.IsRequired,
                IsActive = dto.IsActive
            };

            var created = await _repository.CreateStepAsync(step);
            _logger.LogInformation("Created onboarding step: {StepKey}", dto.StepKey);

            return MapToStepResponseDto(created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating onboarding step");
            throw;
        }
    }

    /// <summary>
    /// Update an existing onboarding step
    /// </summary>
    public async Task<OnboardingStepResponseDto> UpdateStepAsync(UpdateOnboardingStepDto dto)
    {
        try
        {
            var step = new OnboardingStep
            {
                Id = dto.Id,
                StepKey = dto.StepKey,
                StepName = dto.StepName,
                Description = dto.Description,
                DisplayOrder = dto.DisplayOrder,
                IsRequired = dto.IsRequired,
                IsActive = dto.IsActive
            };

            var updated = await _repository.UpdateStepAsync(step);
            _logger.LogInformation("Updated onboarding step: {StepId}", dto.Id);

            return MapToStepResponseDto(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating onboarding step {StepId}", dto.Id);
            throw;
        }
    }

    /// <summary>
    /// Delete (soft delete) an onboarding step
    /// </summary>
    public async Task<bool> DeleteStepAsync(int stepId)
    {
        try
        {
            var result = await _repository.DeleteStepAsync(stepId);
            if (result)
            {
                _logger.LogInformation("Deleted onboarding step: {StepId}", stepId);
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting onboarding step {StepId}", stepId);
            throw;
        }
    }

    /// <summary>
    /// Reorder onboarding steps
    /// </summary>
    public async Task<bool> ReorderStepsAsync(ReorderStepsDto dto)
    {
        try
        {
            var stepOrders = dto.Steps.ToDictionary(s => s.StepId, s => s.DisplayOrder);
            var result = await _repository.ReorderStepsAsync(stepOrders);
            
            if (result)
            {
                _logger.LogInformation("Reordered {Count} onboarding steps", dto.Steps.Count);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering onboarding steps");
            throw;
        }
    }

    #endregion

    #region Onboarding Questions

    /// <summary>
    /// Get questions for a specific step
    /// </summary>
    public async Task<List<OnboardingQuestionResponseDto>> GetQuestionsByStepIdAsync(int stepId, bool includeInactive = false)
    {
        try
        {
            var questions = includeInactive
                ? await _repository.GetQuestionsByStepIdIncludingInactiveAsync(stepId)
                : await _repository.GetQuestionsByStepIdAsync(stepId);

            return questions.Select(MapToQuestionResponseDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting questions for step {StepId}", stepId);
            throw;
        }
    }

    /// <summary>
    /// Get question by ID
    /// </summary>
    public async Task<OnboardingQuestionResponseDto?> GetQuestionByIdAsync(int questionId, bool includeInactive = false)
    {
        try
        {
            var question = includeInactive
                ? await _repository.GetQuestionByIdIncludingInactiveAsync(questionId)
                : await _repository.GetQuestionByIdAsync(questionId);

            return question != null ? MapToQuestionResponseDto(question) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting question {QuestionId}", questionId);
            throw;
        }
    }

    /// <summary>
    /// Create a new onboarding question
    /// </summary>
    public async Task<OnboardingQuestionResponseDto> CreateQuestionAsync(CreateOnboardingQuestionDto dto)
    {
        try
        {
            // Verify step exists
            var step = await _repository.GetStepByIdAsync(dto.OnboardingStepId);
            if (step == null)
            {
                throw new InvalidOperationException($"Onboarding step with ID {dto.OnboardingStepId} not found");
            }

            var question = new OnboardingQuestion
            {
                OnboardingStepId = dto.OnboardingStepId,
                QuestionKey = dto.QuestionKey,
                QuestionText = dto.QuestionText,
                QuestionType = dto.QuestionType,
                ValidationRules = dto.ValidationRules,
                Options = dto.Options,
                Placeholder = dto.Placeholder,
                HelpText = dto.HelpText,
                IsRequired = dto.IsRequired,
                DisplayOrder = dto.DisplayOrder,
                IsActive = dto.IsActive
            };

            var created = await _repository.CreateQuestionAsync(question);
            _logger.LogInformation("Created onboarding question: {QuestionKey} for step {StepId}", 
                dto.QuestionKey, dto.OnboardingStepId);

            return MapToQuestionResponseDto(created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating onboarding question");
            throw;
        }
    }

    /// <summary>
    /// Update an existing onboarding question
    /// </summary>
    public async Task<OnboardingQuestionResponseDto> UpdateQuestionAsync(UpdateOnboardingQuestionDto dto)
    {
        try
        {
            var question = new OnboardingQuestion
            {
                Id = dto.Id,
                OnboardingStepId = dto.OnboardingStepId,
                QuestionKey = dto.QuestionKey,
                QuestionText = dto.QuestionText,
                QuestionType = dto.QuestionType,
                ValidationRules = dto.ValidationRules,
                Options = dto.Options,
                Placeholder = dto.Placeholder,
                HelpText = dto.HelpText,
                IsRequired = dto.IsRequired,
                DisplayOrder = dto.DisplayOrder,
                IsActive = dto.IsActive
            };

            var updated = await _repository.UpdateQuestionAsync(question);
            _logger.LogInformation("Updated onboarding question: {QuestionId}", dto.Id);

            return MapToQuestionResponseDto(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating onboarding question {QuestionId}", dto.Id);
            throw;
        }
    }

    /// <summary>
    /// Delete (soft delete) an onboarding question
    /// </summary>
    public async Task<bool> DeleteQuestionAsync(int questionId)
    {
        try
        {
            var result = await _repository.DeleteQuestionAsync(questionId);
            if (result)
            {
                _logger.LogInformation("Deleted onboarding question: {QuestionId}", questionId);
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting onboarding question {QuestionId}", questionId);
            throw;
        }
    }

    /// <summary>
    /// Reorder questions within a step
    /// </summary>
    public async Task<bool> ReorderQuestionsAsync(ReorderQuestionsDto dto)
    {
        try
        {
            var questionOrders = dto.Questions.ToDictionary(q => q.QuestionId, q => q.DisplayOrder);
            var result = await _repository.ReorderQuestionsAsync(dto.StepId, questionOrders);
            
            if (result)
            {
                _logger.LogInformation("Reordered {Count} questions for step {StepId}", 
                    dto.Questions.Count, dto.StepId);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering questions for step {StepId}", dto.StepId);
            throw;
        }
    }

    #endregion

    #region Bulk Operations

    /// <summary>
    /// Create a step with questions in one transaction
    /// </summary>
    public async Task<OnboardingStepResponseDto> CreateStepWithQuestionsAsync(CreateStepWithQuestionsDto dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            // Create step
            var stepResponse = await CreateStepAsync(dto.Step);
            
            // Create questions
            foreach (var questionDto in dto.Questions)
            {
                questionDto.OnboardingStepId = stepResponse.Id;
                await CreateQuestionAsync(questionDto);
            }

            await transaction.CommitAsync();
            
            _logger.LogInformation("Created step {StepKey} with {QuestionCount} questions", 
                dto.Step.StepKey, dto.Questions.Count);

            // Reload with questions
            return (await GetStepByIdAsync(stepResponse.Id))!;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating step with questions");
            throw;
        }
    }

    #endregion

    #region Statistics

    /// <summary>
    /// Get onboarding statistics
    /// </summary>
    public async Task<OnboardingStatisticsDto> GetStatisticsAsync()
    {
        try
        {
            var allSteps = await _context.OnboardingSteps.ToListAsync();
            var activeSteps = allSteps.Where(s => s.IsActive).ToList();
            
            var allQuestions = await _context.Set<OnboardingQuestion>().ToListAsync();
            var activeQuestions = allQuestions.Where(q => q.IsActive).ToList();
            
            var allProgress = await _context.UserOnboardingProgress.ToListAsync();
            var usersStarted = allProgress.Select(p => p.UserId).Distinct().Count();
            
            // Users who completed all required steps
            var requiredStepIds = activeSteps.Where(s => s.IsRequired).Select(s => s.Id).ToList();
            var usersCompleted = allProgress
                .Where(p => p.IsCompleted && p.OnboardingStepId.HasValue && requiredStepIds.Contains(p.OnboardingStepId.Value))
                .GroupBy(p => p.UserId)
                .Count(g => g.Count() == requiredStepIds.Count);

            var completionRate = usersStarted > 0 ? (double)usersCompleted / usersStarted * 100 : 0;

            var stepStats = new List<StepStatisticsDto>();
            foreach (var step in activeSteps)
            {
                var stepProgress = allProgress.Where(p => p.OnboardingStepId == step.Id).ToList();
                var stepUsersStarted = stepProgress.Count;
                var stepUsersCompleted = stepProgress.Count(p => p.IsCompleted);
                var stepCompletionRate = stepUsersStarted > 0 
                    ? (double)stepUsersCompleted / stepUsersStarted * 100 
                    : 0;

                var completedProgress = stepProgress.Where(p => p.IsCompleted && p.CompletedAt.HasValue).ToList();
                var avgTime = completedProgress.Any()
                    ? TimeSpan.FromTicks((long)completedProgress
                        .Select(p => (p.CompletedAt!.Value - p.CreatedAt).Ticks)
                        .Average())
                    : TimeSpan.Zero;

                stepStats.Add(new StepStatisticsDto
                {
                    StepId = step.Id,
                    StepName = step.StepName,
                    UsersStarted = stepUsersStarted,
                    UsersCompleted = stepUsersCompleted,
                    CompletionRate = stepCompletionRate,
                    AverageCompletionTime = avgTime
                });
            }

            return new OnboardingStatisticsDto
            {
                TotalSteps = allSteps.Count,
                ActiveSteps = activeSteps.Count,
                TotalQuestions = allQuestions.Count,
                ActiveQuestions = activeQuestions.Count,
                TotalUsersStarted = usersStarted,
                TotalUsersCompleted = usersCompleted,
                CompletionRate = completionRate,
                StepStatistics = stepStats
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting onboarding statistics");
            throw;
        }
    }

    #endregion

    #region Helper Methods

    private OnboardingStepResponseDto MapToStepResponseDto(OnboardingStep step)
    {
        return new OnboardingStepResponseDto
        {
            Id = step.Id,
            StepKey = step.StepKey,
            StepName = step.StepName,
            Description = step.Description,
            DisplayOrder = step.DisplayOrder,
            IsRequired = step.IsRequired,
            IsActive = step.IsActive,
            CreatedAt = step.CreatedAt,
            UpdatedAt = step.UpdatedAt,
            QuestionCount = step.Questions?.Count ?? 0,
            Questions = step.Questions?.Select(MapToQuestionResponseDto).ToList() ?? new List<OnboardingQuestionResponseDto>()
        };
    }

    private OnboardingQuestionResponseDto MapToQuestionResponseDto(OnboardingQuestion question)
    {
        return new OnboardingQuestionResponseDto
        {
            Id = question.Id,
            OnboardingStepId = question.OnboardingStepId,
            QuestionKey = question.QuestionKey,
            QuestionText = question.QuestionText,
            QuestionType = question.QuestionType,
            ValidationRules = question.ValidationRules,
            Options = question.Options,
            Placeholder = question.Placeholder,
            HelpText = question.HelpText,
            IsRequired = question.IsRequired,
            DisplayOrder = question.DisplayOrder,
            IsActive = question.IsActive,
            CreatedAt = question.CreatedAt,
            UpdatedAt = question.UpdatedAt
        };
    }

    #endregion
}
