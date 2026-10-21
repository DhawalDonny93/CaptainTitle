using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Services.BackgroundJobs.Jobs;

public class CleanupJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CleanupJob> _logger;

    public CleanupJob(IServiceProvider serviceProvider, ILogger<CleanupJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Clean up expired refresh tokens
    /// </summary>
    public async Task CleanupExpiredRefreshTokensAsync()
    {
        _logger.LogInformation("Starting cleanup of expired refresh tokens...");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var expiredTokens = await dbContext.RefreshTokens
                .Where(rt => rt.ExpiresAt < DateTime.UtcNow)
                .ToListAsync();

            if (expiredTokens.Any())
            {
                dbContext.RefreshTokens.RemoveRange(expiredTokens);
                await dbContext.SaveChangesAsync();

                _logger.LogInformation("Cleaned up {Count} expired refresh tokens", expiredTokens.Count);
            }
            else
            {
                _logger.LogInformation("No expired refresh tokens found");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired refresh tokens");
            throw;
        }
    }

    /// <summary>
    /// Clean up old audit logs (older than 90 days)
    /// </summary>
    public async Task CleanupOldAuditLogsAsync()
    {
        _logger.LogInformation("Starting cleanup of old audit logs...");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var cutoffDate = DateTime.UtcNow.AddDays(-90);
            var oldLogs = await dbContext.AuditLogs
                .Where(al => al.CreatedAt < cutoffDate)
                .ToListAsync();

            if (oldLogs.Any())
            {
                dbContext.AuditLogs.RemoveRange(oldLogs);
                await dbContext.SaveChangesAsync();

                _logger.LogInformation("Cleaned up {Count} old audit logs", oldLogs.Count);
            }
            else
            {
                _logger.LogInformation("No old audit logs found");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old audit logs");
            throw;
        }
    }

    /// <summary>
    /// Clean up inactive users (not logged in for 365 days)
    /// </summary>
    public async Task CleanupInactiveUsersAsync()
    {
        _logger.LogInformation("Starting cleanup of inactive users...");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var cutoffDate = DateTime.UtcNow.AddDays(-365);
            var inactiveUsers = await dbContext.Users
                .Where(u => !u.IsActive && u.UpdatedAt < cutoffDate)
                .ToListAsync();

            if (inactiveUsers.Any())
            {
                // Soft delete or mark for deletion
                foreach (var user in inactiveUsers)
                {
                    user.IsActive = false;
                    // You might want to anonymize data instead of deleting
                    // user.Email = $"deleted_{user.Id}@deleted.com";
                }

                await dbContext.SaveChangesAsync();

                _logger.LogInformation("Marked {Count} inactive users for cleanup", inactiveUsers.Count);
            }
            else
            {
                _logger.LogInformation("No inactive users found");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up inactive users");
            throw;
        }
    }

    /// <summary>
    /// Clean up expired password reset tokens
    /// </summary>
    public async Task CleanupExpiredPasswordResetTokensAsync()
    {
        _logger.LogInformation("Starting cleanup of expired password reset tokens...");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var expiredTokens = await dbContext.PasswordResets
                .Where(prt => prt.ExpiresAt < DateTime.UtcNow)
                .ToListAsync();

            if (expiredTokens.Any())
            {
                dbContext.PasswordResets.RemoveRange(expiredTokens);
                await dbContext.SaveChangesAsync();

                _logger.LogInformation("Cleaned up {Count} expired password reset tokens", expiredTokens.Count);
            }
            else
            {
                _logger.LogInformation("No expired password reset tokens found");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired password reset tokens");
            throw;
        }
    }

    /// <summary>
    /// Clean up old notifications (older than 30 days)
    /// </summary>
    public async Task CleanupOldNotificationsAsync()
    {
        _logger.LogInformation("Starting cleanup of old notifications...");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var cutoffDate = DateTime.UtcNow.AddDays(-30);
            var oldNotifications = await dbContext.InAppNotifications
                .Where(n => n.IsRead && n.CreatedAt < cutoffDate)
                .ToListAsync();

            if (oldNotifications.Any())
            {
                dbContext.InAppNotifications.RemoveRange(oldNotifications);
                await dbContext.SaveChangesAsync();

                _logger.LogInformation("Cleaned up {Count} old notifications", oldNotifications.Count);
            }
            else
            {
                _logger.LogInformation("No old notifications found");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old notifications");
            throw;
        }
    }

    /// <summary>
    /// Run all cleanup tasks
    /// </summary>
    public async Task RunAllCleanupTasksAsync()
    {
        _logger.LogInformation("Starting all cleanup tasks...");

        try
        {
            await CleanupExpiredRefreshTokensAsync();
            await CleanupExpiredPasswordResetTokensAsync();
            await CleanupOldAuditLogsAsync();
            await CleanupOldNotificationsAsync();
            await CleanupInactiveUsersAsync();

            _logger.LogInformation("All cleanup tasks completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running cleanup tasks");
            throw;
        }
    }

    /// <summary>
    /// Generate daily statistics report
    /// </summary>
    public async Task GenerateDailyStatsAsync()
    {
        _logger.LogInformation("Generating daily statistics...");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var today = DateTime.UtcNow.Date;

            var stats = new
            {
                TotalUsers = await dbContext.Users.CountAsync(),
                NewUsersToday = await dbContext.Users.CountAsync(u => u.CreatedAt >= today),
                TotalSubscriptions = await dbContext.Subscriptions.CountAsync()
            };

            _logger.LogInformation("Daily Stats: {@Stats}", stats);

            // You could save these stats to a database table or send them via email
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating daily statistics");
            throw;
        }
    }
}
