using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AppBackendBasic.Infrastructure.Repositories;

public class OnboardingRepository : IOnboardingRepository
{
    private readonly ApplicationDbContext _context;

    public OnboardingRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // Onboarding Steps
    public async Task<List<OnboardingStep>> GetAllStepsAsync()
    {
        return await _context.OnboardingSteps
            .Include(s => s.Questions.Where(q => q.IsActive))
            .Where(s => s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync();
    }

    public async Task<OnboardingStep?> GetStepByIdAsync(int stepId)
    {
        return await _context.OnboardingSteps
            .Include(s => s.Questions.Where(q => q.IsActive))
            .FirstOrDefaultAsync(s => s.Id == stepId && s.IsActive);
    }

    public async Task<OnboardingStep?> GetStepByKeyAsync(string stepKey)
    {
        return await _context.OnboardingSteps
            .Include(s => s.Questions.Where(q => q.IsActive))
            .FirstOrDefaultAsync(s => s.StepKey == stepKey && s.IsActive);
    }

    public async Task<List<OnboardingStep>> GetAllStepsIncludingInactiveAsync()
    {
        return await _context.OnboardingSteps
            .Include(s => s.Questions)
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync();
    }

    public async Task<OnboardingStep?> GetStepByIdIncludingInactiveAsync(int stepId)
    {
        return await _context.OnboardingSteps
            .Include(s => s.Questions)
            .FirstOrDefaultAsync(s => s.Id == stepId);
    }

    // Onboarding Steps - Write
    public async Task<OnboardingStep> CreateStepAsync(OnboardingStep step)
    {
        step.CreatedAt = DateTime.UtcNow;
        step.UpdatedAt = DateTime.UtcNow;
        
        _context.OnboardingSteps.Add(step);
        await _context.SaveChangesAsync();
        
        return step;
    }

    public async Task<OnboardingStep> UpdateStepAsync(OnboardingStep step)
    {
        var existing = await _context.OnboardingSteps.FindAsync(step.Id);
        if (existing == null)
            throw new InvalidOperationException($"Onboarding step with ID {step.Id} not found");

        existing.StepKey = step.StepKey;
        existing.StepName = step.StepName;
        existing.Description = step.Description;
        existing.DisplayOrder = step.DisplayOrder;
        existing.IsRequired = step.IsRequired;
        existing.IsActive = step.IsActive;
        // TODO: Fix UpdatedAt timestamp handling - commented out due to timezone conversion issue
        // Current issue: Need to determine if UpdatedAt should be set to current UTC time or preserved with conversion
        // Action: Verify desired behavior (always update to now, or preserve previous value), uncomment appropriate line
        // existing.UpdatedAt = DateTime.UtcNow;
        // existing.UpdatedAt = existing.UpdatedAt.ToUniversalTime();

        _context.OnboardingSteps.Update(existing);
        await _context.SaveChangesAsync();

        return existing;
    }

    public async Task<bool> DeleteStepAsync(int stepId)
    {
        var step = await _context.OnboardingSteps.FindAsync(stepId);
        if (step == null)
            return false;

        // Soft delete - set IsActive to false
        step.IsActive = false;
        step.UpdatedAt = DateTime.UtcNow;
        
        _context.OnboardingSteps.Update(step);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ReorderStepsAsync(Dictionary<int, int> stepOrders)
    {
        foreach (var (stepId, order) in stepOrders)
        {
            var step = await _context.OnboardingSteps.FindAsync(stepId);
            if (step != null)
            {
                step.DisplayOrder = order;
                step.UpdatedAt = DateTime.UtcNow;
                _context.OnboardingSteps.Update(step);
            }
        }

        await _context.SaveChangesAsync();
        return true;
    }

    // Onboarding Questions
    public async Task<List<OnboardingQuestion>> GetQuestionsByStepIdAsync(int stepId)
    {
        return await _context.OnboardingQuestions
            .Where(q => q.OnboardingStepId == stepId && q.IsActive)
            .OrderBy(q => q.DisplayOrder)
            .ToListAsync();
    }

    public async Task<OnboardingQuestion?> GetQuestionByIdAsync(int questionId)
    {
        return await _context.OnboardingQuestions
            .FirstOrDefaultAsync(q => q.Id == questionId && q.IsActive);
    }

    public async Task<List<OnboardingQuestion>> GetQuestionsByStepIdIncludingInactiveAsync(int stepId)
    {
        return await _context.OnboardingQuestions
            .Where(q => q.OnboardingStepId == stepId) 
            .OrderBy(q => q.DisplayOrder)
            .ToListAsync();
    }

    public async Task<OnboardingQuestion?> GetQuestionByIdIncludingInactiveAsync(int questionId)
    {
        return await _context.OnboardingQuestions
            .FirstOrDefaultAsync(q => q.Id == questionId);
    }

    // Onboarding Questions - Write
    public async Task<OnboardingQuestion> CreateQuestionAsync(OnboardingQuestion question)
    {
        question.CreatedAt = DateTime.UtcNow;
        question.UpdatedAt = DateTime.UtcNow;
        
        _context.OnboardingQuestions.Add(question);
        await _context.SaveChangesAsync();
        
        return question;
    }

    public async Task<OnboardingQuestion> UpdateQuestionAsync(OnboardingQuestion question)
    {
        var existing = await _context.OnboardingQuestions.FindAsync(question.Id);
        if (existing == null)
            throw new InvalidOperationException($"Onboarding question with ID {question.Id} not found");

        existing.OnboardingStepId = question.OnboardingStepId;
        existing.QuestionKey = question.QuestionKey;
        existing.QuestionText = question.QuestionText;
        existing.QuestionType = question.QuestionType;
        existing.ValidationRules = question.ValidationRules;
        existing.Options = question.Options;
        existing.Placeholder = question.Placeholder;
        existing.HelpText = question.HelpText;
        existing.IsRequired = question.IsRequired;
        existing.DisplayOrder = question.DisplayOrder;
        existing.IsActive = question.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;

        _context.OnboardingQuestions.Update(existing);
        await _context.SaveChangesAsync();

        return existing;
    }

    public async Task<bool> DeleteQuestionAsync(int questionId)
    {
        var question = await _context.OnboardingQuestions.FindAsync(questionId);
        if (question == null)
            return false;

        // Soft delete - set IsActive to false
        question.IsActive = false;
        question.UpdatedAt = DateTime.UtcNow;
        
        _context.OnboardingQuestions.Update(question);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ReorderQuestionsAsync(int stepId, Dictionary<int, int> questionOrders)
    {
        foreach (var (questionId, order) in questionOrders)
        {
            var question = await _context.OnboardingQuestions
                .FirstOrDefaultAsync(q => q.Id == questionId && q.OnboardingStepId == stepId);
            
            if (question != null)
            {
                question.DisplayOrder = order;
                question.UpdatedAt = DateTime.UtcNow;
                _context.OnboardingQuestions.Update(question);
            }
        }

        await _context.SaveChangesAsync();
        return true;
    }

    // User Progress
    public async Task<List<UserOnboardingProgress>> GetUserProgressAsync(int userId)
    {
        return await _context.UserOnboardingProgress
            .Include(p => p.OnboardingStep)
            .Where(p => p.UserId == userId)
            .ToListAsync();
    }

    public async Task<UserOnboardingProgress?> GetUserStepProgressAsync(int userId, int? stepId)
    {
        return await _context.UserOnboardingProgress
            .FirstOrDefaultAsync(p => p.UserId == userId && p.OnboardingStepId == stepId);
    }

    public async Task CreateOrUpdateProgressAsync(UserOnboardingProgress progress)
    {
        var existing = await GetUserStepProgressAsync(progress.UserId, progress.OnboardingStepId);
        
        if (existing != null)
        {
            existing.IsCompleted = progress.IsCompleted;
            existing.CompletedAt = progress.CompletedAt;
            existing.UpdatedAt = DateTime.UtcNow;
            _context.UserOnboardingProgress.Update(existing);
        }
        else
        {
            _context.UserOnboardingProgress.Add(progress);
        }
        
        await _context.SaveChangesAsync();
    }

    public async Task MarkStepAsCompletedAsync(int userId, int stepId)
    {
        var progress = await GetUserStepProgressAsync(userId, stepId);
        
        if (progress != null)
        {
            progress.IsCompleted = true;
            progress.CompletedAt = DateTime.UtcNow;
            progress.UpdatedAt = DateTime.UtcNow;
            _context.UserOnboardingProgress.Update(progress);
        }
        else
        {
            _context.UserOnboardingProgress.Add(new UserOnboardingProgress
            {
                UserId = userId,
                OnboardingStepId = stepId,
                IsCompleted = true,
                CompletedAt = DateTime.UtcNow
            });
        }
        
        await _context.SaveChangesAsync();
    }

    // User Answers
    public async Task<List<UserOnboardingAnswer>> GetUserAnswersAsync(int userId)
    {
        return await _context.UserOnboardingAnswers
            .Include(a => a.OnboardingQuestion)
            .Where(a => a.UserId == userId)
            .ToListAsync();
    }

    public async Task<List<UserOnboardingAnswer>> GetUserAnswersByStepAsync(int userId, int stepId)
    {
        return await _context.UserOnboardingAnswers
            .Include(a => a.OnboardingQuestion)
            .Where(a => a.UserId == userId && a.OnboardingQuestion.OnboardingStepId == stepId)
            .ToListAsync();
    }

    public async Task<UserOnboardingAnswer?> GetUserAnswerAsync(int userId, int? questionId)
    {
        return await _context.UserOnboardingAnswers
            .FirstOrDefaultAsync(a => a.UserId == userId && a.OnboardingQuestionId == questionId);
    }

    public async Task SaveAnswerAsync(UserOnboardingAnswer answer)
    {
        var existing = await GetUserAnswerAsync(answer.UserId, answer.OnboardingQuestionId);
        
        if (existing != null)
        {
            existing.AnswerValue = answer.AnswerValue;
            existing.UpdatedAt = DateTime.UtcNow;
            _context.UserOnboardingAnswers.Update(existing);
        }
        else
        {
            _context.UserOnboardingAnswers.Add(answer);
        }
        
        await _context.SaveChangesAsync();
    }

    public async Task SaveAnswersAsync(List<UserOnboardingAnswer> answers)
    {
        foreach (var answer in answers)
        {
            var existing = await GetUserAnswerAsync(answer.UserId, answer.OnboardingQuestionId);
            
            if (existing != null)
            {
                existing.AnswerValue = answer.AnswerValue;
                existing.UpdatedAt = DateTime.UtcNow;
                _context.UserOnboardingAnswers.Update(existing);
            }
            else
            {
                _context.UserOnboardingAnswers.Add(answer);
            }
        }
        
        await _context.SaveChangesAsync();
    }

    // Onboarding Sync (Offline-first)
    public async Task<UserOnboardingSync?> GetUserOnboardingSyncAsync(int userId)
    {
        return await _context.UserOnboardingSyncs
            .FirstOrDefaultAsync(s => s.UserId == userId);
    }

    public async Task<UserOnboardingSync> CreateUserOnboardingSyncAsync(UserOnboardingSync sync)
    {
        _context.UserOnboardingSyncs.Add(sync);
        await _context.SaveChangesAsync();
        return sync;
    }

    public async Task<UserOnboardingProgress> CreateUserProgressAsync(UserOnboardingProgress progress)
    {
        _context.UserOnboardingProgress.Add(progress);
        await _context.SaveChangesAsync();
        return progress;
    }

    public async Task<UserOnboardingAnswer> CreateUserAnswerAsync(UserOnboardingAnswer answer)
    {
        _context.UserOnboardingAnswers.Add(answer);
        await _context.SaveChangesAsync();
        return answer;
    }

    public async Task DeleteUserOnboardingDataAsync(int userId)
    {
        // Delete in correct order due to foreign keys
        var answers = await _context.UserOnboardingAnswers
            .Where(a => a.UserId == userId)
            .ToListAsync();
        _context.UserOnboardingAnswers.RemoveRange(answers);

        var progress = await _context.UserOnboardingProgress
            .Where(p => p.UserId == userId)
            .ToListAsync();
        _context.UserOnboardingProgress.RemoveRange(progress);

        var sync = await _context.UserOnboardingSyncs
            .Where(s => s.UserId == userId)
            .ToListAsync();
        _context.UserOnboardingSyncs.RemoveRange(sync);

        await _context.SaveChangesAsync();
    }
}
