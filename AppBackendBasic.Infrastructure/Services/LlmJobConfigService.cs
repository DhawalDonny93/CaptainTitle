using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Infrastructure.Services
{
    public class LlmJobConfigService : ILlmJobConfigService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<LlmJobConfigService> _logger;

        private const string CacheKeyPrefix = "llm_job_cfg_";
        private const string AllConfigsCacheKey = "llm_job_cfg_all";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(30);

        public LlmJobConfigService(
            ApplicationDbContext context,
            IMemoryCache cache,
            ILogger<LlmJobConfigService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<LlmJobResolution> GetJobConfigAsync(
            string jobKey,
            string defaultProvider,
            string defaultModel,
            int defaultMaxTokens = 2000,
            decimal defaultTemperature = 0.7m)
        {
            var cacheKey = CacheKeyPrefix + jobKey;

            if (_cache.TryGetValue(cacheKey, out LlmJobConfig? cached) && cached != null)
            {
                _logger.LogDebug("LlmJobConfig cache hit for {JobKey}", jobKey);
                return ToResolution(cached);
            }

            var config = await _context.LlmJobConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.JobKey == jobKey && c.IsActive);

            if (config != null)
            {
                _cache.Set(cacheKey, config, CacheTtl);
                _logger.LogDebug("LlmJobConfig loaded from DB for {JobKey}: {Provider}/{Model}", jobKey, config.Provider, config.Model);
                return ToResolution(config);
            }

            _logger.LogDebug("No LlmJobConfig found for {JobKey}, using defaults: {Provider}/{Model}", jobKey, defaultProvider, defaultModel);
            return new LlmJobResolution(defaultProvider, defaultModel, defaultMaxTokens, defaultTemperature);
        }

        /// <inheritdoc/>
        public async Task<List<LlmJobConfig>> GetAllAsync()
        {
            if (_cache.TryGetValue(AllConfigsCacheKey, out List<LlmJobConfig>? allCached) && allCached != null)
                return allCached;

            var all = await _context.LlmJobConfigs
                .AsNoTracking()
                .OrderBy(c => c.Category)
                .ThenBy(c => c.JobName)
                .ToListAsync();

            _cache.Set(AllConfigsCacheKey, all, CacheTtl);
            return all;
        }

        /// <inheritdoc/>
        public async Task<LlmJobConfig?> GetByKeyAsync(string jobKey)
        {
            return await _context.LlmJobConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.JobKey == jobKey);
        }

        /// <inheritdoc/>
        public async Task<LlmJobConfig> UpsertAsync(LlmJobConfig config)
        {
            var existing = await _context.LlmJobConfigs
                .FirstOrDefaultAsync(c => c.JobKey == config.JobKey);

            if (existing == null)
            {
                config.CreatedAt = DateTime.UtcNow;
                config.UpdatedAt = DateTime.UtcNow;
                _context.LlmJobConfigs.Add(config);
                _logger.LogInformation("Created LlmJobConfig for {JobKey}: {Provider}/{Model}", config.JobKey, config.Provider, config.Model);
            }
            else
            {
                existing.JobName = config.JobName;
                existing.Description = config.Description;
                existing.Provider = config.Provider;
                existing.Model = config.Model;
                existing.MaxTokens = config.MaxTokens;
                existing.Temperature = config.Temperature;
                existing.IsActive = config.IsActive;
                existing.Category = config.Category;
                existing.UpdatedAt = DateTime.UtcNow;
                config = existing;
                _logger.LogInformation("Updated LlmJobConfig for {JobKey}: {Provider}/{Model}", config.JobKey, config.Provider, config.Model);
            }

            await _context.SaveChangesAsync();

            // Invalidate specific key and the all-list cache
            _cache.Remove(CacheKeyPrefix + config.JobKey);
            _cache.Remove(AllConfigsCacheKey);

            return config;
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteAsync(string jobKey)
        {
            var config = await _context.LlmJobConfigs
                .FirstOrDefaultAsync(c => c.JobKey == jobKey);

            if (config == null)
                return false;

            _context.LlmJobConfigs.Remove(config);
            await _context.SaveChangesAsync();

            _cache.Remove(CacheKeyPrefix + jobKey);
            _cache.Remove(AllConfigsCacheKey);

            _logger.LogInformation("Deleted LlmJobConfig for {JobKey}", jobKey);
            return true;
        }

        /// <inheritdoc/>
        public void InvalidateCache()
        {
            // IMemoryCache doesn't support pattern-based removal; we clear the all-list key.
            // Individual keys will expire naturally or be overwritten on next write.
            _cache.Remove(AllConfigsCacheKey);
            _logger.LogInformation("LlmJobConfig cache invalidated");
        }

        private static LlmJobResolution ToResolution(LlmJobConfig c) =>
            new(c.Provider, c.Model, c.MaxTokens, c.Temperature);
    }
}
