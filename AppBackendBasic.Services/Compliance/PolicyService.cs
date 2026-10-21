using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Services.Compliance;

public class PolicyService : IPolicyService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PolicyService> _logger;

    public PolicyService(ApplicationDbContext context, ILogger<PolicyService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PolicyDocument?> GetCurrentPolicyAsync(string type)
    {
        try
        {
            return await _context.Set<PolicyDocument>()
                .Where(p => p.Type == type && p.IsActive)
                .OrderByDescending(p => p.EffectiveDate)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current policy for type {Type}", type);
            throw;
        }
    }

    public async Task<PolicyDocument?> GetPolicyByVersionAsync(string type, string version)
    {
        try
        {
            return await _context.Set<PolicyDocument>()
                .FirstOrDefaultAsync(p => p.Type == type && p.Version == version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting policy {Type} version {Version}", type, version);
            throw;
        }
    }

    public async Task<bool> AcceptPolicyAsync(int userId, int policyId, string ipAddress, string? userAgent = null, string? deviceId = null)
    {
        try
        {
            // Check if policy exists
            var policy = await _context.Set<PolicyDocument>().FindAsync(policyId);
            if (policy == null)
            {
                _logger.LogWarning("Policy not found: {PolicyId}", policyId);
                return false;
            }

            // Check if user already accepted this specific policy version
            var existingAcceptance = await _context.Set<UserPolicyAcceptance>()
                .FirstOrDefaultAsync(a => a.UserId == userId && a.PolicyId == policyId);

            if (existingAcceptance != null)
            {
                _logger.LogInformation("User {UserId} already accepted policy {PolicyId}", userId, policyId);
                return true;
            }

            // Record acceptance
            var acceptance = new UserPolicyAcceptance
            {
                UserId = userId,
                PolicyId = policyId,
                AcceptedAt = DateTime.UtcNow,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                DeviceId = deviceId
            };

            _context.Set<UserPolicyAcceptance>().Add(acceptance);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} accepted policy {PolicyId} ({Type} v{Version})", 
                userId, policyId, policy.Type, policy.Version);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording policy acceptance for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> HasAcceptedCurrentPolicyAsync(int userId, string type)
    {
        try
        {
            var currentPolicy = await GetCurrentPolicyAsync(type);
            if (currentPolicy == null)
            {
                _logger.LogWarning("No active policy found for type {Type}", type);
                return true; // If no policy exists, consider it accepted
            }

            var hasAccepted = await _context.Set<UserPolicyAcceptance>()
                .AnyAsync(a => a.UserId == userId && a.PolicyId == currentPolicy.Id);

            return hasAccepted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking policy acceptance for user {UserId}, type {Type}", userId, type);
            throw;
        }
    }

    public async Task<List<UserPolicyAcceptance>> GetUserAcceptanceHistoryAsync(int userId)
    {
        try
        {
            return await _context.Set<UserPolicyAcceptance>()
                .Include(a => a.Policy)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.AcceptedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting acceptance history for user {UserId}", userId);
            throw;
        }
    }

    public async Task<PolicyUpdateStatus> CheckPolicyUpdatesAsync(int userId)
    {
        try
        {
            var result = new PolicyUpdateStatus();
            var pendingPolicies = new List<PolicyDocument>();

            // Check Privacy Policy
            var hasAcceptedPrivacy = await HasAcceptedCurrentPolicyAsync(userId, "Privacy");
            if (!hasAcceptedPrivacy)
            {
                var privacyPolicy = await GetCurrentPolicyAsync("Privacy");
                if (privacyPolicy != null)
                {
                    pendingPolicies.Add(privacyPolicy);
                }
            }

            // Check Terms of Service
            var hasAcceptedTerms = await HasAcceptedCurrentPolicyAsync(userId, "Terms");
            if (!hasAcceptedTerms)
            {
                var termsPolicy = await GetCurrentPolicyAsync("Terms");
                if (termsPolicy != null)
                {
                    pendingPolicies.Add(termsPolicy);
                }
            }

            result.PendingPolicies = pendingPolicies;
            result.RequiresAcceptance = pendingPolicies.Any();

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking policy updates for user {UserId}", userId);
            throw;
        }
    }
}
