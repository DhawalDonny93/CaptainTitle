namespace AppBackendBasic.Core.DTOs;

// Request DTOs
public class SendLlmMessageRequest
{
    public int? ConversationId { get; set; } // null for new conversation
    public int PromptTemplateId { get; set; } // Required: Which prompt template to use
    public string? UserContext { get; set; } // Optional: Additional context from user
    public string? ImageBase64 { get; set; } // Optional image
    public string? ImageUrl { get; set; } // Optional image URL
    public Dictionary<string, string>? AdditionalParams { get; set; } // Optional: Extra parameters for prompt
}

public class CreateConversationRequest
{
    public string Title { get; set; } = "New Conversation";
    public string Model { get; set; } = "gpt-4o";
}

// Response DTOs
public class LlmMessageResponse
{
    public int MessageId { get; set; }
    public int ConversationId { get; set; }
    public string Role { get; set; } = string.Empty;
    public object ParsedResponse { get; set; } = new(); // Structured response parsed by backend
    public string? RawContent { get; set; } // Optional: Raw LLM response for debugging
    public string? ImageUrl { get; set; }
    public int? TotalTokens { get; set; }
    public DateTime CreatedAt { get; set; }
    public string PromptTemplateName { get; set; } = string.Empty;
}

public class LlmConversationResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int TotalMessages { get; set; }
    public int TotalTokensUsed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<LlmMessageResponse> Messages { get; set; } = new();
}

public class LlmUsageResponse
{
    public int RequestsUsed { get; set; }
    public int RequestsLimit { get; set; }
    public int RequestsRemaining { get; set; }
    public int TokensUsed { get; set; }
    public int TokensLimit { get; set; }
    public int TokensRemaining { get; set; }
    public int ImagesUsed { get; set; }
    public int ImagesLimit { get; set; }
    public int ImagesRemaining { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string SubscriptionTier { get; set; } = "free";
}

// OpenAI API DTOs
public class OpenAIRequest
{
    public string Model { get; set; } = "gpt-4o";
    public List<OpenAIMessage> Messages { get; set; } = new();
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 1000;
}

public class OpenAIMessage
{
    public string Role { get; set; } = string.Empty;
    public object Content { get; set; } = string.Empty; // Can be string or array for images
}

public class OpenAIMessageContent
{
    public string Type { get; set; } = "text"; // "text" or "image_url"
    public string? Text { get; set; }
    public OpenAIImageUrl? ImageUrl { get; set; }
}

public class OpenAIImageUrl
{
    public string Url { get; set; } = string.Empty;
}

public class OpenAIResponse
{
    public string Id { get; set; } = string.Empty;
    public string Object { get; set; } = string.Empty;
    public long Created { get; set; }
    public string Model { get; set; } = string.Empty;
    public List<OpenAIChoice> Choices { get; set; } = new();
    public OpenAIUsage Usage { get; set; } = new();
}

public class OpenAIChoice
{
    public int Index { get; set; }
    public OpenAIMessage Message { get; set; } = new();
    public string? FinishReason { get; set; }
}

public class OpenAIUsage
{
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
}

// Prompt Template DTOs
public class LlmPromptTemplateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool RequiresImage { get; set; }
    public bool IsActive { get; set; }
}

public class CreatePromptTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = string.Empty;
    public string? UserPromptTemplate { get; set; }
    public string ResponseFormat { get; set; } = "json";
    public string? ResponseSchema { get; set; }
    public string Model { get; set; } = "gpt-4o";
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 1000;
    public bool RequiresImage { get; set; } = false;
    public string Category { get; set; } = "general";
}

public class UpdatePromptTemplateRequest : CreatePromptTemplateRequest
{
    public bool IsActive { get; set; } = true;
}
