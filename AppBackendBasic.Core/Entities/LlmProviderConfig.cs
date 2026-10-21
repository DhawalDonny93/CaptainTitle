using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackendBasic.Core.Entities
{
    public class LlmProviderConfig
    {
        public int Id { get; set; }
        
        [Column("ProviderId")]
        public int ProviderId { get; set; }
        
        public string ConfigKey { get; set; } = string.Empty;
        public string? ConfigValue { get; set; } // Encrypted for API keys
        public bool IsEncrypted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("ProviderId")]
        public virtual LlmProvider Provider { get; set; } = null!;
    }
}
