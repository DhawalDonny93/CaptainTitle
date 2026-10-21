using System;
using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;

namespace AppBackendBasic.Core.Interfaces;

public interface IEmailVerificationService
{
    // Email login OTP methods
    Task<string> SendEmailLoginOtpAsync(string email);
    Task<AuthResponseDto> VerifyEmailLoginOtpAsync(EmailOtpLoginRequestDto request);
    Task<string> ResendEmailOtpAsync(ResendEmailOtpRequestDto request);
    
    // Registration verification methods
    Task<string> SendRegistrationOtpAsync(User user);
    Task<AuthResponseDto> VerifyRegistrationOtpAsync(EmailOtpLoginRequestDto request);
    
    // Password reset methods
    Task<bool> ForgotPasswordAsync(ForgotPasswordRequestDto request);
    Task<bool> ResetPasswordAsync(ResetPasswordRequestDto request);
}
