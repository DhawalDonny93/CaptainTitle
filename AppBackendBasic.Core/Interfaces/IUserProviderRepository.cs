using AppBackendBasic.Core.Entities;

namespace AppBackendBasic.Core.Interfaces;

public interface IUserProviderRepository
{
    Task<UserProvider?> GetByIdAsync(int id);
    Task<UserProvider?> GetByProviderAndProviderIdAsync(string provider, string providerId);
    Task<IEnumerable<UserProvider>> GetByUserIdAsync(int userId);
    Task<UserProvider> CreateAsync(UserProvider userProvider);
    Task<UserProvider> UpdateAsync(UserProvider userProvider);
    Task DeleteAsync(int id);
    Task DeleteByUserIdAsync(int userId);
}
