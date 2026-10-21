# Hangfire Job Pattern Fix

## 🔴 Problem

The `ai-visibility-citation-check` job was configured differently from all other background jobs:

**Incorrect Pattern (Interface):**
```csharp
RecurringJob.AddOrUpdate<IPromptTrackingService>(
    "ai-visibility-citation-check",
    service => service.ProcessAllActivePromptsAsync(),
    Cron.Daily(1),
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
```

**Correct Pattern (Concrete Job Class):**
```csharp
RecurringJob.AddOrUpdate<AppBackendBasic.Infrastructure.BackgroundJobs.DashboardMetricsUpdateJob>(
    "update-dashboard-metrics",
    job => job.ExecuteAsync(),
    "0 */6 * * *",
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
```

---

## 🔍 Root Cause

**Architecture Pattern:**
- **Interfaces** → `AppBackendBasic.Core.Interfaces` (e.g., `IPromptTrackingService`)
- **Service Implementations** → `AppBackendBasic.Infrastructure.Services` (e.g., `PromptTrackingService`)
- **Background Job Wrappers** → `AppBackendBasic.Infrastructure.BackgroundJobs` (e.g., `DashboardMetricsUpdateJob`)

**The Issue:**
The job was directly referencing the **interface** (`IPromptTrackingService`) instead of using a **wrapper job class** in the `BackgroundJobs` folder like all other jobs.

---

## ✅ Solution

### **Created Wrapper Job Class**

**File:** `@/Users/dhawal/Documents/GitHub/AISEO/AISEODevelop/AISEO/AppBackendBasic.Infrastructure/BackgroundJobs/PromptVisibilityCheckJob.cs`

```csharp
using AppBackendBasic.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Infrastructure.BackgroundJobs
{
    public class PromptVisibilityCheckJob
    {
        private readonly IPromptTrackingService _promptTrackingService;
        private readonly ILogger<PromptVisibilityCheckJob> _logger;

        public PromptVisibilityCheckJob(
            IPromptTrackingService promptTrackingService,
            ILogger<PromptVisibilityCheckJob> logger)
        {
            _promptTrackingService = promptTrackingService;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            var startTime = DateTime.UtcNow;
            _logger.LogInformation("Starting Prompt Visibility Check Job at {Time}", startTime);

            try
            {
                await _promptTrackingService.ProcessAllActivePromptsAsync();

                var endTime = DateTime.UtcNow;
                var duration = (endTime - startTime).TotalSeconds;

                _logger.LogInformation(
                    "Prompt Visibility Check Job completed. Duration: {Duration}s",
                    duration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Prompt Visibility Check Job failed");
                throw;
            }
        }
    }
}
```

### **Updated Program.cs**

**File:** `@/Users/dhawal/Documents/GitHub/AISEO/AISEODevelop/AISEO/AppBackendBasic.API/Program.cs`

**Before:**
```csharp
RecurringJob.AddOrUpdate<IPromptTrackingService>(
    "ai-visibility-citation-check",
    service => service.ProcessAllActivePromptsAsync(),
    Cron.Daily(1),
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
```

**After:**
```csharp
RecurringJob.AddOrUpdate<AppBackendBasic.Infrastructure.BackgroundJobs.PromptVisibilityCheckJob>(
    "ai-visibility-citation-check",
    job => job.ExecuteAsync(),
    Cron.Daily(1),
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
```

---

## 📊 Consistent Pattern Across All Jobs

Now all 7 background jobs follow the same pattern:

| Job | Class Location | Pattern |
|-----|---------------|---------|
| AI Visibility Check | `Infrastructure.BackgroundJobs.PromptVisibilityCheckJob` | ✅ Wrapper class |
| Dashboard Metrics | `Infrastructure.BackgroundJobs.DashboardMetricsUpdateJob` | ✅ Wrapper class |
| Visibility Trends | `Infrastructure.BackgroundJobs.VisibilityTrendAggregationJob` | ✅ Wrapper class |
| Citation Aggregation | `Infrastructure.BackgroundJobs.CitationAggregationJob` | ✅ Wrapper class |
| Opportunity Detection | `Infrastructure.BackgroundJobs.OpportunityDetectionJob` | ✅ Wrapper class |
| Prompt Research | `Infrastructure.BackgroundJobs.PromptResearchGenerationJob` | ✅ Wrapper class |
| Site Crawl | `Infrastructure.BackgroundJobs.SiteCrawlJob` | ✅ Wrapper class |

---

## 🔧 Next Steps

1. **Rebuild the application**
   ```bash
   dotnet build
   ```

2. **Restart the application**
   ```bash
   dotnet run --project AppBackendBasic.API
   ```

3. **Verify in Hangfire dashboard** (`/hangfire`)
   - Check that `ai-visibility-citation-check` job is registered
   - Trigger it manually to test
   - Should run without TypeLoadException errors

4. **Monitor logs** for successful execution

---

## 🎯 Why This Pattern?

**Benefits of Wrapper Job Classes:**
1. **Consistent structure** - All jobs follow the same pattern
2. **Better logging** - Job-specific logger with job name
3. **Error handling** - Centralized try-catch in wrapper
4. **Dependency injection** - Hangfire can properly resolve dependencies
5. **Testability** - Easier to unit test wrapper classes
6. **Separation of concerns** - Job orchestration separate from business logic

**Architecture Flow:**
```
Hangfire
  ↓
PromptVisibilityCheckJob (Infrastructure.BackgroundJobs)
  ↓
IPromptTrackingService (Core.Interfaces)
  ↓
PromptTrackingService (Infrastructure.Services)
  ↓
Database
```

---

## ✅ Summary

**Problem:** Job used interface directly instead of wrapper class  
**Solution:** Created `PromptVisibilityCheckJob` wrapper in `Infrastructure.BackgroundJobs`  
**Result:** Consistent pattern across all background jobs, proper dependency resolution  
**Status:** Ready to test after rebuild
