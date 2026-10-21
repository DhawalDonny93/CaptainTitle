using System;

namespace AppBackendBasic.Core.Interfaces;

public interface IApnsService
{
    Task<bool> SendNotificationAsync(string deviceToken, string title, string body, Dictionary<string, object>? data = null);
    Task<bool> SendNotificationToUserAsync(int userId, string title, string body, Dictionary<string, object>? data = null);
    Task<bool> SendBulkNotificationAsync(List<string> deviceTokens, string title, string body);
}
