using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Services.Compliance;

public class AgeVerificationService : IAgeVerificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly ILogger<AgeVerificationService> _logger;
    private readonly IConfiguration _configuration;
    
    private const int MinimumAge = 13; // COPPA requirement
    private const int ParentalConsentAge = 18;
    private const int ConsentTokenExpirationDays = 7;

    public AgeVerificationService(
        ApplicationDbContext context,
        IUserRepository userRepository,
        IEmailService emailService,
        ILogger<AgeVerificationService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _userRepository = userRepository;
        _emailService = emailService;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<AgeVerificationResult> VerifyAgeAsync(int userId, DateTime dateOfBirth)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return new AgeVerificationResult
                {
                    IsVerified = false,
                    Message = "User not found"
                };
            }

            var age = CalculateAge(dateOfBirth);
            
            // Block users under 13 (COPPA)
            if (age < MinimumAge)
            {
                _logger.LogWarning("User {UserId} blocked: Age {Age} is below minimum {MinAge}", 
                    userId, age, MinimumAge);
                
                user.IsActive = false;
                user.AgeVerified = false;
                await _userRepository.UpdateAsync(user);
                
                return new AgeVerificationResult
                {
                    IsVerified = false,
                    Age = age,
                    IsBlocked = true,
                    Message = $"Users must be at least {MinimumAge} years old to use this service."
                };
            }

            // Require parental consent for users 13-17
            if (age < ParentalConsentAge)
            {
                user.AgeVerified = true;
                user.RequiresParentalConsent = true;
                user.DateOfBirth = dateOfBirth;
                await _userRepository.UpdateAsync(user);
                
                _logger.LogInformation("User {UserId} age {Age} requires parental consent", userId, age);
                
                return new AgeVerificationResult
                {
                    IsVerified = true,
                    Age = age,
                    RequiresParentalConsent = true,
                    Message = "Parental consent required for users under 18."
                };
            }

            // Users 18+ are fully verified
            user.AgeVerified = true;
            user.RequiresParentalConsent = false;
            user.ParentalConsentGranted = true; // Not needed, but set to true
            user.DateOfBirth = dateOfBirth;
            await _userRepository.UpdateAsync(user);
            
            _logger.LogInformation("User {UserId} age verified: {Age} years old", userId, age);
            
            return new AgeVerificationResult
            {
                IsVerified = true,
                Age = age,
                RequiresParentalConsent = false,
                Message = "Age verified successfully."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying age for user {UserId}", userId);
            throw;
        }
    }

    public async Task<ParentalConsentResult> RequestParentalConsentAsync(int userId, string parentEmail, string? parentName = null)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return new ParentalConsentResult
                {
                    Success = false,
                    Message = "User not found"
                };
            }

            if (!user.RequiresParentalConsent)
            {
                return new ParentalConsentResult
                {
                    Success = false,
                    Message = "User does not require parental consent"
                };
            }

            // Check if there's already a pending consent request
            var existingConsent = await _context.Set<ParentalConsent>()
                .Where(c => c.UserId == userId && !c.IsGranted && c.ExpiresAt > DateTime.UtcNow)
                .FirstOrDefaultAsync();

            if (existingConsent != null)
            {
                return new ParentalConsentResult
                {
                    Success = false,
                    Message = "A parental consent request is already pending. Please check the parent's email."
                };
            }

            // Create consent request
            var consentToken = Guid.NewGuid().ToString("N");
            var consent = new ParentalConsent
            {
                UserId = userId,
                ParentEmail = parentEmail,
                ParentName = parentName,
                ConsentToken = consentToken,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(ConsentTokenExpirationDays)
            };

            _context.Set<ParentalConsent>().Add(consent);
            await _context.SaveChangesAsync();

            // Send email to parent
            await SendParentalConsentEmailAsync(user, consent);

            _logger.LogInformation("Parental consent requested for user {UserId}, parent email: {ParentEmail}", 
                userId, parentEmail);

            return new ParentalConsentResult
            {
                Success = true,
                Message = "Parental consent request sent. Please check the parent's email.",
                ConsentToken = consentToken,
                ExpiresAt = consent.ExpiresAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting parental consent for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> GrantParentalConsentAsync(string consentToken, string ipAddress)
    {
        try
        {
            var consent = await _context.Set<ParentalConsent>()
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.ConsentToken == consentToken);

            if (consent == null)
            {
                _logger.LogWarning("Invalid consent token: {Token}", consentToken);
                return false;
            }

            if (consent.IsGranted)
            {
                _logger.LogInformation("Consent already granted for token: {Token}", consentToken);
                return true;
            }

            if (consent.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Consent token expired: {Token}", consentToken);
                return false;
            }

            // Grant consent
            consent.IsGranted = true;
            consent.GrantedAt = DateTime.UtcNow;
            consent.IpAddress = ipAddress;

            // Update user
            consent.User.ParentalConsentGranted = true;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Parental consent granted for user {UserId}", consent.UserId);

            // Send confirmation email to user
            await SendConsentGrantedEmailAsync(consent.User);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error granting parental consent for token {Token}", consentToken);
            throw;
        }
    }

    public async Task<bool> MeetsAgeRequirementsAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            return false;

        // Must be age verified
        if (!user.AgeVerified)
            return false;

        // If requires parental consent, must have it granted
        if (user.RequiresParentalConsent && !user.ParentalConsentGranted)
            return false;

        return true;
    }

    public int CalculateAge(DateTime dateOfBirth)
    {
        var today = DateTime.UtcNow;
        var age = today.Year - dateOfBirth.Year;
        
        // Adjust if birthday hasn't occurred this year
        if (dateOfBirth.Date > today.AddYears(-age))
            age--;
        
        return age;
    }

    private async Task SendParentalConsentEmailAsync(User user, ParentalConsent consent)
    {
        try
        {
            var appUrl = _configuration["AppUrl"] ?? "https://yourapp.com";
            var consentUrl = $"{appUrl}/parental-consent/{consent.ConsentToken}";

            var subject = "Parental Consent Required";
            var body = $@"
                <h2>Parental Consent Request</h2>
                <p>Hello {consent.ParentName ?? "Parent/Guardian"},</p>
                <p>Your child {user.FirstName} {user.LastName} ({user.Email}) has registered for our service.</p>
                <p>As they are under 18 years old, we require parental consent to comply with COPPA regulations.</p>
                <p>Please click the link below to grant consent:</p>
                <p><a href=""{consentUrl}"">Grant Parental Consent</a></p>
                <p>This link will expire in {ConsentTokenExpirationDays} days.</p>
                <p>If you did not expect this email or have concerns, please contact our support team.</p>
            ";

            await _emailService.SendEmailAsync(consent.ParentEmail, subject, body, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending parental consent email to {Email}", consent.ParentEmail);
        }
    }

    private async Task SendConsentGrantedEmailAsync(User user)
    {
        try
        {
            if (string.IsNullOrEmpty(user.Email))
                return;

            var subject = "Parental Consent Granted";
            var body = $@"
                <h2>Parental Consent Granted</h2>
                <p>Hello {user.FirstName},</p>
                <p>Your parent/guardian has granted consent for you to use our service.</p>
                <p>You now have full access to all features.</p>
                <p>Thank you for using our service!</p>
            ";

            await _emailService.SendEmailAsync(user.Email, subject, body, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending consent granted email to user {UserId}", user.Id);
        }
    }
}
