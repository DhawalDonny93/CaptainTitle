using System.Text;
using System.Text.Json;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Services.Compliance;

public class DataExportService : IDataExportService
{
    private readonly ApplicationDbContext _context;
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly ILogger<DataExportService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _exportDirectory;
    
    private const int ExpirationHours = 24;

    public DataExportService(
        ApplicationDbContext context,
        IUserRepository userRepository,
        IEmailService emailService,
        ILogger<DataExportService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _userRepository = userRepository;
        _emailService = emailService;
        _logger = logger;
        _configuration = configuration;
        
        // Get export directory from configuration or use default
        _exportDirectory = _configuration.GetValue<string>("DataExport:Directory") 
            ?? Path.Combine(Directory.GetCurrentDirectory(), "exports");
        
        // Ensure directory exists
        Directory.CreateDirectory(_exportDirectory);
    }

    public async Task<DataExportResult> RequestDataExportAsync(int userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            
            if (user == null)
            {
                return new DataExportResult
                {
                    Success = false,
                    Message = "User not found"
                };
            }

            // Check if there's already a pending export
            var existingExport = await _context.Set<DataExport>()
                .Where(e => e.UserId == userId && e.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(e => e.CreatedAt)
                .FirstOrDefaultAsync();

            if (existingExport != null)
            {
                return new DataExportResult
                {
                    Success = false,
                    Message = "An export request is already pending. Please wait for it to complete.",
                    ExportId = existingExport.ExportId,
                    ExpiresAt = existingExport.ExpiresAt
                };
            }

            // Create new export request
            var exportId = Guid.NewGuid().ToString("N");
            var dataExport = new DataExport
            {
                UserId = userId,
                ExportId = exportId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(ExpirationHours),
                IsReady = false
            };

            _context.Set<DataExport>().Add(dataExport);
            await _context.SaveChangesAsync();

            // Generate the export asynchronously
            _ = Task.Run(async () => await GenerateExportAsync(userId, exportId));

            _logger.LogInformation("Data export requested for user {UserId}, export ID: {ExportId}", userId, exportId);

            return new DataExportResult
            {
                Success = true,
                Message = "Export request created. You will receive an email when it's ready.",
                ExportId = exportId,
                ExpiresAt = dataExport.ExpiresAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting data export for user {UserId}", userId);
            throw;
        }
    }

    public async Task<DataExportStatus?> GetExportStatusAsync(int userId, string exportId)
    {
        var dataExport = await _context.Set<DataExport>()
            .FirstOrDefaultAsync(e => e.UserId == userId && e.ExportId == exportId);

        if (dataExport == null)
        {
            return null;
        }

        return new DataExportStatus
        {
            ExportId = dataExport.ExportId,
            IsReady = dataExport.IsReady,
            CreatedAt = dataExport.CreatedAt,
            ExpiresAt = dataExport.ExpiresAt,
            FileSizeBytes = dataExport.FileSizeBytes
        };
    }

    public async Task<byte[]?> DownloadExportAsync(int userId, string exportId)
    {
        try
        {
            var dataExport = await _context.Set<DataExport>()
                .FirstOrDefaultAsync(e => e.UserId == userId && e.ExportId == exportId);

            if (dataExport == null)
            {
                _logger.LogWarning("Export not found: {ExportId} for user {UserId}", exportId, userId);
                return null;
            }

            if (!dataExport.IsReady)
            {
                _logger.LogWarning("Export not ready: {ExportId}", exportId);
                return null;
            }

            if (dataExport.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Export expired: {ExportId}", exportId);
                return null;
            }

            if (string.IsNullOrEmpty(dataExport.FilePath) || !File.Exists(dataExport.FilePath))
            {
                _logger.LogError("Export file not found: {FilePath}", dataExport.FilePath);
                return null;
            }

            // Mark as downloaded
            dataExport.IsDownloaded = true;
            dataExport.DownloadedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Export downloaded: {ExportId} by user {UserId}", exportId, userId);

            return await File.ReadAllBytesAsync(dataExport.FilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading export {ExportId} for user {UserId}", exportId, userId);
            throw;
        }
    }

    public async Task CleanupExpiredExportsAsync()
    {
        try
        {
            var expiredExports = await _context.Set<DataExport>()
                .Where(e => e.ExpiresAt < DateTime.UtcNow)
                .ToListAsync();

            _logger.LogInformation("Cleaning up {Count} expired exports", expiredExports.Count);

            foreach (var export in expiredExports)
            {
                try
                {
                    // Delete file if exists
                    if (!string.IsNullOrEmpty(export.FilePath) && File.Exists(export.FilePath))
                    {
                        File.Delete(export.FilePath);
                        _logger.LogInformation("Deleted export file: {FilePath}", export.FilePath);
                    }

                    // Remove from database
                    _context.Set<DataExport>().Remove(export);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error cleaning up export {ExportId}", export.ExportId);
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Completed cleanup of expired exports");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired exports");
            throw;
        }
    }

    private async Task GenerateExportAsync(int userId, string exportId)
    {
        try
        {
            _logger.LogInformation("Starting export generation for user {UserId}, export {ExportId}", userId, exportId);

            // Collect all user data
            var userData = await CollectUserDataAsync(userId);

            // Serialize to JSON
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var json = JsonSerializer.Serialize(userData, jsonOptions);

            // Save to file
            var fileName = $"user_data_{userId}_{exportId}.json";
            var filePath = Path.Combine(_exportDirectory, fileName);
            await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);

            var fileInfo = new FileInfo(filePath);

            // Update export record
            var dataExport = await _context.Set<DataExport>()
                .FirstOrDefaultAsync(e => e.ExportId == exportId);

            if (dataExport != null)
            {
                dataExport.IsReady = true;
                dataExport.FilePath = filePath;
                dataExport.FileSizeBytes = fileInfo.Length;
                await _context.SaveChangesAsync();

                // Send email notification
                await SendExportReadyEmailAsync(userId, exportId);

                _logger.LogInformation("Export generation completed for user {UserId}, file size: {Size} bytes", 
                    userId, fileInfo.Length);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating export for user {UserId}, export {ExportId}", userId, exportId);
        }
    }

    private async Task<object> CollectUserDataAsync(int userId)
    {
        // Collect all user data from various tables
        var user = await _context.Users
            .Include(u => u.UserProviders)
            .Include(u => u.Subscriptions)
            .Include(u => u.OnboardingProgress)
            .Include(u => u.OnboardingAnswers)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            throw new Exception("User not found");
        }

        var sessions = await _context.UserSessions
            .Where(s => s.UserId == userId)
            .ToListAsync();

        var paywallEvents = await _context.PaywallEvents
            .Where(e => e.UserId == userId)
            .ToListAsync();

        // Create comprehensive data export
        return new
        {
            exportInfo = new
            {
                exportDate = DateTime.UtcNow,
                userId = userId,
                format = "JSON",
                version = "1.0"
            },
            personalInformation = new
            {
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                phoneNumber = user.PhoneNumber,
                dateOfBirth = user.DateOfBirth,
                createdAt = user.CreatedAt,
                updatedAt = user.UpdatedAt
            },
            accountStatus = new
            {
                isActive = user.IsActive,
                emailVerified = user.EmailVerified,
                phoneVerified = user.PhoneVerified,
                onboardingCompleted = user.OnboardingCompleted
            },
            authenticationProviders = user.UserProviders.Select(p => new
            {
                provider = p.Provider,
                providerId = p.ProviderId,
                createdAt = p.CreatedAt
            }).ToList(),
            subscriptions = user.Subscriptions.Select(s => new
            {
                productId = s.ProductId,
                status = s.Status,
                purchaseDate = s.PurchaseDate,
                expiryDate = s.ExpiryDate,
                autoRenew = s.AutoRenew,
                isSandbox = s.IsSandbox,
                createdAt = s.CreatedAt
            }).ToList(),
            
            paywallEvents = paywallEvents.Select(e => new
            {
                paywallId = e.PaywallId,
                eventType = e.EventType,
                createdAt = e.CreatedAt
            }).ToList()
        };
    }

    private async Task SendExportReadyEmailAsync(int userId, string exportId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            
            if (user == null || string.IsNullOrEmpty(user.Email))
                return;

            var downloadUrl = $"{_configuration["AppUrl"]}/api/user/data-export/download/{exportId}";

            var subject = "Your Data Export is Ready";
            var body = $@"
                <h2>Your Data Export is Ready</h2>
                <p>Hello {user.FirstName},</p>
                <p>Your requested data export is now ready for download.</p>
                <p>This export contains all your personal data we have stored.</p>
                <p><strong>Important:</strong> This link will expire in 24 hours.</p>
                <p><a href=""{downloadUrl}"">Download Your Data</a></p>
                <p>Export ID: {exportId}</p>
                <p>If you did not request this export, please contact support immediately.</p>
            ";

            await _emailService.SendEmailAsync(user.Email, subject, body, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending export ready email to user {UserId}", userId);
        }
    }
}
