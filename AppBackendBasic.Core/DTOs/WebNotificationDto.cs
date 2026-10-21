namespace AppBackendBasic.Core.DTOs;

/// <summary>
/// DTO for web notifications (uses unified Notification entity)
/// </summary>
public class WebNotificationDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Type { get; set; } = "info";
    public string? Icon { get; set; }
    public int? UserId { get; set; }
    public bool IsGlobal { get; set; } = false;
    public bool IsDismissible { get; set; } = true;
    public int? DurationMs { get; set; }
    public string? ActionUrl { get; set; }
    public string? ActionText { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class CreateWebNotificationDto
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Type { get; set; } = "info";
    public string? Icon { get; set; }
    public int? UserId { get; set; }
    public bool IsGlobal { get; set; } = false;
    public bool IsDismissible { get; set; } = true;
    public int? DurationMs { get; set; } = 5000;
    public string? ActionUrl { get; set; }
    public string? ActionText { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class WebNotificationFilterDto
{
    public int? UserId { get; set; }
    public string? Type { get; set; }
    public bool? IsRead { get; set; }
    public bool IncludeExpired { get; set; } = false;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
