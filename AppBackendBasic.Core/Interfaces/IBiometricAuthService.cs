using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AppBackendBasic.Core.DTOs;

namespace AppBackendBasic.Core.Interfaces;

public interface IBiometricAuthService
{
    Task<DeviceRegistrationResponseDto> RegisterDeviceAsync(DeviceRegistrationDto request);
    Task<AuthResponseDto> AuthenticateWithBiometricAsync(BiometricAuthRequestDto request);
    Task<bool> RemoveDeviceRegistrationAsync(string deviceId, int userId);
    Task<List<BiometricDeviceInfoDto>> GetUserRegisteredDevicesAsync(int userId);
}
