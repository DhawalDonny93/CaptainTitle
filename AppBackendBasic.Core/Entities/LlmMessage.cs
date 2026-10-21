using System.ComponentModel.DataAnnotations;

namespace AppBackendBasic.Core.Entities;

public class LlmMessage
{
    public int Id { get; set; }
    
    [Required]
    public int ConversationId { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string Role { get; set; } = string.Empty; // user, assistant, system
    
    /// <summary>
    /// For user messages: stores user context/input
    /// For assistant messages: stores raw LLM response
    /// </summary>
    public string? RawContent { get; set; }
    
    /// <summary>
    /// For assistant messages: stores parsed/formatted response sent to frontend
    /// </summary>
    [Required]
    public string ParsedContent { get; set; } = string.Empty;
    
    /// <summary>
    /// Reference to the prompt template used (for user messages)
    /// </summary>
    public int? PromptTemplateId { get; set; }
    
    // For image messages
    public string? ImageUrl { get; set; }
    public string? ImageBase64 { get; set; }
    
    // Token usage for this specific message
    public int? PromptTokens { get; set; }
    public int? CompletionTokens { get; set; }
    public int? TotalTokens { get; set; }
    
    // Response metadata
    public string? FinishReason { get; set; }
    public double? ResponseTimeMs { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public LlmConversation Conversation { get; set; } = null!;
}
