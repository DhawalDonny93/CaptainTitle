using System.ComponentModel.DataAnnotations;

namespace AppBackendBasic.Core.Entities;

/// <summary>
/// Stores prompt templates configured by admin for different use cases
/// </summary>
public class LlmPromptTemplate
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// The system prompt that defines behavior and output format
    /// Supports placeholders: {userInput}, {imageDescription}, {context}
    /// </summary>
    [Required]
    public string SystemPrompt { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional user prompt template
    /// </summary>
    public string? UserPromptTemplate { get; set; }
    
    /// <summary>
    /// Expected response format (json, text, structured)
    /// </summary>
    [MaxLength(20)]
    public string ResponseFormat { get; set; } = "json";
    
    /// <summary>
    /// JSON schema for structured output parsing
    /// </summary>
    public string? ResponseSchema { get; set; }
    
    /// <summary>
    /// Model to use for this template
    /// </summary>
    [MaxLength(50)]
    public string Model { get; set; } = "gpt-4o";
    
    public double Temperature { get; set; } = 0.7;
    
    public int MaxTokens { get; set; } = 1000;
    
    /// <summary>
    /// Whether this template requires image input
    /// </summary>
    public bool RequiresImage { get; set; } = false;
    
    /// <summary>
    /// Whether this template is active and available for use
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Category for organizing templates (e.g., "image_analysis", "text_generation")
    /// </summary>
    [MaxLength(50)]
    public string Category { get; set; } = "general";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Admin user who created this template
    /// </summary>
    public int? CreatedByUserId { get; set; }
}
