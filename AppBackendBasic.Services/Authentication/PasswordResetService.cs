using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace AppBackendBasic.Services.Authentication;

public class PasswordResetService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IRateLimitingService _rateLimitingService;
    private readonly ILogger<PasswordResetService> _logger;

    public PasswordResetService(
        ApplicationDbContext context,
        IEmailService emailService,
        IRateLimitingService rateLimitingService,
        ILogger<PasswordResetService> logger)
    {
        _context = context;
        _emailService = emailService;
        _rateLimitingService = rateLimitingService;
        _logger = logger;
    }

    public async Task<bool> SendPasswordResetEmailAsync(string email)
    {
        try
        {
            // Check rate limiting - max 3 password reset emails per email per hour
            var rateLimitKey = $"password_reset:{email}";
            var isAllowed = await _rateLimitingService.IsAllowedAsync(rateLimitKey, 3, TimeSpan.FromHours(1));
            
            if (!isAllowed)
            {
                _logger.LogWarning("Rate limit exceeded for password reset: {Email}", email);
                throw new InvalidOperationException("Too many password reset requests. Please try again later.");
            }

            // Check if user exists
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            
            if (user == null)
            {
                _logger.LogWarning("Password reset requested for non-existent user: {Email}", email);
                // Return true to prevent user enumeration attacks
                return true;
            }

            // Clean up any existing password resets for this email
            var existingResets = await _context.PasswordResets
                .Where(p => p.Email == email && !p.IsUsed)
                .ToListAsync();

            foreach (var existingReset in existingResets)
            {
                existingReset.IsUsed = true;
            }

            // Generate new reset token
            var token = GenerateResetToken();

            var passwordReset = new PasswordReset
            {
                Email = email,
                Token = token,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                UserId = user.Id
            };

            _context.PasswordResets.Add(passwordReset);
            await _context.SaveChangesAsync();

            // Send password reset email
            var emailSent = await _emailService.SendPasswordResetEmailAsync(email, token);
            
            if (!emailSent)
            {
                _logger.LogError("Failed to send password reset email to {Email}", email);
                throw new InvalidOperationException("Failed to send password reset email. Please try again.");
            }

            _logger.LogInformation("Password reset email sent to: {Email}", email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending password reset email to: {Email}", email);
            throw;
        }
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        try
        {
            var passwordReset = await _context.PasswordResets
                .FirstOrDefaultAsync(p => p.Token == token && 
                                         !p.IsUsed && 
                                         p.ExpiresAt > DateTime.UtcNow);

            if (passwordReset == null)
            {
                _logger.LogWarning("Password reset failed: Invalid or expired token");
                return false;
            }

            // Find user
            var user = await _context.Users.FindAsync(passwordReset.UserId);
            
            if (user == null)
            {
                _logger.LogWarning("Password reset failed: User not found for email: {Email}", passwordReset.Email);
                return false;
            }

            // Update password
            user.PasswordHash = HashPassword(newPassword);
            
            // Mark reset as used
            passwordReset.IsUsed = true;
            
            await _context.SaveChangesAsync();
            _logger.LogInformation("Password reset successful for: {Email}", passwordReset.Email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password");
            throw;
        }
    }

    private string GenerateResetToken()
    {
        return Guid.NewGuid().ToString("N");
    }
    
    private string HashPassword(string password)
    {
        // Generate a 128-bit salt using a cryptographically strong random sequence of bytes
        byte[] salt = new byte[128 / 8];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        // Derive a 256-bit subkey (use HMACSHA256 with 100,000 iterations)
        string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100000,
            numBytesRequested: 256 / 8));

        // Format: {algorithm}${iterations}${salt}${hash}
        return $"PBKDF2$100000${Convert.ToBase64String(salt)}${hashed}";
    }
}
