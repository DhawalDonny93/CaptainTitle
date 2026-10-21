using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Services.Admin;
using Microsoft.EntityFrameworkCore;
using AppBackendBasic.Infrastructure.Data;
using System.Security.Claims;

namespace AppBackendBasic.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")] // Admin role required for all endpoints
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly AdminSeedService _adminSeedService;
    private readonly IPushNotificationService _pushNotificationService;
    private readonly IInAppNotificationService _inAppNotificationService;
    private readonly SystemSettingsService _systemSettingsService;
    private readonly IAdminMonitoringService _monitoringService;
    private readonly IFeatureFlagService _featureFlagService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly ILlmJobConfigService _llmJobConfigService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        ApplicationDbContext context,
        AdminSeedService adminSeedService,
        IPushNotificationService pushNotificationService,
        IInAppNotificationService inAppNotificationService,
        SystemSettingsService systemSettingsService,
        IAdminMonitoringService monitoringService,
        IFeatureFlagService featureFlagService,
        IEmailTemplateService emailTemplateService,
        ILlmJobConfigService llmJobConfigService,
        ILogger<AdminController> logger)
    {
        _context = context;
        _adminSeedService = adminSeedService;
        _pushNotificationService = pushNotificationService;
        _inAppNotificationService = inAppNotificationService;
        _systemSettingsService = systemSettingsService;
        _monitoringService = monitoringService;
        _featureFlagService = featureFlagService;
        _emailTemplateService = emailTemplateService;
        _llmJobConfigService = llmJobConfigService;
        _logger = logger;
    }

    /// <summary>
    /// Get dashboard statistics
    /// </summary>
    [HttpGet("dashboard/stats")]
    public async Task<IActionResult> GetDashboardStats()
    {
        try
        {
            var totalUsers = await _context.Users.CountAsync();
            var activeUsers = await _context.Users.CountAsync(u => u.IsActive && !u.IsDeleted);
            var verifiedUsers = await _context.Users.CountAsync(u => u.EmailVerified);
            var adminUsers = await _context.UserRoles.CountAsync(ur => ur.Role == Roles.Admin);

            return Ok(new
            {
                success = true,
                stats = new
                {
                    totalUsers,
                    activeUsers,
                    verifiedUsers,
                    adminUsers,
                    deletedUsers = totalUsers - activeUsers
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard stats");
            return StatusCode(500, new { message = "Error getting stats" });
        }
    }

    /// <summary>
    /// Get all users with decrypted phone numbers (Admin only)
    /// </summary>
    [HttpGet("users/phone-numbers")]
    public async Task<IActionResult> GetUsersWithPhoneNumbers()
    {
        try
        {

            var users = await _context.Set<Core.Entities.User>()
                .Where(u => u.PhoneNumber != null)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    PhoneNumber = u.PhoneNumber, // Will be decrypted automatically by EF Core
                    u.PhoneVerified,
                    u.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                count = users.Count,
                users = users
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users with phone numbers");
            return StatusCode(500, new { message = "Error retrieving users" });
        }
    }

    /// <summary>
    /// Get specific user's decrypted phone number
    /// </summary>
    [HttpGet("users/{userId}/phone-number")]
    public async Task<IActionResult> GetUserPhoneNumber(int userId)
    {
        try
        {
            var user = await _context.Set<Core.Entities.User>()
                .Where(u => u.Id == userId)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.FirstName,
                    PhoneNumber = u.PhoneNumber, // Decrypted automatically
                    u.PhoneVerified
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound(new { message = "User not found" });

            return Ok(new
            {
                success = true,
                user = user
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user phone number for {UserId}", userId);
            return StatusCode(500, new { message = "Error retrieving user" });
        }
    }

    /// <summary>
    /// Search users by phone number (decrypted search)
    /// </summary>
    [HttpGet("users/search-by-phone")]
    public async Task<IActionResult> SearchByPhoneNumber([FromQuery] string phoneNumber)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return BadRequest(new { message = "Phone number is required" });

            // EF Core will decrypt and compare
            var users = await _context.Set<Core.Entities.User>()
                .Where(u => u.PhoneNumber == phoneNumber)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    PhoneNumber = u.PhoneNumber,
                    u.PhoneVerified,
                    u.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                count = users.Count,
                users = users
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching users by phone number");
            return StatusCode(500, new { message = "Error searching users" });
        }
    }

    /// <summary>
    /// Get encryption statistics
    /// </summary>
    [HttpGet("encryption/stats")]
    public async Task<IActionResult> GetEncryptionStats()
    {
        try
        {
            var totalUsers = await _context.Set<Core.Entities.User>().CountAsync();
            var usersWithPhone = await _context.Set<Core.Entities.User>()
                .CountAsync(u => u.PhoneNumber != null);
            var usersWithDob = await _context.Set<Core.Entities.User>()
                .CountAsync(u => u.DateOfBirth != null);

            return Ok(new
            {
                success = true,
                stats = new
                {
                    totalUsers = totalUsers,
                    usersWithEncryptedPhone = usersWithPhone,
                    usersWithEncryptedDob = usersWithDob,
                    encryptionMethod = "ASP.NET Core Data Protection",
                    encryptedFields = new[] { "PhoneNumber", "DateOfBirth" }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting encryption stats");
            return StatusCode(500, new { message = "Error getting stats" });
        }
    }

    /// <summary>
    /// Assign admin role to a user
    /// </summary>
    [HttpPost("users/{userId}/assign-admin")]
    public async Task<IActionResult> AssignAdminRole(int userId)
    {
        try
        {
            var success = await _adminSeedService.AssignAdminRoleAsync(userId);

            if (!success)
                return NotFound(new { message = "User not found" });

            return Ok(new
            {
                success = true,
                message = "Admin role assigned successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning admin role to user {UserId}", userId);
            return StatusCode(500, new { message = "Error assigning admin role" });
        }
    }

    /// <summary>
    /// Remove admin role from a user
    /// </summary>
    [HttpDelete("users/{userId}/remove-admin")]
    public async Task<IActionResult> RemoveAdminRole(int userId)
    {
        try
        {
            var success = await _adminSeedService.RemoveAdminRoleAsync(userId);

            if (!success)
                return NotFound(new { message = "User does not have admin role" });

            return Ok(new
            {
                success = true,
                message = "Admin role removed successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing admin role from user {UserId}", userId);
            return StatusCode(500, new { message = "Error removing admin role" });
        }
    }

    /// <summary>
    /// Get all admin users
    /// </summary>
    [HttpGet("users/admins")]
    public async Task<IActionResult> GetAdminUsers()
    {
        try
        {
            var adminUsers = await _context.UserRoles
                .Where(ur => ur.Role == Roles.Admin)
                .Include(ur => ur.User)
                .Select(ur => new
                {
                    ur.User.Id,
                    ur.User.Email,
                    ur.User.FirstName,
                    ur.User.LastName,
                    ur.User.IsActive,
                    ur.CreatedAt,
                    RoleAssignedAt = ur.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                count = adminUsers.Count,
                admins = adminUsers
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting admin users");
            return StatusCode(500, new { message = "Error getting admin users" });
        }
    }

    /// <summary>
    /// Get user by email
    /// </summary>
    [HttpGet("users/by-email/{email}")]
    public async Task<IActionResult> GetUserByEmail(string email)
    {
        try
        {
            var user = await _context.Users
                .Where(u => u.Email == email && !u.IsDeleted)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    u.IsActive,
                    u.EmailVerified,
                    u.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound(new { message = "User not found" });

            return Ok(new
            {
                success = true,
                user
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by email {Email}", email);
            return StatusCode(500, new { message = "Error getting user" });
        }
    }

    /// <summary>
    /// Check if current user is admin
    /// </summary>
    [HttpGet("auth/check")]
    [AllowAnonymous]
    public IActionResult CheckAdminAuth()
    {
        var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
        var isAdmin = User.IsInRole(Roles.Admin);
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Ok(new
        {
            success = true,
            isAuthenticated,
            isAdmin,
            userId,
            roles = User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList()
        });
    }

    /// <summary>
    /// Get all users with pagination
    /// </summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = "CreatedAt",
        [FromQuery] string? sortOrder = "desc")
    {
        try
        {
            var query = _context.Users.AsQueryable();

            // Apply sorting
            query = sortBy?.ToLower() switch
            {
                "email" => sortOrder?.ToLower() == "asc" 
                    ? query.OrderBy(u => u.Email) 
                    : query.OrderByDescending(u => u.Email),
                "firstname" => sortOrder?.ToLower() == "asc" 
                    ? query.OrderBy(u => u.FirstName) 
                    : query.OrderByDescending(u => u.FirstName),
                "createdat" => sortOrder?.ToLower() == "asc" 
                    ? query.OrderBy(u => u.CreatedAt) 
                    : query.OrderByDescending(u => u.CreatedAt),
                _ => query.OrderByDescending(u => u.CreatedAt)
            };

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var users = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    u.IsActive,
                    u.EmailVerified,
                    u.PhoneVerified,
                    u.IsDeleted,
                    u.CreatedAt,
                    u.UpdatedAt,
                    Roles = _context.UserRoles
                        .Where(ur => ur.UserId == u.Id)
                        .Select(ur => ur.Role)
                        .ToList()
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                page,
                pageSize,
                totalCount,
                totalPages,
                users
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users");
            return StatusCode(500, new { message = "Error retrieving users" });
        }
    }

    /// <summary>
    /// Search users by email, name, or phone
    /// </summary>
    [HttpGet("users/search")]
    public async Task<IActionResult> SearchUsers(
        [FromQuery] string query,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest(new { message = "Search query is required" });

            var searchQuery = query.ToLower();

            var usersQuery = _context.Users
                .Where(u => 
                    (u.Email != null && u.Email.ToLower().Contains(searchQuery)) ||
                    (u.FirstName != null && u.FirstName.ToLower().Contains(searchQuery)) ||
                    (u.LastName != null && u.LastName.ToLower().Contains(searchQuery)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(query)));

            var totalCount = await usersQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var users = await usersQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    u.PhoneNumber,
                    u.IsActive,
                    u.EmailVerified,
                    u.IsDeleted,
                    u.CreatedAt,
                    Roles = _context.UserRoles
                        .Where(ur => ur.UserId == u.Id)
                        .Select(ur => ur.Role)
                        .ToList()
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                query,
                page,
                pageSize,
                totalCount,
                totalPages,
                users
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching users");
            return StatusCode(500, new { message = "Error searching users" });
        }
    }

    /// <summary>
    /// Get user details by ID
    /// </summary>
    [HttpGet("users/{userId}")]
    public async Task<IActionResult> GetUserDetails(int userId)
    {
        try
        {
            var user = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    u.PhoneNumber,
                    u.Gender,
                    u.DateOfBirth,
                    u.ProfilePhotoUrl,
                    u.IsActive,
                    u.EmailVerified,
                    u.PhoneVerified,
                    u.OnboardingCompleted,
                    u.AgeVerified,
                    u.RequiresParentalConsent,
                    u.ParentalConsentGranted,
                    u.FailedLoginAttempts,
                    u.LockoutEnd,
                    u.IsDeleted,
                    u.DeletedAt,
                    u.ScheduledDeletionDate,
                    u.CreatedAt,
                    u.UpdatedAt,
                    Roles = _context.UserRoles
                        .Where(ur => ur.UserId == u.Id)
                        .Select(ur => ur.Role)
                        .ToList(),
                    SessionCount = _context.Set<UserSession>()
                        .Count(s => s.UserId == u.Id && s.ExpiresAt > DateTime.UtcNow),
                    SubscriptionCount = _context.Set<Subscription>()
                        .Count(s => s.UserId == u.Id)
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound(new { message = "User not found" });

            return Ok(new
            {
                success = true,
                user
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user details for {UserId}", userId);
            return StatusCode(500, new { message = "Error retrieving user details" });
        }
    }

    /// <summary>
    /// Suspend user account
    /// </summary>
    [HttpPut("users/{userId}/suspend")]
    public async Task<IActionResult> SuspendUser(int userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return NotFound(new { message = "User not found" });

            if (!user.IsActive)
                return BadRequest(new { message = "User is already suspended" });

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            // Invalidate all active sessions by setting expiry to now
            var activeSessions = await _context.Set<UserSession>()
                .Where(s => s.UserId == userId && s.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();

            foreach (var session in activeSessions)
            {
                session.ExpiresAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} suspended by admin", userId);

            return Ok(new
            {
                success = true,
                message = "User suspended successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suspending user {UserId}", userId);
            return StatusCode(500, new { message = "Error suspending user" });
        }
    }

    /// <summary>
    /// Activate user account
    /// </summary>
    [HttpPut("users/{userId}/activate")]
    public async Task<IActionResult> ActivateUser(int userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return NotFound(new { message = "User not found" });

            if (user.IsActive)
                return BadRequest(new { message = "User is already active" });

            user.IsActive = true;
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} activated by admin", userId);

            return Ok(new
            {
                success = true,
                message = "User activated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating user {UserId}", userId);
            return StatusCode(500, new { message = "Error activating user" });
        }
    }

    /// <summary>
    /// Toggle user status (suspend/activate)
    /// </summary>
    [HttpPost("users/{userId}/toggle-status")]
    public async Task<IActionResult> ToggleUserStatus(int userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return NotFound(new { message = "User not found" });

            // Toggle status
            user.IsActive = !user.IsActive;
            user.UpdatedAt = DateTime.UtcNow;

            if (!user.IsActive)
            {
                // If suspending, invalidate all active sessions
                var activeSessions = await _context.Set<UserSession>()
                    .Where(s => s.UserId == userId && s.ExpiresAt > DateTime.UtcNow)
                    .ToListAsync();

                foreach (var session in activeSessions)
                {
                    session.ExpiresAt = DateTime.UtcNow;
                }
            }
            else
            {
                // If activating, clear lockout
                user.FailedLoginAttempts = 0;
                user.LockoutEnd = null;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} status toggled to {Status} by admin", userId, user.IsActive ? "active" : "suspended");

            return Ok(new
            {
                success = true,
                message = user.IsActive ? "User activated successfully" : "User suspended successfully",
                isActive = user.IsActive
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling user status for {UserId}", userId);
            return StatusCode(500, new { message = "Error toggling user status" });
        }
    }

    /// <summary>
    /// Delete user account (admin override)
    /// </summary>
    [HttpDelete("users/{userId}")]
    public async Task<IActionResult> DeleteUser(int userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return NotFound(new { message = "User not found" });

            // Check if user is an admin
            var isAdmin = await _context.UserRoles
                .AnyAsync(ur => ur.UserId == userId && ur.Role == Roles.Admin);

            if (isAdmin)
            {
                var adminCount = await _context.UserRoles
                    .CountAsync(ur => ur.Role == Roles.Admin);

                if (adminCount <= 1)
                    return BadRequest(new { message = "Cannot delete the last admin user" });
            }

            // Mark as deleted
            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} deleted by admin", userId);

            return Ok(new
            {
                success = true,
                message = "User deleted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", userId);
            return StatusCode(500, new { message = "Error deleting user" });
        }
    }

    /// <summary>
    /// Get user statistics
    /// </summary>
    [HttpGet("users/statistics")]
    public async Task<IActionResult> GetUserStatistics()
    {
        try
        {
            var totalUsers = await _context.Users.CountAsync();
            var activeUsers = await _context.Users.CountAsync(u => u.IsActive && !u.IsDeleted);
            var suspendedUsers = await _context.Users.CountAsync(u => !u.IsActive && !u.IsDeleted);
            var deletedUsers = await _context.Users.CountAsync(u => u.IsDeleted);
            var verifiedUsers = await _context.Users.CountAsync(u => u.EmailVerified);
            var unverifiedUsers = await _context.Users.CountAsync(u => !u.EmailVerified);

            // Users created in last 30 days
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var newUsers = await _context.Users.CountAsync(u => u.CreatedAt >= thirtyDaysAgo);

            // Users by role
            var usersByRole = await _context.UserRoles
                .GroupBy(ur => ur.Role)
                .Select(g => new { Role = g.Key, Count = g.Count() })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                statistics = new
                {
                    totalUsers,
                    activeUsers,
                    suspendedUsers,
                    deletedUsers,
                    verifiedUsers,
                    unverifiedUsers,
                    newUsersLast30Days = newUsers,
                    usersByRole
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user statistics");
            return StatusCode(500, new { message = "Error getting statistics" });
        }
    }

    // ===== NOTIFICATION PUSHING ENDPOINTS =====

    /// <summary>
    /// Send notification to specific user
    /// </summary>
    [HttpPost("notifications/send-to-user")]
    public async Task<IActionResult> SendNotificationToUser([FromBody] SendNotificationRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Body))
                return BadRequest(new { message = "Title and body are required" });

            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null)
                return NotFound(new { message = "User not found" });

            // Create in-app notification
            var inAppNotification = await _inAppNotificationService.CreateNotificationAsync(
                request.UserId,
                request.Title,
                request.Body,
                request.Type ?? "info",
                request.Category,
                request.ActionUrl,
                request.ActionText,
                request.Priority ?? 0);

            // Send push notification
            var pushMessage = new PushNotificationMessage
            {
                Title = request.Title,
                Body = request.Body,
                Category = request.Category,
                Data = request.Data?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? string.Empty)
            };

            var pushResult = await _pushNotificationService.SendNotificationAsync(request.UserId, pushMessage);

            _logger.LogInformation("Admin sent notification to user {UserId}", request.UserId);

            return Ok(new
            {
                success = true,
                message = "Notification sent successfully",
                inAppNotificationId = inAppNotification.Id,
                pushResult = new
                {
                    pushResult.Success,
                    pushResult.SuccessCount,
                    pushResult.FailureCount
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to user");
            return StatusCode(500, new { message = "Error sending notification" });
        }
    }

    /// <summary>
    /// Broadcast notification to all users
    /// </summary>
    [AllowAnonymous]
    [HttpPost("notifications/broadcast")]
    public async Task<IActionResult> BroadcastNotification([FromBody] BroadcastNotificationRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Body))
                return BadRequest(new { message = "Title and body are required" });

            // Get all active users
            var userIds = await _context.Users
                .Where(u => u.IsActive && !u.IsDeleted)
                .Select(u => u.Id)
                .ToListAsync();

            int inAppCreated = 0;
            int pushSent = 0;
            int pushFailed = 0;

            foreach (var userId in userIds)
            {
                try
                {
                    // Create in-app notification
                    await _inAppNotificationService.CreateNotificationAsync(
                        userId,
                        request.Title,
                        request.Body,
                        request.Type ?? "info",
                        request.Category,
                        request.ActionUrl,
                        request.ActionText,
                        request.Priority ?? 0);
                    inAppCreated++;

                    // Send push notification
                    var pushMessage = new PushNotificationMessage
                    {
                        Title = request.Title,
                        Body = request.Body,
                        Category = request.Category,
                        Data = request.Data?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? string.Empty)
                    };

                    var pushResult = await _pushNotificationService.SendNotificationAsync(userId, pushMessage);
                    if (pushResult.Success)
                        pushSent += pushResult.SuccessCount;
                    else
                        pushFailed += pushResult.FailureCount;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending notification to user {UserId}", userId);
                }
            }

            _logger.LogInformation("Admin broadcast notification to {Count} users", userIds.Count);

            return Ok(new
            {
                success = true,
                message = "Broadcast completed",
                results = new
                {
                    totalUsers = userIds.Count,
                    inAppNotificationsCreated = inAppCreated,
                    pushNotificationsSent = pushSent,
                    pushNotificationsFailed = pushFailed
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting notification");
            return StatusCode(500, new { message = "Error broadcasting notification" });
        }
    }

    /// <summary>
    /// Send notification to user segment
    /// </summary>
    [HttpPost("notifications/send-to-segment")]
    public async Task<IActionResult> SendNotificationToSegment([FromBody] SendToSegmentRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Body))
                return BadRequest(new { message = "Title and body are required" });

            if (string.IsNullOrWhiteSpace(request.Segment))
                return BadRequest(new { message = "Segment is required" });

            // Build query based on segment
            var query = _context.Users.Where(u => u.IsActive && !u.IsDeleted);

            query = request.Segment.ToLower() switch
            {
                "verified" => query.Where(u => u.EmailVerified),
                "unverified" => query.Where(u => !u.EmailVerified),
                "subscribers" => query.Where(u => _context.Set<Subscription>().Any(s => s.UserId == u.Id && s.Status == "active")),
                "free" => query.Where(u => !_context.Set<Subscription>().Any(s => s.UserId == u.Id && s.Status == "active")),
                "new" => query.Where(u => u.CreatedAt >= DateTime.UtcNow.AddDays(-30)),
                _ => query
            };

            var userIds = await query.Select(u => u.Id).ToListAsync();

            if (!userIds.Any())
                return BadRequest(new { message = $"No users found in segment '{request.Segment}'" });

            int inAppCreated = 0;
            int pushSent = 0;
            int pushFailed = 0;

            foreach (var userId in userIds)
            {
                try
                {
                    // Create in-app notification
                    await _inAppNotificationService.CreateNotificationAsync(
                        userId,
                        request.Title,
                        request.Body,
                        request.Type ?? "info",
                        request.Category,
                        request.ActionUrl,
                        request.ActionText,
                        request.Priority ?? 0);
                    inAppCreated++;

                    // Send push notification
                    var pushMessage = new PushNotificationMessage
                    {
                        Title = request.Title,
                        Body = request.Body,
                        Category = request.Category,
                        Data = request.Data?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? string.Empty)
                    };

                    var pushResult = await _pushNotificationService.SendNotificationAsync(userId, pushMessage);
                    if (pushResult.Success)
                        pushSent += pushResult.SuccessCount;
                    else
                        pushFailed += pushResult.FailureCount;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending notification to user {UserId}", userId);
                }
            }

            _logger.LogInformation("Admin sent notification to segment '{Segment}' ({Count} users)", request.Segment, userIds.Count);

            return Ok(new
            {
                success = true,
                message = $"Notification sent to {request.Segment} segment",
                results = new
                {
                    segment = request.Segment,
                    totalUsers = userIds.Count,
                    inAppNotificationsCreated = inAppCreated,
                    pushNotificationsSent = pushSent,
                    pushNotificationsFailed = pushFailed
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to segment");
            return StatusCode(500, new { message = "Error sending notification" });
        }
    }

    // ===== ANALYTICS ENDPOINTS =====

    /// <summary>
    /// Get notification history
    /// </summary>
    [HttpGet("notifications/history")]
    public async Task<IActionResult> GetNotificationHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var skip = (page - 1) * pageSize;
            
            // Get in-app notifications with aggregated stats
            var notifications = await _context.Set<InAppNotification>()
                .OrderByDescending(n => n.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .GroupBy(n => new { n.Title, n.Body, n.Type, n.CreatedAt.Date })
                .Select(g => new
                {
                    id = Guid.NewGuid(),
                    title = g.Key.Title,
                    message = g.Key.Body,
                    type = g.Count() > 1 ? "Broadcast" : "Individual",
                    recipients = g.Count(),
                    sentAt = g.Max(n => n.CreatedAt),
                    status = "Delivered"
                })
                .ToListAsync();

            var totalCount = await _context.Set<InAppNotification>().CountAsync();

            return Ok(new
            {
                success = true,
                page,
                pageSize,
                totalCount,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                notifications
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification history");
            return StatusCode(500, new { message = "Error getting notification history" });
        }
    }

    /// <summary>
    /// Get notification statistics
    /// </summary>
    [HttpGet("notifications/stats")]
    public async Task<IActionResult> GetNotificationStats()
    {
        try
        {
            var now = DateTime.UtcNow;
            var today = now.Date;
            var weekAgo = now.AddDays(-7);
            var monthAgo = now.AddDays(-30);

            var allNotifications = await _context.Set<InAppNotification>().ToListAsync();

            var totalSent = allNotifications.Count;
            var sentToday = allNotifications.Count(n => n.CreatedAt.Date == today);
            var sentThisWeek = allNotifications.Count(n => n.CreatedAt >= weekAgo);
            var sentThisMonth = allNotifications.Count(n => n.CreatedAt >= monthAgo);

            var readNotifications = allNotifications.Count(n => n.IsRead);
            var deliveryRate = totalSent > 0 ? (double)totalSent / totalSent * 100 : 100;
            var openRate = totalSent > 0 ? (double)readNotifications / totalSent * 100 : 0;

            return Ok(new
            {
                success = true,
                totalSent,
                sentToday,
                sentThisWeek,
                sentThisMonth,
                deliveryRate = Math.Round(deliveryRate, 1),
                openRate = Math.Round(openRate, 1)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification stats");
            return StatusCode(500, new { message = "Error getting notification stats" });
        }
    }

    /// <summary>
    /// Get audit logs
    /// </summary>
    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 50,
        [FromQuery] string? action = null,
        [FromQuery] string? userId = null)
    {
        try
        {
            var skip = (page - 1) * pageSize;
            
            var query = _context.Set<AuditLog>().AsQueryable();

            if (!string.IsNullOrWhiteSpace(action))
                query = query.Where(a => a.Action.Contains(action));

            if (!string.IsNullOrWhiteSpace(userId) && int.TryParse(userId, out var userInt))
                query = query.Where(a => a.UserId == userInt);

            var totalCount = await query.CountAsync();
            
            var logs = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .Select(a => new
                {
                    id = a.Id,
                    userId = a.UserId,
                    action = a.Action,
                    entityType = a.EntityType,
                    entityId = a.EntityId,
                    changes = a.Changes,
                    ipAddress = a.IpAddress,
                    userAgent = a.UserAgent,
                    timestamp = a.CreatedAt,
                    success = a.Success,
                    errorMessage = a.ErrorMessage
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                page,
                pageSize,
                totalCount,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                logs
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit logs");
            return StatusCode(500, new { message = "Error getting audit logs" });
        }
    }

    /// <summary>
    /// Get aggregated analytics (AdminUI compatibility endpoint)
    /// </summary>
    [HttpGet("analytics")]
    public async Task<IActionResult> GetAggregatedAnalytics([FromQuery] string period = "30")
    {
        try
        {
            int days = int.TryParse(period, out var d) ? d : 30;

            var userGrowth = await GetUserGrowthData(days);
            var engagement = await GetEngagementData(days);
            var subscriptions = await GetSubscriptionData(days);
            var notifications = await GetNotificationData(days);

            return Ok(new
            {
                success = true,
                period = $"{days} days",
                userGrowth,
                engagement,
                subscriptions,
                notifications
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting aggregated analytics");
            return StatusCode(500, new { message = "Error getting analytics" });
        }
    }

    private async Task<object> GetUserGrowthData(int days)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);
        var newUsers = await _context.Users.CountAsync(u => u.CreatedAt >= startDate);
        var totalUsers = await _context.Users.CountAsync(u => !u.IsDeleted);
        return new { newUsers, totalUsers, days };
    }

    private async Task<object> GetEngagementData(int days)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);
        var activeSessions = await _context.Set<UserSession>()
            .CountAsync(s => s.CreatedAt >= startDate);
        return new { activeSessions, days };
    }

    private async Task<object> GetSubscriptionData(int days)
    {
        var activeSubscriptions = await _context.Set<Subscription>()
            .CountAsync(s => s.Status == "active");
        return new { activeSubscriptions, days };
    }

    private async Task<object> GetNotificationData(int days)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);
        var sentCount = await _context.Set<InAppNotification>()
            .CountAsync(n => n.CreatedAt >= startDate);
        return new { sentCount, days };
    }

    /// <summary>
    /// Get user growth analytics
    /// </summary>
    [HttpGet("analytics/user-growth")]
    public async Task<IActionResult> GetUserGrowthAnalytics([FromQuery] int days = 30)
    {
        try
        {
            var startDate = DateTime.UtcNow.AddDays(-days);
            
            var dailySignups = await _context.Users
                .Where(u => u.CreatedAt >= startDate)
                .GroupBy(u => u.CreatedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            var totalUsers = await _context.Users.CountAsync();
            var activeUsers = await _context.Users.CountAsync(u => u.IsActive && !u.IsDeleted);
            var newUsersInPeriod = await _context.Users.CountAsync(u => u.CreatedAt >= startDate);

            return Ok(new
            {
                success = true,
                period = $"Last {days} days",
                totalUsers,
                activeUsers,
                newUsersInPeriod,
                dailySignups
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user growth analytics");
            return StatusCode(500, new { message = "Error getting analytics" });
        }
    }

    /// <summary>
    /// Get engagement metrics
    /// </summary>
    [HttpGet("analytics/engagement")]
    public async Task<IActionResult> GetEngagementMetrics([FromQuery] int days = 30)
    {
        try
        {
            var startDate = DateTime.UtcNow.AddDays(-days);

            // Active sessions
            var activeSessions = await _context.Set<UserSession>()
                .CountAsync(s => s.ExpiresAt > DateTime.UtcNow);

            // Daily active users (users with sessions in last 24 hours)
            var oneDayAgo = DateTime.UtcNow.AddDays(-1);
            var dailyActiveUsers = await _context.Set<UserSession>()
                .Where(s => s.LastActiveAt >= oneDayAgo)
                .Select(s => s.UserId)
                .Distinct()
                .CountAsync();

            // Weekly active users
            var oneWeekAgo = DateTime.UtcNow.AddDays(-7);
            var weeklyActiveUsers = await _context.Set<UserSession>()
                .Where(s => s.LastActiveAt >= oneWeekAgo)
                .Select(s => s.UserId)
                .Distinct()
                .CountAsync();

            // Monthly active users
            var oneMonthAgo = DateTime.UtcNow.AddDays(-30);
            var monthlyActiveUsers = await _context.Set<UserSession>()
                .Where(s => s.LastActiveAt >= oneMonthAgo)
                .Select(s => s.UserId)
                .Distinct()
                .CountAsync();

            // Platform distribution
            var platformDistribution = await _context.Set<UserSession>()
                .Where(s => s.ExpiresAt > DateTime.UtcNow)
                .GroupBy(s => s.Platform)
                .Select(g => new
                {
                    Platform = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                activeSessions,
                dailyActiveUsers,
                weeklyActiveUsers,
                monthlyActiveUsers,
                platformDistribution
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting engagement metrics");
            return StatusCode(500, new { message = "Error getting metrics" });
        }
    }

    /// <summary>
    /// Get subscription analytics
    /// </summary>
    [HttpGet("analytics/subscriptions")]
    public async Task<IActionResult> GetSubscriptionAnalytics([FromQuery] int days = 30)
    {
        try
        {
            var startDate = DateTime.UtcNow.AddDays(-days);

            var totalSubscriptions = await _context.Set<Subscription>().CountAsync();
            var activeSubscriptions = await _context.Set<Subscription>()
                .CountAsync(s => s.Status == "active");
            var canceledSubscriptions = await _context.Set<Subscription>()
                .CountAsync(s => s.Status == "canceled");
            var expiredSubscriptions = await _context.Set<Subscription>()
                .CountAsync(s => s.Status == "expired");

            // New subscriptions in period
            var newSubscriptions = await _context.Set<Subscription>()
                .CountAsync(s => s.PurchaseDate >= startDate);

            // Subscriptions by platform
            var subscriptionsByPlatform = await _context.Set<Subscription>()
                .Where(s => s.Status == "active")
                .GroupBy(s => s.Platform)
                .Select(g => new
                {
                    Platform = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            // Subscriptions by product
            var subscriptionsByProduct = await _context.Set<Subscription>()
                .Where(s => s.Status == "active")
                .GroupBy(s => s.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            // Daily subscription trend
            var dailySubscriptions = await _context.Set<Subscription>()
                .Where(s => s.PurchaseDate >= startDate)
                .GroupBy(s => s.PurchaseDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            return Ok(new
            {
                success = true,
                period = $"Last {days} days",
                totalSubscriptions,
                activeSubscriptions,
                canceledSubscriptions,
                expiredSubscriptions,
                newSubscriptions,
                subscriptionsByPlatform,
                subscriptionsByProduct,
                dailySubscriptions
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription analytics");
            return StatusCode(500, new { message = "Error getting analytics" });
        }
    }

    /// <summary>
    /// Get notification analytics
    /// </summary>
    [HttpGet("analytics/notifications")]
    public async Task<IActionResult> GetNotificationAnalytics([FromQuery] int days = 30)
    {
        try
        {
            var startDate = DateTime.UtcNow.AddDays(-days);

            // In-app notifications
            var totalInAppNotifications = await _context.Set<InAppNotification>()
                .CountAsync(n => n.CreatedAt >= startDate);
            var readInAppNotifications = await _context.Set<InAppNotification>()
                .CountAsync(n => n.CreatedAt >= startDate && n.IsRead);
            var unreadInAppNotifications = await _context.Set<InAppNotification>()
                .CountAsync(n => n.CreatedAt >= startDate && !n.IsRead);

            // Device tokens (push notification capability)
            var totalDeviceTokens = await _context.Set<DeviceToken>().CountAsync();
            var activeDeviceTokens = await _context.Set<DeviceToken>()
                .CountAsync(d => d.IsActive);

            // Device tokens by platform
            var deviceTokensByPlatform = await _context.Set<DeviceToken>()
                .Where(d => d.IsActive)
                .GroupBy(d => d.Platform)
                .Select(g => new
                {
                    Platform = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            // Daily notification trend
            var dailyNotifications = await _context.Set<InAppNotification>()
                .Where(n => n.CreatedAt >= startDate)
                .GroupBy(n => n.CreatedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            // Notification read rate
            var readRate = totalInAppNotifications > 0 
                ? (double)readInAppNotifications / totalInAppNotifications * 100 
                : 0;

            return Ok(new
            {
                success = true,
                period = $"Last {days} days",
                inAppNotifications = new
                {
                    total = totalInAppNotifications,
                    read = readInAppNotifications,
                    unread = unreadInAppNotifications,
                    readRate = Math.Round(readRate, 2)
                },
                pushNotifications = new
                {
                    totalDeviceTokens,
                    activeDeviceTokens,
                    deviceTokensByPlatform
                },
                dailyNotifications
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification analytics");
            return StatusCode(500, new { message = "Error getting analytics" });
        }
    }

    /// <summary>
    /// Get comprehensive dashboard summary
    /// </summary>
    [HttpGet("analytics/dashboard-summary")]
    public async Task<IActionResult> GetDashboardSummary()
    {
        try
        {
            var now = DateTime.UtcNow;
            var oneDayAgo = now.AddDays(-1);
            var oneWeekAgo = now.AddDays(-7);
            var oneMonthAgo = now.AddDays(-30);

            // User metrics
            var totalUsers = await _context.Users.CountAsync();
            var activeUsers = await _context.Users.CountAsync(u => u.IsActive && !u.IsDeleted);
            var newUsersToday = await _context.Users.CountAsync(u => u.CreatedAt >= oneDayAgo);
            var newUsersThisWeek = await _context.Users.CountAsync(u => u.CreatedAt >= oneWeekAgo);
            var newUsersThisMonth = await _context.Users.CountAsync(u => u.CreatedAt >= oneMonthAgo);

            // Subscription metrics
            var activeSubscriptions = await _context.Set<Subscription>()
                .CountAsync(s => s.Status == "active");
            var newSubscriptionsToday = await _context.Set<Subscription>()
                .CountAsync(s => s.PurchaseDate >= oneDayAgo);
            var newSubscriptionsThisWeek = await _context.Set<Subscription>()
                .CountAsync(s => s.PurchaseDate >= oneWeekAgo);

            // Engagement metrics
            var activeSessions = await _context.Set<UserSession>()
                .CountAsync(s => s.ExpiresAt > now);
            var dailyActiveUsers = await _context.Set<UserSession>()
                .Where(s => s.LastActiveAt >= oneDayAgo)
                .Select(s => s.UserId)
                .Distinct()
                .CountAsync();

            // Notification metrics
            var unreadNotifications = await _context.Set<InAppNotification>()
                .CountAsync(n => !n.IsRead);
            var notificationsSentToday = await _context.Set<InAppNotification>()
                .CountAsync(n => n.CreatedAt >= oneDayAgo);

            return Ok(new
            {
                success = true,
                timestamp = now,
                users = new
                {
                    total = totalUsers,
                    active = activeUsers,
                    newToday = newUsersToday,
                    newThisWeek = newUsersThisWeek,
                    newThisMonth = newUsersThisMonth
                },
                subscriptions = new
                {
                    active = activeSubscriptions,
                    newToday = newSubscriptionsToday,
                    newThisWeek = newSubscriptionsThisWeek
                },
                engagement = new
                {
                    activeSessions,
                    dailyActiveUsers
                },
                notifications = new
                {
                    unread = unreadNotifications,
                    sentToday = notificationsSentToday
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard summary");
            return StatusCode(500, new { message = "Error getting summary" });
        }
    }

    // ===== MONETIZATION & REVENUE ENDPOINTS =====

    /// <summary>
    /// Get churn analysis
    /// </summary>
    [HttpGet("analytics/churn")]
    public async Task<IActionResult> GetChurnAnalysis([FromQuery] int days = 30)
    {
        try
        {
            var startDate = DateTime.UtcNow.AddDays(-days);

            // Total subscriptions at start of period
            var totalSubscriptionsStart = await _context.Set<Subscription>()
                .CountAsync(s => s.PurchaseDate < startDate);

            // Canceled subscriptions in period
            var canceledInPeriod = await _context.Set<Subscription>()
                .CountAsync(s => s.Status == "canceled" && s.UpdatedAt >= startDate);

            // Expired subscriptions in period
            var expiredInPeriod = await _context.Set<Subscription>()
                .CountAsync(s => s.Status == "expired" && s.ExpiryDate >= startDate && s.ExpiryDate <= DateTime.UtcNow);

            // Calculate churn rate
            var totalChurned = canceledInPeriod + expiredInPeriod;
            var churnRate = totalSubscriptionsStart > 0 
                ? (double)totalChurned / totalSubscriptionsStart * 100 
                : 0;

            // Daily churn trend
            var dailyChurn = await _context.Set<Subscription>()
                .Where(s => (s.Status == "canceled" || s.Status == "expired") && s.UpdatedAt >= startDate)
                .GroupBy(s => s.UpdatedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            // Churn by platform
            var churnByPlatform = await _context.Set<Subscription>()
                .Where(s => (s.Status == "canceled" || s.Status == "expired") && s.UpdatedAt >= startDate)
                .GroupBy(s => s.Platform)
                .Select(g => new
                {
                    Platform = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                period = $"Last {days} days",
                churnMetrics = new
                {
                    totalSubscriptionsStart,
                    canceledInPeriod,
                    expiredInPeriod,
                    totalChurned,
                    churnRate = Math.Round(churnRate, 2)
                },
                dailyChurn,
                churnByPlatform
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting churn analysis");
            return StatusCode(500, new { message = "Error getting churn analysis" });
        }
    }

    /// <summary>
    /// Get revenue reporting
    /// </summary>
    [HttpGet("analytics/revenue")]
    public async Task<IActionResult> GetRevenueReport([FromQuery] int days = 30)
    {
        try
        {
            var startDate = DateTime.UtcNow.AddDays(-days);

            // Note: Receipt entity doesn't have Price field, so we'll track counts
            // In a real implementation, you'd need to add price tracking

            // Active subscriptions value (count by product)
            var activeSubscriptionsByProduct = await _context.Set<Subscription>()
                .Where(s => s.Status == "active")
                .GroupBy(s => s.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            // MRR estimation (Monthly Recurring Revenue) - based on active subscriptions
            var activeSubscriptions = await _context.Set<Subscription>()
                .CountAsync(s => s.Status == "active");

            return Ok(new
            {
                success = true,
                period = $"Last {days} days",
                subscriptions = new
                {
                    activeCount = activeSubscriptions,
                    activeByProduct = activeSubscriptionsByProduct,
                    estimatedMRR = $"{activeSubscriptions} active subscriptions"
                },
                note = "Revenue tracking via RevenueCat webhooks - Receipt entity removed"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting revenue report");
            return StatusCode(500, new { message = "Error getting revenue report" });
        }
    }

    /// <summary>
    /// Get subscription lifecycle metrics
    /// </summary>
    [HttpGet("analytics/subscription-lifecycle")]
    public async Task<IActionResult> GetSubscriptionLifecycle([FromQuery] int days = 30)
    {
        try
        {
            var startDate = DateTime.UtcNow.AddDays(-days);

            // New subscriptions
            var newSubscriptions = await _context.Set<Subscription>()
                .CountAsync(s => s.PurchaseDate >= startDate);

            // Renewals (subscriptions with auto-renew enabled)
            var renewals = await _context.Set<Subscription>()
                .CountAsync(s => s.Status == "active" && s.AutoRenew && s.PurchaseDate >= startDate);

            // Cancellations
            var cancellations = await _context.Set<Subscription>()
                .CountAsync(s => s.Status == "canceled" && s.UpdatedAt >= startDate);

            // Expirations
            var expirations = await _context.Set<Subscription>()
                .CountAsync(s => s.Status == "expired" && s.ExpiryDate >= startDate);

            // Trial conversions (subscriptions that were trials and are now active)
            var activeNonTrials = await _context.Set<Subscription>()
                .CountAsync(s => s.Status == "active" && !s.IsSandbox);

            // Lifecycle by product
            var lifecycleByProduct = await _context.Set<Subscription>()
                .Where(s => s.PurchaseDate >= startDate)
                .GroupBy(s => new { s.ProductId, s.Status })
                .Select(g => new
                {
                    g.Key.ProductId,
                    g.Key.Status,
                    Count = g.Count()
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                period = $"Last {days} days",
                lifecycle = new
                {
                    newSubscriptions,
                    renewals,
                    cancellations,
                    expirations,
                    activeNonTrials,
                    netGrowth = newSubscriptions - cancellations - expirations
                },
                lifecycleByProduct
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription lifecycle");
            return StatusCode(500, new { message = "Error getting subscription lifecycle" });
        }
    }

    // ===== SYSTEM SETTINGS ENDPOINTS =====

    /// <summary>
    /// Get all system settings
    /// </summary>
    [HttpGet("settings")]
    public async Task<IActionResult> GetAllSettings()
    {
        try
        {
            var settings = await _systemSettingsService.GetAllSettingsAsync();

            return Ok(new
            {
                success = true,
                count = settings.Count,
                settings
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system settings");
            return StatusCode(500, new { message = "Error getting settings" });
        }
    }

    /// <summary>
    /// Get settings by category
    /// </summary>
    [HttpGet("settings/category/{category}")]
    public async Task<IActionResult> GetSettingsByCategory(string category)
    {
        try
        {
            var settings = await _systemSettingsService.GetSettingsByCategoryAsync(category);

            return Ok(new
            {
                success = true,
                category,
                count = settings.Count,
                settings
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting settings for category {Category}", category);
            return StatusCode(500, new { message = "Error getting settings" });
        }
    }

    /// <summary>
    /// Get specific setting
    /// </summary>
    [HttpGet("settings/{key}")]
    public async Task<IActionResult> GetSetting(string key)
    {
        try
        {
            var setting = await _systemSettingsService.GetSettingAsync(key);

            if (setting == null)
                return NotFound(new { message = "Setting not found" });

            return Ok(new
            {
                success = true,
                setting
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting setting {Key}", key);
            return StatusCode(500, new { message = "Error getting setting" });
        }
    }

    /// <summary>
    /// Create or update setting
    /// </summary>
    [HttpPut("settings/{key}")]
    public async Task<IActionResult> UpsertSetting(string key, [FromBody] UpsertSettingRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Value))
                return BadRequest(new { message = "Value is required" });

            var setting = await _systemSettingsService.UpsertSettingAsync(
                key,
                request.Value,
                request.Type ?? "string",
                request.Category ?? "general",
                request.Description,
                request.IsPublic ?? false);

            return Ok(new
            {
                success = true,
                message = "Setting updated successfully",
                setting
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting setting {Key}", key);
            return StatusCode(500, new { message = "Error updating setting" });
        }
    }

    /// <summary>
    /// Delete setting
    /// </summary>
    [HttpDelete("settings/{key}")]
    public async Task<IActionResult> DeleteSetting(string key)
    {
        try
        {
            var success = await _systemSettingsService.DeleteSettingAsync(key);

            if (!success)
                return NotFound(new { message = "Setting not found" });

            return Ok(new
            {
                success = true,
                message = "Setting deleted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting setting {Key}", key);
            return StatusCode(500, new { message = "Error deleting setting" });
        }
    }

    /// <summary>
    /// Bulk update settings (AdminUI compatibility endpoint)
    /// </summary>
    [HttpPost("settings/update")]
    public async Task<IActionResult> BulkUpdateSettings([FromBody] BulkUpdateSettingsRequest request)
    {
        try
        {
            if (request.Settings == null || !request.Settings.Any())
                return BadRequest(new { message = "No settings provided" });

            var updatedSettings = new List<SystemSetting>();
            var errors = new List<string>();

            foreach (var setting in request.Settings)
            {
                try
                {
                    var updated = await _systemSettingsService.UpsertSettingAsync(
                        setting.Key,
                        setting.Value,
                        setting.Type ?? "string",
                        setting.Category ?? "general",
                        setting.Description,
                        setting.IsPublic ?? false);
                    
                    updatedSettings.Add(updated);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating setting {Key}", setting.Key);
                    errors.Add($"Failed to update {setting.Key}: {ex.Message}");
                }
            }

            return Ok(new
            {
                success = errors.Count == 0,
                message = errors.Count == 0 
                    ? "All settings updated successfully" 
                    : $"{updatedSettings.Count} settings updated, {errors.Count} failed",
                updatedCount = updatedSettings.Count,
                failedCount = errors.Count,
                errors = errors.Any() ? errors : null,
                settings = updatedSettings
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk updating settings");
            return StatusCode(500, new { message = "Error updating settings" });
        }
    }

    // ============================================
    // PHASE 7: ADMIN CONSOLE ENHANCEMENTS
    // ============================================

    /// <summary>
    /// Get admin dashboard overview with analytics and health
    /// </summary>
    [HttpGet("dashboard/overview")]
    public async Task<IActionResult> GetAdminDashboardOverview()
    {
        try
        {
            var overview = await _monitoringService.GetAdminDashboardAsync();
            return Ok(overview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting admin dashboard overview");
            return StatusCode(500, new { message = "Error getting dashboard overview" });
        }
    }

    /// <summary>
    /// Get system health metrics
    /// </summary>
    [HttpGet("monitoring/health")]
    public async Task<IActionResult> GetSystemHealth()
    {
        try
        {
            var health = await _monitoringService.GetSystemHealthAsync();
            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system health");
            return StatusCode(500, new { message = "Error getting system health" });
        }
    }

    /// <summary>
    /// Get platform analytics
    /// </summary>
    [HttpGet("analytics/platform")]
    public async Task<IActionResult> GetPlatformAnalytics([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var dateRange = new DateRangeDto
            {
                StartDate = startDate ?? DateTime.UtcNow.AddDays(-30),
                EndDate = endDate ?? DateTime.UtcNow
            };

            var analytics = await _monitoringService.GetPlatformAnalyticsAsync(dateRange);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting platform analytics");
            return StatusCode(500, new { message = "Error getting analytics" });
        }
    }

    /// <summary>
    /// Get usage statistics
    /// </summary>
    [HttpGet("analytics/usage")]
    public async Task<IActionResult> GetUsageStatistics([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var dateRange = new DateRangeDto
            {
                StartDate = startDate ?? DateTime.UtcNow.AddDays(-30),
                EndDate = endDate ?? DateTime.UtcNow
            };

            var usage = await _monitoringService.GetUsageStatisticsAsync(dateRange);
            return Ok(usage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting usage statistics");
            return StatusCode(500, new { message = "Error getting usage statistics" });
        }
    }

    /// <summary>
    /// Get cost analytics
    /// </summary>
    [HttpGet("analytics/costs")]
    public async Task<IActionResult> GetCostAnalytics([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var dateRange = new DateRangeDto
            {
                StartDate = startDate ?? DateTime.UtcNow.AddDays(-30),
                EndDate = endDate ?? DateTime.UtcNow
            };

            var costs = await _monitoringService.GetCostAnalyticsAsync(dateRange);
            return Ok(costs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cost analytics");
            return StatusCode(500, new { message = "Error getting cost analytics" });
        }
    }

    /// <summary>
    /// Get admin audit logs
    /// </summary>
    [HttpGet("admin-audit-logs")]
    public async Task<IActionResult> GetAdminAuditLogs([FromQuery] int? adminUserId = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        try
        {
            var logs = await _monitoringService.GetAuditLogsAsync(adminUserId, page, pageSize);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit logs");
            return StatusCode(500, new { message = "Error getting audit logs" });
        }
    }

    /// <summary>
    /// Get all feature flags
    /// </summary>
    [HttpGet("feature-flags")]
    public async Task<IActionResult> GetFeatureFlags()
    {
        try
        {
            var flags = await _featureFlagService.GetAllFlagsAsync();
            return Ok(flags);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting feature flags");
            return StatusCode(500, new { message = "Error getting feature flags" });
        }
    }

    /// <summary>
    /// Create a new feature flag
    /// </summary>
    [HttpPost("feature-flags")]
    public async Task<IActionResult> CreateFeatureFlag([FromBody] CreateFeatureFlagDto dto)
    {
        try
        {
            var flag = await _featureFlagService.CreateFlagAsync(dto);
            return Ok(flag);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating feature flag");
            return StatusCode(500, new { message = "Error creating feature flag" });
        }
    }

    /// <summary>
    /// Update a feature flag
    /// </summary>
    [HttpPut("feature-flags/{id}")]
    public async Task<IActionResult> UpdateFeatureFlag(int id, [FromBody] UpdateFeatureFlagDto dto)
    {
        try
        {
            var flag = await _featureFlagService.UpdateFlagAsync(id, dto);
            if (flag == null)
                return NotFound(new { message = "Feature flag not found" });

            return Ok(flag);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating feature flag {Id}", id);
            return StatusCode(500, new { message = "Error updating feature flag" });
        }
    }

    /// <summary>
    /// Delete a feature flag
    /// </summary>
    [HttpDelete("feature-flags/{id}")]
    public async Task<IActionResult> DeleteFeatureFlag(int id)
    {
        try
        {
            var success = await _featureFlagService.DeleteFlagAsync(id);
            if (!success)
                return NotFound(new { message = "Feature flag not found" });

            return Ok(new { message = "Feature flag deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting feature flag {Id}", id);
            return StatusCode(500, new { message = "Error deleting feature flag" });
        }
    }

    /// <summary>
    /// Check if a feature flag is enabled
    /// </summary>
    [HttpGet("feature-flags/{flagKey}/enabled")]
    public async Task<IActionResult> IsFeatureEnabled(string flagKey, [FromQuery] int? userId = null)
    {
        try
        {
            var isEnabled = await _featureFlagService.IsFlagEnabledAsync(flagKey, userId);
            return Ok(new { flagKey, isEnabled });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking feature flag {FlagKey}", flagKey);
            return StatusCode(500, new { message = "Error checking feature flag" });
        }
    }

    /// <summary>
    /// Toggle a feature flag on/off
    /// </summary>
    [HttpPost("feature-flags/{id}/toggle")]
    public async Task<IActionResult> ToggleFeatureFlag(int id)
    {
        try
        {
            var flag = await _context.FeatureFlags.FindAsync(id);
            if (flag == null)
                return NotFound(new { message = "Feature flag not found" });

            flag.IsEnabled = !flag.IsEnabled;
            flag.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            return Ok(flag);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling feature flag {Id}", id);
            return StatusCode(500, new { message = "Error toggling feature flag" });
        }
    }

    /// <summary>
    /// Export audit logs to CSV or JSON
    /// </summary>
    [HttpGet("audit-logs/export")]
    public async Task<IActionResult> ExportAuditLogs(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] string format = "csv")
    {
        try
        {
            var logs = await _context.AuditLogs
                .Where(l => l.CreatedAt >= startDate && l.CreatedAt <= endDate)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            if (format.ToLower() == "json")
            {
                return File(
                    System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(logs)),
                    "application/json",
                    $"audit-logs-{DateTime.UtcNow:yyyyMMdd}.json"
                );
            }

            // CSV export
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Id,UserId,Action,EntityType,EntityId,IpAddress,CreatedAt");
            
            foreach (var log in logs)
            {
                csv.AppendLine($"{log.Id},{log.UserId},{log.Action},{log.EntityType},{log.EntityId},{log.IpAddress},{log.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            }

            return File(
                System.Text.Encoding.UTF8.GetBytes(csv.ToString()),
                "text/csv",
                $"audit-logs-{DateTime.UtcNow:yyyyMMdd}.csv"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting audit logs");
            return StatusCode(500, new { message = "Error exporting audit logs" });
        }
    }

    // ============================================
    // EMAIL TEMPLATES MANAGEMENT
    // ============================================

    /// <summary>
    /// Get all email templates
    /// </summary>
    [HttpGet("email-templates")]
    public async Task<IActionResult> GetEmailTemplates()
    {
        try
        {
            var templates = await _emailTemplateService.GetAllTemplatesAsync();
            return Ok(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email templates");
            return StatusCode(500, new { message = "Error getting email templates" });
        }
    }

    /// <summary>
    /// Get email template by ID
    /// </summary>
    [HttpGet("email-templates/{id}")]
    public async Task<IActionResult> GetEmailTemplate(int id)
    {
        try
        {
            var template = await _emailTemplateService.GetTemplateByIdAsync(id);
            if (template == null)
                return NotFound(new { message = "Email template not found" });

            return Ok(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email template {Id}", id);
            return StatusCode(500, new { message = "Error getting email template" });
        }
    }

    /// <summary>
    /// Create email template
    /// </summary>
    [HttpPost("email-templates")]
    public async Task<IActionResult> CreateEmailTemplate([FromBody] CreateEmailTemplateDto dto)
    {
        try
        {
            var template = await _emailTemplateService.CreateTemplateAsync(dto);
            return Ok(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating email template");
            return StatusCode(500, new { message = "Error creating email template" });
        }
    }

    /// <summary>
    /// Update email template
    /// </summary>
    [HttpPut("email-templates/{id}")]
    public async Task<IActionResult> UpdateEmailTemplate(int id, [FromBody] UpdateEmailTemplateDto dto)
    {
        try
        {
            var template = await _emailTemplateService.UpdateTemplateAsync(id, dto);
            if (template == null)
                return NotFound(new { message = "Email template not found" });

            return Ok(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating email template {Id}", id);
            return StatusCode(500, new { message = "Error updating email template" });
        }
    }

    /// <summary>
    /// Delete email template
    /// </summary>
    [HttpDelete("email-templates/{id}")]
    public async Task<IActionResult> DeleteEmailTemplate(int id)
    {
        try
        {
            var success = await _emailTemplateService.DeleteTemplateAsync(id);
            if (!success)
                return NotFound(new { message = "Email template not found" });

            return Ok(new { message = "Email template deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting email template {Id}", id);
            return StatusCode(500, new { message = "Error deleting email template" });
        }
    }

    // ============================================
    // LLM PROVIDERS ADMIN MANAGEMENT
    // ============================================

    /// <summary>
    /// Get all LLM providers (admin)
    /// </summary>
    [HttpGet("llm-providers")]
    public async Task<IActionResult> GetLLMProviders()
    {
        try
        {
            var providers = await _context.LlmProviders
                .AsNoTracking()
                .OrderBy(p => p.Priority)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.DisplayName,
                    p.ApiEndpoint,
                    p.IsActive,
                    p.Priority,
                    p.MaxTokens,
                    p.CostPer1kInputTokens,
                    p.CostPer1kOutputTokens,
                    p.RateLimitPerMinute,
                    p.CreatedAt,
                    p.UpdatedAt
                })
                .ToListAsync();

            return Ok(providers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting LLM providers");
            return StatusCode(500, new { message = "Error getting LLM providers" });
        }
    }

    /// <summary>
    /// Get LLM provider by ID (admin)
    /// </summary>
    [HttpGet("llm-providers/{id}")]
    public async Task<IActionResult> GetLLMProvider(int id)
    {
        try
        {
            var provider = await _context.LlmProviders
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);
                
            if (provider == null)
                return NotFound(new { message = "LLM provider not found" });

            // Return DTO to avoid circular reference issues
            var dto = new
            {
                provider.Id,
                provider.Name,
                provider.DisplayName,
                provider.ApiEndpoint,
                provider.IsActive,
                provider.Priority,
                provider.MaxTokens,
                provider.CostPer1kInputTokens,
                provider.CostPer1kOutputTokens,
                provider.RateLimitPerMinute,
                provider.CreatedAt,
                provider.UpdatedAt
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting LLM provider {Id}", id);
            return StatusCode(500, new { message = "Error getting LLM provider" });
        }
    }

    /// <summary>
    /// Create LLM provider (admin)
    /// </summary>
    [HttpPost("llm-providers")]
    public async Task<IActionResult> CreateLLMProvider([FromBody] CreateLLMProviderDto dto)
    {
        try
        {
            var provider = new Core.Entities.LlmProvider
            {
                Name = dto.Name,
                DisplayName = dto.DisplayName ?? dto.Name,
                ApiEndpoint = dto.ApiEndpoint,
                IsActive = dto.IsActive ?? true,
                Priority = dto.Priority ?? 0,
                MaxTokens = dto.MaxTokens ?? 4000,
                CostPer1kInputTokens = dto.CostPer1kInputTokens,
                CostPer1kOutputTokens = dto.CostPer1kOutputTokens,
                RateLimitPerMinute = dto.RateLimitPerMinute ?? 60,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.LlmProviders.Add(provider);
            await _context.SaveChangesAsync();

            // Add API key as config if provided
            if (!string.IsNullOrEmpty(dto.ApiKey))
            {
                var config = new Core.Entities.LlmProviderConfig
                {
                    ProviderId = provider.Id,
                    ConfigKey = "ApiKey",
                    ConfigValue = dto.ApiKey,
                    IsEncrypted = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.LlmProviderConfigs.Add(config);
                await _context.SaveChangesAsync();
            }

            return Ok(provider);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating LLM provider");
            return StatusCode(500, new { message = "Error creating LLM provider" });
        }
    }

    /// <summary>
    /// Update LLM provider (admin)
    /// </summary>
    [HttpPut("llm-providers/{id}")]
    public async Task<IActionResult> UpdateLLMProvider(int id, [FromBody] UpdateLLMProviderDto dto)
    {
        try
        {
            var provider = await _context.LlmProviders.FindAsync(id);
            if (provider == null)
                return NotFound(new { message = "LLM provider not found" });

            if (!string.IsNullOrEmpty(dto.Name)) provider.Name = dto.Name;
            if (!string.IsNullOrEmpty(dto.DisplayName)) provider.DisplayName = dto.DisplayName;
            if (!string.IsNullOrEmpty(dto.ApiEndpoint)) provider.ApiEndpoint = dto.ApiEndpoint;
            if (dto.IsActive.HasValue) provider.IsActive = dto.IsActive.Value;
            if (dto.Priority.HasValue) provider.Priority = dto.Priority.Value;
            if (dto.MaxTokens.HasValue) provider.MaxTokens = dto.MaxTokens.Value;
            if (dto.CostPer1kInputTokens.HasValue) provider.CostPer1kInputTokens = dto.CostPer1kInputTokens.Value;
            if (dto.CostPer1kOutputTokens.HasValue) provider.CostPer1kOutputTokens = dto.CostPer1kOutputTokens.Value;
            if (dto.RateLimitPerMinute.HasValue) provider.RateLimitPerMinute = dto.RateLimitPerMinute.Value;
            
            provider.UpdatedAt = DateTime.UtcNow;

            // Update API key if provided
            if (!string.IsNullOrEmpty(dto.ApiKey))
            {
                var apiKeyConfig = await _context.LlmProviderConfigs
                    .FirstOrDefaultAsync(c => c.ProviderId == id && c.ConfigKey == "ApiKey");
                
                if (apiKeyConfig != null)
                {
                    apiKeyConfig.ConfigValue = dto.ApiKey;
                    apiKeyConfig.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    _context.LlmProviderConfigs.Add(new Core.Entities.LlmProviderConfig
                    {
                        ProviderId = id,
                        ConfigKey = "ApiKey",
                        ConfigValue = dto.ApiKey,
                        IsEncrypted = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();
            return Ok(provider);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating LLM provider {Id}", id);
            return StatusCode(500, new { message = "Error updating LLM provider" });
        }
    }

    /// <summary>
    /// Delete LLM provider (admin)
    /// </summary>
    [HttpDelete("llm-providers/{id}")]
    public async Task<IActionResult> DeleteLLMProvider(int id)
    {
        try
        {
            var provider = await _context.LlmProviders.FindAsync(id);
            if (provider == null)
                return NotFound(new { message = "LLM provider not found" });

            _context.LlmProviders.Remove(provider);
            await _context.SaveChangesAsync();

            return Ok(new { message = "LLM provider deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting LLM provider {Id}", id);
            return StatusCode(500, new { message = "Error deleting LLM provider" });
        }
    }

    /// <summary>
    /// Test LLM provider connection (admin)
    /// </summary>
    [HttpPost("llm-providers/{id}/test")]
    public async Task<IActionResult> TestLLMProvider(int id)
    {
        try
        {
            var provider = await _context.LlmProviders.FindAsync(id);
            if (provider == null)
                return NotFound(new { message = "LLM provider not found" });

            // Simple test - check if provider is active
            return Ok(new 
            { 
                success = provider.IsActive,
                message = provider.IsActive ? "Provider is active" : "Provider is inactive",
                provider = provider.Name
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing LLM provider {Id}", id);
            return StatusCode(500, new { message = "Error testing LLM provider" });
        }
    }

    // ============================================
    // LLM JOB CONFIGURATION (per-job provider+model)
    // ============================================

    /// <summary>
    /// Get all LLM job configs
    /// </summary>
    [HttpGet("llm-job-configs")]
    public async Task<IActionResult> GetLlmJobConfigs()
    {
        try
        {
            var configs = await _llmJobConfigService.GetAllAsync();
            return Ok(configs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting LLM job configs");
            return StatusCode(500, new { message = "Error getting LLM job configs" });
        }
    }

    /// <summary>
    /// Get a single LLM job config by job key
    /// </summary>
    [HttpGet("llm-job-configs/{jobKey}")]
    public async Task<IActionResult> GetLlmJobConfig(string jobKey)
    {
        try
        {
            var config = await _llmJobConfigService.GetByKeyAsync(jobKey);
            if (config == null)
                return NotFound(new { message = $"No config found for job key '{jobKey}'" });

            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting LLM job config {JobKey}", jobKey);
            return StatusCode(500, new { message = "Error getting LLM job config" });
        }
    }

    /// <summary>
    /// Create or update a LLM job config (upsert by JobKey)
    /// </summary>
    [HttpPut("llm-job-configs/{jobKey}")]
    public async Task<IActionResult> UpsertLlmJobConfig(string jobKey, [FromBody] UpsertLlmJobConfigDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Model))
                return BadRequest(new { message = "Model is required" });

            if (string.IsNullOrWhiteSpace(dto.Provider))
                return BadRequest(new { message = "Provider is required" });

            var config = new LlmJobConfig
            {
                JobKey = jobKey,
                JobName = dto.JobName,
                Description = dto.Description,
                Provider = dto.Provider,
                Model = dto.Model,
                MaxTokens = dto.MaxTokens,
                Temperature = dto.Temperature,
                IsActive = dto.IsActive,
                Category = dto.Category
            };

            var saved = await _llmJobConfigService.UpsertAsync(config);

            return Ok(new
            {
                success = true,
                message = "LLM job config saved successfully",
                config = saved
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting LLM job config {JobKey}", jobKey);
            return StatusCode(500, new { message = "Error saving LLM job config" });
        }
    }

    /// <summary>
    /// Delete a LLM job config by job key
    /// </summary>
    [HttpDelete("llm-job-configs/{jobKey}")]
    public async Task<IActionResult> DeleteLlmJobConfig(string jobKey)
    {
        try
        {
            var deleted = await _llmJobConfigService.DeleteAsync(jobKey);
            if (!deleted)
                return NotFound(new { message = $"No config found for job key '{jobKey}'" });

            return Ok(new { success = true, message = "LLM job config deleted. Job will fall back to default provider/model." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting LLM job config {JobKey}", jobKey);
            return StatusCode(500, new { message = "Error deleting LLM job config" });
        }
    }

    /// <summary>
    /// Invalidate the LLM job config cache (force reload from DB on next request)
    /// </summary>
    [HttpPost("llm-job-configs/invalidate-cache")]
    public IActionResult InvalidateLlmJobConfigCache()
    {
        _llmJobConfigService.InvalidateCache();
        return Ok(new { success = true, message = "LLM job config cache invalidated" });
    }

    // ============================================
    // SUBSCRIPTION & QUOTA MANAGEMENT
    // ============================================

    /// <summary>
    /// Get all subscription plans
    /// </summary>
    [HttpGet("subscription-plans")]
    public async Task<IActionResult> GetSubscriptionPlans()
    {
        try
        {
            var plans = await _context.SubscriptionPlans
                .Where(p => p.IsActive)
                .OrderBy(p => p.SortOrder)
                .ToListAsync();

            return Ok(plans);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription plans");
            return StatusCode(500, new { message = "Error getting subscription plans" });
        }
    }

    /// <summary>
    /// Create subscription plan
    /// </summary>
    [HttpPost("subscription-plans")]
    public async Task<IActionResult> CreateSubscriptionPlan([FromBody] Core.Entities.SubscriptionPlan plan)
    {
        try
        {
            plan.CreatedAt = DateTime.UtcNow;
            plan.UpdatedAt = DateTime.UtcNow;
            
            _context.SubscriptionPlans.Add(plan);
            await _context.SaveChangesAsync();

            return Ok(plan);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subscription plan");
            return StatusCode(500, new { message = "Error creating subscription plan" });
        }
    }

    /// <summary>
    /// Update subscription plan
    /// </summary>
    [HttpPut("subscription-plans/{id}")]
    public async Task<IActionResult> UpdateSubscriptionPlan(int id, [FromBody] Core.Entities.SubscriptionPlan plan)
    {
        try
        {
            var existing = await _context.SubscriptionPlans.FindAsync(id);
            if (existing == null)
                return NotFound(new { message = "Subscription plan not found" });

            existing.Name = plan.Name;
            existing.DisplayName = plan.DisplayName;
            existing.Description = plan.Description;
            existing.MonthlyPrice = plan.MonthlyPrice;
            existing.YearlyPrice = plan.YearlyPrice;
            existing.Currency = plan.Currency;
            existing.IsActive = plan.IsActive;
            existing.SortOrder = plan.SortOrder;
            existing.MaxProjects = plan.MaxProjects;
            existing.MaxPromptsPerProject = plan.MaxPromptsPerProject;
            existing.MaxVisibilityChecksPerMonth = plan.MaxVisibilityChecksPerMonth;
            existing.MaxSiteAuditsPerMonth = plan.MaxSiteAuditsPerMonth;
            existing.MaxContentArticles = plan.MaxContentArticles;
            existing.MaxLlmRequestsPerMonth = plan.MaxLlmRequestsPerMonth;
            existing.MaxLlmTokensPerMonth = plan.MaxLlmTokensPerMonth;
            existing.AllowCompetitorTracking = plan.AllowCompetitorTracking;
            existing.AllowAdvancedAnalytics = plan.AllowAdvancedAnalytics;
            existing.AllowApiAccess = plan.AllowApiAccess;
            existing.AllowWhiteLabel = plan.AllowWhiteLabel;
            existing.AllowPrioritySupport = plan.AllowPrioritySupport;
            existing.AllowCustomIntegrations = plan.AllowCustomIntegrations;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(existing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subscription plan {Id}", id);
            return StatusCode(500, new { message = "Error updating subscription plan" });
        }
    }

    /// <summary>
    /// Delete subscription plan
    /// </summary>
    [HttpDelete("subscription-plans/{id}")]
    public async Task<IActionResult> DeleteSubscriptionPlan(int id)
    {
        try
        {
            var plan = await _context.SubscriptionPlans.FindAsync(id);
            if (plan == null)
                return NotFound(new { message = "Subscription plan not found" });

            // Check if any users are on this plan
            var usersOnPlan = await _context.UserSubscriptions
                .CountAsync(s => s.SubscriptionPlanId == id && s.Status == "Active");

            if (usersOnPlan > 0)
                return BadRequest(new { message = $"Cannot delete plan with {usersOnPlan} active subscriptions" });

            _context.SubscriptionPlans.Remove(plan);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Subscription plan deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting subscription plan {Id}", id);
            return StatusCode(500, new { message = "Error deleting subscription plan" });
        }
    }

    /// <summary>
    /// Get all user subscriptions with pagination
    /// </summary>
    [HttpGet("user-subscriptions")]
    public async Task<IActionResult> GetUserSubscriptions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? status = null)
    {
        try
        {
            var query = _context.UserSubscriptions
                .Include(s => s.User)
                .Include(s => s.SubscriptionPlan)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(s => s.Status == status);

            var total = await query.CountAsync();
            var subscriptions = await query
                .OrderByDescending(s => s.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new
                {
                    s.Id,
                    s.UserId,
                    UserEmail = s.User.Email,
                    s.SubscriptionPlanId,
                    PlanName = s.SubscriptionPlan.Name,
                    s.Status,
                    s.BillingCycle,
                    s.StartDate,
                    s.EndDate,
                    s.CurrentPrice,
                    s.NextBillingDate,
                    s.IsTrialActive,
                    s.TrialEndsAt,
                    s.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                total,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(total / (double)pageSize),
                subscriptions
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user subscriptions");
            return StatusCode(500, new { message = "Error getting user subscriptions" });
        }
    }

    /// <summary>
    /// Assign subscription plan to user
    /// </summary>
    [HttpPost("users/{userId}/assign-plan")]
    public async Task<IActionResult> AssignPlanToUser(int userId, [FromBody] AssignPlanRequest request)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "User not found" });

            var plan = await _context.SubscriptionPlans.FindAsync(request.PlanId);
            if (plan == null)
                return NotFound(new { message = "Subscription plan not found" });

            // Cancel existing active subscription
            var existingSubscription = await _context.UserSubscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == "Active");

            if (existingSubscription != null)
            {
                existingSubscription.Status = "Cancelled";
                existingSubscription.CancelledAt = DateTime.UtcNow;
                existingSubscription.EndDate = DateTime.UtcNow;
                existingSubscription.UpdatedAt = DateTime.UtcNow;
            }

            // Create new subscription
            var subscription = new Core.Entities.UserSubscription
            {
                UserId = userId,
                SubscriptionPlanId = request.PlanId,
                Status = "Active",
                BillingCycle = request.BillingCycle ?? "Monthly",
                StartDate = DateTime.UtcNow,
                CurrentPrice = request.BillingCycle == "Yearly" ? plan.YearlyPrice : plan.MonthlyPrice,
                NextBillingDate = request.BillingCycle == "Yearly" 
                    ? DateTime.UtcNow.AddYears(1) 
                    : DateTime.UtcNow.AddMonths(1),
                IsTrialActive = request.IsTrialActive ?? false,
                TrialEndsAt = request.IsTrialActive == true ? DateTime.UtcNow.AddDays(14) : null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.UserSubscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            // Initialize quotas
            await InitializeUserQuotas(subscription.Id, plan);

            return Ok(new
            {
                message = "Subscription plan assigned successfully",
                subscription
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning plan to user {UserId}", userId);
            return StatusCode(500, new { message = "Error assigning subscription plan" });
        }
    }

    /// <summary>
    /// Get subscription metrics
    /// </summary>
    [HttpGet("subscription-metrics")]
    public async Task<IActionResult> GetSubscriptionMetrics()
    {
        try
        {
            var totalSubscriptions = await _context.UserSubscriptions.CountAsync();
            var activeSubscriptions = await _context.UserSubscriptions.CountAsync(s => s.Status == "Active");
            var trialSubscriptions = await _context.UserSubscriptions.CountAsync(s => s.IsTrialActive);
            var cancelledSubscriptions = await _context.UserSubscriptions.CountAsync(s => s.Status == "Cancelled");
            var expiredSubscriptions = await _context.UserSubscriptions.CountAsync(s => s.Status == "Expired");

            var activeMonthlyRevenue = await _context.UserSubscriptions
                .Where(s => s.Status == "Active" && s.BillingCycle == "Monthly")
                .SumAsync(s => s.CurrentPrice);

            var activeYearlyRevenue = await _context.UserSubscriptions
                .Where(s => s.Status == "Active" && s.BillingCycle == "Yearly")
                .SumAsync(s => s.CurrentPrice);

            var mrr = activeMonthlyRevenue + (activeYearlyRevenue / 12);
            var arr = mrr * 12;

            var subscriptionsByPlan = await _context.UserSubscriptions
                .Where(s => s.Status == "Active")
                .Include(s => s.SubscriptionPlan)
                .GroupBy(s => s.SubscriptionPlan.Name)
                .Select(g => new { Plan = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Plan, x => x.Count);

            var revenueByPlan = await _context.UserSubscriptions
                .Where(s => s.Status == "Active")
                .Include(s => s.SubscriptionPlan)
                .GroupBy(s => s.SubscriptionPlan.Name)
                .Select(g => new { Plan = g.Key, Revenue = g.Sum(s => s.CurrentPrice) })
                .ToDictionaryAsync(x => x.Plan, x => x.Revenue);

            var metrics = new
            {
                totalSubscriptions,
                activeSubscriptions,
                trialSubscriptions,
                cancelledSubscriptions,
                expiredSubscriptions,
                monthlyRecurringRevenue = mrr,
                annualRecurringRevenue = arr,
                subscriptionsByPlan,
                revenueByPlan,
                churnRate = totalSubscriptions > 0 ? (double)cancelledSubscriptions / totalSubscriptions * 100 : 0,
                averageRevenuePerUser = activeSubscriptions > 0 ? (double)mrr / activeSubscriptions : 0
            };

            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription metrics");
            return StatusCode(500, new { message = "Error getting subscription metrics" });
        }
    }

    /// <summary>
    /// Get user quota status
    /// </summary>
    [HttpGet("users/{userId}/quotas")]
    public async Task<IActionResult> GetUserQuotas(int userId)
    {
        try
        {
            var subscription = await _context.UserSubscriptions
                .Include(s => s.UsageQuotas)
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == "Active");

            if (subscription == null)
                return NotFound(new { message = "No active subscription found for user" });

            var quotas = subscription.UsageQuotas.Select(q => new
            {
                q.QuotaType,
                Limit = q.QuotaLimit,
                Used = q.QuotaUsed,
                Remaining = q.QuotaLimit - q.QuotaUsed,
                PercentageUsed = q.QuotaLimit > 0 ? (double)q.QuotaUsed / q.QuotaLimit * 100 : 0,
                q.PeriodStart,
                q.PeriodEnd,
                IsExceeded = q.QuotaUsed >= q.QuotaLimit,
                IsNearLimit = q.QuotaLimit > 0 && (double)q.QuotaUsed / q.QuotaLimit >= 0.8
            }).ToList();

            return Ok(quotas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user quotas for user {UserId}", userId);
            return StatusCode(500, new { message = "Error getting user quotas" });
        }
    }

    /// <summary>
    /// Reset user quotas manually
    /// </summary>
    [HttpPost("users/{userId}/quotas/reset")]
    public async Task<IActionResult> ResetUserQuotas(int userId)
    {
        try
        {
            var subscription = await _context.UserSubscriptions
                .Include(s => s.UsageQuotas)
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == "Active");

            if (subscription == null)
                return NotFound(new { message = "No active subscription found for user" });

            foreach (var quota in subscription.UsageQuotas)
            {
                quota.QuotaUsed = 0;
                quota.PeriodStart = DateTime.UtcNow;
                quota.PeriodEnd = DateTime.UtcNow.AddMonths(1);
                quota.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "User quotas reset successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting user quotas for user {UserId}", userId);
            return StatusCode(500, new { message = "Error resetting user quotas" });
        }
    }

    private async Task InitializeUserQuotas(int subscriptionId, Core.Entities.SubscriptionPlan plan)
    {
        var quotas = new List<Core.Entities.UsageQuota>
        {
            new() {
                UserSubscriptionId = subscriptionId,
                QuotaType = "VisibilityChecks",
                QuotaLimit = plan.MaxVisibilityChecksPerMonth,
                PeriodStart = DateTime.UtcNow,
                PeriodEnd = DateTime.UtcNow.AddMonths(1),
                IsResetMonthly = true
            },
            new() {
                UserSubscriptionId = subscriptionId,
                QuotaType = "SiteAudits",
                QuotaLimit = plan.MaxSiteAuditsPerMonth,
                PeriodStart = DateTime.UtcNow,
                PeriodEnd = DateTime.UtcNow.AddMonths(1),
                IsResetMonthly = true
            },
            new() {
                UserSubscriptionId = subscriptionId,
                QuotaType = "LlmRequests",
                QuotaLimit = plan.MaxLlmRequestsPerMonth,
                PeriodStart = DateTime.UtcNow,
                PeriodEnd = DateTime.UtcNow.AddMonths(1),
                IsResetMonthly = true
            },
            new() {
                UserSubscriptionId = subscriptionId,
                QuotaType = "LlmTokens",
                QuotaLimit = plan.MaxLlmTokensPerMonth,
                PeriodStart = DateTime.UtcNow,
                PeriodEnd = DateTime.UtcNow.AddMonths(1),
                IsResetMonthly = true
            }
        };

        _context.UsageQuotas.AddRange(quotas);
        await _context.SaveChangesAsync();
    }
}

// ===== REQUEST MODELS =====
// All DTOs have been moved to AppBackendBasic.Core.DTOs namespace for standardization

public class AssignPlanRequest
{
    public int PlanId { get; set; }
    public string? BillingCycle { get; set; } = "Monthly";
    public bool? IsTrialActive { get; set; }
}

public class CreateLLMProviderDto
{
    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? ApiEndpoint { get; set; }
    public string? ApiKey { get; set; }
    public bool? IsActive { get; set; }
    public int? Priority { get; set; }
    public int? MaxTokens { get; set; }
    public decimal? CostPer1kInputTokens { get; set; }
    public decimal? CostPer1kOutputTokens { get; set; }
    public int? RateLimitPerMinute { get; set; }
}

public class UpdateLLMProviderDto
{
    public string? Name { get; set; }
    public string? DisplayName { get; set; }
    public string? ApiEndpoint { get; set; }
    public string? ApiKey { get; set; }
    public bool? IsActive { get; set; }
    public int? Priority { get; set; }
    public int? MaxTokens { get; set; }
    public decimal? CostPer1kInputTokens { get; set; }
    public decimal? CostPer1kOutputTokens { get; set; }
    public int? RateLimitPerMinute { get; set; }
}

public class UpsertLlmJobConfigDto
{
    public string JobKey { get; set; } = string.Empty;
    public string JobName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Provider { get; set; } = "OpenAI";
    public string Model { get; set; } = string.Empty;
    public int MaxTokens { get; set; } = 2000;
    public decimal Temperature { get; set; } = 0.7m;
    public bool IsActive { get; set; } = true;
    public string Category { get; set; } = "General";
}
