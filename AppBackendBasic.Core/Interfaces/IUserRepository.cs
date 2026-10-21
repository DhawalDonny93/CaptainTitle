using AppBackendBasic.Core.Entities;

namespace AppBackendBasic.Core.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByPhoneNumberAsync(string phoneNumber);
    Task<User?> GetByProviderAsync(string provider, string providerId);
    Task<User> CreateAsync(User user);
    Task<User> UpdateAsync(User user);
    Task DeleteAsync(int id);
    Task<RefreshToken?> GetRefreshTokenAsync(string token);
    Task<RefreshToken> AddRefreshTokenAsync(RefreshToken refreshToken);
    Task UpdateRefreshTokenAsync(RefreshToken refreshToken);
    Task DeleteRefreshTokenAsync(string token);
    Task<UserSession> AddUserSessionAsync(UserSession userSession);
    Task UpdateUserSessionAsync(UserSession userSession);
}
