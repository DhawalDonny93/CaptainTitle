using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;

namespace AppBackendBasic.Core.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> AppleSignInAsync(AppleSignInRequestDto request);
    Task<AuthResponseDto> GoogleSignInAsync(GoogleSignInRequestDto request);
    Task<AuthResponseDto> EmailLoginAsync(EmailLoginRequestDto request);
    Task<AuthResponseDto> EmailRegisterAsync(EmailRegisterRequestDto request);
    Task<string> SendOtpAsync(SendOtpRequestDto request);
    Task<AuthResponseDto> VerifyOtpAsync(VerifyOtpRequestDto request);
    Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request);
    Task<bool> RevokeTokenAsync(string refreshToken);
    Task<bool> LogoutAsync(string refreshToken);
    
}
