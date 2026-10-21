using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;

namespace AppBackendBasic.Services.Authentication;

public class BiometricAuthService : IBiometricAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<BiometricAuthService> _logger;

    public BiometricAuthService(
        ApplicationDbContext context,
        ITokenService tokenService,
        IUserRepository userRepository,
        ILogger<BiometricAuthService> logger)
    {
        _context = context;
        _tokenService = tokenService;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<DeviceRegistrationResponseDto> RegisterDeviceAsync(DeviceRegistrationDto request)
    {
        try
        {
            // Check if device is already registered
            var existingDevice = await _context.DeviceRegistrations
                .FirstOrDefaultAsync(d => d.UserId == request.UserId && d.DeviceId == request.DeviceId);

            if (existingDevice != null)
            {
                // Update existing registration
                existingDevice.PublicKey = request.PublicKey;
                existingDevice.DeviceName = request.DeviceName;
                existingDevice.DeviceType = request.DeviceType;
                existingDevice.LastUsedAt = DateTime.UtcNow;
                existingDevice.IsActive = true;
            }
            else
            {
                // Create new registration
                var deviceRegistration = new DeviceRegistration
                {
                    UserId = request.UserId,
                    DeviceId = request.DeviceId,
                    PublicKey = request.PublicKey,
                    DeviceName = request.DeviceName,
                    DeviceType = request.DeviceType,
                    RegisteredAt = DateTime.UtcNow,
                    LastUsedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.DeviceRegistrations.Add(deviceRegistration);
            }

            await _context.SaveChangesAsync();

            return new DeviceRegistrationResponseDto
            {
                Success = true,
                Message = "Device registered successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering device for user {UserId}", request.UserId);
            throw;
        }
    }

    public async Task<AuthResponseDto> AuthenticateWithBiometricAsync(BiometricAuthRequestDto request)
    {
        try
        {
            // Find the device registration
            var deviceRegistration = await _context.DeviceRegistrations
                .FirstOrDefaultAsync(d => d.DeviceId == request.DeviceId && d.IsActive);

            if (deviceRegistration == null)
            {
                _logger.LogWarning("Biometric authentication failed: Device not registered {DeviceId}", request.DeviceId);
                return new AuthResponseDto { AccessToken = string.Empty, RefreshToken = string.Empty };
            }

            // Verify the signature using the stored public key
            bool isSignatureValid = VerifySignature(request.Signature, request.Nonce, deviceRegistration.PublicKey);

            if (!isSignatureValid)
            {
                _logger.LogWarning("Biometric authentication failed: Invalid signature for device {DeviceId}", request.DeviceId);
                return new AuthResponseDto { AccessToken = string.Empty, RefreshToken = string.Empty };
            }

            // Get the user
            var user = await _userRepository.GetByIdAsync(deviceRegistration.UserId);

            if (user == null)
            {
                _logger.LogWarning("Biometric authentication failed: User not found {UserId}", deviceRegistration.UserId);
                return new AuthResponseDto { AccessToken = string.Empty, RefreshToken = string.Empty };
            }

            // Update last used timestamp
            deviceRegistration.LastUsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Generate tokens
            var accessToken = await _tokenService.GenerateAccessTokenAsync(user);
            var refreshToken = _tokenService.GenerateRefreshToken();
            
            // Save refresh token
            var refreshTokenEntity = new RefreshToken
            {
                Token = refreshToken,
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7) // Default to 7 days
            };
            
            await _context.RefreshTokens.AddAsync(refreshTokenEntity);
            await _context.SaveChangesAsync();

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    UserName = user.FirstName,
                    LastName = user.LastName,
                    EmailVerified = user.EmailVerified,
                    PhoneVerified = user.PhoneVerified
                },
                ExpiresIn = 3600 // Default to 1 hour
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during biometric authentication for device {DeviceId}", request.DeviceId);
            throw;
        }
    }

    private bool VerifySignature(string signature, string nonce, string publicKey)
    {
        // Implement signature verification using the stored public key
        try
        {
            // Convert the base64 signature and public key to byte arrays
            byte[] signatureBytes = Convert.FromBase64String(signature);
            byte[] publicKeyBytes = Convert.FromBase64String(publicKey);
            byte[] dataToVerify = System.Text.Encoding.UTF8.GetBytes(nonce);

            // Use appropriate cryptographic algorithm (e.g., RSA or ECDSA)
            using (var rsa = System.Security.Cryptography.RSA.Create())
            {
                rsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);
                return rsa.VerifyData(dataToVerify, signatureBytes, System.Security.Cryptography.HashAlgorithmName.SHA256, System.Security.Cryptography.RSASignaturePadding.Pkcs1);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying signature");
            return false;
        }
    }

    public async Task<bool> RemoveDeviceRegistrationAsync(string deviceId, int userId)
    {
        try
        {
            var deviceRegistration = await _context.DeviceRegistrations
                .FirstOrDefaultAsync(d => d.DeviceId == deviceId && d.UserId == userId);

            if (deviceRegistration == null)
            {
                return false;
            }

            deviceRegistration.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing device registration for user {UserId}, device {DeviceId}", userId, deviceId);
            throw;
        }
    }

    public async Task<List<BiometricDeviceInfoDto>> GetUserRegisteredDevicesAsync(int userId)
    {
        try
        {
            var devices = await _context.DeviceRegistrations
                .Where(d => d.UserId == userId && d.IsActive)
                .Select(d => new BiometricDeviceInfoDto
                {
                    DeviceId = d.DeviceId,
                    DeviceName = d.DeviceName,
                    DeviceType = d.DeviceType,
                    RegisteredAt = d.RegisteredAt,
                    LastUsedAt = d.LastUsedAt
                })
                .ToListAsync();

            return devices;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving registered devices for user {UserId}", userId);
            throw;
        }
    }
}
