using System.ComponentModel.DataAnnotations;

namespace AppBackendBasic.Core.Entities;

/// <summary>
/// User role for role-based access control
/// </summary>
public class UserRole
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Role { get; set; } = "User"; // User, Admin, Moderator
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    // Navigation property
    public virtual User User { get; set; } = null!;
}

/// <summary>
/// Predefined roles
/// </summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Moderator = "Moderator";
    public const string User = "User";
    
    public static readonly string[] AllRoles = { Admin, Moderator, User };
}
