using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;
using AppBackendBasic.Services.Onboarding;

namespace AppBackendBasic.Services.Authentication;

/// <summary>
/// Service for handling Apple Sign-In authentication with identity token verification
/// </summary>
public class AppleAuthService : IAppleAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IUserRepository _userRepository;
    private readonly IUserProviderRepository _userProviderRepository;
    private readonly ILogger<AppleAuthService> _logger;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly OnboardingDataPopulationService _dataPopulationService;
    
    private const string ApplePublicKeysUrl = "https://appleid.apple.com/auth/keys";
    private const string AppleIssuer = "https://appleid.apple.com";
    
    public AppleAuthService(
        ApplicationDbContext context,
        ITokenService tokenService,
        IUserRepository userRepository,
        IUserProviderRepository userProviderRepository,
        ILogger<AppleAuthService> logger,
        IConfiguration configuration,
        HttpClient httpClient,
        OnboardingDataPopulationService dataPopulationService)
    {
        _context = context;
        _tokenService = tokenService;
        _userRepository = userRepository;
        _userProviderRepository = userProviderRepository;
        _logger = logger;
        _configuration = configuration;
        _httpClient = httpClient;
        _dataPopulationService = dataPopulationService;
    }

    public async Task<AuthResponseDto> AuthenticateWithAppleAsync(AppleSignInRequestDto request)
    {
        try
        {
            _logger.LogInformation("Starting Apple Sign-In authentication");

            // Step 1: Validate the identity token with Apple
            var tokenPayload = await ValidateIdentityTokenAsync(request.IdentityToken);
            
            if (tokenPayload == null)
            {
                _logger.LogWarning("Apple identity token validation failed");
                return new AuthResponseDto
                {
                    AccessToken = string.Empty,
                    RefreshToken = string.Empty,
                    Message = "Invalid Apple identity token"
                };
            }

            var appleUserId = tokenPayload.Subject; // This is the unique Apple user ID
            _logger.LogInformation("Apple token validated successfully. Apple User ID: {AppleUserId}", appleUserId);

            // Step 2: Check if user exists by Apple user ID
            var existingProvider = await _userProviderRepository.GetByProviderAndProviderIdAsync("apple", appleUserId);
            User user;

            if (existingProvider != null)
            {
                // Step 3a: Existing user - return full profile
                _logger.LogInformation("Existing Apple user found: {UserId}", existingProvider.UserId);
                user = await _userRepository.GetByIdAsync(existingProvider.UserId);
                
                if (user == null)
                {
                    _logger.LogError("User not found for provider: {ProviderId}", existingProvider.Id);
                    return new AuthResponseDto
                    {
                        AccessToken = string.Empty,
                        RefreshToken = string.Empty,
                        Message = "User account not found"
                    };
                }
            }
            else
            {
                // Step 3b: New user - create account
                _logger.LogInformation("New Apple user, creating account");
                
                // Extract user info from token or request
                var email = tokenPayload.Email ?? request.User?.Email;
                var firstName = request.User?.Name?.FirstName ?? "Apple";
                var lastName = request.User?.Name?.LastName ?? "User";
                
                // Check if email already exists (user might have registered with email before)
                User? existingUser = null;
                if (!string.IsNullOrEmpty(email))
                {
                    existingUser = await _userRepository.GetByEmailAsync(email);
                }

                if (existingUser != null)
                {
                    // Link Apple account to existing user
                    _logger.LogInformation("Linking Apple account to existing user: {UserId}", existingUser.Id);
                    user = existingUser;
                }
                else
                {
                    // Create new user
                    user = new User
                    {
                        Email = email ?? $"{appleUserId}@privaterelay.appleid.com",
                        PhoneNumber = string.Empty,
                        FirstName = firstName,
                        LastName = lastName,
                        EmailVerified = tokenPayload.EmailVerified,
                        PhoneVerified = false,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _context.Users.AddAsync(user);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("New user created: {UserId}, Email: {Email}", user.Id, user.Email);
                    
                    // Create default entries for new user (onboarding, preferences, etc.)
                    try
                    {
                        await _dataPopulationService.CreateDefaultEntriesForUserAsync(user.Id);
                        _logger.LogInformation("Default entries created for new user: {UserId}", user.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to create default entries for user {UserId}", user.Id);
                        // Don't fail the authentication if default entries creation fails
                    }
                }

                // Create UserProvider entry to link Apple ID
                var userProvider = new UserProvider
                {
                    UserId = user.Id,
                    Provider = "apple",
                    ProviderId = appleUserId,
                    ProviderData = JsonSerializer.Serialize(new
                    {
                        email = tokenPayload.Email,
                        emailVerified = tokenPayload.EmailVerified,
                        isPrivateEmail = tokenPayload.IsPrivateEmail,
                        firstName = firstName,
                        lastName = lastName
                    }),
                    CreatedAt = DateTime.UtcNow
                };

                await _context.UserProviders.AddAsync(userProvider);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Apple provider linked to user: {UserId}", user.Id);
            }

            // Step 4: Generate JWT tokens
            var accessToken = await _tokenService.GenerateAccessTokenAsync(user);
            var refreshToken = _tokenService.GenerateRefreshToken();

            // Save refresh token
            var refreshTokenEntity = new RefreshToken
            {
                Token = refreshToken,
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            await _context.RefreshTokens.AddAsync(refreshTokenEntity);
            await _context.SaveChangesAsync();

            // Track device if device info provided
            if (request.DeviceInfo != null && !string.IsNullOrEmpty(request.DeviceInfo.DeviceId))
            {
                try
                {
                    var existingDevice = await _context.Set<DeviceToken>()
                        .FirstOrDefaultAsync(d => d.UserId == user.Id && d.DeviceId == request.DeviceInfo.DeviceId);

                    if (existingDevice != null)
                    {
                        // Update existing device
                        existingDevice.Platform = request.DeviceInfo.Platform ?? existingDevice.Platform;
                        existingDevice.AppVersion = request.DeviceInfo.AppVersion ?? existingDevice.AppVersion;
                        existingDevice.UpdatedAt = DateTime.UtcNow;
                        existingDevice.IsActive = true;
                    }
                    else
                    {
                        // Register new device
                        var deviceToken = new DeviceToken
                        {
                            UserId = user.Id,
                            DeviceId = request.DeviceInfo.DeviceId,
                            Platform = request.DeviceInfo.Platform ?? "ios",
                            AppVersion = request.DeviceInfo.AppVersion,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        await _context.Set<DeviceToken>().AddAsync(deviceToken);
                    }
                    
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Device tracked for user: {UserId}, DeviceId: {DeviceId}", 
                        user.Id, request.DeviceInfo.DeviceId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to track device for user {UserId}", user.Id);
                    // Don't fail authentication if device tracking fails
                }
            }

            // Step 5: Check subscription status
            var hasActiveSubscription = await _context.Subscriptions
                .AnyAsync(s => s.UserId == user.Id && 
                              s.Status == "active" && 
                              s.ExpiryDate.HasValue && 
                              s.ExpiryDate.Value > DateTime.UtcNow);

            // Step 6: Return consistent response format
            _logger.LogInformation("Apple Sign-In successful for user: {UserId}", user.Id);
            
            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = 3600, // 1 hour
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    UserName = user.FirstName,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    EmailVerified = user.EmailVerified,
                    PhoneVerified = user.PhoneVerified,
                    IsSubscribed = hasActiveSubscription
                },
                Message = "Apple Sign-In successful"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Apple Sign-In authentication");
            return new AuthResponseDto
            {
                AccessToken = string.Empty,
                RefreshToken = string.Empty,
                Message = $"Authentication failed: {ex.Message}"
            };
        }
    }

    public async Task<AppleIdTokenPayload?> ValidateIdentityTokenAsync(string identityToken)
    {
        try
        {
            _logger.LogInformation("Validating Apple identity token");

            // Step 1: Decode JWT header to get Key ID (kid)
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(identityToken);
            var kid = jwtToken.Header.Kid;

            if (string.IsNullOrEmpty(kid))
            {
                _logger.LogWarning("Apple identity token missing Key ID (kid)");
                return null;
            }

            // Step 2: Fetch Apple's public keys
            var publicKeys = await GetApplePublicKeysAsync();
            var publicKey = publicKeys.Keys.FirstOrDefault(k => k.KeyId == kid);

            if (publicKey == null)
            {
                _logger.LogWarning("Apple public key not found for kid: {Kid}", kid);
                return null;
            }

            // Step 3: Create RSA security key from Apple's public key
            var rsa = RSA.Create();
            rsa.ImportParameters(new RSAParameters
            {
                Modulus = Base64UrlDecode(publicKey.Modulus),
                Exponent = Base64UrlDecode(publicKey.Exponent)
            });

            var securityKey = new RsaSecurityKey(rsa);

            // Step 4: Validate the token
            // Support multiple audiences (native iOS, Expo, etc.)
            var allowedAudiences = _configuration.GetSection("Apple:AllowedAudiences").Get<List<string>>() 
                ?? new List<string> { _configuration["Apple:ClientId"] ?? string.Empty };
            
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = AppleIssuer,
                ValidateAudience = true,
                ValidAudiences = allowedAudiences, // Support multiple audiences
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = securityKey,
                ClockSkew = TimeSpan.FromMinutes(5)
            };
            
            _logger.LogInformation("Validating token with audiences: {Audiences}", string.Join(", ", allowedAudiences));

            var principal = handler.ValidateToken(identityToken, validationParameters, out var validatedToken);

            // Step 5: Extract and return payload
            // The payload is already a JSON string, no need to Base64 decode
            var payloadJson = jwtToken.Payload.SerializeToJson();
            var payload = JsonSerializer.Deserialize<AppleIdTokenPayload>(payloadJson);

            _logger.LogInformation("Apple identity token validated successfully. Subject: {Subject}, Email: {Email}", 
                payload?.Subject, payload?.Email);
            return payload;
        }
        catch (SecurityTokenExpiredException ex)
        {
            _logger.LogWarning(ex, "Apple identity token has expired");
            return null;
        }
        catch (SecurityTokenInvalidSignatureException ex)
        {
            _logger.LogWarning(ex, "Apple identity token has invalid signature");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Apple identity token");
            return null;
        }
    }

    private async Task<ApplePublicKeysResponse> GetApplePublicKeysAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync(ApplePublicKeysUrl);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var keys = JsonSerializer.Deserialize<ApplePublicKeysResponse>(content);

            if (keys == null || !keys.Keys.Any())
            {
                throw new Exception("No public keys returned from Apple");
            }

            return keys;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Apple public keys");
            throw;
        }
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var output = input;
        output = output.Replace('-', '+').Replace('_', '/');
        
        switch (output.Length % 4)
        {
            case 2: output += "=="; break;
            case 3: output += "="; break;
        }
        
        return Convert.FromBase64String(output);
    }
}
