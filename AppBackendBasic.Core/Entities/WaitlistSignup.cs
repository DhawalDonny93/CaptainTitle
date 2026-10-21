using System.ComponentModel.DataAnnotations;

namespace AppBackendBasic.Core.Entities;

public class WaitlistSignup
{
    public int Id { get; set; }

    [Required]
    [MaxLength(254)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [MaxLength(512)]
    public string? UserAgent { get; set; }

    [MaxLength(20)]
    public string? Language { get; set; }

    [MaxLength(100)]
    public string? Timezone { get; set; }

    [MaxLength(20)]
    public string? ScreenResolution { get; set; }

    [MaxLength(500)]
    public string? Referrer { get; set; }

    public DateTime SignedUpAt { get; set; } = DateTime.UtcNow;
}
