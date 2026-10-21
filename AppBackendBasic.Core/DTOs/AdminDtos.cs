namespace AppBackendBasic.Core.DTOs;

/// <summary>
/// Request to upsert a system setting
/// </summary>
public class UpsertSettingRequest
{
    public string Value { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Category { get; set; }
    public string? Description { get; set; }
    public bool? IsPublic { get; set; }
}

/// <summary>
/// Request to bulk update settings
/// </summary>
public class BulkUpdateSettingsRequest
{
    public List<SettingUpdateDto> Settings { get; set; } = new();
}

/// <summary>
/// Setting update DTO
/// </summary>
public class SettingUpdateDto
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Category { get; set; }
    public string? Description { get; set; }
    public bool? IsPublic { get; set; }
}

/// <summary>
/// Request to search audit logs
/// </summary>
public class AuditSearchRequest
{
    public int? UserId { get; set; }
    public string? Action { get; set; }
    public string? Resource { get; set; }
    public string? EntityType { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool? Success { get; set; }
    public int? Limit { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// Dashboard statistics response
/// </summary>
public class DashboardStatsDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int VerifiedUsers { get; set; }
    public int AdminUsers { get; set; }
    public int DeletedUsers { get; set; }
}

/// <summary>
/// Admin user info DTO
/// </summary>
public class AdminUserDto
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Response for user lookup by email
/// Note: Uses UserDto from AuthResponseDto.cs
/// </summary>
public class UserByEmailResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public UserDto? User { get; set; }
}
