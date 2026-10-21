using System;

namespace AppBackendBasic.Core.Entities
{
    public class AdminAuditLog
    {
        public int Id { get; set; }
        public int AdminUserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? EntityType { get; set; }
        public string? EntityId { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation
        public virtual User AdminUser { get; set; } = null!;
    }
}
