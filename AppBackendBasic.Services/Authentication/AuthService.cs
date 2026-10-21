
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;
using AppBackendBasic.Services.Onboarding;
using AppBackendBasic.Services.Device;

namespace AppBackendBasic.Services.Authentication;

// Helper classes for Apple Sign-In
public class AppleUserInfo
{
    public string Subject { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool EmailVerified { get; set; }
}

public class AppleUserName
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}

public partial class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IGoogleAuthService _googleAuthService;
    private readonly EmailVerificationService _emailVerificationService;
    private readonly PasswordResetService _passwordResetService;
    private readonly IEmailService _emailService;
    private readonly IOtpService _otpService;
    private readonly ApplicationDbContext _context;
    private readonly IUserProviderRepository _userProviderRepository;
    private readonly IRateLimitingService _rateLimitingService;
    private readonly OnboardingDataPopulationService _dataPopulationService;
    private readonly DeviceManagementService _deviceManagementService;

    public AuthService(
    IUserRepository userRepository,
    ITokenService tokenService,
    IGoogleAuthService googleAuthService,
    EmailVerificationService emailVerificationService,
    PasswordResetService passwordResetService,
    IEmailService emailService,
    IOtpService otpService,
    ApplicationDbContext context,
    IUserProviderRepository userProviderRepository,
    IRateLimitingService rateLimitingService,
    OnboardingDataPopulationService dataPopulationService,
    DeviceManagementService deviceManagementService,
    ILogger<AuthService> logger,
    IConfiguration configuration)
{
    _userRepository = userRepository;
    _tokenService = tokenService;
    _googleAuthService = googleAuthService;
    _emailVerificationService = emailVerificationService;
    _passwordResetService = passwordResetService;
    _emailService = emailService;
    _otpService = otpService;
    _context = context;
    _userProviderRepository = userProviderRepository;
        _rateLimitingService = rateLimitingService;
    _dataPopulationService = dataPopulationService;
    _deviceManagementService = deviceManagementService;
    _logger = logger;
    _configuration = configuration;
}

    public async Task<AuthResponseDto> EmailLoginAsync(EmailLoginRequestDto request)
    {
        try
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            
            if (user == null)
            {
                _logger.LogWarning("Login attempt failed: User not found with email {Email}", request.Email);
                return new AuthResponseDto { AccessToken = string.Empty, RefreshToken = string.Empty };
            }

            // Check if account is locked out
            if (user.IsLockedOut)
            {
                var remainingTime = user.LockoutEnd.Value - DateTime.UtcNow;
                _logger.LogWarning("Login attempt failed: Account locked for user {Email}, remaining time: {Minutes} minutes", 
                    request.Email, remainingTime.TotalMinutes);
                
                return new AuthResponseDto 
                { 
                    AccessToken = string.Empty, 
                    RefreshToken = string.Empty,
                    IsLockedOut = true,
                    LockoutEnd = user.LockoutEnd,
                    Message = $"Account is locked. Please try again in {Math.Ceiling(remainingTime.TotalMinutes)} minutes."
                };
            }

            if (!VerifyPasswordHash(request.Password, user.PasswordHash))
            {
                // Increment failed login attempts
                user.FailedLoginAttempts++;
                
                var maxAttempts = int.Parse(_configuration["AccountLockout:MaxFailedAttempts"] ?? "5");
                var lockoutMinutes = int.Parse(_configuration["AccountLockout:LockoutDurationMinutes"] ?? "15");
                
                if (user.FailedLoginAttempts >= maxAttempts)
                {
                    // Lock the account
                    user.LockoutEnd = DateTime.UtcNow.AddMinutes(lockoutMinutes);
                    _logger.LogWarning("Account locked for user {Email} after {Attempts} failed attempts", 
                        request.Email, user.FailedLoginAttempts);
                }
                
                await _userRepository.UpdateAsync(user);
                
                _logger.LogWarning("Login attempt failed: Invalid password for user {Email}, attempt {Attempt}/{Max}", 
                    request.Email, user.FailedLoginAttempts, maxAttempts);
                
                return new AuthResponseDto 
                { 
                    AccessToken = string.Empty, 
                    RefreshToken = string.Empty,
                    Message = user.FailedLoginAttempts >= maxAttempts 
                        ? $"Account locked due to too many failed attempts. Please try again in {lockoutMinutes} minutes."
                        : $"Invalid credentials. {maxAttempts - user.FailedLoginAttempts} attempts remaining."
                };
            }

            // Successful login - reset failed attempts and lockout
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            await _userRepository.UpdateAsync(user);

            return await GenerateAuthResponseAsync(user, request.DeviceInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during email login for {Email}", request.Email);
            throw;
        }
    }

    public async Task<AuthResponseDto> EmailRegisterAsync(EmailRegisterRequestDto request)
    {
        try
        {
            _logger.LogInformation("Starting registration for email: {Email}", request.Email);
            
            // Create user object
            var user = new User
            {
                FirstName = request.UserName,
                Email = request.Email,
                PasswordHash = CreatePasswordHash(request.Password),
                EmailVerified = false // Email verification required
            };

            // Try to create user - let database unique constraint handle duplicates
            try
            {
                _logger.LogInformation("Attempting to create user in database for email: {Email}", request.Email);
                await _userRepository.CreateAsync(user);
                _logger.LogInformation("User created successfully with ID {UserId} for email {Email}", user.Id, request.Email);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex) when (
                ex.InnerException?.Message?.Contains("IX_User_Email") == true ||
                ex.InnerException?.Message?.Contains("duplicate key") == true ||
                ex.InnerException?.Message?.Contains("23505") == true)
            {
                _logger.LogWarning("Registration failed: Email {Email} already exists (duplicate key constraint). Error: {Error}", 
                    request.Email, ex.InnerException?.Message ?? ex.Message);
                return new AuthResponseDto { AccessToken = string.Empty, RefreshToken = string.Empty };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating user for email {Email}", request.Email);
                throw;
            }
            
            // NOTE: Default entries (preferences) are NOT created during registration
            // to avoid DbContext tracking issues that cause duplicate inserts.
            // Preferences will be created lazily when first accessed by the user.
            
            try
            {
                // Send verification OTP
                var sessionId = await _emailVerificationService.SendRegistrationOtpAsync(user);
                
                // Return response with verification required flag
                return new AuthResponseDto
                {
                    AccessToken = string.Empty,
                    RefreshToken = string.Empty,
                    RequiresEmailVerification = true,
                    EmailVerificationSessionId = sessionId,
                    User = new UserDto
                    {
                        Id = user.Id,
UserName=user.FirstName,
                        Email = user.Email,
                        EmailVerified = false,
                        Provider = "Email"
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send verification email to {Email}", request.Email);
                
                // Return error response
                return new AuthResponseDto
                {
                    AccessToken = string.Empty,
                    RefreshToken = string.Empty,
                    RequiresEmailVerification = true,
                    User = new UserDto
                    {
                        Id = user.Id,
UserName=user.FirstName,
                        Email = user.Email,
                        EmailVerified = false,
                        Provider = "Email"
                    }
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during email registration for {Email}", request.Email);
            throw;
        }
    }

    public async Task<AuthResponseDto> GoogleSignInAsync(GoogleSignInRequestDto request)
{
    try
    {
        // Validate the Google ID token using GoogleAuthService
        var googleTokenInfo = await _googleAuthService.ValidateGoogleTokenAsync(request.IdToken);
        
        if (googleTokenInfo == null)
        {
            _logger.LogWarning("Google sign-in failed: Invalid token");
            return new AuthResponseDto { AccessToken = string.Empty, RefreshToken = string.Empty };
        }
        
        string providerId = googleTokenInfo.Sub;
        string email = googleTokenInfo.Email;
        string name = googleTokenInfo.Name;
        
        var existingUser = await _userRepository.GetByProviderAsync("Google", providerId);
        
        if (existingUser == null)
        {
            // Check if user exists with the same email
            existingUser = await _userRepository.GetByEmailAsync(email);
            
            if (existingUser != null)
            {
                // Link the Google account to the existing user
                existingUser.UserProviders.Add(new UserProvider
                {
                    Provider = "Google",
                    ProviderId = providerId,
                    UserId = existingUser.Id
                });
                
                await _userRepository.UpdateAsync(existingUser);
            }
            else
            {
                // Create a new user
                var names = name.Split(' ');
                var firstName = names.FirstOrDefault() ?? "";
                var lastName = names.Length > 1 ? string.Join(" ", names.Skip(1)) : "";
                
                existingUser = new User
                {
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    EmailVerified = googleTokenInfo.EmailVerified
                };
                
                existingUser.UserProviders.Add(new UserProvider
                {
                    Provider = "Google",
                    ProviderId = providerId
                });
                
                await _userRepository.CreateAsync(existingUser);
                
                // NOTE: Default entries (preferences) are NOT created during registration
                // to avoid DbContext tracking issues that cause duplicate inserts.
                // Preferences will be created lazily when first accessed by the user.
            }
        }

        return await GenerateAuthResponseAsync(existingUser, request.DeviceInfo);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error during Google sign-in");
        throw;
    }
}

    public async Task<AuthResponseDto> AppleSignInAsync(AppleSignInRequestDto request)
{
    try
    {
        // Validate the identity token
        var validatedToken = await VerifyAppleToken(request.IdentityToken);
        
        // Extract user information from the token
        var appleUserInfo = ExtractAppleUserInfo(validatedToken);
        
        // Get user name from authorization code (only available on first login)
        var userName = await GetUserNameFromAuthCode(request.AuthorizationCode);
        
        // Use the Apple user identifier as the provider ID
        string providerId = appleUserInfo.Subject;
        string? email = appleUserInfo.Email;
        string? firstName = userName?.FirstName ?? request.User?.Name?.FirstName;
        string? lastName = userName?.LastName ?? request.User?.Name?.LastName;
        
        var existingUser = await _userRepository.GetByProviderAsync("Apple", providerId);
        
        if (existingUser == null && !string.IsNullOrEmpty(email))
        {
            // Check if user exists with the same email
            existingUser = await _userRepository.GetByEmailAsync(email);
            
            if (existingUser != null)
            {
                // Link the Apple account to the existing user
                existingUser.UserProviders.Add(new UserProvider
                {
                    Provider = "Apple",
                    ProviderId = providerId,
                    UserId = existingUser.Id
                });
                
                await _userRepository.UpdateAsync(existingUser);
            }
            else
            {
                // Create a new user
                existingUser = new User
                {
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    EmailVerified = appleUserInfo.EmailVerified
                };
                
                existingUser.UserProviders.Add(new UserProvider
                {
                    Provider = "Apple",
                    ProviderId = providerId
                });
                
                await _userRepository.CreateAsync(existingUser);
                
                // NOTE: Default entries (preferences) are NOT created during registration
                // to avoid DbContext tracking issues that cause duplicate inserts.
                // Preferences will be created lazily when first accessed by the user.
            }
        }

        if (existingUser == null)
        {
            _logger.LogWarning("Apple sign-in failed: Unable to create or find user");
            return new AuthResponseDto { AccessToken = string.Empty, RefreshToken = string.Empty };
        }

        return await GenerateAuthResponseAsync(existingUser, request.DeviceInfo);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error during Apple sign-in");
        throw;
    }
}

    private async Task<AppleUserName> GetUserNameFromAuthCode(string authorizationCode)
{
    // This is only available on first login
    if (string.IsNullOrEmpty(authorizationCode))
    {
        return null;
    }
    
    try
    {
        // Exchange authorization code for access token
        var httpClient = new HttpClient();
        var tokenRequestBody = new Dictionary<string, string>
        {
            { "client_id", _configuration["Apple:ClientId"] },
            { "client_secret", GenerateClientSecret() }, // Generate using your private key
            { "code", authorizationCode },
            { "grant_type", "authorization_code" },
            { "redirect_uri", _configuration["Apple:RedirectUri"] }
        };
        
        var tokenResponse = await httpClient.PostAsync(
            "https://appleid.apple.com/auth/token",
            new FormUrlEncodedContent(tokenRequestBody)
        );
        
        var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
        var tokenData = JsonDocument.Parse(tokenContent).RootElement;
        
        // The user data is in the id_token
        if (tokenData.TryGetProperty("id_token", out var idToken))
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(idToken.GetString());
            
            // User name might be in the user claim for first-time sign-ins
            if (jwt.Claims.FirstOrDefault(c => c.Type == "user")?.Value is string userJson)
            {
                var userData = JsonDocument.Parse(userJson).RootElement;
                
                return new AppleUserName
                {
                    FirstName = userData.TryGetProperty("name", out var name) ? 
                        (name.TryGetProperty("firstName", out var firstName) ? firstName.GetString() : null) : null,
                    LastName = userData.TryGetProperty("name", out var _name) ? 
                        (_name.TryGetProperty("lastName", out var lastName) ? lastName.GetString() : null) : null
                };
            }
        }
        
        return null;
    }
    catch
    {
        return null;
    }
}private string GenerateClientSecret()
{
    // Load your private key (should be stored securely)
    var privateKey = _configuration["Apple:PrivateKey"];
    var keyId = _configuration["Apple:KeyId"];
    var teamId = _configuration["Apple:TeamId"];
    var clientId = _configuration["Apple:ClientId"];
    
    // Create JWT header
    var header = new Dictionary<string, object>
    {
        { "alg", "ES256" },
        { "kid", keyId }
    };
    
    // Create JWT payload
    var payload = new Dictionary<string, object>
    {
        { "iss", teamId },
        { "iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
        { "exp", DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds() },
        { "aud", "https://appleid.apple.com" },
        { "sub", clientId }
    };
    
    // Sign the JWT using the ES256 algorithm and your private key
    var key = ECDsa.Create();
    key.ImportPkcs8PrivateKey(Convert.FromBase64String(privateKey), out _);
    
    var tokenHandler = new JwtSecurityTokenHandler();
    var token = tokenHandler.CreateJwtSecurityToken(
        issuer: null,
        audience: null,
        subject: null,
        notBefore: null,
        expires: null,
        issuedAt: null,
        signingCredentials: new SigningCredentials(
            new ECDsaSecurityKey(key),
            SecurityAlgorithms.EcdsaSha256
        )
    );
    
    // Set the custom header and payload
    foreach (var item in header)
    {
        token.Header[item.Key] = item.Value;
    }
    
    foreach (var item in payload)
    {
        token.Payload[item.Key] = item.Value;
    }
    
    return tokenHandler.WriteToken(token);
}
private AppleUserInfo ExtractAppleUserInfo(JwtSecurityToken validatedToken)
{
    var userInfo = new AppleUserInfo
    {
        Subject = validatedToken.Subject, // This is the user's unique Apple ID
        Email = validatedToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value,
        EmailVerified = bool.Parse(validatedToken.Claims.FirstOrDefault(c => c.Type == "email_verified")?.Value ?? "false")
    };
    
    return userInfo;
}
// 1. Get the Apple public key to verify the token
public async Task<JwtSecurityToken> VerifyAppleToken(string identityToken)
{
    // Fetch Apple's public keys
    var httpClient = new HttpClient();
    var appleKeyResponse = await httpClient.GetStringAsync("https://appleid.apple.com/auth/keys");
    var appleKeys = JsonDocument.Parse(appleKeyResponse).RootElement;
    
    // Parse the JWT without validation first to get the key ID (kid)
    var handler = new JwtSecurityTokenHandler();
    var jwt = handler.ReadJwtToken(identityToken);
    var kid = jwt.Header["kid"].ToString();
    
    // Find the matching key in Apple's keys
    JsonElement? matchingKey = null;
    foreach (var key in appleKeys.GetProperty("keys").EnumerateArray())
    {
        if (key.GetProperty("kid").GetString() == kid)
        {
            matchingKey = key;
            break;
        }
    }
    
    if (matchingKey == null)
    {
        throw new SecurityTokenValidationException("No matching Apple key found");
    }
    
    // Create RSA key using Apple's public key components
    var rsa = new RSACryptoServiceProvider();
    var rsaParameters = new RSAParameters
    {
        Modulus = Base64UrlDecode(matchingKey.Value.GetProperty("n").GetString()),
        Exponent = Base64UrlDecode(matchingKey.Value.GetProperty("e").GetString())
    };
    rsa.ImportParameters(rsaParameters);
    
    // Validate the token
    var validationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = "https://appleid.apple.com",
        ValidateAudience = true,
        ValidAudience = _configuration["Apple:ClientId"], // Your Apple Service ID
        IssuerSigningKey = new RsaSecurityKey(rsa),
        ValidateLifetime = true
    };
    
    SecurityToken validatedToken;
    handler.ValidateToken(identityToken, validationParameters, out validatedToken);
    
    return (JwtSecurityToken)validatedToken;
}

private byte[] Base64UrlDecode(string input)
{
    var output = input
        .Replace('-', '+')
        .Replace('_', '/');
    
    switch (output.Length % 4)
    {
        case 0: break;
        case 2: output += "=="; break;
        case 3: output += "="; break;
        default: throw new ArgumentException("Invalid base64url string");
    }
    
    return Convert.FromBase64String(output);
}

        public async Task<string> SendOtpAsync(SendOtpRequestDto request)
    {
        try
        {
            // Use the OtpService to generate and send OTP via SMS
            var sessionId = await _otpService.GenerateAndStoreOtpAsync(request);
            _logger.LogInformation("OTP request processed for {PhoneNumber}", request.PhoneNumber);
            return sessionId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending OTP to {PhoneNumber}", request.PhoneNumber);
            throw;
        }
    }

    public async Task<AuthResponseDto> VerifyOtpAsync(VerifyOtpRequestDto request)
    {
        try
        {
            // Use OtpService to validate the OTP
            var isValid = await _otpService.ValidateOtpAsync(request.PhoneNumber,request.CountryCode, request.Otp, request.MobileSessionID);
            
            if (!isValid)
            {
                _logger.LogWarning("OTP verification failed for {PhoneNumber}", request.PhoneNumber);
                return new AuthResponseDto { AccessToken = string.Empty, RefreshToken = string.Empty };
            }
            
            var existingUser = await _userRepository.GetByPhoneNumberAsync(request.PhoneNumber);
            
            if (existingUser == null)
            {
                // Create a new user with phone number
                existingUser = new User
                {
                    PhoneNumber = request.PhoneNumber,
                    PhoneVerified = true
                };
                
                await _userRepository.CreateAsync(existingUser);
            }
            else
            {
                // Update phone verification status
                existingUser.PhoneVerified = true;
                await _userRepository.UpdateAsync(existingUser);
            }
            
            return await GenerateAuthResponseAsync(existingUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying OTP for {PhoneNumber}", request.PhoneNumber);
            throw;
        }
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request)
    {
        try
        {
            var refreshToken = await _userRepository.GetRefreshTokenAsync(request.RefreshToken);
            
            if (refreshToken == null)
            {
                _logger.LogWarning("Refresh token not found or expired");
                return new AuthResponseDto { AccessToken = string.Empty, RefreshToken = string.Empty };
            }
            
            var user = refreshToken.User;
            
            // Revoke the current refresh token
            refreshToken.IsRevoked = true;
            await _userRepository.UpdateRefreshTokenAsync(refreshToken);
            
            // Generate a new token pair
            return await GenerateAuthResponseAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            throw;
        }
    }

    public async Task<bool> RevokeTokenAsync(string token)
    {
        try
        {
            var refreshToken = await _userRepository.GetRefreshTokenAsync(token);
            
            if (refreshToken == null)
            {
                return false;
            }
            
            refreshToken.IsRevoked = true;
            await _userRepository.UpdateRefreshTokenAsync(refreshToken);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking token");
            throw;
        }
    }

    public async Task<bool> LogoutAsync(string token)
    {
        try
        {
            // Delete the refresh token from database completely
            await _userRepository.DeleteRefreshTokenAsync(token);
            
            _logger.LogInformation("Refresh token deleted successfully during logout");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout - deleting refresh token");
            return false;
        }
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _userRepository.GetByIdAsync(userId);
    }
    
    public async Task<string> SendSmsLoginOtpAsync(SmsLoginRequestDto request)
{
    try
    {
        // Use existing OTP service which now has rate limiting and Twilio
        var sendOtpRequest = new SendOtpRequestDto { PhoneNumber = request.PhoneNumber };
        return await SendOtpAsync(sendOtpRequest);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error sending SMS login OTP to {PhoneNumber}", request.PhoneNumber);
        throw;
    }
}

public async Task<AuthResponseDto> VerifySmsLoginOtpAsync(SmsLoginVerifyRequestDto request)
{
    try
    {
        // Use existing OTP verification which now has rate limiting
        var verifyOtpRequest = new VerifyOtpRequestDto 
        { 
            PhoneNumber = request.PhoneNumber,
            Otp = request.Otp,
            MobileSessionID = request.SessionId
        };
        
        return await VerifyOtpAsync(verifyOtpRequest);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error verifying SMS login OTP for {PhoneNumber}", request.PhoneNumber);
        throw;
    }
}

    private async Task<AuthResponseDto> GenerateAuthResponseAsync(User user, DeviceInfoDto? deviceInfo = null)
    {
        // Ensure user has a team (create default team if needed)
        await EnsureUserHasTeamAsync(user.Id);

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
        
        // Always create a session for logged-in users
        var idleTimeoutMinutes = int.Parse(_configuration["Session:IdleTimeoutMinutes"] ?? "30");
        
        var userSession = new UserSession
        {
            UserId = user.Id,
            DeviceId = deviceInfo?.DeviceId ?? $"web-{Guid.NewGuid().ToString().Substring(0, 8)}",
            DeviceModel = deviceInfo?.Model ?? "Web Browser",
            Platform = deviceInfo?.Platform ?? "Web",
            AppVersion = deviceInfo?.AppVersion ?? "1.0.0",
            CreatedAt = DateTime.UtcNow,
            LastActiveAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(idleTimeoutMinutes),
            TimeoutMinutes = idleTimeoutMinutes
        };
        
        await _userRepository.AddUserSessionAsync(userSession);
        
        // Register or update device token if device info is provided
        if (deviceInfo != null)
        {
            try
            {
                await _deviceManagementService.RegisterOrUpdateDeviceTokenAsync(user.Id, deviceInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register device for user {UserId}", user.Id);
                // Don't fail login if device registration fails
            }
        }
        
        // Check if user has an active subscription
        bool requiresSubscription = true;
        try
        {
            var hasActiveSubscription = await _context.UserSubscriptions
                .AnyAsync(s => s.UserId == user.Id && s.Status == "Active");
            
            requiresSubscription = !hasActiveSubscription;
            
            if (requiresSubscription)
            {
                _logger.LogInformation("User {UserId} does not have an active subscription, will redirect to pricing", user.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking subscription status for user {UserId}", user.Id);
            // Don't fail login if subscription check fails
        }
        
        // Load UserProfile with SiteAudit status to minimize DB calls
        UserProfilePreferencesDto? profileDto = null;
        try
        {
            var userProfile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == user.Id);
            
            if (userProfile != null)
            {
                profileDto = new UserProfilePreferencesDto
                {
                    Language = userProfile.Language,
                    Theme = userProfile.Theme,
                    Timezone = userProfile.Timezone,
                    ExtendedProperties = userProfile.ExtendedProperties
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user profile for user {UserId}", user.Id);
            // Don't fail login if profile load fails
        }
        
        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            RequiresSubscription = requiresSubscription,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.FirstName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                EmailVerified = user.EmailVerified,
                PhoneVerified = user.PhoneVerified,
                IsActive = user.IsActive,
                IsSubscribed = user.IsSubscribed,
                OnboardingCompleted = user.OnboardingCompleted,
                Provider = user.UserProviders.FirstOrDefault()?.Provider ?? user.PhoneNumber ?? "Email",
                CreatedAt = user.CreatedAt,
                Profile = profileDto
            },
            ExpiresIn = int.Parse(_configuration["JWT:ExpirationMinutes"] ?? "60") * 60 // Convert to seconds
        };
    }

    private string CreatePasswordHash(string password)
    {
        using var hmac = new HMACSHA512();
        var salt = hmac.Key;
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        
        return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);
    }

    private bool VerifyPasswordHash(string password, string? storedHash)
    {
        if (string.IsNullOrEmpty(storedHash))
        {
            return false;
        }
        
        var parts = storedHash.Split(':');
        if (parts.Length != 2)
        {
            return false;
        }
        
        var salt = Convert.FromBase64String(parts[0]);
        var hash = Convert.FromBase64String(parts[1]);
        
        using var hmac = new HMACSHA512(salt);
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        
        for (int i = 0; i < computedHash.Length; i++)
        {
            if (computedHash[i] != hash[i])
            {
                return false;
            }
        }
        
        return true;
    }

    private string GenerateOtp()
    {
        // Generate a 6-digit OTP
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }

    /// <summary>
    /// Ensures user has at least one active team. Creates a default team if needed.
    /// </summary>
    private async Task EnsureUserHasTeamAsync(int userId)
    {
        try
        {
            // Check if user already has an active team mapping
            var existingMapping = await _context.UserTeamMappings
                .Where(m => m.UserId == userId && m.IsActive)
                .FirstOrDefaultAsync();

            if (existingMapping != null)
            {
                _logger.LogDebug("User {UserId} already has active team mapping", userId);
                return;
            }

            // Create a default team for the user
            var team = new Team
            {
                Name = $"Personal Workspace",
                IsActive = true
            };

            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            // Create the user-team mapping
            var mapping = new UserTeamMapping
            {
                UserId = userId,
                TeamId = team.Id,
                IsActive = true,
                Role = "Owner"
            };

            _context.UserTeamMappings.Add(mapping);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created default team {TeamId} for user {UserId}", team.Id, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring user {UserId} has a team", userId);
            // Don't fail login if team creation fails - user can onboard later
        }
    }
}