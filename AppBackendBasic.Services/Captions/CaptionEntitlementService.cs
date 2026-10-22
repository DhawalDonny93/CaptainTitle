using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AppBackendBasic.Services.Captions;

public static class CaptionCreditType
{
    public const string Testing = "testing";
    public const string Free = "free";
    public const string Oneoff = "oneoff";
    public const string Basic = "basic";
}

public interface ICaptionEntitlementService
{
    Task<CaptionUsage> GetOrCreateUsageAsync(int userId, CancellationToken ct = default);
    Task<CaptionUsageDto> GetUsageDtoAsync(int userId, CancellationToken ct = default);
    /// <summary>Which credit bucket pays for a video of this length, or null when the user must buy something.</summary>
    Task<string?> DetermineCreditAsync(int userId, double durationSeconds, CancellationToken ct = default);
    Task<string> DescribeDenialAsync(double durationSeconds, CancellationToken ct = default);
    Task ConsumeAsync(int userId, string creditType, CancellationToken ct = default);
    /// <summary>Applies a confirmed payment. Returns false when nothing was granted (purchase cap reached).</summary>
    Task<bool> GrantAsync(int userId, CaptionPlan plan, CancellationToken ct = default);
    Task<bool> HasReachedPurchaseCapAsync(int userId, CaptionPlan plan, CancellationToken ct = default);
}

/// <summary>Entitlement ladder from the old captions.py: testing -> free -> one-off pass -> Basic. All limits are admin-configurable.</summary>
public class CaptionEntitlementService : ICaptionEntitlementService
{
    private readonly ApplicationDbContext _db;
    private readonly ICaptionCatalogService _catalog;

    public CaptionEntitlementService(ApplicationDbContext db, ICaptionCatalogService catalog)
    {
        _db = db; _catalog = catalog;
    }

    public async Task<CaptionUsage> GetOrCreateUsageAsync(int userId, CancellationToken ct = default)
    {
        // Read-only snapshot: balances are changed with set-based UPDATEs (ExecuteUpdate) that bypass the change tracker,
        // so a tracked copy would go stale within the same request.
        var usage = await _db.CaptionUsages.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId, ct);
        if (usage != null) return usage;
        var limits = await _catalog.GetLimitsAsync(ct);
        usage = new CaptionUsage { UserId = userId, FreeRemaining = limits.FreeVideos };
        _db.CaptionUsages.Add(usage);
        try
        {
            await _db.SaveChangesAsync(ct);
            _db.Entry(usage).State = EntityState.Detached;
            return usage;
        }
        catch (DbUpdateException)
        {
            // Concurrent first request created it (unique index on UserId) - read the winner.
            _db.Entry(usage).State = EntityState.Detached;
            return await _db.CaptionUsages.AsNoTracking().FirstAsync(u => u.UserId == userId, ct);
        }
    }

    public async Task<CaptionUsageDto> GetUsageDtoAsync(int userId, CancellationToken ct = default)
    {
        var usage = await GetOrCreateUsageAsync(userId, ct);
        var limits = await _catalog.GetLimitsAsync(ct);
        var plans = await _catalog.GetActivePlansAsync(ct);
        return new CaptionUsageDto
        {
            FreeRemaining = usage.FreeRemaining, OneoffCredits = usage.OneoffCredits, OneoffPurchaseCount = usage.OneoffPurchaseCount,
            BasicCredits = usage.BasicCredits, SubscriptionActive = usage.SubscriptionActive, VideosCreated = usage.VideosCreated,
            UnlimitedTesting = limits.UnlimitedTesting,
            Plans = plans.Select(ToSummary).ToList(),
        };
    }

    public static CaptionPlanSummaryDto ToSummary(CaptionPlan p) => new()
    {
        Id = p.Key, Name = p.Name, Price = p.Price, Currency = p.Currency, Kind = p.Kind, Videos = p.Videos,
        MaxSeconds = p.MaxSeconds, MaxPurchases = p.MaxPurchasesPerUser, Description = p.Description, Featured = p.IsFeatured,
    };

    public async Task<string?> DetermineCreditAsync(int userId, double durationSeconds, CancellationToken ct = default)
    {
        var usage = await GetOrCreateUsageAsync(userId, ct);
        var limits = await _catalog.GetLimitsAsync(ct);
        var plans = await _catalog.GetActivePlansAsync(ct);
        int oneoffMax = plans.Where(p => p.Kind == "payment").Select(p => p.MaxSeconds).DefaultIfEmpty(0).Max();
        int basicMax = plans.Where(p => p.Kind == "subscription").Select(p => p.MaxSeconds).DefaultIfEmpty(0).Max();

        if (limits.UnlimitedTesting && durationSeconds <= limits.AbsoluteMaxSeconds) return CaptionCreditType.Testing;
        if (durationSeconds <= limits.FreeMaxSeconds && usage.FreeRemaining > 0) return CaptionCreditType.Free;
        if (durationSeconds <= oneoffMax && usage.OneoffCredits > 0) return CaptionCreditType.Oneoff;
        if (durationSeconds <= basicMax && usage.BasicCredits > 0) return CaptionCreditType.Basic;
        return null;
    }

    public async Task<string> DescribeDenialAsync(double durationSeconds, CancellationToken ct = default)
    {
        var plans = await _catalog.GetActivePlansAsync(ct);
        int longest = plans.Select(p => p.MaxSeconds).DefaultIfEmpty(0).Max();
        return durationSeconds > longest
            ? $"Current plans process up to {longest} seconds"
            : "Choose a video pass or Basic plan to continue";
    }

    public async Task ConsumeAsync(int userId, string creditType, CancellationToken ct = default)
    {
        await GetOrCreateUsageAsync(userId, ct);
        var now = DateTime.UtcNow;
        var q = _db.CaptionUsages.Where(u => u.UserId == userId);
        switch (creditType)
        {
            case CaptionCreditType.Testing:
                await q.ExecuteUpdateAsync(s => s.SetProperty(u => u.VideosCreated, u => u.VideosCreated + 1).SetProperty(u => u.UpdatedAt, now), ct);
                break;
            case CaptionCreditType.Free:
                await q.ExecuteUpdateAsync(s => s.SetProperty(u => u.FreeRemaining, u => u.FreeRemaining > 0 ? u.FreeRemaining - 1 : 0)
                    .SetProperty(u => u.VideosCreated, u => u.VideosCreated + 1).SetProperty(u => u.UpdatedAt, now), ct);
                break;
            case CaptionCreditType.Oneoff:
                await q.ExecuteUpdateAsync(s => s.SetProperty(u => u.OneoffCredits, u => u.OneoffCredits > 0 ? u.OneoffCredits - 1 : 0)
                    .SetProperty(u => u.VideosCreated, u => u.VideosCreated + 1).SetProperty(u => u.UpdatedAt, now), ct);
                break;
            case CaptionCreditType.Basic:
                await q.ExecuteUpdateAsync(s => s.SetProperty(u => u.BasicCredits, u => u.BasicCredits > 0 ? u.BasicCredits - 1 : 0)
                    .SetProperty(u => u.VideosCreated, u => u.VideosCreated + 1).SetProperty(u => u.UpdatedAt, now), ct);
                break;
        }
    }

    public async Task<bool> HasReachedPurchaseCapAsync(int userId, CaptionPlan plan, CancellationToken ct = default)
    {
        if (plan.Kind != "payment" || plan.MaxPurchasesPerUser == null) return false;
        var usage = await GetOrCreateUsageAsync(userId, ct);
        return usage.OneoffPurchaseCount >= plan.MaxPurchasesPerUser;
    }

    public async Task<bool> GrantAsync(int userId, CaptionPlan plan, CancellationToken ct = default)
    {
        var usage = await GetOrCreateUsageAsync(userId, ct);
        var now = DateTime.UtcNow;
        var q = _db.CaptionUsages.Where(u => u.UserId == userId);
        if (plan.Kind == "payment")
        {
            if (plan.MaxPurchasesPerUser != null && usage.OneoffPurchaseCount >= plan.MaxPurchasesPerUser) return false;
            int videos = Math.Max(1, plan.Videos);
            await q.ExecuteUpdateAsync(s => s.SetProperty(u => u.OneoffCredits, u => u.OneoffCredits + videos)
                .SetProperty(u => u.OneoffPurchaseCount, u => u.OneoffPurchaseCount + 1).SetProperty(u => u.UpdatedAt, now), ct);
        }
        else
        {
            int videos = plan.Videos;
            await q.ExecuteUpdateAsync(s => s.SetProperty(u => u.BasicCredits, videos)
                .SetProperty(u => u.SubscriptionActive, true).SetProperty(u => u.UpdatedAt, now), ct);
        }
        return true;
    }
}
