using AppBackendBasic.Core.DTOs;

namespace AppBackendBasic.Core.Interfaces
{
    public interface ILlmProvider
    {
        string ProviderName { get; }
        
        Task<LlmResponseDto> SendRequestAsync(LlmRequestDto request);
        
        Task<bool> TestConnectionAsync();
        
        Task<LlmResponseDto> GenerateContentAsync(ContentGenerationRequest request);
    }
}
