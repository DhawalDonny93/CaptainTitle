using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AppBackendBasic.Core.Interfaces;
using System.Text.Json;

namespace AppBackendBasic.Services.Authentication;

public class TwilioSmsService : ISmsService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TwilioSmsService> _logger;
    private readonly string _accountSid;
    private readonly string _authToken;
    private readonly string _fromPhoneNumber;

    public TwilioSmsService(HttpClient httpClient, ILogger<TwilioSmsService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _accountSid = configuration["Twilio:AccountSid"] ?? throw new ArgumentNullException("Twilio:AccountSid");
        _authToken = configuration["Twilio:AuthToken"] ?? throw new ArgumentNullException("Twilio:AuthToken");
        _fromPhoneNumber = configuration["Twilio:FromPhoneNumber"] ?? throw new ArgumentNullException("Twilio:FromPhoneNumber");

        // Set up HTTP client for Twilio API
        var credentials = System.Text.Encoding.ASCII.GetBytes($"{_accountSid}:{_authToken}");
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(credentials));
    }

    public async Task<bool> SendSmsAsync(string phoneNumber, string message)
    {
        try
        {
            var requestBody = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("From", _fromPhoneNumber),
                new KeyValuePair<string, string>("To", phoneNumber),
                new KeyValuePair<string, string>("Body", message)
            });

            var response = await _httpClient.PostAsync(
                $"https://api.twilio.com/2010-04-01/Accounts/{_accountSid}/Messages.json",
                requestBody);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("SMS sent successfully to {PhoneNumber}", phoneNumber);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send SMS to {PhoneNumber}. Status: {StatusCode}, Error: {Error}", 
                    phoneNumber, response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS to {PhoneNumber}", phoneNumber);
            return false;
        }
    }

    public async Task<bool> SendOtpAsync(string phoneNumber, string otp)
    {
        var message = $"Your verification code is: {otp}. This code will expire in 5 minutes.";
        return await SendSmsAsync(phoneNumber, message);
    }
}
