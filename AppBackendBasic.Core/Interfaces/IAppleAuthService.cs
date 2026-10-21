using AppBackendBasic.Core.DTOs;

namespace AppBackendBasic.Core.Interfaces;

/// <summary>
/// Service for handling Apple Sign-In authentication
/// </summary>
public interface IAppleAuthService
{
    /// <summary>
    /// Authenticates a user with Apple identity token
    /// </summary>
    /// <param name="request">Apple sign-in request containing identity token</param>
    /// <returns>Authentication response with tokens and user info</returns>
    Task<AuthResponseDto> AuthenticateWithAppleAsync(AppleSignInRequestDto request);
    
    /// <summary>
    /// Validates Apple identity token and extracts user information
    /// </summary>
    /// <param name="identityToken">JWT identity token from Apple</param>
    /// <returns>Decoded token payload</returns>
    Task<AppleIdTokenPayload?> ValidateIdentityTokenAsync(string identityToken);
}
