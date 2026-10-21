using AppBackendBasic.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Services.BackgroundJobs.Jobs;

public class EmailQueueJob
{
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailQueueJob> _logger;

    public EmailQueueJob(IEmailService emailService, ILogger<EmailQueueJob> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Process pending emails in the queue
    /// </summary>
    public async Task ProcessEmailQueueAsync()
    {
        _logger.LogInformation("Starting email queue processing...");

        try
        {
            // In a real implementation, you would fetch pending emails from a queue/database
            // For now, this is a placeholder for the logic
            
            var processedCount = 0;
            var failedCount = 0;

            // Example: Process up to 50 emails per run
            // var pendingEmails = await _emailRepository.GetPendingEmailsAsync(50);
            
            // foreach (var email in pendingEmails)
            // {
            //     try
            //     {
            //         await _emailService.SendEmailAsync(email.To, email.Subject, email.Body);
            //         await _emailRepository.MarkAsProcessedAsync(email.Id);
            //         processedCount++;
            //     }
            //     catch (Exception ex)
            //     {
            //         _logger.LogError(ex, "Failed to send email to {To}", email.To);
            //         await _emailRepository.MarkAsFailedAsync(email.Id, ex.Message);
            //         failedCount++;
            //     }
            // }

            _logger.LogInformation("Email queue processing completed. Processed: {Processed}, Failed: {Failed}", 
                processedCount, failedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing email queue");
            throw;
        }
    }

    /// <summary>
    /// Send a single email (for immediate processing)
    /// </summary>
    public async Task SendEmailAsync(string to, string subject, string body)
    {
        _logger.LogInformation("Sending email to {To} with subject: {Subject}", to, subject);

        try
        {
            await _emailService.SendEmailAsync(to, subject, body,body);
            _logger.LogInformation("Email sent successfully to {To}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            throw;
        }
    }

    /// <summary>
    /// Send welcome email to new user
    /// </summary>
    public async Task SendWelcomeEmailAsync(string userEmail, string userName)
    {
        _logger.LogInformation("Sending welcome email to {Email}", userEmail);

        try
        {
            var subject = "Welcome to BasicFramework!";
            var body = $@"
                <h1>Welcome, {userName}!</h1>
                <p>Thank you for joining BasicFramework. We're excited to have you on board!</p>
                <p>Get started by exploring our features and setting up your profile.</p>
                <p>If you have any questions, feel free to reach out to our support team.</p>
                <p>Best regards,<br>The BasicFramework Team</p>
            ";

            await _emailService.SendEmailAsync(userEmail, subject, body,body);
            _logger.LogInformation("Welcome email sent to {Email}", userEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to {Email}", userEmail);
            throw;
        }
    }

    /// <summary>
    /// Send password reset email
    /// </summary>
    public async Task SendPasswordResetEmailAsync(string userEmail, string resetToken)
    {
        _logger.LogInformation("Sending password reset email to {Email}", userEmail);

        try
        {
            var subject = "Password Reset Request";
            var resetLink = $"https://yourapp.com/reset-password?token={resetToken}";
            var body = $@"
                <h1>Password Reset Request</h1>
                <p>We received a request to reset your password.</p>
                <p>Click the link below to reset your password:</p>
                <p><a href='{resetLink}'>Reset Password</a></p>
                <p>This link will expire in 1 hour.</p>
                <p>If you didn't request this, please ignore this email.</p>
                <p>Best regards,<br>The BasicFramework Team</p>
            ";

            await _emailService.SendEmailAsync(userEmail, subject, body,body);
            _logger.LogInformation("Password reset email sent to {Email}", userEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", userEmail);
            throw;
        }
    }

    /// <summary>
    /// Send subscription expiry reminder
    /// </summary>
    public async Task SendSubscriptionExpiryReminderAsync(string userEmail, DateTime expiryDate)
    {
        _logger.LogInformation("Sending subscription expiry reminder to {Email}", userEmail);

        try
        {
            var daysRemaining = (expiryDate - DateTime.UtcNow).Days;
            var subject = $"Your Subscription Expires in {daysRemaining} Days";
            var body = $@"
                <h1>Subscription Expiry Reminder</h1>
                <p>Your subscription will expire on {expiryDate:MMMM dd, yyyy}.</p>
                <p>To continue enjoying our premium features, please renew your subscription.</p>
                <p><a href='https://yourapp.com/subscription/renew'>Renew Now</a></p>
                <p>Thank you for being a valued member!</p>
                <p>Best regards,<br>The BasicFramework Team</p>
            ";

            await _emailService.SendEmailAsync(userEmail, subject, body,body);
            _logger.LogInformation("Subscription expiry reminder sent to {Email}", userEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send subscription expiry reminder to {Email}", userEmail);
            throw;
        }
    }
}
