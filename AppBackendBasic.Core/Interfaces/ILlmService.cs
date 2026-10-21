using AppBackendBasic.Core.DTOs;

namespace AppBackendBasic.Core.Interfaces;

public interface ILlmService
{
    Task<LlmMessageResponse> SendMessageAsync(int userId, SendLlmMessageRequest request);
    Task<LlmConversationResponse> GetConversationAsync(int userId, int conversationId);
    Task<List<LlmConversationResponse>> GetUserConversationsAsync(int userId, int page = 1, int pageSize = 20);
    Task<LlmConversationResponse> CreateConversationAsync(int userId, CreateConversationRequest request);
    Task<bool> DeleteConversationAsync(int userId, int conversationId);
    Task<LlmUsageResponse> GetUsageAsync(int userId);
    Task<bool> CheckRateLimitAsync(int userId);
}
