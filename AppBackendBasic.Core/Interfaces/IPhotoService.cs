namespace AppBackendBasic.Core.Interfaces;

/// <summary>
/// Service for handling profile photo uploads and management
/// </summary>
public interface IPhotoService
{
    /// <summary>
    /// Upload profile photo
    /// </summary>
    Task<PhotoUploadResult> UploadProfilePhotoAsync(int userId, Stream photoStream, string fileName, string contentType);
    
    /// <summary>
    /// Delete profile photo
    /// </summary>
    Task<bool> DeleteProfilePhotoAsync(int userId);
    
    /// <summary>
    /// Get profile photo URL
    /// </summary>
    Task<string?> GetProfilePhotoUrlAsync(int userId);
}

public class PhotoUploadResult
{
    public bool Success { get; set; }
    public string? Url { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? ErrorMessage { get; set; }
}
