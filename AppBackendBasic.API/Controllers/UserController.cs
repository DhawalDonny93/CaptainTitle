using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Interfaces;

namespace AppBackendBasic.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IAccountDeletionService _accountDeletionService;
    private readonly IDataExportService _dataExportService;
    private readonly IPhotoService _photoService;
    private readonly ILogger<UserController> _logger;

    public UserController(
        IUserRepository userRepository, 
        IAccountDeletionService accountDeletionService,
        IDataExportService dataExportService,
        IPhotoService photoService,
        ILogger<UserController> logger)
    {
        _userRepository = userRepository;
        _accountDeletionService = accountDeletionService;
        _dataExportService = dataExportService;
        _photoService = photoService;
        _logger = logger;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var userInt))
        {
            _logger.LogWarning("Invalid user ID in token");
            return Unauthorized(new { message = "Invalid user ID in token." });
        }
        
        var user = await _userRepository.GetByIdAsync(userInt);
        
        if (user == null)
        {
            _logger.LogWarning("User not found: {UserId}", userId);
            return NotFound(new { message = "User not found." });
        }
        
        var userDto = new UserProfileDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            ProfilePhotoUrl = user.ProfilePhotoUrl,
            DateOfBirth = user.DateOfBirth,
            EmailVerified = user.EmailVerified,
            PhoneVerified = user.PhoneVerified,
            OnboardingCompleted = user.OnboardingCompleted,
            Provider = user.UserProviders.FirstOrDefault()?.Provider ?? "Email",
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
        
        return Ok(new { success= true,user = userDto });
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var userInt))
        {
            _logger.LogWarning("Invalid user ID in token");
            return Unauthorized(new { message = "Invalid user ID in token." });
        }
        
        var user = await _userRepository.GetByIdAsync(userInt);
        
        if (user == null)
        {
            _logger.LogWarning("User not found: {UserId}", userId);
            return NotFound(new { message = "User not found." });
        }
        
        // Update user properties
        if (!string.IsNullOrEmpty(request.FirstName))
            user.FirstName = request.FirstName;
            
        if (!string.IsNullOrEmpty(request.LastName))
            user.LastName = request.LastName;
        
        if (!string.IsNullOrEmpty(request.PhoneNumber))
            user.PhoneNumber = request.PhoneNumber;
        // DHAWAL
        if (request.DateOfBirth.HasValue)
            user.DateOfBirth = request.DateOfBirth;
        
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);
        
        _logger.LogInformation("Profile updated for user: {UserId}", userId);
        
        var userDto = new UserProfileDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            ProfilePhotoUrl = user.ProfilePhotoUrl,
            DateOfBirth = user.DateOfBirth,
            EmailVerified = user.EmailVerified,
            PhoneVerified = user.PhoneVerified,
            OnboardingCompleted = user.OnboardingCompleted,
            Provider = user.UserProviders.FirstOrDefault()?.Provider ?? "Email",
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
        
        return Ok(new { success= true,user = userDto });
    }

    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var userInt))
        {
            _logger.LogWarning("Invalid user ID in token");
            return Unauthorized(new { message = "Invalid user ID in token." });
        }
        
        var user = await _userRepository.GetByIdAsync(userInt);
        
        if (user == null)
        {
            _logger.LogWarning("User not found: {UserId}", userId);
            return NotFound(new { message = "User not found." });
        }
        
        var sessions = user.UserSessions.Select(s => new
        {
            s.Id,
            s.DeviceId,
            s.DeviceModel,
            s.Platform,
            s.AppVersion,
            s.CreatedAt,
            s.LastActiveAt
        }).ToList();
        
        return Ok(sessions);
    }

    // Account Deletion Endpoints (GDPR - Right to be Forgotten)
    
    [HttpPost("account/request-deletion")]
    public async Task<IActionResult> RequestAccountDeletion([FromBody] RequestDeletionDto request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var userInt))
        {
            _logger.LogWarning("Invalid user ID in token");
            return Unauthorized(new { message = "Invalid user ID in token." });
        }

        var result = await _accountDeletionService.RequestDeletionAsync(userInt, request.Password, request.Reason);
        
        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        _logger.LogInformation("Account deletion requested for user {UserId}", userId);
        return Ok(new
        {
            success = true,
            message = result.Message,
            scheduledDeletionDate = result.ScheduledDeletionDate,
            gracePeriodDays = result.GracePeriodDays
        });
    }

    [HttpPost("account/cancel-deletion")]
    public async Task<IActionResult> CancelAccountDeletion()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var userInt))
        {
            _logger.LogWarning("Invalid user ID in token");
            return Unauthorized(new { message = "Invalid user ID in token." });
        }

        var success = await _accountDeletionService.CancelDeletionAsync(userInt);
        
        if (!success)
        {
            return BadRequest(new { message = "No pending deletion found or grace period expired." });
        }

        _logger.LogInformation("Account deletion cancelled for user {UserId}", userId);
        return Ok(new { success = true, message = "Account deletion cancelled successfully." });
    }

    [HttpGet("account/deletion-status")]
    public async Task<IActionResult> GetDeletionStatus()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var userInt))
        {
            _logger.LogWarning("Invalid user ID in token");
            return Unauthorized(new { message = "Invalid user ID in token." });
        }

        var status = await _accountDeletionService.GetDeletionStatusAsync(userInt);
        
        if (status == null)
        {
            return Ok(new { isPending = false });
        }

        return Ok(new
        {
            isPending = status.IsPending,
            scheduledDeletionDate = status.ScheduledDeletionDate,
            daysRemaining = status.DaysRemaining,
            reason = status.Reason
        });
    }

    // Data Export Endpoints (GDPR - Right to Data Portability)
    
    [HttpPost("data-export/request")]
    public async Task<IActionResult> RequestDataExport()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var userInt))
        {
            _logger.LogWarning("Invalid user ID in token");
            return Unauthorized(new { message = "Invalid user ID in token." });
        }

        var result = await _dataExportService.RequestDataExportAsync(userInt);
        
        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        _logger.LogInformation("Data export requested for user {UserId}", userId);
        return Ok(new
        {
            success = true,
            message = result.Message,
            exportId = result.ExportId,
            expiresAt = result.ExpiresAt
        });
    }

    [HttpGet("data-export/status/{exportId}")]
    public async Task<IActionResult> GetDataExportStatus(string exportId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var userInt))
        {
            _logger.LogWarning("Invalid user ID in token");
            return Unauthorized(new { message = "Invalid user ID in token." });
        }

        var status = await _dataExportService.GetExportStatusAsync(userInt, exportId);
        
        if (status == null)
        {
            return NotFound(new { message = "Export not found." });
        }

        return Ok(new
        {
            exportId = status.ExportId,
            isReady = status.IsReady,
            createdAt = status.CreatedAt,
            expiresAt = status.ExpiresAt,
            fileSizeBytes = status.FileSizeBytes
        });
    }

    [HttpGet("data-export/download/{exportId}")]
    public async Task<IActionResult> DownloadDataExport(string exportId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var userInt))
        {
            _logger.LogWarning("Invalid user ID in token");
            return Unauthorized(new { message = "Invalid user ID in token." });
        }

        var fileData = await _dataExportService.DownloadExportAsync(userInt, exportId);
        
        if (fileData == null)
        {
            return NotFound(new { message = "Export not found or expired." });
        }

        _logger.LogInformation("Data export downloaded for user {UserId}, export {ExportId}", userId, exportId);
        
        return File(fileData, "application/json", $"user_data_{exportId}.json");
    }

}

