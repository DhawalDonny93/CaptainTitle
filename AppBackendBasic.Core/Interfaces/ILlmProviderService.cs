using AppBackendBasic.Core.DTOs;

namespace AppBackendBasic.Core.Interfaces
{
    public interface ILlmProviderService
    {
        Task<LlmResponseDto> SendRequestAsync(LlmRequestDto request);
        
        Task<ContentGenerationResponse> GenerateContentAsync(ContentGenerationRequest request);
        
        Task<PromptTestResponse> TestPromptAsync(PromptTestRequest request);
        
        Task<CitationAnalysisResponse> AnalyzeCitationsAsync(CitationAnalysisRequest request);
        
        Task<SentimentAnalysisResponse> AnalyzeSentimentAsync(SentimentAnalysisRequest request);
        
        Task<DraftReplyResponse> GenerateDraftReplyAsync(DraftReplyRequest request);
        
        Task<List<string>> GetAvailableProvidersAsync();
        
        Task<Dictionary<string, object>> GetCostTrackingAsync(int? userId, int? projectId, DateTime? startDate, DateTime? endDate);
    }
}
