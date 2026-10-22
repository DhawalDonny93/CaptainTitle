using AppBackendBasic.Core.DTOs;

namespace AppBackendBasic.Core.Interfaces
{
    public interface ILlmProviderService
    {
        Task<LlmResponseDto> SendRequestAsync(LlmRequestDto request);

        Task<List<string>> GetAvailableProvidersAsync();
        
        Task<Dictionary<string, object>> GetCostTrackingAsync(int? userId, int? projectId, DateTime? startDate, DateTime? endDate);
    }
}
