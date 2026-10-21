using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;
using AppBackendBasic.Core.DTOs;
using Microsoft.Extensions.Configuration;

namespace AppBackendBasic.Services.Authentication;

public class EmailVerificationService:IEmailVerificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly PasswordResetService _passwordResetService;
    private readonly IRateLimitingService _rateLimitingService;
    private readonly ILogger<EmailVerificationService> _logger;
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;
    private readonly IGoogleAuthService _googleAuthService;
    private readonly IOtpService _otpService;
    private readonly IUserProviderRepository _userProviderRepository;

    public EmailVerificationService(
        IUserRepository userRepository,
        ApplicationDbContext context,
        IEmailService emailService,
        PasswordResetService passwordResetService,
        IRateLimitingService rateLimitingService,
        ILogger<EmailVerificationService> logger,
    ITokenService tokenService,
    IGoogleAuthService googleAuthService,
    IOtpService otpService,
    IUserProviderRepository userProviderRepository,
    IConfiguration configuration)
    {
        _context = context;
        _userRepository = userRepository;
        _emailService = emailService;
        _passwordResetService = passwordResetService;
        _rateLimitingService = rateLimitingService;
        _logger = logger;
    _tokenService = tokenService;
    _googleAuthService = googleAuthService;
    _otpService = otpService;
    _userProviderRepository = userProviderRepository;
    _configuration = configuration;
    }
public async Task<string> SendEmailLoginOtpAsync(string email)
    {
        try
        {
            // Check if user exists
            var user = await _userRepository.GetByEmailAsync(email);
            
            if (user == null)
            {
                _logger.LogWarning("Email login OTP requested for non-existent user: {Email}", email);
                throw new InvalidOperationException("User not found with this email address.");
            }

            // Check if email is verified
            if (!user.EmailVerified)
            {
                _logger.LogWarning("Email login OTP requested for unverified email: {Email}", email);
                throw new InvalidOperationException("Email address is not verified. Please verify your email first.");
            }

            // Check rate limiting - max 3 OTP emails per email per 15 minutes
            var rateLimitKey = $"email_otp:{email}";
            var isAllowed = await _rateLimitingService.IsAllowedAsync(rateLimitKey, 3, TimeSpan.FromMinutes(15));
            
            if (!isAllowed)
            {
                _logger.LogWarning("Rate limit exceeded for email OTP: {Email}", email);
                throw new InvalidOperationException("Too many OTP requests. Please try again later.");
            }

            // Generate and send OTP
            return await SendOtpToEmailAsync(email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email login OTP to: {Email}", email);
            throw;
        }
    }
    
    public async Task<string> SendRegistrationOtpAsync(User user)
    {
        try
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            
            // Check rate limiting - max 3 OTP emails per email per 15 minutes
            var email = user.Email;
            var rateLimitKey = $"email_otp:{email}";
            var isAllowed = await _rateLimitingService.IsAllowedAsync(rateLimitKey, 3, TimeSpan.FromMinutes(15));
            
            if (!isAllowed)
            {
                _logger.LogWarning("Rate limit exceeded for registration OTP: {Email}", email);
                throw new InvalidOperationException("Too many OTP requests. Please try again later.");
            }
            
            // Generate and send OTP
            return await SendOtpToEmailAsync(email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending registration OTP to: {Email}", user.Email);
            throw;
        }
    }
    
    private async Task<string> SendOtpToEmailAsync(string email)
    {
        try
        {
            // Clean up any existing OTPs for this email
            var existingOtps = await _context.OtpVerifications
                .Where(o => o.Email == email && !o.IsUsed)
                .AsNoTracking()
                .ToListAsync();

            if (existingOtps.Any())
            {
                // Mark existing OTPs as used
                await _context.OtpVerifications
                    .Where(o => o.Email == email && !o.IsUsed)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(o => o.IsUsed, true));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up existing OTPs for email: {Email}", email);
            // Continue anyway - we can still create a new OTP
        }

        // Generate new OTP and session ID
        var otp = GenerateOtp();
        _logger.LogInformation("Email OTP for {Email} is {otp}", email,otp);
        var sessionId = Guid.NewGuid().ToString();

        var otpVerification = new OtpVerification
        {
            Email = email, // Using email as the identifier
            Otp = otp,
            SessionId = sessionId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };

        _context.OtpVerifications.Add(otpVerification);
        await _context.SaveChangesAsync();
        
        // Send OTP via email (non-blocking - don't fail if email service is down)
        try
        {
            var emailSent = await _emailService.SendOtpEmailAsync(email, otp);
            
            if (emailSent)
            {
                _logger.LogInformation("Email OTP sent successfully to: {Email}", email);
            }
            else
            {
                _logger.LogWarning("Failed to send OTP email to {Email}. OTP saved in database: {Otp}", email, otp);
                // Don't throw - OTP is saved in DB, user can still verify if they get the OTP through other means
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending OTP email to {Email}. OTP saved in database: {Otp}", email, otp);
            // Don't throw - allow registration to continue even if email fails
        }

        return sessionId;
    }

    public async Task<AuthResponseDto> VerifyEmailLoginOtpAsync(EmailOtpLoginRequestDto request)
    {
        try
        {
            // Validate OTP
            var isValid = await _otpService.ValidateOtpEmailAsync(request.Email, request.Otp, request.SessionId);
            
            if (!isValid)
            {
                _logger.LogWarning("Email login OTP verification failed for: {Email}", request.Email);
                return new AuthResponseDto { AccessToken = string.Empty, RefreshToken = string.Empty };
            }
            
            // Get user by email
            var user = await _userRepository.GetByEmailAsync(request.Email);
            
            if (user == null)
            {
                _logger.LogWarning("Email login failed: User not found for email: {Email}", request.Email);
                return new AuthResponseDto { AccessToken = string.Empty, RefreshToken = string.Empty };
            }
            
            // Generate tokens
            return await GenerateAuthResponseAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying email login OTP for: {Email}", request.Email);
            throw;
        }
    }
    
    public async Task<AuthResponseDto> VerifyRegistrationOtpAsync(EmailOtpLoginRequestDto request)
    {
        try
        {
            // Validate OTP
            var isValid = await _otpService.ValidateOtpAsync(request.Email, string.Empty, request.Otp, request.SessionId, true);
            
            if (!isValid)
            {
                _logger.LogWarning("Registration OTP verification failed for: {Email}", request.Email);
                return new AuthResponseDto { AccessToken = string.Empty, RefreshToken = string.Empty };
            }
            
            // Get user by email
            var user = await _userRepository.GetByEmailAsync(request.Email);
            
            if (user == null)
            {
                _logger.LogWarning("Registration verification failed: User not found for email: {Email}", request.Email);
                return new AuthResponseDto { AccessToken = string.Empty, RefreshToken = string.Empty };
            }
            
            // Update user's email verification status
            user.EmailVerified = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
            
            _logger.LogInformation("Email verified successfully for user: {Email}", request.Email);
            
            // Generate tokens
            return await GenerateAuthResponseAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying registration OTP for: {Email}", request.Email);
            throw;
        }
    }
    
    public async Task<string> ResendEmailOtpAsync(ResendEmailOtpRequestDto request)
    {
        try
        {
            // Check rate limiting first - max 3 OTP emails per email per 15 minutes
            var rateLimitKey = $"email_otp:{request.Email}";
            var isAllowed = await _rateLimitingService.IsAllowedAsync(rateLimitKey, 3, TimeSpan.FromMinutes(15));
            
            if (!isAllowed)
            {
                _logger.LogWarning("Rate limit exceeded for resend OTP: {Email}", request.Email);
                throw new InvalidOperationException("Too many OTP requests. Please try again later.");
            }
            
            // Invalidate ALL existing OTPs for this email (not just the session)
            var existingOtps = await _context.OtpVerifications
                .Where(o => o.Email == request.Email && !o.IsUsed)
                .ToListAsync();
            
            if (existingOtps.Any())
            {
                foreach (var otp in existingOtps)
                {
                    otp.IsUsed = true;
                }
                await _context.SaveChangesAsync();
                _logger.LogInformation("Invalidated {Count} existing OTPs for email: {Email}", existingOtps.Count, request.Email);
            }
            
            // Generate and send new OTP with new session
            var newSessionId = await SendOtpToEmailAsync(request.Email);
            _logger.LogInformation("New OTP sent with session {SessionId} for email: {Email}", newSessionId, request.Email);
            
            return newSessionId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resending OTP to: {Email}", request.Email);
            throw;
        }
    }

    // Password reset methods
    public async Task<bool> ForgotPasswordAsync(ForgotPasswordRequestDto request)
    {
        try
        {
            return await _passwordResetService.SendPasswordResetEmailAsync(request.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing forgot password request for: {Email}", request.Email);
            throw;
        }
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordRequestDto request)
    {
        try
        {
            return await _passwordResetService.ResetPasswordAsync(request.Token, request.NewPassword);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password with token");
            throw;
        }
    }

    private async Task<AuthResponseDto> GenerateAuthResponseAsync(User user)
    {
        var accessToken = await _tokenService.GenerateAccessTokenAsync(user);
        var refreshToken = _tokenService.GenerateRefreshToken();
        
        // Save refresh token
        var refreshTokenEntity = new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(int.Parse(_configuration["JWT:RefreshTokenExpirationDays"] ?? "7"))
        };
        
        await _userRepository.AddRefreshTokenAsync(refreshTokenEntity);
        
        // No device info handling in this method
        
        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                EmailVerified = user.EmailVerified,
                PhoneVerified = user.PhoneVerified,
                Provider = user.UserProviders.FirstOrDefault()?.Provider ?? "Email"
            },
            ExpiresIn = int.Parse(_configuration["JWT:ExpirationMinutes"] ?? "60") * 60 // Convert to seconds
        };
    }
    private string GenerateOtp()
    {
        // Generate a 6-digit OTP
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }
}
