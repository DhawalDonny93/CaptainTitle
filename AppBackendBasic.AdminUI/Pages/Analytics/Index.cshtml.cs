using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AppBackendBasic.AdminUI.Services;

namespace AppBackendBasic.AdminUI.Pages.Analytics;

public class IndexModel : PageModel
{
    private readonly AuthService _authService;
    private readonly ApiClient _apiClient;
    private readonly ILogger<IndexModel> _logger;

    public AnalyticsData Analytics { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string Period { get; set; } = "month";

    public IndexModel(
        AuthService authService,
        ApiClient apiClient,
        ILogger<IndexModel> logger)
    {
        _authService = authService;
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!_authService.IsAuthenticated())
        {
            return RedirectToPage("/Login");
        }

        try
        {
            // Convert period to days
            int days = Period.ToLower() switch
            {
                "week" => 7,
                "month" => 30,
                "quarter" => 90,
                "year" => 365,
                _ => 30
            };

            // Fetch analytics from API
            var apiData = await _apiClient.GetAsync<AnalyticsApiResponse>($"/admin/analytics?period={days}");
            
            if (apiData != null && apiData.Success)
            {
                Analytics = MapApiResponseToAnalyticsData(apiData);
            }
            else
            {
                Analytics = new AnalyticsData();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading analytics");
            Analytics = new AnalyticsData();
        }

        return Page();
    }

    private AnalyticsData MapApiResponseToAnalyticsData(AnalyticsApiResponse apiResponse)
    {
        var userGrowth = apiResponse.UserGrowth;
        var engagement = apiResponse.Engagement;
        var subscriptions = apiResponse.Subscriptions;
        var notifications = apiResponse.Notifications;

        return new AnalyticsData
        {
            TotalUsers = userGrowth?.TotalUsers ?? 0,
            ActiveUsers = engagement?.ActiveSessions ?? 0,
            NewUsersToday = 0, // Not provided by API
            NewUsersThisMonth = userGrowth?.NewUsers ?? 0,
            TotalSubscriptions = subscriptions?.ActiveSubscriptions ?? 0,
            ActiveSubscriptions = subscriptions?.ActiveSubscriptions ?? 0,
            TotalRevenue = 0, // Not provided by API
            MonthlyRecurringRevenue = 0, // Not provided by API
            ChurnRate = 0, // Not provided by API
            AverageRevenuePerUser = 0, // Not provided by API
            UserGrowthData = new List<ChartDataPoint>(),
            SubscriptionData = new List<ChartDataPoint>(),
            RevenueData = new List<ChartDataPoint>(),
            TopCountries = new List<CountryData>(),
            DeviceStats = new DeviceStatistics()
        };
    }
}

public class AnalyticsData
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int NewUsersToday { get; set; }
    public int NewUsersThisMonth { get; set; }
    public int TotalSubscriptions { get; set; }
    public int ActiveSubscriptions { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal MonthlyRecurringRevenue { get; set; }
    public double ChurnRate { get; set; }
    public decimal AverageRevenuePerUser { get; set; }
    public List<ChartDataPoint> UserGrowthData { get; set; } = new();
    public List<ChartDataPoint> SubscriptionData { get; set; } = new();
    public List<ChartDataPoint> RevenueData { get; set; } = new();
    public List<CountryData> TopCountries { get; set; } = new();
    public DeviceStatistics DeviceStats { get; set; } = new();
}

public class ChartDataPoint
{
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
}

public class CountryData
{
    public string Country { get; set; } = string.Empty;
    public int Users { get; set; }
    public double Percentage { get; set; }
}

public class DeviceStatistics
{
    public double Desktop { get; set; }
    public double Mobile { get; set; }
    public double Tablet { get; set; }
}

public class AnalyticsApiResponse
{
    public bool Success { get; set; }
    public string Period { get; set; } = string.Empty;
    public UserGrowthData? UserGrowth { get; set; }
    public EngagementData? Engagement { get; set; }
    public SubscriptionData? Subscriptions { get; set; }
    public NotificationData? Notifications { get; set; }
}

public class UserGrowthData
{
    public int NewUsers { get; set; }
    public int TotalUsers { get; set; }
    public int Days { get; set; }
}

public class EngagementData
{
    public int ActiveSessions { get; set; }
    public int Days { get; set; }
}

public class SubscriptionData
{
    public int ActiveSubscriptions { get; set; }
    public int Days { get; set; }
}

public class NotificationData
{
    public int SentCount { get; set; }
    public int Days { get; set; }
}
