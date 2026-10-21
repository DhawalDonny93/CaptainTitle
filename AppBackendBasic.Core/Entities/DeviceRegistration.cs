using System;

namespace AppBackendBasic.Core.Entities;

public class DeviceRegistration
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string DeviceId { get; set; }
    public string PublicKey { get; set; }
    public string DeviceName { get; set; }
    public string DeviceType { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public bool IsActive { get; set; }
    
    public User User { get; set; }
}
