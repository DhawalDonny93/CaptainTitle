namespace AppBackendBasic.Core.Interfaces;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string to, string subject, string plainTextContent, string htmlContent);
    Task<bool> SendVerificationEmailAsync(string to, string verificationToken);
    Task<bool> SendPasswordResetEmailAsync(string to, string resetToken);
    Task<bool> SendOtpEmailAsync(string to, string otp);
    
    // Transactional Emails
    Task<bool> SendAccountCreatedEmailAsync(string to, string firstName);
    Task<bool> SendAccountDeletedEmailAsync(string to, string firstName);
    Task<bool> SendSubscriptionRenewedEmailAsync(string to, string firstName, string productName, DateTime expiryDate);
    Task<bool> SendPaymentFailedEmailAsync(string to, string firstName, string productName, string reason);
    Task<bool> SendProfileUpdatedEmailAsync(string to, string firstName);
}

public class EmailResult
{
    public bool Success { get; set; }
    public string? MessageId { get; set; }
    public string? ErrorMessage { get; set; }
}
