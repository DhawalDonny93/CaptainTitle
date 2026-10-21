using System.Text.Json.Serialization;

namespace AppBackendBasic.Core.DTOs;

// Event Tracking DTOs
public class TrackEventRequestDto
{
    [JsonPropertyName("eventName")]
    public string EventName { get; set; } = string.Empty;
    
    [JsonPropertyName("properties")]
    public Dictionary<string, object>? Properties { get; set; }
    
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;
    
    [JsonPropertyName("timestamp")]
    public DateTime? Timestamp { get; set; }
}

public class TrackEventResponseDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    
    [JsonPropertyName("eventId")]
    public string EventId { get; set; } = string.Empty;
    
    [JsonPropertyName("mixpanelTracked")]
    public bool MixpanelTracked { get; set; }
}

// User Properties DTOs
public class UserPropertiesRequestDto
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;
    
    [JsonPropertyName("properties")]
    public Dictionary<string, object> Properties { get; set; } = new();
}

public class UserPropertiesResponseDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    
    [JsonPropertyName("mixpanelTracked")]
    public bool MixpanelTracked { get; set; }
}

public class IncrementPropertyRequestDto
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;
    
    [JsonPropertyName("property")]
    public string Property { get; set; } = string.Empty;
    
    [JsonPropertyName("value")]
    public double Value { get; set; } = 1;
}

public class IncrementPropertyResponseDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    
    [JsonPropertyName("property")]
    public string Property { get; set; } = string.Empty;
    
    [JsonPropertyName("newValue")]
    public double NewValue { get; set; }
}

// Revenue Tracking DTOs
public class TrackRevenueRequestDto
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;
    
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
    
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "USD";
    
    [JsonPropertyName("productId")]
    public string ProductId { get; set; } = string.Empty;
    
    [JsonPropertyName("transactionId")]
    public string TransactionId { get; set; } = string.Empty;
    
    [JsonPropertyName("properties")]
    public Dictionary<string, object>? Properties { get; set; }
}

public class TrackRevenueResponseDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    
    [JsonPropertyName("revenueId")]
    public string RevenueId { get; set; } = string.Empty;
    
    [JsonPropertyName("mixpanelTracked")]
    public bool MixpanelTracked { get; set; }
}

// User Analytics DTOs
public class UserAnalyticsDto
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;
    
    [JsonPropertyName("properties")]
    public Dictionary<string, object> Properties { get; set; } = new();
    
    [JsonPropertyName("events")]
    public UserEventsDto Events { get; set; } = new();
    
    [JsonPropertyName("engagement")]
    public UserEngagementDto Engagement { get; set; } = new();
    
    [JsonPropertyName("revenue")]
    public UserRevenueDto Revenue { get; set; } = new();
}

public class UserEventsDto
{
    [JsonPropertyName("total_events")]
    public int TotalEvents { get; set; }
    
    [JsonPropertyName("unique_events")]
    public int UniqueEvents { get; set; }
    
    [JsonPropertyName("last_event")]
    public string? LastEvent { get; set; }
    
    [JsonPropertyName("last_event_time")]
    public DateTime? LastEventTime { get; set; }
}

public class UserEngagementDto
{
    [JsonPropertyName("sessions")]
    public int Sessions { get; set; }
    
    [JsonPropertyName("avg_session_duration")]
    public int AvgSessionDuration { get; set; }
    
    [JsonPropertyName("days_active")]
    public int DaysActive { get; set; }
}

public class UserRevenueDto
{
    [JsonPropertyName("total_revenue")]
    public decimal TotalRevenue { get; set; }
    
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "USD";
    
    [JsonPropertyName("transactions")]
    public int Transactions { get; set; }
    
    [JsonPropertyName("avg_transaction_value")]
    public decimal AvgTransactionValue { get; set; }
}

// Dashboard DTOs
public class DashboardAnalyticsDto
{
    [JsonPropertyName("period")]
    public PeriodDto Period { get; set; } = new();
    
    [JsonPropertyName("users")]
    public DashboardUsersDto Users { get; set; } = new();
    
    [JsonPropertyName("engagement")]
    public DashboardEngagementDto Engagement { get; set; } = new();
    
    [JsonPropertyName("revenue")]
    public DashboardRevenueDto Revenue { get; set; } = new();
    
    [JsonPropertyName("subscriptions")]
    public DashboardSubscriptionsDto Subscriptions { get; set; } = new();
    
    [JsonPropertyName("topEvents")]
    public List<TopEventDto> TopEvents { get; set; } = new();
}

public class PeriodDto
{
    [JsonPropertyName("startDate")]
    public DateTime StartDate { get; set; }
    
    [JsonPropertyName("endDate")]
    public DateTime EndDate { get; set; }
}

public class DashboardUsersDto
{
    [JsonPropertyName("total")]
    public int Total { get; set; }
    
    [JsonPropertyName("new")]
    public int New { get; set; }
    
    [JsonPropertyName("active")]
    public int Active { get; set; }
    
    [JsonPropertyName("churned")]
    public int Churned { get; set; }
}

public class DashboardEngagementDto
{
    [JsonPropertyName("total_sessions")]
    public int TotalSessions { get; set; }
    
    [JsonPropertyName("avg_session_duration")]
    public int AvgSessionDuration { get; set; }
    
    [JsonPropertyName("total_events")]
    public int TotalEvents { get; set; }
    
    [JsonPropertyName("dau")]
    public int Dau { get; set; }
    
    [JsonPropertyName("mau")]
    public int Mau { get; set; }
    
    [JsonPropertyName("wau")]
    public int Wau { get; set; }
}

public class DashboardRevenueDto
{
    [JsonPropertyName("total")]
    public decimal Total { get; set; }
    
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "USD";
    
    [JsonPropertyName("transactions")]
    public int Transactions { get; set; }
    
    [JsonPropertyName("avg_transaction_value")]
    public decimal AvgTransactionValue { get; set; }
    
    [JsonPropertyName("mrr")]
    public decimal Mrr { get; set; }
    
    [JsonPropertyName("arr")]
    public decimal Arr { get; set; }
}

public class DashboardSubscriptionsDto
{
    [JsonPropertyName("active")]
    public int Active { get; set; }
    
    [JsonPropertyName("new")]
    public int New { get; set; }
    
    [JsonPropertyName("cancelled")]
    public int Cancelled { get; set; }
    
    [JsonPropertyName("conversion_rate")]
    public decimal ConversionRate { get; set; }
}

public class TopEventDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("count")]
    public int Count { get; set; }
}

// Funnel DTOs
public class CreateFunnelRequestDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("steps")]
    public List<FunnelStepDto> Steps { get; set; } = new();
    
    [JsonPropertyName("filters")]
    public Dictionary<string, object>? Filters { get; set; }
}

public class FunnelStepDto
{
    [JsonPropertyName("event")]
    public string Event { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class CreateFunnelResponseDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("funnelId")]
    public string FunnelId { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("steps")]
    public int Steps { get; set; }
}

public class FunnelResultsDto
{
    [JsonPropertyName("funnelId")]
    public string FunnelId { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("period")]
    public PeriodDto Period { get; set; } = new();
    
    [JsonPropertyName("steps")]
    public List<FunnelStepResultDto> Steps { get; set; } = new();
    
    [JsonPropertyName("overall_conversion")]
    public decimal OverallConversion { get; set; }
    
    [JsonPropertyName("total_users")]
    public int TotalUsers { get; set; }
}

public class FunnelStepResultDto
{
    [JsonPropertyName("step")]
    public int Step { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("event")]
    public string Event { get; set; } = string.Empty;
    
    [JsonPropertyName("users")]
    public int Users { get; set; }
    
    [JsonPropertyName("conversion")]
    public decimal Conversion { get; set; }
    
    [JsonPropertyName("dropoff")]
    public decimal Dropoff { get; set; }
}

// Export DTOs
public class ExportRequestDto
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // "events", "users", "revenue"
    
    [JsonPropertyName("format")]
    public string Format { get; set; } = "csv"; // "csv", "json"
    
    [JsonPropertyName("startDate")]
    public DateTime StartDate { get; set; }
    
    [JsonPropertyName("endDate")]
    public DateTime EndDate { get; set; }
    
    [JsonPropertyName("filters")]
    public Dictionary<string, object>? Filters { get; set; }
}

public class ExportResponseDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("exportId")]
    public string ExportId { get; set; } = string.Empty;
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = "processing"; // "processing", "completed", "failed"
    
    [JsonPropertyName("downloadUrl")]
    public string? DownloadUrl { get; set; }
    
    [JsonPropertyName("estimatedTime")]
    public int? EstimatedTime { get; set; }
    
    [JsonPropertyName("expiresAt")]
    public DateTime? ExpiresAt { get; set; }
    
    [JsonPropertyName("fileSize")]
    public long? FileSize { get; set; }
}

// Cohort DTOs
public class CreateCohortRequestDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("criteria")]
    public CohortCriteriaDto Criteria { get; set; } = new();
}

public class CohortCriteriaDto
{
    [JsonPropertyName("event")]
    public string Event { get; set; } = string.Empty;
    
    [JsonPropertyName("startDate")]
    public DateTime StartDate { get; set; }
    
    [JsonPropertyName("endDate")]
    public DateTime EndDate { get; set; }
    
    [JsonPropertyName("properties")]
    public Dictionary<string, object>? Properties { get; set; }
}

public class CreateCohortResponseDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("cohortId")]
    public string CohortId { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("userCount")]
    public int UserCount { get; set; }
    
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}
