using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;

namespace AppBackendBasic.Core.Interfaces;

public interface IOtpService
{
    Task<string> GenerateAndStoreOtpAsync(SendOtpRequestDto sendOtpRequestDto);
    Task<bool> ValidateOtpAsync(string phoneNumber, string countryCode, string otp, string sessionId, bool isEmail = false);
    Task<bool> ValidateOtpEmailAsync(string emailId, string otp, string sessionId);
    Task<bool> IsOtpValidAsync(string sessionId);
    Task InvalidateOtpAsync(string sessionId);
    Task CleanupExpiredOtpsAsync();
}
