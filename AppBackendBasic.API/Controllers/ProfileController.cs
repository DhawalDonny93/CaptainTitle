using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AppBackendBasic.Core.Interfaces;

namespace AppBackendBasic.API.Controllers;

[ApiController]
[Authorize]
[Route("api/user/profile")]
public class ProfileController : ControllerBase
{
    private readonly IPhotoService _photoService;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(IPhotoService photoService, ILogger<ProfileController> logger)
    {
        _photoService = photoService;
        _logger = logger;
    }

    [HttpPost("photo")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10MB
    public async Task<IActionResult> UploadPhoto(IFormFile photo)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var userInt))
        {
            _logger.LogWarning("Invalid user ID in token");
            return Unauthorized(new { message = "Invalid user ID in token." });
        }

        if (photo == null || photo.Length == 0)
        {
            return BadRequest(new { message = "No photo provided" });
        }

        // Validate file size
        if (photo.Length > 10 * 1024 * 1024)
        {
            return BadRequest(new { message = "File size exceeds 10MB limit" });
        }

        // Validate content type
        var allowedTypes = new[] { "image/jpeg", "image/png", "image/heic" };
        if (!allowedTypes.Contains(photo.ContentType.ToLower()))
        {
            return BadRequest(new { message = "Invalid file type. Allowed: JPEG, PNG, HEIC" });
        }

        using var stream = photo.OpenReadStream();
        var result = await _photoService.UploadProfilePhotoAsync(
            userInt, 
            stream, 
            photo.FileName, 
            photo.ContentType);

        if (!result.Success)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        _logger.LogInformation("Profile photo uploaded for user {UserId}", userId);

        return Ok(new
        {
            success = true,
            url = result.Url,
            thumbnailUrl = result.ThumbnailUrl
        });
    }

    [HttpDelete("photo")]
    public async Task<IActionResult> DeletePhoto()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var userInt))
        {
            _logger.LogWarning("Invalid user ID in token");
            return Unauthorized(new { message = "Invalid user ID in token." });
        }

        var success = await _photoService.DeleteProfilePhotoAsync(userInt);

        if (!success)
        {
            return BadRequest(new { message = "Failed to delete photo" });
        }

        _logger.LogInformation("Profile photo deleted for user {UserId}", userId);

        return Ok(new
        {
            success = true,
            defaultPhotoUrl = "https://cdn.yourapp.com/defaults/avatar.png"
        });
    }
}
