using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Infrastructure.Services
{
    public class BrandProfileService : IBrandProfileService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BrandProfileService> _logger;
        private readonly string _uploadsPath;

        public BrandProfileService(
            ApplicationDbContext context,
            ILogger<BrandProfileService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            
            // Get uploads path from configuration or use default
            var basePath = configuration["UploadsBasePath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            _uploadsPath = Path.Combine(basePath, "uploads", "logos");
            
            // Ensure uploads directory exists
            if (!Directory.Exists(_uploadsPath))
            {
                Directory.CreateDirectory(_uploadsPath);
            }
        }

        public async Task<BrandProfile?> GetByProjectIdAsync(int projectId)
        {
            return await _context.BrandProfiles
                .FirstOrDefaultAsync(bp => bp.ProjectId == projectId);
        }

        public async Task<BrandProfile> CreateOrUpdateAsync(int projectId, string? overview, string? description, string? topics)
        {
            var existing = await _context.BrandProfiles
                .FirstOrDefaultAsync(bp => bp.ProjectId == projectId);

            if (existing != null)
            {
                existing.Overview = overview;
                existing.Description = description;
                existing.Topics = topics;
                existing.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return existing;
            }

            var brandProfile = new BrandProfile
            {
                ProjectId = projectId,
                Overview = overview,
                Description = description,
                Topics = topics,
                CreatedAt = DateTime.UtcNow
            };

            _context.BrandProfiles.Add(brandProfile);
            await _context.SaveChangesAsync();
            return brandProfile;
        }

        public async Task<string?> UploadLogoAsync(int projectId, Stream fileStream, string fileName)
        {
            try
            {
                var extension = Path.GetExtension(fileName).ToLowerInvariant();
                var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".svg", ".webp" };
                
                if (!allowedExtensions.Contains(extension))
                {
                    throw new InvalidOperationException("Invalid file type. Only PNG, JPG, SVG, and WebP are allowed.");
                }

                var uniqueFileName = $"project_{projectId}_{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(_uploadsPath, uniqueFileName);

                using (var fileStreamOut = new FileStream(filePath, FileMode.Create))
                {
                    await fileStream.CopyToAsync(fileStreamOut);
                }

                var logoUrl = $"/uploads/logos/{uniqueFileName}";

                // Update brand profile with logo URL
                var brandProfile = await GetByProjectIdAsync(projectId);
                if (brandProfile != null)
                {
                    brandProfile.LogoUrl = logoUrl;
                    brandProfile.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                return logoUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading logo for project {ProjectId}", projectId);
                throw;
            }
        }

        public async Task DeleteAsync(int projectId)
        {
            var brandProfile = await _context.BrandProfiles
                .FirstOrDefaultAsync(bp => bp.ProjectId == projectId);

            if (brandProfile != null)
            {
                _context.BrandProfiles.Remove(brandProfile);
                await _context.SaveChangesAsync();
            }
        }
    }
}
