using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AppBackendBasic.Core.Interfaces;
using System.Text.Json;
using AppBackendBasic.Services.Notifications;

namespace AppBackendBasic.Services.Authentication;

public class SendGridEmailService : IEmailService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SendGridEmailService> _logger;
    private readonly UnsubscribeTokenService _tokenService;
    private readonly string _apiKey;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly string _baseUrl;

    public SendGridEmailService(
        HttpClient httpClient, 
        ILogger<SendGridEmailService> logger, 
        UnsubscribeTokenService tokenService,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _tokenService = tokenService;
        _apiKey = configuration["SendGrid:ApiKey"] ?? throw new ArgumentNullException("SendGrid:ApiKey");
        _fromEmail = configuration["SendGrid:FromEmail"] ?? throw new ArgumentNullException("SendGrid:FromEmail");
        _fromName = configuration["SendGrid:FromName"] ?? configuration["SendGrid:FromEmail"];
        _baseUrl = configuration["AppUrl"] ?? "https://yourapp.com";

        // Set up HTTP client for SendGrid API
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string plainTextContent, string htmlContent)
    {
        try
        {
            var message = new
            {
                personalizations = new[]
                {
                    new { to = new[] { new { email = to } } }
                },
                from = new { email = _fromEmail, name = _fromName },
                subject,
                content = new[]
                {
                    new { type = "text/plain", value = plainTextContent },
                    new { type = "text/html", value = htmlContent }
                }
            };

            var json = JsonSerializer.Serialize(message);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://api.sendgrid.com/v3/mail/send", content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Email sent successfully to {Email}", to);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send email to {Email}. Status: {StatusCode}, Error: {Error}, ApiKey: {ApiKeyPrefix}", 
                    to, response.StatusCode, errorContent, _apiKey?.Substring(0, Math.Min(10, _apiKey?.Length ?? 0)));
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {Email}", to);
            return false;
        }
    }

    public async Task<bool> SendVerificationEmailAsync(string to, string verificationToken)
    {
        var verificationUrl = $"{_baseUrl}/verify-email?token={verificationToken}";
        
        var subject = "Verify Your Email Address";
        var plainTextContent = $"Please verify your email address by clicking this link: {verificationUrl}";
        var htmlContent = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <h2>Email Verification</h2>
                <p>Thank you for registering! Please verify your email address by clicking the button below:</p>
                <p style='margin: 20px 0;'>
                    <a href='{verificationUrl}' style='background-color: #4CAF50; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>
                        Verify Email
                    </a>
                </p>
                <p>If the button doesn't work, you can copy and paste this link into your browser:</p>
                <p>{verificationUrl}</p>
                <p>This link will expire in 24 hours.</p>
            </div>
        ";
        
        return await SendEmailAsync(to, subject, plainTextContent, htmlContent);
    }

    public async Task<bool> SendPasswordResetEmailAsync(string to, string resetToken)
    {
        var resetUrl = $"{_baseUrl}/reset-password?token={resetToken}";
        
        var subject = "Reset Your Password";
        var plainTextContent = $"Reset your password by clicking this link: {resetUrl}";
        var htmlContent = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <h2>Password Reset</h2>
                <p>You requested a password reset. Click the button below to create a new password:</p>
                <p style='margin: 20px 0;'>
                    <a href='{resetUrl}' style='background-color: #2196F3; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>
                        Reset Password
                    </a>
                </p>
                <p>If the button doesn't work, you can copy and paste this link into your browser:</p>
                <p>{resetUrl}</p>
                <p>This link will expire in 1 hour.</p>
                <p>If you didn't request a password reset, please ignore this email.</p>
            </div>
        ";
        
        return await SendEmailAsync(to, subject, plainTextContent, htmlContent);
    }

    public async Task<bool> SendOtpEmailAsync(string to, string otp)
    {
        var subject = "Your One-Time Password";
        var plainTextContent = $"Your verification code is: {otp}. This code will expire in 5 minutes.";
        var htmlContent = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <h2>Your Verification Code</h2>
                <p>Use the following code to complete your verification:</p>
                <div style='background-color: #f0f0f0; padding: 15px; font-size: 24px; text-align: center; letter-spacing: 5px; font-weight: bold; margin: 20px 0;'>
                    {otp}
                </div>
                <p>This code will expire in 5 minutes.</p>
                <p>If you didn't request this code, please ignore this email.</p>
            </div>
        ";
        
        return await SendEmailAsync(to, subject, plainTextContent, htmlContent);
    }

    private string GetUnsubscribeFooter(int userId, string email)
    {
        var unsubscribeToken = _tokenService.GenerateUnsubscribeToken(userId, email);
        var unsubscribeUrl = $"{_baseUrl}/api/unsubscribe/{unsubscribeToken}";
        var preferencesUrl = $"{_baseUrl}/api/unsubscribe/{unsubscribeToken}/preferences";
        
        return $@"
            <div style='margin-top: 40px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #999; text-align: center;'>
                <p>You're receiving this email because you have an account with us.</p>
                <p>
                    <a href='{preferencesUrl}' style='color: #666; text-decoration: underline;'>Manage email preferences</a> | 
                    <a href='{unsubscribeUrl}' style='color: #666; text-decoration: underline;'>Unsubscribe from marketing emails</a>
                </p>
            </div>
        ";
    }

    public async Task<bool> SendAccountCreatedEmailAsync(string to, string firstName)
    {
        // Note: This is a transactional email, so we don't add unsubscribe link
        var subject = "Welcome to Our App! 🎉";
        var plainTextContent = $"Hi {firstName}, your account has been successfully created. Welcome aboard!";
        var htmlContent = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <h2 style='color: #4CAF50;'>Welcome to Our App! 🎉</h2>
                <p>Hi {firstName},</p>
                <p>Your account has been successfully created. We're excited to have you on board!</p>
                <h3>What's Next?</h3>
                <ul>
                    <li>Complete your profile</li>
                    <li>Explore our features</li>
                    <li>Connect with others</li>
                </ul>
                <p style='margin: 20px 0;'>
                    <a href='{_baseUrl}/profile' style='background-color: #4CAF50; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>
                        Get Started
                    </a>
                </p>
                <p>If you have any questions, feel free to reach out to our support team.</p>
                <p>Best regards,<br>The Team</p>
            </div>
        ";
        
        return await SendEmailAsync(to, subject, plainTextContent, htmlContent);
    }

    public async Task<bool> SendAccountDeletedEmailAsync(string to, string firstName)
    {
        var subject = "Account Deletion Confirmation";
        var plainTextContent = $"Hi {firstName}, your account has been permanently deleted as requested.";
        var htmlContent = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <h2>Account Deletion Confirmation</h2>
                <p>Hi {firstName},</p>
                <p>This email confirms that your account has been permanently deleted as requested.</p>
                <p><strong>What this means:</strong></p>
                <ul>
                    <li>All your personal data has been removed</li>
                    <li>Your subscriptions have been cancelled</li>
                    <li>You will no longer receive emails from us</li>
                </ul>
                <p>We're sorry to see you go. If you change your mind, you can always create a new account.</p>
                <p>If you didn't request this deletion, please contact our support team immediately.</p>
                <p>Best regards,<br>The Team</p>
            </div>
        ";
        
        return await SendEmailAsync(to, subject, plainTextContent, htmlContent);
    }

    public async Task<bool> SendSubscriptionRenewedEmailAsync(string to, string firstName, string productName, DateTime expiryDate)
    {
        var subject = "Subscription Renewed Successfully ✅";
        var plainTextContent = $"Hi {firstName}, your {productName} subscription has been renewed until {expiryDate:MMMM dd, yyyy}.";
        var htmlContent = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <h2 style='color: #4CAF50;'>Subscription Renewed Successfully ✅</h2>
                <p>Hi {firstName},</p>
                <p>Great news! Your <strong>{productName}</strong> subscription has been successfully renewed.</p>
                <div style='background-color: #f0f0f0; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <p style='margin: 5px 0;'><strong>Product:</strong> {productName}</p>
                    <p style='margin: 5px 0;'><strong>Renewal Date:</strong> {DateTime.UtcNow:MMMM dd, yyyy}</p>
                    <p style='margin: 5px 0;'><strong>Next Billing Date:</strong> {expiryDate:MMMM dd, yyyy}</p>
                </div>
                <p>You can continue enjoying all premium features without interruption.</p>
                <p style='margin: 20px 0;'>
                    <a href='{_baseUrl}/subscription' style='background-color: #4CAF50; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>
                        View Subscription
                    </a>
                </p>
                <p>Thank you for being a valued subscriber!</p>
                <p>Best regards,<br>The Team</p>
            </div>
        ";
        
        return await SendEmailAsync(to, subject, plainTextContent, htmlContent);
    }

    public async Task<bool> SendPaymentFailedEmailAsync(string to, string firstName, string productName, string reason)
    {
        var subject = "Payment Failed - Action Required ⚠️";
        var plainTextContent = $"Hi {firstName}, your payment for {productName} failed. Please update your payment method.";
        var htmlContent = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <h2 style='color: #f44336;'>Payment Failed - Action Required ⚠️</h2>
                <p>Hi {firstName},</p>
                <p>We were unable to process your payment for <strong>{productName}</strong>.</p>
                <div style='background-color: #fff3cd; border-left: 4px solid #f44336; padding: 15px; margin: 20px 0;'>
                    <p style='margin: 5px 0;'><strong>Reason:</strong> {reason}</p>
                    <p style='margin: 5px 0;'><strong>Product:</strong> {productName}</p>
                </div>
                <p><strong>What you need to do:</strong></p>
                <ol>
                    <li>Update your payment method</li>
                    <li>Ensure sufficient funds are available</li>
                    <li>Try the payment again</li>
                </ol>
                <p style='margin: 20px 0;'>
                    <a href='{_baseUrl}/subscription/payment' style='background-color: #f44336; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>
                        Update Payment Method
                    </a>
                </p>
                <p>If you don't update your payment method, your subscription may be cancelled.</p>
                <p>Need help? Contact our support team.</p>
                <p>Best regards,<br>The Team</p>
            </div>
        ";
        
        return await SendEmailAsync(to, subject, plainTextContent, htmlContent);
    }

    public async Task<bool> SendProfileUpdatedEmailAsync(string to, string firstName)
    {
        var subject = "Profile Updated Successfully";
        var plainTextContent = $"Hi {firstName}, your profile has been updated successfully.";
        var htmlContent = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <h2>Profile Updated Successfully</h2>
                <p>Hi {firstName},</p>
                <p>This email confirms that your profile has been updated successfully.</p>
                <p><strong>What was updated:</strong></p>
                <ul>
                    <li>Profile information</li>
                    <li>Last updated: {DateTime.UtcNow:MMMM dd, yyyy HH:mm} UTC</li>
                </ul>
                <p style='margin: 20px 0;'>
                    <a href='{_baseUrl}/profile' style='background-color: #2196F3; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>
                        View Profile
                    </a>
                </p>
                <p>If you didn't make these changes, please contact our support team immediately.</p>
                <p>Best regards,<br>The Team</p>
            </div>
        ";
        
        return await SendEmailAsync(to, subject, plainTextContent, htmlContent);
    }
}
