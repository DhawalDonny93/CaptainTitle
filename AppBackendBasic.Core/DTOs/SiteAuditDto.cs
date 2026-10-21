using System;
using System.Collections.Generic;

namespace AppBackendBasic.Core.DTOs
{
    public class StartAuditRequest
    {
        public int ProjectId { get; set; }
        public string Url { get; set; } = string.Empty;
    }

    public class AuditCheckResult
    {
        public string CheckName { get; set; } = string.Empty;
        public string CheckType { get; set; } = string.Empty;
        public string Status { get; set; } = "Info";
        public int Score { get; set; }
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, object>? Details { get; set; }
        public string? Recommendation { get; set; }
        public bool AutoFixAvailable { get; set; }
        public int Priority { get; set; }
    }

    public class AuditSummary
    {
        public int RunId { get; set; }
        public string Url { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int OverallScore { get; set; }
        public int TotalChecks { get; set; }
        public int PassedChecks { get; set; }
        public int FailedChecks { get; set; }
        public int WarningChecks { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public List<CategoryScore> CategoryScores { get; set; } = new();
        public List<AuditCheckResult> CriticalIssues { get; set; } = new();
        public List<AuditCheckResult> Warnings { get; set; } = new();
    }

    public class CategoryScore
    {
        public string Category { get; set; } = string.Empty;
        public int Score { get; set; }
        public int MaxScore { get; set; } = 100;
        public string Status { get; set; } = string.Empty;
    }

    public class AuditTrend
    {
        public DateTime Date { get; set; }
        public int OverallScore { get; set; }
        public int LlmsTxtScore { get; set; }
        public int SchemaScore { get; set; }
        public int DomClarityScore { get; set; }
        public int ContentScore { get; set; }
        public int TechnicalScore { get; set; }
    }

    public class DateRangeDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
