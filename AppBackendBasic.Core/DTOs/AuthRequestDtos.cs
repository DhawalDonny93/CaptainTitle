using System.ComponentModel.DataAnnotations;

namespace AppBackendBasic.Core.DTOs;

public class AppleSignInRequestDto
{
    [Required]
    public string IdentityToken { get; set; } = string.Empty;
    
    public string? AuthorizationCode { get; set; }
    public AppleUserDto? User { get; set; }
    
    [Required]
    public string UserIdentifier { get; set; } = string.Empty;
    
    public DeviceInfoDto? DeviceInfo { get; set; }
}

public class AppleUserDto
{
    public AppleNameDto? Name { get; set; }
    public string? Email { get; set; }
}

public class AppleNameDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}

public class GoogleSignInRequestDto
{
    [Required]
    public string IdToken { get; set; } = string.Empty;
    
        public string? AccessToken { get; set; }
        public GoogleUserDto? User { get; set; }
    
    public bool AcceptsTerms { get; set; } = true;
    
    public DeviceInfoDto? DeviceInfo { get; set; }
}

public class GoogleUserDto
{
    public string? Id { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
    public string? Picture { get; set; }
}

public class EmailLoginRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;
    
    public DeviceInfoDto? DeviceInfo { get; set; }
}

public class EmailRegisterRequestDto
{
    [Required]
    [MaxLength(60)]
    public string UserName { get; set; } = string.Empty;
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;
    
    public DeviceInfoDto? DeviceInfo { get; set; }
}

public class SendOtpRequestDto
{
    [Required]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;
    
    public string CountryCode { get; set; } = "+1";
}

public class VerifyOtpRequestDto
{
    [Required]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;
    
    public string CountryCode { get; set; } = "+1";
    
    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Otp { get; set; } = string.Empty;
    
    [Required]
    public string MobileSessionID { get; set; } = string.Empty;
}

public class RefreshTokenRequestDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

public class GitHubSignInRequestDto
{
    [Required]
    public string Code { get; set; } = string.Empty;
    
    public bool AcceptsTerms { get; set; } = true;
    
    public DeviceInfoDto? DeviceInfo { get; set; }
}

public class DeviceInfoDto
{
    public string? Platform { get; set; }
    public string? Version { get; set; }
    public string? Model { get; set; }
    public string? AppVersion { get; set; }
    public string? DeviceId { get; set; }
}
