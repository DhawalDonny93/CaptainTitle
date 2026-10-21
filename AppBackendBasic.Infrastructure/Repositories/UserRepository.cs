using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;

namespace AppBackendBasic.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(ApplicationDbContext context, ILogger<UserRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _context.Users
            .Include(u => u.UserProviders)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .Include(u => u.UserProviders)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
    }

    public async Task<User?> GetByPhoneNumberAsync(string phoneNumber)
    {
        return await _context.Users
            .Include(u => u.UserProviders)
            .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
    }

    public async Task<User?> GetByProviderAsync(string provider, string providerId)
    {
        return await _context.Users
            .Include(u => u.UserProviders)
            .FirstOrDefaultAsync(u => u.UserProviders.Any(up => up.Provider == provider && up.ProviderId == providerId));
    }

    public async Task<User> CreateAsync(User user)
    {
        var requestId = Guid.NewGuid().ToString("N").Substring(0, 8);
        _logger.LogInformation("[{RequestId}] CreateAsync START - Email: {Email}", requestId, user.Email);
        
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Add user to context and save to get the generated ID
            _logger.LogInformation("[{RequestId}] Adding User to context - Email: {Email}", requestId, user.Email);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            _logger.LogInformation("[{RequestId}] User saved - ID: {UserId}", requestId, user.Id);
            
            // Check if UserRole already exists (prevents duplicate on retry)
            var roleExists = await _context.UserRoles
                .AnyAsync(ur => ur.UserId == user.Id && ur.Role == Roles.User);
            
            if (!roleExists)
            {
                var userRole = new UserRole
                {
                    UserId = user.Id,
                    Role = Roles.User,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                
                _logger.LogInformation("[{RequestId}] Adding UserRole - UserId: {UserId}", requestId, user.Id);
                _context.UserRoles.Add(userRole);
                await _context.SaveChangesAsync();
                _logger.LogInformation("[{RequestId}] UserRole saved", requestId);
            }
            else
            {
                _logger.LogInformation("[{RequestId}] UserRole already exists, skipping", requestId);
            }
            
            await transaction.CommitAsync();
            _logger.LogInformation("[{RequestId}] CreateAsync COMPLETED - UserId: {UserId}", requestId, user.Id);
            return user;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "[{RequestId}] CreateAsync FAILED - Email: {Email}, Error: {Message}", 
                requestId, user.Email, ex.Message);
            throw;
        }
    }

    public async Task<User> UpdateAsync(User user)
    {
        // Ensure all DateTime fields are UTC for PostgreSQL compatibility
        user.UpdatedAt = DateTime.UtcNow;
        user.CreatedAt = DateTime.SpecifyKind(user.CreatedAt, DateTimeKind.Utc);
        
        // Convert nullable DateTime fields to UTC
        if (user.DateOfBirth.HasValue)
        {
            user.DateOfBirth = DateTime.SpecifyKind(user.DateOfBirth.Value, DateTimeKind.Utc);
        }
        if (user.LockoutEnd.HasValue)
        {
            user.LockoutEnd = DateTime.SpecifyKind(user.LockoutEnd.Value, DateTimeKind.Utc);
        }
        if (user.DeletedAt.HasValue)
        {
            user.DeletedAt = DateTime.SpecifyKind(user.DeletedAt.Value, DateTimeKind.Utc);
        }
        if (user.ScheduledDeletionDate.HasValue)
        {
            user.ScheduledDeletionDate = DateTime.SpecifyKind(user.ScheduledDeletionDate.Value, DateTimeKind.Utc);
        }
        
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task DeleteAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user != null)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
    {
        var refreshToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == token && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow);

        if (refreshToken != null)
        {
            // Ensure DateTime fields are UTC to avoid PostgreSQL errors when updating
            if (refreshToken.CreatedAt.Kind != DateTimeKind.Utc)
            {
                refreshToken.CreatedAt = DateTime.SpecifyKind(refreshToken.CreatedAt, DateTimeKind.Utc);
            }
            if (refreshToken.ExpiresAt.Kind != DateTimeKind.Utc)
            {
                refreshToken.ExpiresAt = DateTime.SpecifyKind(refreshToken.ExpiresAt, DateTimeKind.Utc);
            }
        }

        return refreshToken;
    }

    public async Task<RefreshToken> AddRefreshTokenAsync(RefreshToken refreshToken)
    {
        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();
        return refreshToken;
    }

    public async Task UpdateRefreshTokenAsync(RefreshToken refreshToken)
    {
        // Ensure DateTime fields are always UTC to avoid PostgreSQL "Kind=Unspecified" errors
        if (refreshToken.CreatedAt.Kind != DateTimeKind.Utc)
        {
            refreshToken.CreatedAt = DateTime.SpecifyKind(refreshToken.CreatedAt, DateTimeKind.Utc);
        }
        if (refreshToken.ExpiresAt.Kind != DateTimeKind.Utc)
        {
            refreshToken.ExpiresAt = DateTime.SpecifyKind(refreshToken.ExpiresAt, DateTimeKind.Utc);
        }

        _context.RefreshTokens.Update(refreshToken);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteRefreshTokenAsync(string token)
    {
        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token);
        
        if (refreshToken != null)
        {
            _context.RefreshTokens.Remove(refreshToken);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<UserSession> AddUserSessionAsync(UserSession userSession)
    {   
        _context.UserSessions.Add(userSession);
        await _context.SaveChangesAsync();
        return userSession;
    }

    public async Task UpdateUserSessionAsync(UserSession userSession)
    {
        userSession.LastActiveAt = DateTime.UtcNow;
        _context.UserSessions.Update(userSession);
        await _context.SaveChangesAsync();
    }
}
