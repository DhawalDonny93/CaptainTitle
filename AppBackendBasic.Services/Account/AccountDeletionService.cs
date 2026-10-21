using System.Security.Cryptography;
using System.Text;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Services.Account;

public class AccountDeletionService : IAccountDeletionService
{
    private readonly ApplicationDbContext _context;
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly ILogger<AccountDeletionService> _logger;
    private readonly IConfiguration _configuration;
    
    private const int DefaultGracePeriodDays = 30;

    public AccountDeletionService(
        ApplicationDbContext context,
        IUserRepository userRepository,
        IEmailService emailService,
        ILogger<AccountDeletionService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _userRepository = userRepository;
        _emailService = emailService;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<DeletionResult> RequestDeletionAsync(int userId, string password, string? reason = null)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            
            if (user == null)
            {
                return new DeletionResult
                {
                    Success = false,
                    Message = "User not found"
                };
            }

            // Verify password for security
            if (!string.IsNullOrEmpty(user.PasswordHash) && !VerifyPasswordHash(password, user.PasswordHash))
            {
                _logger.LogWarning("Account deletion request failed: Invalid password for user {UserId}", userId);
                return new DeletionResult
                {
                    Success = false,
                    Message = "Invalid password"
                };
            }

            // Check if already deleted or pending deletion
            if (user.IsDeleted)
            {
                return new DeletionResult
                {
                    Success = false,
                    Message = "Account is already deleted"
                };
            }

            if (user.ScheduledDeletionDate.HasValue)
            {
                return new DeletionResult
                {
                    Success = false,
                    Message = "Account deletion is already scheduled",
                    ScheduledDeletionDate = user.ScheduledDeletionDate,
                    GracePeriodDays = (int)(user.ScheduledDeletionDate.Value - DateTime.UtcNow).TotalDays
                };
            }

            // Schedule deletion with grace period
            var gracePeriodDays = _configuration.GetValue<int>("AccountDeletion:GracePeriodDays", DefaultGracePeriodDays);
            user.ScheduledDeletionDate = DateTime.UtcNow.AddDays(gracePeriodDays);
            user.DeletionReason = reason;
            user.UpdatedAt = DateTime.UtcNow;

            // Revoke all active sessions and tokens
            await RevokeAllSessionsAndTokensAsync(userId);

            await _userRepository.UpdateAsync(user);

            // Send confirmation email
            await SendDeletionConfirmationEmailAsync(user, gracePeriodDays);

            _logger.LogInformation("Account deletion scheduled for user {UserId}, deletion date: {DeletionDate}", 
                userId, user.ScheduledDeletionDate);

            return new DeletionResult
            {
                Success = true,
                Message = $"Account deletion scheduled. You have {gracePeriodDays} days to cancel.",
                ScheduledDeletionDate = user.ScheduledDeletionDate,
                GracePeriodDays = gracePeriodDays
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting account deletion for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> CancelDeletionAsync(int userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            
            if (user == null || !user.ScheduledDeletionDate.HasValue)
            {
                return false;
            }

            // Check if still within grace period
            if (user.ScheduledDeletionDate.Value < DateTime.UtcNow)
            {
                _logger.LogWarning("Cannot cancel deletion for user {UserId}: Grace period expired", userId);
                return false;
            }

            user.ScheduledDeletionDate = null;
            user.DeletionReason = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);

            // Send cancellation confirmation email
            await SendDeletionCancellationEmailAsync(user);

            _logger.LogInformation("Account deletion cancelled for user {UserId}", userId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling account deletion for user {UserId}", userId);
            throw;
        }
    }

    public async Task ProcessScheduledDeletionsAsync()
    {
        try
        {
            // Find all users scheduled for deletion whose grace period has expired
            var usersToDelete = await _context.Users
                .Where(u => u.ScheduledDeletionDate.HasValue && 
                           u.ScheduledDeletionDate.Value <= DateTime.UtcNow &&
                           !u.IsDeleted)
                .ToListAsync();

            _logger.LogInformation("Processing {Count} scheduled account deletions", usersToDelete.Count);

            foreach (var user in usersToDelete)
            {
                try
                {
                    await PermanentlyDeleteUserDataAsync(user.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting user {UserId}", user.Id);
                    // Continue with other users
                }
            }

            _logger.LogInformation("Completed processing scheduled account deletions");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing scheduled deletions");
            throw;
        }
    }

    public async Task PermanentlyDeleteUserDataAsync(int userId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            
            if (user == null)
            {
                _logger.LogWarning("Cannot delete user {UserId}: User not found", userId);
                return;
            }

            _logger.LogInformation("Starting permanent deletion of user {UserId}", userId);

            // Delete related data (cascade delete should handle most, but we'll be explicit)
            
            // Delete user providers
            var providers = await _context.UserProviders.Where(p => p.UserId == userId).ToListAsync();
            _context.UserProviders.RemoveRange(providers);

            // Delete refresh tokens
            var tokens = await _context.RefreshTokens.Where(t => t.UserId == userId).ToListAsync();
            _context.RefreshTokens.RemoveRange(tokens);

            // Delete sessions
            var sessions = await _context.UserSessions.Where(s => s.UserId == userId).ToListAsync();
            _context.UserSessions.RemoveRange(sessions);

            // Delete subscriptions
            var subscriptions = await _context.Subscriptions.Where(s => s.UserId == userId).ToListAsync();
            _context.Subscriptions.RemoveRange(subscriptions);

            // Delete onboarding data
            var onboardingProgress = await _context.UserOnboardingProgress.Where(p => p.UserId == userId).ToListAsync();
            _context.UserOnboardingProgress.RemoveRange(onboardingProgress);

            var onboardingAnswers = await _context.UserOnboardingAnswers.Where(a => a.UserId == userId).ToListAsync();
            _context.UserOnboardingAnswers.RemoveRange(onboardingAnswers);

            // Anonymize or delete paywall events (keep for analytics but anonymize)
            var paywallEvents = await _context.PaywallEvents.Where(e => e.UserId == userId).ToListAsync();
            foreach (var evt in paywallEvents)
            {
                evt.UserId = null; // Anonymize
            }

            // Mark user as deleted and anonymize PII
            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            user.Email = $"deleted_{userId}@deleted.local";
            user.PhoneNumber = null;
            user.FirstName = "Deleted";
            user.LastName = "User";
            user.PasswordHash = null;
            user.DateOfBirth = null;
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Successfully deleted user {UserId}", userId);

            // Send final confirmation email (if we still have the original email)
            // Note: This should be sent before anonymization in a real implementation
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error permanently deleting user {UserId}", userId);
            throw;
        }
    }

    public async Task<DeletionStatus?> GetDeletionStatusAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        
        if (user == null || !user.ScheduledDeletionDate.HasValue)
        {
            return null;
        }

        var daysRemaining = (int)(user.ScheduledDeletionDate.Value - DateTime.UtcNow).TotalDays;

        return new DeletionStatus
        {
            IsPending = true,
            ScheduledDeletionDate = user.ScheduledDeletionDate,
            DaysRemaining = Math.Max(0, daysRemaining),
            Reason = user.DeletionReason
        };
    }

    private async Task RevokeAllSessionsAndTokensAsync(int userId)
    {
        // Revoke all refresh tokens
        var tokens = await _context.RefreshTokens.Where(t => t.UserId == userId && !t.IsRevoked).ToListAsync();
        foreach (var token in tokens)
        {
            token.IsRevoked = true;
        }

        // Delete all active sessions
        var sessions = await _context.UserSessions.Where(s => s.UserId == userId).ToListAsync();
        _context.UserSessions.RemoveRange(sessions);

        await _context.SaveChangesAsync();
    }

    private async Task SendDeletionConfirmationEmailAsync(User user, int gracePeriodDays)
    {
        try
        {
            if (string.IsNullOrEmpty(user.Email))
                return;

            var subject = "Account Deletion Scheduled";
            var body = $@"
                <h2>Account Deletion Scheduled</h2>
                <p>Hello {user.FirstName},</p>
                <p>Your account deletion has been scheduled for {user.ScheduledDeletionDate:MMMM dd, yyyy}.</p>
                <p>You have {gracePeriodDays} days to cancel this request if you change your mind.</p>
                <p>After this date, all your data will be permanently deleted and cannot be recovered.</p>
                <p>If you did not request this, please contact support immediately.</p>
            ";

            await _emailService.SendEmailAsync(user.Email, subject, body, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending deletion confirmation email to user {UserId}", user.Id);
        }
    }

    private async Task SendDeletionCancellationEmailAsync(User user)
    {
        try
        {
            if (string.IsNullOrEmpty(user.Email))
                return;

            var subject = "Account Deletion Cancelled";
            var body = $@"
                <h2>Account Deletion Cancelled</h2>
                <p>Hello {user.FirstName},</p>
                <p>Your account deletion request has been cancelled.</p>
                <p>Your account remains active and all your data is safe.</p>
            ";

            await _emailService.SendEmailAsync(user.Email, subject, body, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending deletion cancellation email to user {UserId}", user.Id);
        }
    }

    private bool VerifyPasswordHash(string password, string? storedHash)
    {
        if (string.IsNullOrEmpty(storedHash))
            return false;

        var parts = storedHash.Split(':');
        if (parts.Length != 2)
            return false;

        var salt = Convert.FromBase64String(parts[0]);
        var hash = Convert.FromBase64String(parts[1]);

        using var hmac = new HMACSHA512(salt);
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

        return computedHash.SequenceEqual(hash);
    }
}
