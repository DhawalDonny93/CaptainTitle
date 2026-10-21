using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Services.Onboarding;

public class OnboardingService : IOnboardingService
{
    private readonly IOnboardingRepository _onboardingRepository;
    private readonly IUserRepository _userRepository;
    private readonly OnboardingDataPopulationService _dataPopulationService;
    private readonly ILogger<OnboardingService> _logger;

    public OnboardingService(
        IOnboardingRepository onboardingRepository,
        IUserRepository userRepository,
        OnboardingDataPopulationService dataPopulationService,
        ILogger<OnboardingService> logger)
    {
        _onboardingRepository = onboardingRepository;
        _userRepository = userRepository;
        _dataPopulationService = dataPopulationService;
        _logger = logger;
    }

    public async Task<OnboardingProgressDto> GetUserOnboardingProgressAsync(int userId)
    {
        try
        {
            var allSteps = await _onboardingRepository.GetAllStepsAsync();
            var userProgress = await _onboardingRepository.GetUserProgressAsync(userId);
            var userAnswers = await _onboardingRepository.GetUserAnswersAsync(userId);

            var stepDtos = new List<OnboardingStepDto>();

            foreach (var step in allSteps)
            {
                var progress = userProgress.FirstOrDefault(p => p.OnboardingStepId == step.Id);
                var stepAnswers = userAnswers.Where(a => a.OnboardingQuestion.OnboardingStepId == step.Id).ToList();

                var questionDtos = step.Questions.OrderBy(q => q.DisplayOrder).Select(q =>
                {
                    var answer = stepAnswers.FirstOrDefault(a => a.OnboardingQuestionId == q.Id);
                    return new OnboardingQuestionDto
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
                        DisplayOrder = q.DisplayOrder,
                        UserAnswer = answer?.AnswerValue
                    };
                }).ToList();

                stepDtos.Add(new OnboardingStepDto
                {
                    Id = step.Id,
                    StepKey = step.StepKey,
                    StepName = step.StepName,
                    Description = step.Description,
                    DisplayOrder = step.DisplayOrder,
                    IsRequired = step.IsRequired,
                    IsCompleted = progress?.IsCompleted ?? false,
                    CompletedAt = progress?.CompletedAt,
                    Questions = questionDtos
                });
            }

            var completedSteps = stepDtos.Count(s => s.IsCompleted);
            var totalSteps = stepDtos.Count;

            return new OnboardingProgressDto
            {
                IsOnboardingCompleted = completedSteps == totalSteps && totalSteps > 0,
                TotalSteps = totalSteps,
                CompletedSteps = completedSteps,
                CompletionPercentage = totalSteps > 0 ? (double)completedSteps / totalSteps * 100 : 0,
                Steps = stepDtos
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting onboarding progress for user {UserId}", userId);
            throw;
        }
    }

    public async Task<OnboardingStepDto?> GetOnboardingStepAsync(int userId, int stepId)
    {
        try
        {
            var step = await _onboardingRepository.GetStepByIdAsync(stepId);
            if (step == null)
                return null;

            var progress = await _onboardingRepository.GetUserStepProgressAsync(userId, stepId);
            var userAnswers = await _onboardingRepository.GetUserAnswersByStepAsync(userId, stepId);

            var questionDtos = step.Questions.OrderBy(q => q.DisplayOrder).Select(q =>
            {
                var answer = userAnswers.FirstOrDefault(a => a.OnboardingQuestionId == q.Id);
                return new OnboardingQuestionDto
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
                    DisplayOrder = q.DisplayOrder,
                    UserAnswer = answer?.AnswerValue
                };
            }).ToList();

            return new OnboardingStepDto
            {
                Id = step.Id,
                StepKey = step.StepKey,
                StepName = step.StepName,
                Description = step.Description,
                DisplayOrder = step.DisplayOrder,
                IsRequired = step.IsRequired,
                IsCompleted = progress?.IsCompleted ?? false,
                CompletedAt = progress?.CompletedAt,
                Questions = questionDtos
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting onboarding step {StepId} for user {UserId}", stepId, userId);
            throw;
        }
    }

    public async Task<bool> SubmitOnboardingAnswersAsync(int userId, SubmitOnboardingAnswersDto request)
    {
        try
        {
            var step = await _onboardingRepository.GetStepByIdAsync(request.OnboardingStepId);
            if (step == null)
            {
                _logger.LogWarning("Onboarding step {StepId} not found", request.OnboardingStepId);
                return false;
            }

            // Validate all required questions are answered
            var requiredQuestions = step.Questions.Where(q => q.IsRequired).ToList();
            var answeredQuestionIds = request.Answers.Select(a => a.QuestionId).ToHashSet();

            foreach (var requiredQuestion in requiredQuestions)
            {
                if (!answeredQuestionIds.Contains(requiredQuestion.Id))
                {
                    _logger.LogWarning("Required question {QuestionId} not answered", requiredQuestion.Id);
                    return false;
                }
            }

            // Save answers
            var answers = request.Answers.Select(a => new UserOnboardingAnswer
            {
                UserId = userId,
                OnboardingQuestionId = a.QuestionId,
                AnswerValue = a.AnswerValue
            }).ToList();

            await _onboardingRepository.SaveAnswersAsync(answers);

            // Mark step as completed
            await _onboardingRepository.MarkStepAsCompletedAsync(userId, request.OnboardingStepId);

            // Process onboarding answers and populate relevant tables
            await _dataPopulationService.ProcessOnboardingAnswersAsync(userId, step.StepKey, answers);

            _logger.LogInformation("Onboarding answers submitted for user {UserId}, step {StepId}", userId, request.OnboardingStepId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting onboarding answers for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> CompleteOnboardingAsync(int userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", userId);
                return false;
            }

            var progress = await GetUserOnboardingProgressAsync(userId);
            
            if (!progress.IsOnboardingCompleted)
            {
                _logger.LogWarning("User {UserId} has not completed all onboarding steps", userId);
                return false;
            }

            user.OnboardingCompleted = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("Onboarding completed for user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing onboarding for user {UserId}", userId);
            throw;
        }
    }

    public async Task<OnboardingSyncResponseDto> SyncOnboardingDataAsync(int userId, OnboardingSyncRequestDto request)
    {
        try
        {
            _logger.LogInformation("Starting onboarding sync for user {UserId}, version {Version}, {StepCount} steps", 
                userId, request.Version, request.Steps.Count);

            // Validate request
            ValidateSyncRequest(request);

            // Check if user already has synced data
            var existingSync = await _onboardingRepository.GetUserOnboardingSyncAsync(userId);
            if (existingSync != null)
            {
                _logger.LogWarning("User {UserId} already has synced onboarding data from {SyncedAt}", 
                    userId, existingSync.SyncedAt);
                
                // Option: Delete existing and re-sync (recommended for flexibility)
                await _onboardingRepository.DeleteUserOnboardingDataAsync(userId);
                _logger.LogInformation("Deleted existing onboarding data for user {UserId} to allow re-sync", userId);
            }

            var now = DateTime.UtcNow;
            int syncedSteps = 0;
            int syncedAnswers = 0;

            // Create main sync record
            var syncRecord = new UserOnboardingSync
            {
                UserId = userId,
                Version = request.Version,
                IsCompleted = true,
                CompletedAt = request.CompletedAt.ToUniversalTime(),
                SyncedAt = now,
                TotalSteps = request.Steps.Count,
                TotalAnswers = request.Steps.Sum(s => s.Answers?.Count ?? 0),
                CreatedAt = now,
                UpdatedAt = now
            };

            await _onboardingRepository.CreateUserOnboardingSyncAsync(syncRecord);

            // Process each step
            foreach (var stepDto in request.Steps)
            {
                // Get step info from database
                var step = await _onboardingRepository.GetStepByIdAsync(stepDto.StepId);
                if (step == null)
                {
                    _logger.LogWarning("Step {StepId} not found, skipping", stepDto.StepId);
                    continue;
                }

                // Create progress record
                var progressRecord = new UserOnboardingProgress
                {
                    UserId = userId,
                    OnboardingStepId = stepDto.StepId,
                    UserOnboardingSyncId = syncRecord.Id,
                    StepId = stepDto.StepId.ToString(),
                    StepKey = step.StepKey,
                    StepType = step.StepType,
                    IsCompleted = true,
                    CompletedAt = stepDto.CompletedAt.ToUniversalTime(),
                    CreatedAt = now,
                    UpdatedAt = now
                };

                await _onboardingRepository.CreateUserProgressAsync(progressRecord);
                syncedSteps++;

                // Process answers if present
                if (stepDto.Answers != null && stepDto.Answers.Any())
                {
                    foreach (var answerDto in stepDto.Answers)
                    {
                        // Get question info from database
                        var question = await _onboardingRepository.GetQuestionByIdAsync(answerDto.QuestionId);
                        if (question == null)
                        {
                            _logger.LogWarning("Question {QuestionId} not found, skipping", answerDto.QuestionId);
                            continue;
                        }

                        var answerRecord = new UserOnboardingAnswer
                        {
                            UserId = userId,
                            OnboardingQuestionId = answerDto.QuestionId,
                            UserOnboardingProgressId = progressRecord.Id,
                            QuestionId = answerDto.QuestionId.ToString(),
                            QuestionKey = question.QuestionKey,
                            AnswerValue = answerDto.AnswerValue,
                            CreatedAt = now,
                            UpdatedAt = now
                        };

                        await _onboardingRepository.CreateUserAnswerAsync(answerRecord);
                        syncedAnswers++;
                    }
                }
            }

            // Mark user's onboarding as completed
            var user = await _userRepository.GetByIdAsync(userId);
            if (user != null)
            {
                user.OnboardingCompleted = true;
                await _userRepository.UpdateAsync(user);
            }

            _logger.LogInformation("Onboarding sync completed for user {UserId}: {SyncedSteps} steps, {SyncedAnswers} answers", 
                userId, syncedSteps, syncedAnswers);

            return new OnboardingSyncResponseDto
            {
                Success = true,
                Message = "Onboarding data synced successfully",
                SyncedSteps = syncedSteps,
                SyncedAnswers = syncedAnswers,
                UserId = userId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing onboarding data for user {UserId}", userId);
            throw;
        }
    }

    public async Task<UserOnboardingSync?> GetUserOnboardingSyncAsync(int userId)
    {
        try
        {
            return await _onboardingRepository.GetUserOnboardingSyncAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting onboarding sync for user {UserId}", userId);
            throw;
        }
    }

    private void ValidateSyncRequest(OnboardingSyncRequestDto request)
    {
        // Validate completedAt is not in the future
        if (request.CompletedAt > DateTime.UtcNow.AddMinutes(5)) // Allow 5 min clock skew
        {
            throw new ArgumentException("CompletedAt cannot be in the future");
        }

        // Validate steps
        var stepIds = new HashSet<int>();
        foreach (var step in request.Steps)
        {
            // Check for duplicate stepIds
            if (!stepIds.Add(step.StepId))
            {
                throw new ArgumentException($"Duplicate stepId: {step.StepId}");
            }

            // Validate step completedAt <= overall completedAt
            if (step.CompletedAt > request.CompletedAt)
            {
                throw new ArgumentException($"Step {step.StepId} completedAt is after overall completedAt");
            }

            // Validate answers
            if (step.Answers != null && step.Answers.Any())
            {
                // Check for duplicate questionIds
                var questionIds = new HashSet<int>();
                foreach (var answer in step.Answers)
                {
                    if (!questionIds.Add(answer.QuestionId))
                    {
                        throw new ArgumentException($"Duplicate questionId in step {step.StepId}: {answer.QuestionId}");
                    }
                }
            }
        }
    }

}
