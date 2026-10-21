using AppBackendBasic.Core.Entities;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace AppBackendBasic.Services.Admin;

/// <summary>
/// Service for seeding initial admin user
/// </summary>
public class AdminSeedService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdminSeedService> _logger;

    public AdminSeedService(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<AdminSeedService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Seed initial admin user if no admin exists
    /// </summary>
    public async Task SeedAdminUserAsync()
    {
        try
        {
            // Check if any admin exists
            var adminExists = await _context.UserRoles
                .AnyAsync(ur => ur.Role == Roles.Admin);

            if (adminExists)
            {
                _logger.LogInformation("Admin user already exists. Skipping seed.");
                return;
            }

            // Get admin credentials from configuration
            var adminEmail = _configuration["AdminSeed:Email"];
            var adminPassword = _configuration["AdminSeed:Password"];
            var adminFirstName = _configuration["AdminSeed:FirstName"] ?? "Admin";
            var adminLastName = _configuration["AdminSeed:LastName"] ?? "User";

            if (string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminPassword))
            {
                _logger.LogWarning("Admin seed credentials not configured. Skipping admin user creation.");
                return;
            }

            // Check if user with this email already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == adminEmail);

            User adminUser;

            if (existingUser != null)
            {
                adminUser = existingUser;
                _logger.LogInformation("Using existing user {Email} for admin role", adminEmail);
            }
            else
            {
                // Create new admin user
                adminUser = new User
                {
                    Email = adminEmail,
                    FirstName = adminFirstName,
                    LastName = adminLastName,
                    PasswordHash = CreatePasswordHash(adminPassword),
                    IsActive = true,
                    EmailVerified = true,
                    OnboardingCompleted = true,
                    AgeVerified = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Users.Add(adminUser);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created new admin user: {Email}", adminEmail);
            }

            // Add Admin role
            var adminRole = new UserRole
            {
                UserId = adminUser.Id,
                Role = Roles.Admin,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.UserRoles.Add(adminRole);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Admin role assigned to user: {Email}", adminEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding admin user");
            throw;
        }
    }

    /// <summary>
    /// Assign admin role to existing user
    /// </summary>
    public async Task<bool> AssignAdminRoleAsync(int userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", userId);
                return false;
            }

            // Check if user already has admin role
            var hasAdminRole = await _context.UserRoles
                .AnyAsync(ur => ur.UserId == userId && ur.Role == Roles.Admin);

            if (hasAdminRole)
            {
                _logger.LogInformation("User {UserId} already has admin role", userId);
                return true;
            }

            var adminRole = new UserRole
            {
                UserId = userId,
                Role = Roles.Admin,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.UserRoles.Add(adminRole);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Admin role assigned to user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning admin role to user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Remove admin role from user
    /// </summary>
    public async Task<bool> RemoveAdminRoleAsync(int userId)
    {
        try
        {
            var adminRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.Role == Roles.Admin);

            if (adminRole == null)
            {
                _logger.LogWarning("User {UserId} does not have admin role", userId);
                return false;
            }

            _context.UserRoles.Remove(adminRole);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Admin role removed from user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing admin role from user {UserId}", userId);
            return false;
        }
    }

    private string CreatePasswordHash(string password)
    {
        using var hmac = new HMACSHA512();
        var salt = hmac.Key;
        var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        
        // Use salt:hash format to match AuthService
        return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);
    }
}
