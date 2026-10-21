using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;

namespace AppBackendBasic.Core.Interfaces;

public interface IOnboardingService
{
    Task<OnboardingProgressDto> GetUserOnboardingProgressAsync(int userId);
    Task<OnboardingStepDto?> GetOnboardingStepAsync(int userId, int stepId);
    Task<bool> SubmitOnboardingAnswersAsync(int userId, SubmitOnboardingAnswersDto request);
    Task<bool> CompleteOnboardingAsync(int userId);
    
    /// <summary>
    /// Sync offline onboarding data from client (iOS app)
    /// </summary>
    Task<OnboardingSyncResponseDto> SyncOnboardingDataAsync(int userId, OnboardingSyncRequestDto request);
    
    /// <summary>
    /// Check if user has already synced onboarding data
    /// </summary>
    Task<UserOnboardingSync?> GetUserOnboardingSyncAsync(int userId);
}
