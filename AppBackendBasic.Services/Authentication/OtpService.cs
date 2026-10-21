using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;
using AppBackendBasic.Core.DTOs;

namespace AppBackendBasic.Services.Authentication;

public class OtpService : IOtpService
{
    private readonly ApplicationDbContext _context;
    private readonly ISmsService _smsService;
    private readonly IRateLimitingService _rateLimitingService;
    private readonly ILogger<OtpService> _logger;

    public OtpService(
        ApplicationDbContext context, 
        ISmsService smsService,
        IRateLimitingService rateLimitingService,
        ILogger<OtpService> logger)
    {
        _context = context;
        _smsService = smsService;
        _rateLimitingService = rateLimitingService;
        _logger = logger;
    }

    public async Task<string> GenerateAndStoreOtpAsync(SendOtpRequestDto sendOtpRequestDto)
    {
            var phoneNumber = sendOtpRequestDto.CountryCode+sendOtpRequestDto.PhoneNumber;
        try
        {
            // Check rate limiting - max 3 OTP requests per phone number per 15 minutes
            var rateLimitKey = $"otp_request:{phoneNumber}";
            var isAllowed = await _rateLimitingService.IsAllowedAsync(rateLimitKey, 3, TimeSpan.FromMinutes(15));
            
            if (!isAllowed)
            {
                _logger.LogWarning("Rate limit exceeded for OTP request: {PhoneNumber}", phoneNumber);
                throw new InvalidOperationException("Too many OTP requests. Please try again later.");
            }

            // Clean up any existing OTPs for this phone number
            var existingCount = await _context.OtpVerifications
                .Where(o => o.PhoneNumber == phoneNumber && !o.IsUsed)
                .AsNoTracking()
                .CountAsync();

            if (existingCount > 0)
            {
                await _context.OtpVerifications
                    .Where(o => o.PhoneNumber == phoneNumber && !o.IsUsed)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(o => o.IsUsed, true));
            }

            // Generate new OTP and session ID
            var otp = GenerateOtp();
            var sessionId = Guid.NewGuid().ToString();

            var otpVerification = new OtpVerification
            {
                PhoneNumber = phoneNumber,
                Otp = otp,
                SessionId = sessionId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5)
            };

            _context.OtpVerifications.Add(otpVerification);
            await _context.SaveChangesAsync();

            // Send OTP via SMS using TwilioSmsService
            var smsSent = await _smsService.SendOtpAsync(phoneNumber, otp);
            
            if (!smsSent)
            {
                _logger.LogError("Failed to send SMS OTP to {PhoneNumber}", phoneNumber);
                throw new InvalidOperationException("Failed to send OTP. Please try again.");
            }

            _logger.LogInformation("OTP generated and sent to phone number: {PhoneNumber}", phoneNumber);
            
            return sessionId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating OTP for phone number: {PhoneNumber}", phoneNumber);
            throw;
        }
    }

    public async Task<bool> ValidateOtpAsync(string requestorId, string countryCode, string otp, string sessionId, bool isEmail = false)
    {
        try
        {
            // Check rate limiting for OTP validation - max 5 attempts per session
            var rateLimitKey = $"otp_validate:{sessionId}";
            var isAllowed = await _rateLimitingService.IsAllowedAsync(rateLimitKey, 5, TimeSpan.FromMinutes(10));
            
            if (!isAllowed)
            {
                _logger.LogWarning("Rate limit exceeded for OTP validation: {SessionId}", sessionId);
                return false;
            }

            OtpVerification? otpVerification;
            
            if (isEmail)
            {
                otpVerification = await _context.OtpVerifications
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.SessionId == sessionId && 
                                             o.Email == requestorId && 
                                             !o.IsUsed && 
                                             o.ExpiresAt > DateTime.UtcNow);
            }
            else
            {
                otpVerification = await _context.OtpVerifications
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.SessionId == sessionId && 
                                             o.PhoneNumber == countryCode+requestorId && 
                                             !o.IsUsed && 
                                             o.ExpiresAt > DateTime.UtcNow);
            }

            if (otpVerification == null)
            {
                _logger.LogWarning("OTP validation failed: Invalid session or expired OTP for phone: {requestorId}", requestorId);
                return false;
            }

            // Check if max attempts exceeded
            if (otpVerification.AttemptCount >= otpVerification.MaxAttempts)
            {
                await _context.OtpVerifications
                    .Where(o => o.Id == otpVerification.Id)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(o => o.IsUsed, true));
                _logger.LogWarning("OTP validation failed: Max attempts exceeded for phone: {requestorId}", requestorId);
                return false;
            }

            // Validate OTP
            if (otpVerification.Otp != otp)
            {
                // Increment attempt count on failure
                await _context.OtpVerifications
                    .Where(o => o.Id == otpVerification.Id)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(o => o.AttemptCount, otpVerification.AttemptCount + 1));
                _logger.LogWarning("OTP validation failed: Invalid OTP for phone: {requestorId}", requestorId);
                return false;
            }

            // Mark OTP as used on success
            await _context.OtpVerifications
                .Where(o => o.Id == otpVerification.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(o => o.IsUsed, true));
            await _rateLimitingService.ResetAsync(rateLimitKey);

            _logger.LogInformation("OTP validation successful for phone: {requestorId}", requestorId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating OTP for phone: {requestorId}", requestorId);
            throw;
        }
    }
public async Task<bool> ValidateOtpEmailAsync(string emailId, string otp, string sessionId)
    {
        try
        {
            // Check rate limiting for OTP validation - max 5 attempts per session
            var rateLimitKey = $"otp_validate:{sessionId}";
            var isAllowed = await _rateLimitingService.IsAllowedAsync(rateLimitKey, 5, TimeSpan.FromMinutes(10));
            
            if (!isAllowed)
            {
                _logger.LogWarning("Rate limit exceeded for OTP validation: {SessionId}", sessionId);
                return false;
            }

            var otpVerification = await _context.OtpVerifications
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.SessionId == sessionId && 
                                         o.Email == emailId && 
                                         !o.IsUsed && 
                                         o.ExpiresAt > DateTime.UtcNow);

            if (otpVerification == null)
            {
                _logger.LogWarning("OTP validation failed: Invalid session or expired OTP for Email: {PhoneNumber}", emailId);
                return false;
            }

            // Check if max attempts exceeded
            if (otpVerification.AttemptCount >= otpVerification.MaxAttempts)
            {
                await _context.OtpVerifications
                    .Where(o => o.Id == otpVerification.Id)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(o => o.IsUsed, true));
                _logger.LogWarning("OTP validation failed: Max attempts exceeded for Email: {Email}", emailId);
                return false;
            }

            // Validate OTP
            if (otpVerification.Otp != otp)
            {
                // Increment attempt count on failure
                await _context.OtpVerifications
                    .Where(o => o.Id == otpVerification.Id)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(o => o.AttemptCount, otpVerification.AttemptCount + 1));
                _logger.LogWarning("OTP validation failed: Invalid OTP for phone: {Email}", emailId);
                return false;
            }

            // Mark OTP as used on success
            await _context.OtpVerifications
                .Where(o => o.Id == otpVerification.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(o => o.IsUsed, true));
            await _rateLimitingService.ResetAsync(rateLimitKey);

            _logger.LogInformation("OTP validation successful for phone: {Email}", emailId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating OTP for phone: {Email}", emailId);
            throw;
        }
    }
    public async Task<bool> IsOtpValidAsync(string sessionId)
    {
        try
        {
            var exists = await _context.OtpVerifications
                .AsNoTracking()
                .AnyAsync(o => o.SessionId == sessionId && 
                              !o.IsUsed && 
                              o.ExpiresAt > DateTime.UtcNow);

            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking OTP validity for session: {SessionId}", sessionId);
            throw;
        }
    }

    public async Task InvalidateOtpAsync(string sessionId)
    {
        try
        {
            await _context.OtpVerifications
                .Where(o => o.SessionId == sessionId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(o => o.IsUsed, true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating OTP for session: {SessionId}", sessionId);
            throw;
        }
    }

    public async Task CleanupExpiredOtpsAsync()
    {
        try
        {
            var deletedCount = await _context.OtpVerifications
                .Where(o => o.ExpiresAt < DateTime.UtcNow || o.IsUsed)
                .ExecuteDeleteAsync();

            if (deletedCount > 0)
            {
                _logger.LogInformation("Cleaned up {Count} expired OTPs", deletedCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired OTPs");
            throw;
        }
    }

    private string GenerateOtp()
    {
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }
}
