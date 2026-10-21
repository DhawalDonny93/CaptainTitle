using System.ComponentModel.DataAnnotations;

namespace AppBackendBasic.Core.DTOs;

public class SmsLoginRequestDto
{
    [Required]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;
}

public class SmsLoginVerifyRequestDto
{
    [Required]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;
    
    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Otp { get; set; } = string.Empty;
    
    [Required]
    public string SessionId { get; set; } = string.Empty;
}
