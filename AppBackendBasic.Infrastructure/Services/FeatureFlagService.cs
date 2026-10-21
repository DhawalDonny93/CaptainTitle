using System.Text.Json;
using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Infrastructure.Services
{
    public class FeatureFlagService : IFeatureFlagService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FeatureFlagService> _logger;

        public FeatureFlagService(
            ApplicationDbContext context,
            ILogger<FeatureFlagService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<FeatureFlag>> GetAllFlagsAsync()
        {
            return await _context.FeatureFlags
                .OrderBy(f => f.DisplayName)
                .ToListAsync();
        }

        public async Task<FeatureFlag?> GetFlagByKeyAsync(string flagKey)
        {
            return await _context.FeatureFlags
                .FirstOrDefaultAsync(f => f.FlagKey == flagKey);
        }

        public async Task<FeatureFlag> CreateFlagAsync(CreateFeatureFlagDto dto)
        {
            var flag = new FeatureFlag
            {
                FlagKey = dto.FlagKey,
                DisplayName = dto.DisplayName,
                Description = dto.Description,
                IsEnabled = dto.IsEnabled,
                RolloutPercentage = dto.RolloutPercentage,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.FeatureFlags.Add(flag);
            await _context.SaveChangesAsync();

            return flag;
        }

        public async Task<FeatureFlag?> UpdateFlagAsync(int id, UpdateFeatureFlagDto dto)
        {
            var flag = await _context.FeatureFlags.FindAsync(id);
            if (flag == null)
                return null;

            if (dto.IsEnabled.HasValue)
                flag.IsEnabled = dto.IsEnabled.Value;

            if (dto.RolloutPercentage.HasValue)
                flag.RolloutPercentage = dto.RolloutPercentage.Value;

            if (!string.IsNullOrEmpty(dto.EnabledForUserIds))
                flag.EnabledForUserIds = dto.EnabledForUserIds;

            if (!string.IsNullOrEmpty(dto.EnabledForRoles))
                flag.EnabledForRoles = dto.EnabledForRoles;

            flag.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return flag;
        }

        public async Task<bool> DeleteFlagAsync(int id)
        {
            var flag = await _context.FeatureFlags.FindAsync(id);
            if (flag == null)
                return false;

            _context.FeatureFlags.Remove(flag);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsFlagEnabledAsync(string flagKey, int? userId = null, string? role = null)
        {
            var flag = await GetFlagByKeyAsync(flagKey);
            if (flag == null)
                return false;

            if (!flag.IsEnabled)
                return false;

            // Check user-specific enablement
            if (userId.HasValue && !string.IsNullOrEmpty(flag.EnabledForUserIds))
            {
                try
                {
                    var userIds = JsonSerializer.Deserialize<List<int>>(flag.EnabledForUserIds);
                    if (userIds != null && userIds.Contains(userId.Value))
                        return true;
                }
                catch { }
            }

            // Check role-specific enablement
            if (!string.IsNullOrEmpty(role) && !string.IsNullOrEmpty(flag.EnabledForRoles))
            {
                try
                {
                    var roles = JsonSerializer.Deserialize<List<string>>(flag.EnabledForRoles);
                    if (roles != null && roles.Contains(role))
                        return true;
                }
                catch { }
            }

            // Check rollout percentage
            if (flag.RolloutPercentage >= 100)
                return true;

            if (flag.RolloutPercentage > 0 && userId.HasValue)
            {
                var hash = Math.Abs(userId.Value.GetHashCode() % 100);
                return hash < flag.RolloutPercentage;
            }

            return flag.RolloutPercentage >= 100;
        }
    }
}
