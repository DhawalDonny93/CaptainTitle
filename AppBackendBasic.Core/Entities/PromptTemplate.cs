using System;

namespace AppBackendBasic.Core.Entities
{
    public class PromptTemplate
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Template { get; set; } = string.Empty;
        public string? VariablesJson { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public int UsageCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
