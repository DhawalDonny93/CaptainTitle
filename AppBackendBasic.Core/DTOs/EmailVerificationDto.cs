using System.ComponentModel.DataAnnotations;

namespace AppBackendBasic.Core.DTOs;

public class SendEmailVerificationRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class VerifyEmailRequestDto
{
    [Required]
    public string Token { get; set; } = string.Empty;
}

public class EmailOtpLoginRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string Otp { get; set; } = string.Empty;
    
    [Required]
    public string SessionId { get; set; } = string.Empty;
}

public class ResendEmailOtpRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    public string? SessionId { get; set; }
}
