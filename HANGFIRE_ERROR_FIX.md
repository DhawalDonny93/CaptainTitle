# Hangfire TypeLoadException Fix

## 🔴 Error

```
System.TypeLoadException: Could not load type 'AppBackendBasic.Core.Interfaces.IVisibilityCheckService' 
from assembly 'AppBackendBasic.Core, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'.
```

**Error Location:** Hangfire dashboard when trying to trigger recurring jobs

---

## 🔍 Root Cause

Hangfire has an **old recurring job** stored in the database that references `IVisibilityCheckService`, a service that no longer exists in the codebase. This service was refactored/merged into `IPromptTrackingService`.

When Hangfire tries to deserialize and execute this old job, it fails because the type no longer exists.

---

## ✅ Solution

### **Option 1: Remove Old Job via SQL (Recommended)**

Run the cleanup script to remove the old job from Hangfire's database:

**File:** `@/Users/dhawal/Documents/GitHub/AISEO/AISEODevelop/AISEO/HANGFIRE_OLD_JOB_CLEANUP.sql`

```sql
-- Remove old recurring job entries for IVisibilityCheckService
DELETE FROM hangfire.set
WHERE key LIKE 'recurring-jobs%' 
AND value::json->>'Job'::text LIKE '%IVisibilityCheckService%';
```

### **Option 2: Remove via Hangfire Dashboard**

1. Navigate to `/hangfire` in your browser
2. Go to "Recurring Jobs" tab
3. Find any job referencing `IVisibilityCheckService`
4. Click "Delete" button

### **Option 3: Remove via Code**

Add this to your `Program.cs` startup (temporary, remove after first run):

```csharp
// Remove old job if it exists
RecurringJob.RemoveIfExists("check-visibility-old");
RecurringJob.RemoveIfExists("visibility-check");
```

---

## 🔄 Current Job Configuration

The **correct job** is now configured as:

```csharp
RecurringJob.AddOrUpdate<IPromptTrackingService>(
    "ai-visibility-citation-check",
    service => service.ProcessAllActivePromptsAsync(),
    Cron.Daily(1),
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
```

**Job ID:** `ai-visibility-citation-check`  
**Service:** `IPromptTrackingService` (not IVisibilityCheckService)  
**Method:** `ProcessAllActivePromptsAsync()`

---

## 🧪 Verification Steps

1. **Run the cleanup SQL script**
2. **Restart the application**
   ```bash
   dotnet build
   dotnet run --project AppBackendBasic.API
   ```
3. **Check Hangfire dashboard** at `/hangfire`
4. **Verify recurring jobs** - Should see `ai-visibility-citation-check` without errors
5. **Trigger the job manually** to test

---

## 📋 What This Job Does

The `ai-visibility-citation-check` job:
- Checks all active prompts for visibility across AI platforms
- Extracts citations from AI responses
- Updates `PromptVisibilityMetrics`, `Citations`, `PromptPlatforms` tables
- Runs daily at 1 AM UTC

**Documentation:** See `BACKGROUND_JOBS_DOCUMENTATION.md` section 1

---

## 🚨 Prevention

To prevent this in the future:
1. When refactoring/removing services, check Hangfire for jobs using them
2. Remove old recurring jobs before deploying
3. Use `RecurringJob.RemoveIfExists()` in migration code
4. Monitor Hangfire logs for TypeLoadException errors

---

## ✅ Summary

**Problem:** Old Hangfire job references deleted `IVisibilityCheckService`  
**Solution:** Remove old job from Hangfire database using SQL script  
**Result:** Application runs without TypeLoadException errors  
**New Job:** `ai-visibility-citation-check` using `IPromptTrackingService`
