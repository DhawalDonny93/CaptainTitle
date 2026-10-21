using System;

namespace AppBackendBasic.Core.Entities
{
    public class FeatureFlag
    {
        public int Id { get; set; }
        public string FlagKey { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsEnabled { get; set; }
        public string? EnabledForUserIds { get; set; } // JSON array
        public string? EnabledForRoles { get; set; } // JSON array
        public int RolloutPercentage { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
