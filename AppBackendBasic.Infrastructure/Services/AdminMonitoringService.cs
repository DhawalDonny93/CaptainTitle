using System.Diagnostics;
using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Infrastructure.Services
{
    public class AdminMonitoringService : IAdminMonitoringService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminMonitoringService> _logger;

        public AdminMonitoringService(
            ApplicationDbContext context,
            ILogger<AdminMonitoringService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<SystemHealthSnapshot> GetSystemHealthAsync()
        {
            var process = Process.GetCurrentProcess();
            var cpuUsage = GetCpuUsage();
            var memoryUsage = process.WorkingSet64 / (1024 * 1024); // Convert to MB

            var activeConnections = await GetActiveConnectionsAsync();
            var errorRate = await GetErrorRateAsync();
            var backgroundJobsRunning = 0; // Would integrate with Hangfire
            var backgroundJobsFailed = 0;

            var healthScore = CalculateHealthScore(cpuUsage, memoryUsage, errorRate);
            var status = healthScore >= 80 ? "Healthy" : healthScore >= 60 ? "Warning" : "Critical";

            return new SystemHealthSnapshot
            {
                CpuUsagePercent = (decimal)cpuUsage,
                MemoryUsageMb = memoryUsage,
                ActiveConnections = activeConnections,
                RequestsPerMinute = 0, // Would track via middleware
                AverageResponseTimeMs = 0, // Would track via middleware
                ErrorRate = errorRate,
                BackgroundJobsRunning = backgroundJobsRunning,
                BackgroundJobsFailed = backgroundJobsFailed,
                HealthScore = healthScore,
                Status = status
            };
        }

        public async Task<List<SystemHealthMetrics>> GetHealthHistoryAsync(DateRangeDto dateRange)
        {
            return await _context.SystemHealthMetrics
                .Where(m => m.MetricDate >= dateRange.StartDate && m.MetricDate <= dateRange.EndDate)
                .OrderBy(m => m.MetricDate)
                .ToListAsync();
        }

        public async Task RecordHealthMetricsAsync()
        {
            try
            {
                var health = await GetSystemHealthAsync();

                var metrics = new SystemHealthMetrics
                {
                    MetricDate = DateTime.UtcNow,
                    CpuUsagePercent = health.CpuUsagePercent,
                    MemoryUsageMb = health.MemoryUsageMb,
                    ActiveConnections = health.ActiveConnections,
                    RequestsPerMinute = health.RequestsPerMinute,
                    AverageResponseTimeMs = health.AverageResponseTimeMs,
                    ErrorRate = health.ErrorRate,
                    BackgroundJobsRunning = health.BackgroundJobsRunning,
                    BackgroundJobsFailed = health.BackgroundJobsFailed,
                    CreatedAt = DateTime.UtcNow
                };

                _context.SystemHealthMetrics.Add(metrics);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording health metrics");
            }
        }

        public async Task<PlatformAnalytics> GetPlatformAnalyticsAsync(DateRangeDto dateRange)
        {
            var totalUsers = await _context.Users.CountAsync();
            var activeUsers = await _context.Users.CountAsync(u => !u.IsDeleted);
            var totalProjects = await _context.Projects.CountAsync();
            var activeProjects = await _context.Projects.CountAsync(p => p.IsActive);
            var totalPrompts = await _context.PromptTrackings.CountAsync();
            var totalCitations = await _context.Citations.CountAsync();
            var totalAudits = await _context.SiteAuditRuns.CountAsync();
            var totalOpportunities = await _context.CommunityOpportunities.CountAsync();

            var totalLlmCost = await _context.LlmCostTrackings
                .Where(c => c.CreatedAt >= dateRange.StartDate && c.CreatedAt <= dateRange.EndDate)
                .SumAsync(c => c.TotalCost);

            var totalLlmRequests = await _context.LlmRequests
                .Where(r => r.CreatedAt >= dateRange.StartDate && r.CreatedAt <= dateRange.EndDate)
                .CountAsync();

            return new PlatformAnalytics
            {
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                TotalProjects = totalProjects,
                ActiveProjects = activeProjects,
                TotalPrompts = totalPrompts,
                TotalCitations = totalCitations,
                TotalAudits = totalAudits,
                TotalOpportunities = totalOpportunities,
                TotalLlmCost = totalLlmCost,
                TotalLlmRequests = totalLlmRequests
            };
        }

        public async Task<UsageStatistics> GetUsageStatisticsAsync(DateRangeDto dateRange)
        {
            var costByProvider = await _context.LlmCostTrackings
                .Include(c => c.Provider)
                .Where(c => c.CreatedAt >= dateRange.StartDate && c.CreatedAt <= dateRange.EndDate)
                .GroupBy(c => c.Provider.Name)
                .Select(g => new { Provider = g.Key, Cost = g.Sum(c => c.TotalCost) })
                .ToDictionaryAsync(x => x.Provider, x => x.Cost);

            return new UsageStatistics
            {
                CostByProvider = costByProvider,
                RequestsByEndpoint = new Dictionary<string, int>(),
                RequestsByUser = new Dictionary<string, int>(),
                DailyActiveUsers = new List<TrendPoint>(),
                DailyRequests = new List<TrendPoint>()
            };
        }

        public async Task<CostAnalytics> GetCostAnalyticsAsync(DateRangeDto dateRange)
        {
            var costs = await _context.LlmCostTrackings
                .Include(c => c.Provider)
                .Where(c => c.CreatedAt >= dateRange.StartDate && c.CreatedAt <= dateRange.EndDate)
                .ToListAsync();

            var totalCost = costs.Sum(c => c.TotalCost);
            var totalRequests = costs.Count;

            var costByProvider = costs
                .GroupBy(c => c.Provider.Name)
                .ToDictionary(g => g.Key, g => g.Sum(c => c.TotalCost));

            var costByModel = costs
                .GroupBy(c => c.Provider.Name)
                .ToDictionary(g => g.Key, g => g.Sum(c => c.TotalCost));

            var dailyCosts = costs
                .GroupBy(c => c.CreatedAt.Date)
                .Select(g => new TrendPoint { Date = g.Key, Value = g.Sum(c => c.TotalCost) })
                .OrderBy(t => t.Date)
                .ToList();

            return new CostAnalytics
            {
                TotalCost = totalCost,
                CostByProvider = costByProvider,
                CostByModel = costByModel,
                DailyCosts = dailyCosts,
                AverageCostPerRequest = totalRequests > 0 ? totalCost / totalRequests : 0,
                TotalRequests = totalRequests
            };
        }

        public async Task<AdminDashboardOverview> GetAdminDashboardAsync()
        {
            var dateRange = new DateRangeDto
            {
                StartDate = DateTime.UtcNow.AddDays(-30),
                EndDate = DateTime.UtcNow
            };

            var analytics = await GetPlatformAnalyticsAsync(dateRange);
            var health = await GetSystemHealthAsync();
            var recentLogs = await GetAuditLogsAsync(page: 1, pageSize: 10);

            return new AdminDashboardOverview
            {
                Analytics = analytics,
                Health = health,
                RecentJobs = new List<BackgroundJobStatus>(),
                RecentAuditLogs = recentLogs
            };
        }

        public async Task<List<AdminAuditLog>> GetAuditLogsAsync(int? adminUserId = null, int page = 1, int pageSize = 50)
        {
            var query = _context.AdminAuditLogs.AsQueryable();

            if (adminUserId.HasValue)
                query = query.Where(l => l.AdminUserId == adminUserId.Value);

            return await query
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task LogAdminActionAsync(int adminUserId, string action, string? entityType = null, string? entityId = null, string? oldValue = null, string? newValue = null, string? ipAddress = null, string? userAgent = null)
        {
            var log = new AdminAuditLog
            {
                AdminUserId = adminUserId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                OldValue = oldValue,
                NewValue = newValue,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CreatedAt = DateTime.UtcNow
            };

            _context.AdminAuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        private double GetCpuUsage()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                var startTime = DateTime.UtcNow;
                var startCpuUsage = process.TotalProcessorTime;

                Thread.Sleep(500);

                var endTime = DateTime.UtcNow;
                var endCpuUsage = process.TotalProcessorTime;

                var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
                var totalMsPassed = (endTime - startTime).TotalMilliseconds;
                var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);

                return cpuUsageTotal * 100;
            }
            catch
            {
                return 0;
            }
        }

        private async Task<int> GetActiveConnectionsAsync()
        {
            try
            {
                // This would query the database for active connections
                // For now, return a mock value
                return await Task.FromResult(10);
            }
            catch
            {
                return 0;
            }
        }

        private async Task<decimal> GetErrorRateAsync()
        {
            try
            {
                // This would calculate error rate from logs
                // For now, return a mock value
                return await Task.FromResult(0.5m);
            }
            catch
            {
                return 0;
            }
        }

        private int CalculateHealthScore(double cpuUsage, long memoryUsageMb, decimal errorRate)
        {
            int score = 100;

            if (cpuUsage > 70) score -= 20;
            else if (cpuUsage > 50) score -= 10;

            if (memoryUsageMb > 2048) score -= 20;
            else if (memoryUsageMb > 1024) score -= 10;

            if (errorRate > 5) score -= 30;
            else if (errorRate > 2) score -= 15;

            return Math.Max(0, score);
        }
    }
}
