using System;

namespace AppBackendBasic.Core.DTOs;

public class BiometricAuthRequestDto
{
    public string DeviceId { get; set; }
    public string Signature { get; set; }
    public string Nonce { get; set; }
}

public class DeviceRegistrationDto
{
    public int UserId { get; set; }
    public string DeviceId { get; set; }
    public string PublicKey { get; set; }
    public string DeviceName { get; set; }
    public string DeviceType { get; set; }
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
}

public class DeviceRegistrationResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; }
}

public class BiometricDeviceInfoDto
{
    public string DeviceId { get; set; }
    public string DeviceName { get; set; }
    public string DeviceType { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
}
