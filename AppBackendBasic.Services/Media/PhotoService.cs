using AppBackendBasic.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace AppBackendBasic.Services.Media;

public class PhotoService : IPhotoService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PhotoService> _logger;
    
    private readonly string _uploadDirectory;
    private readonly long _maxFileSizeBytes = 10 * 1024 * 1024; // 10MB
    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".heic" };

    public PhotoService(
        IUserRepository userRepository,
        IConfiguration configuration,
        ILogger<PhotoService> logger)
    {
        _userRepository = userRepository;
        _configuration = configuration;
        _logger = logger;
        
        _uploadDirectory = _configuration["PhotoUpload:Directory"] ?? "./uploads/profiles";
        
        // Ensure directory exists
        Directory.CreateDirectory(_uploadDirectory);
    }

    public async Task<PhotoUploadResult> UploadProfilePhotoAsync(
        int userId, 
        Stream photoStream, 
        string fileName, 
        string contentType)
    {
        try
        {
            // Validate file size
            if (photoStream.Length > _maxFileSizeBytes)
            {
                return new PhotoUploadResult
                {
                    Success = false,
                    ErrorMessage = "File size exceeds 10MB limit"
                };
            }

            // Validate file extension
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
            {
                return new PhotoUploadResult
                {
                    Success = false,
                    ErrorMessage = "Invalid file type. Allowed: JPEG, PNG, HEIC"
                };
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return new PhotoUploadResult
                {
                    Success = false,
                    ErrorMessage = "User not found"
                };
            }

            // Delete old photo if exists
            if (!string.IsNullOrEmpty(user.ProfilePhotoUrl))
            {
                await DeleteProfilePhotoAsync(userId);
            }

            // Create user directory
            var userDirectory = Path.Combine(_uploadDirectory, userId.ToString());
            Directory.CreateDirectory(userDirectory);

            // Generate file names
            var timestamp = DateTime.UtcNow.Ticks;
            var originalPath = Path.Combine(userDirectory, $"photo_original_{timestamp}{extension}");
            var largePath = Path.Combine(userDirectory, $"photo_large_{timestamp}.jpg");
            var mediumPath = Path.Combine(userDirectory, $"photo_medium_{timestamp}.jpg");
            var smallPath = Path.Combine(userDirectory, $"photo_small_{timestamp}.jpg");

            // Save original
            using (var fileStream = new FileStream(originalPath, FileMode.Create))
            {
                photoStream.Position = 0;
                await photoStream.CopyToAsync(fileStream);
            }

            // Generate thumbnails
            photoStream.Position = 0;
            using (var image = await Image.LoadAsync(photoStream))
            {
                // Large: 800x800
                await GenerateThumbnailAsync(image, largePath, 800);
                
                // Medium: 300x300
                await GenerateThumbnailAsync(image, mediumPath, 300);
                
                // Small: 100x100
                await GenerateThumbnailAsync(image, smallPath, 100);
            }

            // Generate URLs (in production, use CDN URLs)
            var baseUrl = _configuration["AppUrl"] ?? "https://localhost:5001";
            var photoUrl = $"{baseUrl}/uploads/profiles/{userId}/photo_large_{timestamp}.jpg";
            var thumbnailUrl = $"{baseUrl}/uploads/profiles/{userId}/photo_small_{timestamp}.jpg";

            // Update user record
            user.ProfilePhotoUrl = photoUrl;
            user.ProfilePhotoThumbnailUrl = thumbnailUrl;
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("Profile photo uploaded for user {UserId}", userId);

            return new PhotoUploadResult
            {
                Success = true,
                Url = photoUrl,
                ThumbnailUrl = thumbnailUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading profile photo for user {UserId}", userId);
            return new PhotoUploadResult
            {
                Success = false,
                ErrorMessage = "Failed to upload photo"
            };
        }
    }

    public async Task<bool> DeleteProfilePhotoAsync(int userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return false;

            // Delete physical files
            var userDirectory = Path.Combine(_uploadDirectory, userId.ToString());
            if (Directory.Exists(userDirectory))
            {
                Directory.Delete(userDirectory, true);
            }

            // Update user record
            user.ProfilePhotoUrl = null;
            user.ProfilePhotoThumbnailUrl = null;
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("Profile photo deleted for user {UserId}", userId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting profile photo for user {UserId}", userId);
            return false;
        }
    }

    public async Task<string?> GetProfilePhotoUrlAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        return user?.ProfilePhotoUrl;
    }

    private async Task GenerateThumbnailAsync(Image image, string outputPath, int size)
    {
        var clone = image.Clone(ctx => ctx.Resize(new ResizeOptions
        {
            Size = new Size(size, size),
            Mode = ResizeMode.Crop
        }));

        await clone.SaveAsJpegAsync(outputPath);
    }
}
