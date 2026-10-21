using System;

namespace AppBackendBasic.Core.Entities
{
    public class SystemHealthMetrics
    {
        public int Id { get; set; }
        public DateTime MetricDate { get; set; } = DateTime.UtcNow;
        public decimal? CpuUsagePercent { get; set; }
        public long? MemoryUsageMb { get; set; }
        public int? ActiveConnections { get; set; }
        public int? RequestsPerMinute { get; set; }
        public int? AverageResponseTimeMs { get; set; }
        public decimal? ErrorRate { get; set; }
        public int? BackgroundJobsRunning { get; set; }
        public int? BackgroundJobsFailed { get; set; }
        public int? DatabaseConnectionPoolSize { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
