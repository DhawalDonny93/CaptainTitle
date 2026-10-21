namespace AppBackendBasic.Core.Entities;

public class PasswordReset
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public int? UserId { get; set; }
    public User? User { get; set; }
}
