using Microsoft.EntityFrameworkCore;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;

namespace AppBackendBasic.Infrastructure.Repositories;

public class UserProviderRepository : IUserProviderRepository
{
    private readonly ApplicationDbContext _context;

    public UserProviderRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserProvider?> GetByIdAsync(int id)
    {
        return await _context.UserProviders
            .Include(up => up.User)
            .FirstOrDefaultAsync(up => up.Id == id);
    }

    public async Task<UserProvider?> GetByProviderAndProviderIdAsync(string provider, string providerId)
    {
        return await _context.UserProviders
            .Include(up => up.User)
            .FirstOrDefaultAsync(up => up.Provider == provider && up.ProviderId == providerId);
    }

    public async Task<IEnumerable<UserProvider>> GetByUserIdAsync(int userId)
    {
        return await _context.UserProviders
            .Where(up => up.UserId == userId)
            .ToListAsync();
    }

    public async Task<UserProvider> CreateAsync(UserProvider userProvider)
    {
        _context.UserProviders.Add(userProvider);
        await _context.SaveChangesAsync();
        return userProvider;
    }

    public async Task<UserProvider> UpdateAsync(UserProvider userProvider)
    {
        _context.UserProviders.Update(userProvider);
        await _context.SaveChangesAsync();
        return userProvider;
    }

    public async Task DeleteAsync(int id)
    {
        var userProvider = await _context.UserProviders.FindAsync(id);
        if (userProvider != null)
        {
            _context.UserProviders.Remove(userProvider);
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteByUserIdAsync(int userId)
    {
        var userProviders = await _context.UserProviders
            .Where(up => up.UserId == userId)
            .ToListAsync();
            
        if (userProviders.Any())
        {
            _context.UserProviders.RemoveRange(userProviders);
            await _context.SaveChangesAsync();
        }
    }
}
