using System;

namespace AppBackendBasic.Core.Entities
{
    public class EmailTemplate
    {
        public int Id { get; set; }
        public string TemplateKey { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string HtmlBody { get; set; } = string.Empty;
        public string? TextBody { get; set; }
        public string? Variables { get; set; } // JSON array
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
