// using System;
// using AppBackendBasic.Core.Interfaces;
// using AppBackendBasic.Infrastructure.Data;
// using Microsoft.Extensions.Configuration;
// using Microsoft.Extensions.Logging;

// namespace AppBackendBasic.Services.Services;

// public class ApnsService: IApnsService
// {
// private readonly IConfiguration _configuration;
//     private readonly ILogger<ApnsService> _logger;
//     private readonly ApplicationDbContext _context;
//     private readonly ApnsClient _apnsClient;

//     public ApnsService(IConfiguration configuration, ILogger<ApnsService> logger, ApplicationDbContext context)
//     {
//         _configuration = configuration;
//         _logger = logger;
//         _context = context;
        
//         // Initialize APNs client
//         var p8FileContents = File.ReadAllText(_configuration["APNs:P8FilePath"]);
//         var options = new ApnsJwtOptions
//         {
//             BundleId = _configuration["APNs:BundleId"],
//             TeamId = _configuration["APNs:TeamId"],
//             KeyId = _configuration["APNs:KeyId"],
//             P8PrivateKey = p8FileContents
//         };
        
//         var env = _configuration["APNs:Environment"] == "Production" 
//             ? ApnsEnvironment.Production 
//             : ApnsEnvironment.Development;
            
//         _apnsClient = ApnsClient.CreateUsingJwt(new HttpClient(), options, env);
//     }

//     public async Task<bool> SendNotificationAsync(string deviceToken, string title, string body, Dictionary<string, object>? data = null)
//     {
//         try
//         {
//             var push = new ApplePush(ApplePushType.Alert)
//                 .AddAlert(title, body)
//                 .AddSound("default")
//                 .AddBadge(1);

//             if (data != null)
//             {
//                 foreach (var kvp in data)
//                 {
//                     push.AddCustomProperty(kvp.Key, kvp.Value);
//                 }
//             }

//             var response = await _apnsClient.SendAsync(push, deviceToken);
            
//             if (response.IsSuccessful)
//             {
//                 _logger.LogInformation("Push notification sent successfully to {DeviceToken}", deviceToken);
//                 return true;
//             }
//             else
//             {
//                 _logger.LogWarning("Failed to send push notification: {Reason}", response.Reason);
                
//                 // Handle invalid tokens
//                 if (response.Reason == ApnsResponseReason.BadDeviceToken || 
//                     response.Reason == ApnsResponseReason.Unregistered)
//                 {
//                     await DeactivateDeviceTokenAsync(deviceToken);
//                 }
                
//                 return false;
//             }
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "Error sending push notification");
//             return false;
//         }
//     }

//     public async Task<bool> SendNotificationToUserAsync(int userId, string title, string body, Dictionary<string, object>? data = null)
//     {
//         var deviceTokens = await _context.DeviceTokens
//             .Where(dt => dt.UserId == userId && dt.IsActive && dt.Platform == "iOS")
//             .Select(dt => dt.DeviceToken)
//             .ToListAsync();

//         if (!deviceTokens.Any())
//         {
//             _logger.LogWarning("No active device tokens found for user {UserId}", userId);
//             return false;
//         }

//         var results = new List<bool>();
//         foreach (var token in deviceTokens)
//         {
//             var result = await SendNotificationAsync(token, title, body, data);
//             results.Add(result);
//         }

//         return results.Any(r => r);
//     }

//     public async Task<bool> SendBulkNotificationAsync(List<string> deviceTokens, string title, string body)
//     {
//         var tasks = deviceTokens.Select(token => SendNotificationAsync(token, title, body));
//         var results = await Task.WhenAll(tasks);
//         return results.Any(r => r);
//     }

//     private async Task DeactivateDeviceTokenAsync(string deviceToken)
//     {
//         var token = await _context.DeviceTokens.FirstOrDefaultAsync(dt => dt.DeviceToken == deviceToken);
//         if (token != null)
//         {
//             token.IsActive = false;
//             token.UpdatedAt = DateTime.UtcNow;
//             await _context.SaveChangesAsync();
//         }
//     }
// }
