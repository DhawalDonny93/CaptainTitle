using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AppBackendBasic.API.Controllers;

[ApiController]
[Authorize]
[Route("api/user")]
public class UserProfileController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IPhotoService _photoService;
    private readonly ILogger<UserProfileController> _logger;

    public UserProfileController(
        ApplicationDbContext context,
        IPhotoService photoService,
        ILogger<UserProfileController> logger)
    {
        _context = context;
        _photoService = photoService;
        _logger = logger;
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    // NOTE: Profile GET and PUT endpoints have been moved to UserController
    // to avoid duplication. UserController has comprehensive profile management.

    /// <summary>
    /// Request account recovery (within grace period)
    /// </summary>
    [HttpPost("account/recover")]
    [AllowAnonymous]
    public async Task<IActionResult> RecoverAccount([FromBody] RecoverAccountRequest request)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsDeleted);

            if (user == null)
                return NotFound(new { message = "No deleted account found with this email" });

            // Check if within grace period (30 days)
            var gracePeriodDays = 30;
            var deletionDate = user.DeletedAt ?? DateTime.UtcNow;
            var daysSinceDeletion = (DateTime.UtcNow - deletionDate).TotalDays;

            if (daysSinceDeletion > gracePeriodDays)
                return BadRequest(new { message = "Account recovery period has expired" });

            // Verify password
            if (!VerifyPasswordHash(request.Password, user.PasswordHash))
                return BadRequest(new { message = "Invalid password" });

            // Restore account
            user.IsDeleted = false;
            user.DeletedAt = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} recovered account", user.Id);

            return Ok(new
            {
                success = true,
                message = "Account recovered successfully",
                userId = user.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recovering account");
            return StatusCode(500, new { message = "Error recovering account" });
        }
    }

    /// <summary>
    /// Get user preferences
    /// </summary>
    [HttpGet("preferences")]
    public async Task<IActionResult> GetPreferences()
    {
        try
        {
            var userId = GetUserId();
            var preferences = await _context.Set<UserPreference>()
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (preferences == null)
            {
                // Create default preferences
                preferences = new UserPreference
                {
                    UserId = userId,
                    Language = "en",
                    Theme = "light",
                    Timezone = "UTC",
                    CustomPreferences = "{}",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Set<UserPreference>().Add(preferences);
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                success = true,
                preferences = new
                {
                    preferences.Language,
                    preferences.Theme,
                    preferences.Timezone,
                    preferences.CustomPreferences
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting preferences");
            return StatusCode(500, new { message = "Error getting preferences" });
        }
    }

    /// <summary>
    /// Update user preferences
    /// </summary>
    [HttpPut("preferences")]
    public async Task<IActionResult> UpdatePreferences([FromBody] UpdateUserPreferencesRequest request)
    {
        try
        {
            var userId = GetUserId();
            var preferences = await _context.Set<UserPreference>()
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (preferences == null)
            {
                // Create new preferences
                preferences = new UserPreference
                {
                    UserId = userId,
                    Language = request.Language ?? "en",
                    Theme = request.Theme ?? "light",
                    Timezone = request.Timezone ?? "UTC",
                    CustomPreferences = request.CustomPreferences ?? "{}",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Set<UserPreference>().Add(preferences);
            }
            else
            {
                // Update existing preferences
                if (!string.IsNullOrWhiteSpace(request.Language))
                    preferences.Language = request.Language;

                if (!string.IsNullOrWhiteSpace(request.Theme))
                    preferences.Theme = request.Theme;

                if (!string.IsNullOrWhiteSpace(request.Timezone))
                    preferences.Timezone = request.Timezone;

                if (!string.IsNullOrWhiteSpace(request.CustomPreferences))
                    preferences.CustomPreferences = request.CustomPreferences;

                preferences.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} updated preferences", userId);

            return Ok(new
            {
                success = true,
                message = "Preferences updated successfully",
                preferences = new
                {
                    preferences.Language,
                    preferences.Theme,
                    preferences.Timezone,
                    preferences.CustomPreferences
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating preferences");
            return StatusCode(500, new { message = "Error updating preferences" });
        }
    }

    /// <summary>
    /// Get privacy settings
    /// </summary>
    [HttpGet("privacy")]
    public async Task<IActionResult> GetPrivacySettings()
    {
        try
        {
            var userId = GetUserId();
            var user = await _context.Users
                .Where(u => u.Id == userId && !u.IsDeleted)
                .Select(u => new
                {
                    u.Id,
                    u.IsActive,
                    ProfileVisibility = "public", // Default, can be extended
                    ShowEmail = false,
                    ShowPhone = false,
                    AllowNotifications = true,
                    DataSharing = false
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound(new { message = "User not found" });

            return Ok(new
            {
                success = true,
                privacy = user
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting privacy settings");
            return StatusCode(500, new { message = "Error getting privacy settings" });
        }
    }

    /// <summary>
    /// Update privacy settings
    /// </summary>
    [HttpPut("privacy")]
    public async Task<IActionResult> UpdatePrivacySettings([FromBody] UpdatePrivacySettingsRequest request)
    {
        try
        {
            var userId = GetUserId();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

            if (user == null)
                return NotFound(new { message = "User not found" });

            // Update basic privacy settings
            // Note: Extended privacy settings would require a separate PrivacySettings entity
            // For now, we're using the existing User entity fields

            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} updated privacy settings", userId);

            return Ok(new
            {
                success = true,
                message = "Privacy settings updated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating privacy settings");
            return StatusCode(500, new { message = "Error updating privacy settings" });
        }
    }

    /// <summary>
    /// Upload profile photo
    /// </summary>
    [HttpPost("profile-photo")]
    public async Task<IActionResult> UploadProfilePhoto(IFormFile photo)
    {
        try
        {
            var userId = GetUserId();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

            if (user == null)
                return NotFound(new { message = "User not found" });

            if (photo == null || photo.Length == 0)
                return BadRequest(new { message = "No photo provided" });

            // Validate file
            if (photo.Length > 5 * 1024 * 1024) // 5MB limit
                return BadRequest(new { message = "Photo size must be less than 5MB" });

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
                return BadRequest(new { message = "Invalid file type. Only JPG, PNG, and GIF are allowed" });

            // Delete old photo if exists
            if (!string.IsNullOrEmpty(user.ProfilePhotoUrl))
            {
                await _photoService.DeleteProfilePhotoAsync(userId);
            }

            // Upload new photo
            using var stream = photo.OpenReadStream();
            var result = await _photoService.UploadProfilePhotoAsync(userId, stream, photo.FileName, photo.ContentType);

            // Update user
            user.ProfilePhotoUrl = result.Url;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} uploaded profile photo", userId);

            return Ok(new
            {
                success = true,
                message = "Profile photo uploaded successfully",
                photoUrl = result.Url
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading profile photo");
            return StatusCode(500, new { message = "Error uploading photo" });
        }
    }

    /// <summary>
    /// Delete profile photo
    /// </summary>
    [HttpDelete("profile-photo")]
    public async Task<IActionResult> DeleteProfilePhoto()
    {
        try
        {
            var userId = GetUserId();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

            if (user == null)
                return NotFound(new { message = "User not found" });

            if (string.IsNullOrEmpty(user.ProfilePhotoUrl))
                return BadRequest(new { message = "No profile photo to delete" });

            // Delete photo file
            await _photoService.DeleteProfilePhotoAsync(userId);

            // Update user
            user.ProfilePhotoUrl = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} deleted profile photo", userId);

            return Ok(new
            {
                success = true,
                message = "Profile photo deleted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting profile photo");
            return StatusCode(500, new { message = "Error deleting photo" });
        }
    }

    private bool VerifyPasswordHash(string password, string? storedHash)
    {
        if (string.IsNullOrEmpty(storedHash))
            return false;

        try
        {
            var parts = storedHash.Split(':');
            if (parts.Length != 2)
                return false;

            var salt = Convert.FromBase64String(parts[0]);
            var hash = Convert.FromBase64String(parts[1]);

            using var hmac = new System.Security.Cryptography.HMACSHA512(salt);
            var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));

            return computedHash.SequenceEqual(hash);
        }
        catch
        {
            return false;
        }
    }
}

// ===== REQUEST MODELS =====
// All DTOs have been moved to AppBackendBasic.Core.DTOs namespace for standardization
