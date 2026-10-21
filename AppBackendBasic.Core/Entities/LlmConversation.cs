using System.ComponentModel.DataAnnotations;

namespace AppBackendBasic.Core.Entities;

public class LlmConversation
{
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = "New Conversation";
    
    [MaxLength(20)]
    public string Model { get; set; } = "gpt-4o"; // gpt-4o, gpt-4-turbo, gpt-3.5-turbo
    
    public int TotalMessages { get; set; } = 0;
    
    public int TotalTokensUsed { get; set; } = 0;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsDeleted { get; set; } = false;
    
    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<LlmMessage> Messages { get; set; } = new List<LlmMessage>();
}
