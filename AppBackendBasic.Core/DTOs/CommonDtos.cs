namespace AppBackendBasic.Core.DTOs;

/// <summary>Shared by the admin monitoring DTOs (previously defined inside SEO-specific DTO files).</summary>
public class TrendPoint
{
    public DateTime Date { get; set; }
    public decimal Value { get; set; }
}

public class DateRangeDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}
