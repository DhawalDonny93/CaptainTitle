using AppBackendBasic.Core.Entities;

namespace AppBackendBasic.Core.Interfaces;

public interface IOnboardingRepository
{
    // Onboarding Steps - Read
    Task<List<OnboardingStep>> GetAllStepsAsync();
    Task<List<OnboardingStep>> GetAllStepsIncludingInactiveAsync();
    Task<OnboardingStep?> GetStepByIdAsync(int stepId);
    Task<OnboardingStep?> GetStepByIdIncludingInactiveAsync(int stepId);
    Task<OnboardingStep?> GetStepByKeyAsync(string stepKey);
    
    // Onboarding Steps - Write
    Task<OnboardingStep> CreateStepAsync(OnboardingStep step);
    Task<OnboardingStep> UpdateStepAsync(OnboardingStep step);
    Task<bool> DeleteStepAsync(int stepId);
    Task<bool> ReorderStepsAsync(Dictionary<int, int> stepOrders);
    
    // Onboarding Questions - Read
    Task<List<OnboardingQuestion>> GetQuestionsByStepIdAsync(int stepId);
    Task<List<OnboardingQuestion>> GetQuestionsByStepIdIncludingInactiveAsync(int stepId);
    Task<OnboardingQuestion?> GetQuestionByIdAsync(int questionId);
    Task<OnboardingQuestion?> GetQuestionByIdIncludingInactiveAsync(int questionId);
    
    // Onboarding Questions - Write
    Task<OnboardingQuestion> CreateQuestionAsync(OnboardingQuestion question);
    Task<OnboardingQuestion> UpdateQuestionAsync(OnboardingQuestion question);
    Task<bool> DeleteQuestionAsync(int questionId);
    Task<bool> ReorderQuestionsAsync(int stepId, Dictionary<int, int> questionOrders);
    
    // User Progress
    Task<List<UserOnboardingProgress>> GetUserProgressAsync(int userId);
    Task<UserOnboardingProgress?> GetUserStepProgressAsync(int userId, int? stepId);
    Task CreateOrUpdateProgressAsync(UserOnboardingProgress progress);
    Task MarkStepAsCompletedAsync(int userId, int stepId);
    
    // User Answers
    Task<List<UserOnboardingAnswer>> GetUserAnswersAsync(int userId);
    Task<List<UserOnboardingAnswer>> GetUserAnswersByStepAsync(int userId, int stepId);
    Task<UserOnboardingAnswer?> GetUserAnswerAsync(int userId, int? questionId);
    Task SaveAnswerAsync(UserOnboardingAnswer answer);
    Task SaveAnswersAsync(List<UserOnboardingAnswer> answers);
    
    // Onboarding Sync (Offline-first)
    Task<UserOnboardingSync?> GetUserOnboardingSyncAsync(int userId);
    Task<UserOnboardingSync> CreateUserOnboardingSyncAsync(UserOnboardingSync sync);
    Task<UserOnboardingProgress> CreateUserProgressAsync(UserOnboardingProgress progress);
    Task<UserOnboardingAnswer> CreateUserAnswerAsync(UserOnboardingAnswer answer);
    Task DeleteUserOnboardingDataAsync(int userId);
}
