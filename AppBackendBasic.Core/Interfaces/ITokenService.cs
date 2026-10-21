using System.Security.Claims;
using AppBackendBasic.Core.Entities;

namespace AppBackendBasic.Core.Interfaces;

public interface ITokenService
{
    Task<string> GenerateAccessTokenAsync(User user);
    string GenerateRefreshToken();
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    bool ValidateToken(string token);
}
