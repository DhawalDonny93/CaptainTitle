using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AppBackendBasic.Services.Subscriptions;

public class PaywallAnalyticsService : IPaywallAnalyticsService
{
    private readonly ApplicationDbContext _db;

    public PaywallAnalyticsService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<string> TrackEventAsync(int? userId, PaywallEventDto dto)
    {
        var ev = new PaywallEvent
        {
            UserId = userId,
            EventType = dto.EventType,
            PaywallId = dto.PaywallId,
            DeviceId = dto.DeviceId,
            Platform = dto.Platform,
            EventData = dto.EventData != null ? System.Text.Json.JsonSerializer.Serialize(dto.EventData) : null,
            CreatedAt = DateTime.UtcNow
        };
        _db.PaywallEvents.Add(ev);
        await _db.SaveChangesAsync();
        return ev.Id.ToString();
    }

    public async Task<PaywallPerformanceDto> GetPerformanceAsync(DateTime startDate, DateTime endDate, string? paywallId = null)
    {
        var q = _db.PaywallEvents.AsNoTracking().Where(e => e.CreatedAt >= startDate && e.CreatedAt <= endDate);
        if (!string.IsNullOrWhiteSpace(paywallId))
        {
            q = q.Where(e => e.PaywallId == paywallId);
        }

        var impressions = await q.CountAsync(e => e.EventType == "impression");
        var clicks = await q.CountAsync(e => e.EventType == "click");
        var purchases = await q.CountAsync(e => e.EventType == "purchase_complete");

        var tx = await _db.SubscriptionTransactions.AsNoTracking()
            .Where(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate && t.Status == "completed")
            .ToListAsync();

        var revenue = tx.Sum(t => t.Amount);
        var currency = tx.FirstOrDefault()?.Currency ?? "USD";
        var conversionRate = impressions > 0 ? (decimal)purchases / impressions * 100m : 0m;

        var dto = new PaywallPerformanceDto
        {
            Impressions = impressions,
            Clicks = clicks,
            Purchases = purchases,
            ConversionRate = conversionRate,
            Revenue = revenue,
            Currency = currency
        };

        if (string.IsNullOrWhiteSpace(paywallId))
        {
            var paywalls = await _db.PaywallEvents.AsNoTracking()
                .Where(e => e.CreatedAt >= startDate && e.CreatedAt <= endDate)
                .Select(e => e.PaywallId)
                .Distinct()
                .ToListAsync();

            dto.ByPaywall = new Dictionary<string, PaywallPerformanceDto>();
            foreach (var p in paywalls)
            {
                dto.ByPaywall[p] = await GetPerformanceAsync(startDate, endDate, p);
            }
        }

        return dto;
    }
}
