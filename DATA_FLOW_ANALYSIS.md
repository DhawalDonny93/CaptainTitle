# AISEO Platform - Data Flow & Feature Completeness Analysis
**Date:** March 29, 2026  
**Focus:** Backend Data Generation vs Frontend Expectations

---

## 🎯 EXECUTIVE SUMMARY

This document analyzes whether the backend is **actually generating and populating** the data that the frontend displays. Many frontend components have **fallback/mock data** or show **hardcoded values** when real data is missing.

### Critical Findings:
1. ✅ **Working:** Citations data flow (with recent fixes)
2. ⚠️ **Partially Working:** Dashboard metrics (returns empty if no data)
3. ❌ **Not Working:** Prompt research visibility scores (using mock calculations)
4. ❌ **Missing:** Competitive intelligence data generation
5. ❌ **Missing:** Real-time trend calculations

---

## 📊 COMPONENT-BY-COMPONENT ANALYSIS

### 1. **Dashboard Page** (`Dashboard.tsx`)

#### Frontend Expectations:
```typescript
interface DashboardOverview {
  overallVisibilityScore: number;      // Main metric
  totalPrompts: number;                // Count of prompts
  totalCitations: number;              // Count of citations
  totalOpportunities: number;          // Count of opportunities
  averagePosition: number;             // Avg position in AI responses
  trendDirection: string;              // "up", "down", "stable"
  lastUpdated: string;                 // Last update timestamp
  platformBreakdown: PlatformMetric[]; // Per-platform metrics
}
```

#### Backend Implementation:
**API Endpoint:** `GET /api/visibility/dashboard?projectId={id}`  
**Controller:** `VisibilityDashboardController.cs`  
**Service:** `VisibilityDashboardService.GetDashboardOverviewAsync()`

**Data Source:** `VisibilityDashboardMetrics` table

#### ⚠️ **ISSUE IDENTIFIED:**

**Lines 26-35 in VisibilityDashboardService.cs:**
```csharp
var latestMetrics = await _context.VisibilityDashboardMetrics
    .Where(m => m.ProjectId == projectId && m.Period == "daily")
    .OrderByDescending(m => m.MetricDate)
    .FirstOrDefaultAsync();

if (latestMetrics == null)
{
    // Return empty dashboard if no metrics exist
    return new DashboardOverview();
}
```

**Problem:** Returns **empty object** if no metrics exist in database.

**Frontend Fallback (Lines 133-150 in Dashboard.tsx):**
```typescript
const stats = [
  {
    title: 'AI Visibility',
    value: dashboardData?.overallVisibilityScore?.toString() || '87', // ❌ HARDCODED
    change: '+12%',  // ❌ HARDCODED
  },
  {
    title: 'Citations',
    value: dashboardData?.totalCitations?.toString() || '142', // ❌ HARDCODED
    change: '+23',   // ❌ HARDCODED
  },
  // ...
]
```

#### ✅ **SOLUTION REQUIRED:**

**Background Job:** `DashboardMetricsUpdateJob` (Already exists!)
- **Schedule:** Every 6 hours
- **Location:** `AppBackendBasic.Infrastructure/BackgroundJobs/DashboardMetricsUpdateJob.cs`

**Check if it's populating data:**
```sql
-- Run this to check if metrics are being generated
SELECT 
    "ProjectId",
    "MetricDate",
    "OverallVisibilityScore",
    "TotalPrompts",
    "TotalCitations",
    "TotalOpportunities",
    "CreatedAt"
FROM "VisibilityDashboardMetrics"
ORDER BY "CreatedAt" DESC
LIMIT 10;
```

**If empty, the job needs to be triggered manually or data needs to be seeded.**

---

### 2. **Prompt Research Page** (`PromptResearch.tsx`)

#### Frontend Expectations:
```typescript
interface PromptData {
  id: number;
  text: string;
  topic: string;
  visibilityRank: number;
  visibilityScore: number;        // 0-100 score
  visibilityChange: number;       // Change from previous
  averagePosition: number;        // Position in AI responses
  positionChange: number;         // Change from previous
  citationShare: number;          // Share of citations
  citationChange: number;         // Change from previous
}
```

#### Backend Implementation:
**API Endpoint:** `GET /api/prompts/research?topic={topic}&limit={limit}`  
**Service:** `PromptResearchService.GetResearchDataAsync()`

#### ❌ **CRITICAL ISSUE IDENTIFIED:**

**Lines 80-84 in PromptResearch.tsx:**
```typescript
prompts: group.prompts.map((p, index) => ({
  id: p.id,
  text: p.text,
  topic: group.topic,
  visibilityRank: index + 1,
  visibilityScore: Math.round((p.relevanceScore || 0) * 100),
  visibilityChange: Math.random() * 10 - 5,  // ❌ MOCK DATA!
  averagePosition: 1 + Math.random() * 3,     // ❌ MOCK DATA!
  positionChange: Math.random() * 0.5 - 0.25, // ❌ MOCK DATA!
  citationShare: Math.random() * 2,           // ❌ MOCK DATA!
  citationChange: Math.random() * 5 - 2.5     // ❌ MOCK DATA!
}))
```

**Problem:** Frontend is **generating random mock data** for:
- `visibilityChange`
- `averagePosition`
- `positionChange`
- `citationShare`
- `citationChange`

#### ✅ **SOLUTION REQUIRED:**

**Backend needs to return these fields from `PromptResearchData` table:**

**Database Schema Check:**
```sql
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name = 'PromptResearchDatas';
```

**Expected columns:**
- `VisibilityScore` ✅ (exists as `RelevanceScore`)
- `AveragePosition` ❌ **MISSING**
- `CitationCount` ❌ **MISSING**
- `PreviousVisibilityScore` ❌ **MISSING** (for calculating change)
- `PreviousPosition` ❌ **MISSING** (for calculating change)

**Required Database Migration:**
```sql
-- Migration: 003_Add_PromptResearch_Metrics.sql

BEGIN;

ALTER TABLE "PromptResearchDatas"
ADD COLUMN IF NOT EXISTS "AveragePosition" DOUBLE PRECISION DEFAULT 0,
ADD COLUMN IF NOT EXISTS "CitationCount" INTEGER DEFAULT 0,
ADD COLUMN IF NOT EXISTS "CitationShare" DOUBLE PRECISION DEFAULT 0,
ADD COLUMN IF NOT EXISTS "PreviousRelevanceScore" DOUBLE PRECISION,
ADD COLUMN IF NOT EXISTS "PreviousPosition" DOUBLE PRECISION,
ADD COLUMN IF NOT EXISTS "PreviousCitationCount" INTEGER,
ADD COLUMN IF NOT EXISTS "LastCalculatedAt" TIMESTAMP WITH TIME ZONE;

-- Add index for performance
CREATE INDEX IF NOT EXISTS "IX_PromptResearchDatas_Topic_RelevanceScore" 
ON "PromptResearchDatas"("Topic", "RelevanceScore" DESC);

COMMIT;
```

**Required Backend Service Update:**

**File:** `AppBackendBasic.Infrastructure/Services/PromptResearchService.cs`

**Add method to calculate metrics:**
```csharp
public async Task UpdatePromptMetricsAsync(int promptResearchId)
{
    var promptResearch = await _context.PromptResearchDatas
        .FindAsync(promptResearchId);
    
    if (promptResearch == null) return;
    
    // Get prompt tracking data for this prompt
    var promptTracking = await _context.PromptTrackings
        .Where(pt => pt.PromptText.ToLower().Contains(promptResearch.Text.ToLower()))
        .OrderByDescending(pt => pt.CreatedAt)
        .FirstOrDefaultAsync();
    
    if (promptTracking != null)
    {
        // Calculate average position from platform trackings
        var platformTrackings = await _context.PromptPlatformTrackings
            .Where(ppt => ppt.PromptTrackingId == promptTracking.Id)
            .ToListAsync();
        
        if (platformTrackings.Any())
        {
            promptResearch.AveragePosition = platformTrackings
                .Where(pt => pt.Position.HasValue)
                .Average(pt => pt.Position.Value);
        }
        
        // Get citation count
        var citationCount = await _context.Citations
            .Where(c => c.PromptText.ToLower().Contains(promptResearch.Text.ToLower()))
            .CountAsync();
        
        promptResearch.CitationCount = citationCount;
        
        // Calculate citation share (percentage of total citations for this topic)
        var totalTopicCitations = await _context.Citations
            .Join(_context.PromptResearchDatas,
                c => true,
                pr => pr.Topic == promptResearch.Topic,
                (c, pr) => c)
            .CountAsync();
        
        promptResearch.CitationShare = totalTopicCitations > 0 
            ? (double)citationCount / totalTopicCitations * 100 
            : 0;
        
        promptResearch.LastCalculatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
    }
}
```

**Add background job to update metrics:**

**File:** `AppBackendBasic.Infrastructure/BackgroundJobs/PromptResearchMetricsUpdateJob.cs` (NEW FILE)

```csharp
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Infrastructure.BackgroundJobs
{
    public class PromptResearchMetricsUpdateJob
    {
        private readonly ApplicationDbContext _context;
        private readonly IPromptResearchService _promptResearchService;
        private readonly ILogger<PromptResearchMetricsUpdateJob> _logger;

        public PromptResearchMetricsUpdateJob(
            ApplicationDbContext context,
            IPromptResearchService promptResearchService,
            ILogger<PromptResearchMetricsUpdateJob> logger)
        {
            _context = context;
            _promptResearchService = promptResearchService;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            _logger.LogInformation("Starting prompt research metrics update job");

            try
            {
                var allPromptResearch = await _context.PromptResearchDatas
                    .Where(pr => pr.IsActive)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} active prompt research items to update", 
                    allPromptResearch.Count);

                foreach (var promptResearch in allPromptResearch)
                {
                    try
                    {
                        // Store previous values for change calculation
                        promptResearch.PreviousRelevanceScore = promptResearch.RelevanceScore;
                        promptResearch.PreviousPosition = promptResearch.AveragePosition;
                        promptResearch.PreviousCitationCount = promptResearch.CitationCount;

                        // Update current metrics
                        await _promptResearchService.UpdatePromptMetricsAsync(promptResearch.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error updating metrics for prompt research {Id}", 
                            promptResearch.Id);
                    }
                }

                _logger.LogInformation("Completed prompt research metrics update job");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in prompt research metrics update job");
                throw;
            }
        }
    }
}
```

**Register job in Program.cs:**
```csharp
// Add after line 294
builder.Services.AddScoped<AppBackendBasic.Infrastructure.BackgroundJobs.PromptResearchMetricsUpdateJob>();

// Add recurring job after line 533
RecurringJob.AddOrUpdate<AppBackendBasic.Infrastructure.BackgroundJobs.PromptResearchMetricsUpdateJob>(
    "update-prompt-research-metrics",
    job => job.ExecuteAsync(),
    "0 */12 * * *", // Every 12 hours
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
```

---

### 3. **Sources Page** (`Sources.tsx`) - Citations

#### Status: ✅ **RECENTLY FIXED**

**API Endpoint:** `GET /api/citations/top-domains`  
**Service:** `CitationTrackingService.GetTopCitedDomainsAsync()`

**Recent Fix Applied:**
- Backend now returns correct DTO format (`domain`, `citationCount`, `averagePosition`)
- Frontend filters null domains
- Fallback to `Citations` table if `CitationAggregations` is empty

**Verification Query:**
```sql
-- Check if citations are being tracked
SELECT 
    "SourceDomain",
    COUNT(*) AS "CitationCount",
    AVG("Position") AS "AvgPosition"
FROM "Citations"
WHERE "ProjectId" = 1  -- Replace with actual project ID
GROUP BY "SourceDomain"
ORDER BY "CitationCount" DESC
LIMIT 10;
```

**If empty, citations need to be generated by:**
1. Running prompt tracking job
2. Manually triggering citation detection
3. Seeding test data using `SEED_test_citations_data.sql`

---

### 4. **Competitors Page** (`Competitors.tsx`)

#### Frontend Expectations:
```typescript
interface Competitor {
  id: number;
  domain: string;
  type: 'Direct' | 'SERP';
  visibilityScore: number;
  citationShare: number;
  averagePosition: number;
  trend: 'up' | 'down' | 'stable';
}
```

#### Backend Implementation:
**API Endpoint:** `GET /api/brandprofile/competitors?projectId={id}`  
**Service:** `BrandProfileService` (via `CompetitorService`)

#### ⚠️ **ISSUE IDENTIFIED:**

**Check if competitors are being tracked:**
```sql
-- Check CompetitiveIntelligence table
SELECT 
    "ProjectId",
    "CompetitorDomain",
    "CompetitorType",
    "VisibilityScore",
    "CitationShare",
    "AveragePosition",
    "CreatedAt"
FROM "CompetitiveIntelligence"
ORDER BY "CreatedAt" DESC
LIMIT 10;
```

**If empty, competitive data is NOT being generated.**

#### ✅ **SOLUTION REQUIRED:**

**The competitive intelligence data should be populated by:**

1. **Prompt Tracking Job** - When analyzing AI responses, detect competitor mentions
2. **Citation Tracking** - Track which competitors are being cited
3. **Manual Competitor Addition** - User adds competitors via UI

**Check if `OpportunityDetectionJob` is generating competitive data:**

**File:** `AppBackendBasic.Infrastructure/BackgroundJobs/OpportunityDetectionJob.cs`

**This job should:**
- Analyze prompt responses for competitor mentions
- Calculate competitor visibility scores
- Update `CompetitiveIntelligence` table

**Verification:**
```sql
-- Check if job is creating competitive intelligence
SELECT COUNT(*) FROM "CompetitiveIntelligence";

-- If 0, the job is not populating data
```

---

### 5. **Visibility Dashboard** (Multiple Pages)

#### Frontend Expectations:
- Trend charts showing visibility over time
- Platform-specific breakdowns
- Competitive comparisons
- Alert notifications

#### Backend Implementation:
**Service:** `VisibilityDashboardService`  
**Data Source:** `VisibilityDashboardMetrics` table

#### ⚠️ **ISSUE IDENTIFIED:**

**The `DashboardMetricsUpdateJob` must be running to populate:**
- `VisibilityDashboardMetrics` table
- `VisibilityAlerts` table
- `VisibilityGoals` table

**Verification:**
```sql
-- Check if dashboard metrics are being generated
SELECT 
    "ProjectId",
    "MetricDate",
    "Period",
    "OverallVisibilityScore",
    "TotalPrompts",
    "TotalCitations",
    "CreatedAt"
FROM "VisibilityDashboardMetrics"
ORDER BY "CreatedAt" DESC
LIMIT 20;

-- Check if alerts are being generated
SELECT 
    "ProjectId",
    "AlertType",
    "Severity",
    "Message",
    "CreatedAt"
FROM "VisibilityAlerts"
ORDER BY "CreatedAt" DESC
LIMIT 10;
```

**If both are empty, background jobs are not running or not generating data.**

---

## 🔧 MISSING API IMPLEMENTATIONS

### 1. **Real-time Visibility Tracking**

**Frontend Calls:** `GET /api/visibility/trends`  
**Backend:** ✅ Implemented  
**Data Generation:** ❌ **NOT RUNNING**

**Problem:** `VisibilityTrendAggregationJob` may not be populating trend data.

**Check:**
```sql
SELECT COUNT(*) FROM "VisibilityDashboardMetrics" WHERE "Period" = 'daily';
```

**If 0, job needs to be triggered or debugged.**

---

### 2. **Opportunity Detection**

**Frontend Calls:** Various opportunity-related endpoints  
**Backend:** ✅ Implemented  
**Data Generation:** ❌ **UNCERTAIN**

**Tables to check:**
```sql
-- Content gap opportunities
SELECT COUNT(*) FROM "ContentGapOpportunities";

-- Community opportunities
SELECT COUNT(*) FROM "CommunityOpportunities";

-- Opportunity scans
SELECT COUNT(*) FROM "OpportunityScans";
```

**If all are 0, `OpportunityDetectionJob` is not generating data.**

---

### 3. **Site Audit Data**

**Frontend:** `SiteAudits.tsx`  
**Backend:** ✅ Implemented  
**Data Generation:** ⚠️ **USER-TRIGGERED**

**This is correct behavior** - audits are triggered manually by user.

**However, check if audit results are being stored:**
```sql
SELECT 
    "ProjectId",
    "Url",
    "Status",
    "IssuesFound",
    "CreatedAt"
FROM "SiteAuditRuns"
ORDER BY "CreatedAt" DESC
LIMIT 10;
```

---

## 📋 COMPLETE IMPLEMENTATION CHECKLIST

### **PHASE 1: Data Generation Verification** ⚠️

**Check if background jobs are actually running:**

- [ ] **1.1** Verify Hangfire dashboard shows jobs executing
- [ ] **1.2** Check database for recent job execution timestamps
- [ ] **1.3** Review application logs for job execution

**SQL Verification Script:**
```sql
-- Check all critical tables for recent data
SELECT 'VisibilityDashboardMetrics' AS "Table", COUNT(*) AS "Count", MAX("CreatedAt") AS "LatestEntry"
FROM "VisibilityDashboardMetrics"
UNION ALL
SELECT 'PromptTrackings', COUNT(*), MAX("CreatedAt")
FROM "PromptTrackings"
UNION ALL
SELECT 'Citations', COUNT(*), MAX("CreatedAt")
FROM "Citations"
UNION ALL
SELECT 'CitationAggregations', COUNT(*), MAX("CreatedAt")
FROM "CitationAggregations"
UNION ALL
SELECT 'CompetitiveIntelligence', COUNT(*), MAX("CreatedAt")
FROM "CompetitiveIntelligence"
UNION ALL
SELECT 'ContentGapOpportunities', COUNT(*), MAX("CreatedAt")
FROM "ContentGapOpportunities"
UNION ALL
SELECT 'VisibilityAlerts', COUNT(*), MAX("CreatedAt")
FROM "VisibilityAlerts";
```

---

### **PHASE 2: Fix Prompt Research Metrics** ❌

**Priority:** HIGH (Frontend using mock data)

- [ ] **2.1** Run database migration `003_Add_PromptResearch_Metrics.sql`
- [ ] **2.2** Create `PromptResearchMetricsUpdateJob.cs`
- [ ] **2.3** Add `UpdatePromptMetricsAsync()` method to `PromptResearchService`
- [ ] **2.4** Register job in `Program.cs`
- [ ] **2.5** Update frontend DTO to include new fields
- [ ] **2.6** Remove mock data calculations from `PromptResearch.tsx`
- [ ] **2.7** Test with real data

**Files to Create/Modify:**
- `Database_Migrations/003_Add_PromptResearch_Metrics.sql` (NEW)
- `AppBackendBasic.Infrastructure/BackgroundJobs/PromptResearchMetricsUpdateJob.cs` (NEW)
- `AppBackendBasic.Infrastructure/Services/PromptResearchService.cs` (MODIFY)
- `AppBackendBasic.API/Program.cs` (MODIFY - register job)
- `AppBackendBasic.WebUI/src/pages/PromptResearch.tsx` (MODIFY - remove mocks)

---

### **PHASE 3: Ensure Dashboard Metrics Generation** ⚠️

**Priority:** HIGH (Returns empty dashboard)

- [ ] **3.1** Verify `DashboardMetricsUpdateJob` is registered
- [ ] **3.2** Manually trigger job execution
- [ ] **3.3** Check logs for errors
- [ ] **3.4** Verify data appears in `VisibilityDashboardMetrics` table
- [ ] **3.5** Add seed data if needed for testing

**Seed Data Script:**
```sql
-- Seed initial dashboard metrics for testing
INSERT INTO "VisibilityDashboardMetrics" (
    "ProjectId",
    "MetricDate",
    "Period",
    "OverallVisibilityScore",
    "VisibilityChange",
    "TotalPrompts",
    "ActivePrompts",
    "MentionRate",
    "TotalCitations",
    "CitationShare",
    "SiteAuditScore",
    "TotalOpportunities",
    "HighPriorityOpportunities",
    "PlatformMetrics",
    "CreatedAt"
) VALUES (
    1,  -- Replace with actual project ID
    CURRENT_DATE,
    'daily',
    87.5,
    5.2,
    45,
    38,
    84.4,
    142,
    12.3,
    78.0,
    38,
    12,
    '[{"platform":"ChatGPT","visibilityScore":92.0,"mentionCount":85,"averagePosition":1.8,"change":3.5},{"platform":"Perplexity","visibilityScore":83.0,"mentionCount":42,"averagePosition":2.1,"change":1.2}]',
    NOW()
);
```

---

### **PHASE 4: Competitive Intelligence Data** ❌

**Priority:** MEDIUM (Feature exists but no data)

- [ ] **4.1** Verify `OpportunityDetectionJob` includes competitive analysis
- [ ] **4.2** Check if job is extracting competitor mentions from AI responses
- [ ] **4.3** Ensure `CompetitiveIntelligence` table is being populated
- [ ] **4.4** Add manual competitor tracking feature
- [ ] **4.5** Test competitive analysis display

**Check Implementation:**
```bash
# Search for competitive intelligence logic
grep -r "CompetitiveIntelligence" AppBackendBasic.Infrastructure/BackgroundJobs/
```

---

### **PHASE 5: Frontend Mock Data Removal** ❌

**Priority:** MEDIUM (After backend fixes)

**Files with Mock/Fallback Data:**

1. **`Dashboard.tsx` (Lines 133-150)**
   - Remove hardcoded fallback values
   - Show loading state or "No data" message instead

2. **`PromptResearch.tsx` (Lines 80-84)**
   - Remove `Math.random()` calculations
   - Use actual backend data

3. **`SiteAudits.tsx` (Lines 144-179)**
   - Remove `generateMockIssues()` function (already deprecated)

4. **`OnboardingFlowNew.tsx` (Lines 153-156)**
   - Remove mock data fallback
   - Show proper error message

**Implementation:**
```typescript
// BEFORE (Dashboard.tsx)
value: dashboardData?.overallVisibilityScore?.toString() || '87'

// AFTER
value: dashboardData?.overallVisibilityScore?.toString() || 'N/A'
// OR show loading skeleton
{!dashboardData ? <LoadingSkeleton /> : <MetricCard data={dashboardData} />}
```

---

## 🔍 DATA FLOW VERIFICATION GUIDE

### **For Each Feature, Verify:**

1. **User Action** → Frontend API Call
2. **Frontend API Call** → Backend Controller
3. **Backend Controller** → Service Method
4. **Service Method** → Database Query
5. **Database** → Has Actual Data (not empty)
6. **Background Job** → Populates Data Regularly

### **Example: Citations Flow**

```
User Views Sources Page
    ↓
Frontend: citationService.getTopCitedDomains(projectId)
    ↓
API: GET /api/citations/top-domains?projectId=1
    ↓
Controller: CitationsController.GetTopCitedDomains()
    ↓
Service: CitationTrackingService.GetTopCitedDomainsAsync()
    ↓
Database: SELECT FROM "Citations" WHERE "ProjectId" = 1
    ↓
Background Job: PromptTrackingService.ProcessAllActivePromptsAsync()
    ↓
Data Generated: Citations inserted during prompt analysis
```

**Verification:**
```sql
-- 1. Check if citations exist
SELECT COUNT(*) FROM "Citations" WHERE "ProjectId" = 1;

-- 2. Check if prompt tracking ran
SELECT COUNT(*) FROM "PromptTrackings" WHERE "ProjectId" = 1;

-- 3. Check last job execution
-- (Check Hangfire dashboard or logs)
```

---

## 🚨 CRITICAL MISSING IMPLEMENTATIONS

### **1. Prompt Visibility Score Calculation**

**Current State:** Frontend calculates `visibilityScore` from `relevanceScore`  
**Required:** Backend should calculate based on:
- Mention frequency across platforms
- Position in AI responses
- Citation count
- Sentiment analysis

**Implementation Location:** `PromptTrackingService` or new `VisibilityScoreCalculator`

---

### **2. Trend Change Calculations**

**Current State:** Frontend uses mock `Math.random()` for changes  
**Required:** Backend should calculate by comparing:
- Current period vs previous period
- Store historical snapshots
- Calculate percentage changes

**Implementation:** Add to background jobs that update metrics

---

### **3. Competitive Benchmarking**

**Current State:** `CompetitiveIntelligence` table may be empty  
**Required:** Background job should:
- Extract competitor mentions from AI responses
- Calculate competitor visibility scores
- Compare against user's brand
- Generate competitive insights

**Implementation:** Enhance `OpportunityDetectionJob`

---

## 📊 RECOMMENDED TESTING WORKFLOW

### **Step 1: Seed Initial Data**
```sql
-- Run all seed scripts
\i Database_Migrations/SEED_test_citations_data.sql
-- Add more seed scripts as created
```

### **Step 2: Trigger Background Jobs Manually**
```bash
# Via Hangfire dashboard
# Navigate to: http://localhost:5000/hangfire
# Click "Recurring jobs"
# Click "Trigger now" for each job
```

### **Step 3: Verify Data in Database**
```sql
-- Run verification script from Phase 1
```

### **Step 4: Test Frontend**
```bash
cd AppBackendBasic.WebUI
npm run dev
# Navigate to each page and verify data displays
```

### **Step 5: Check for Errors**
```bash
# Check backend logs
tail -f logs/appbackend-*.log

# Check browser console
# Open DevTools → Console
```

---

## 📝 SUMMARY OF REQUIRED ACTIONS

### **Immediate (This Week):**

1. ✅ Run data verification SQL script
2. ✅ Check if background jobs are executing
3. ❌ Create and run `003_Add_PromptResearch_Metrics.sql`
4. ❌ Implement `PromptResearchMetricsUpdateJob`
5. ❌ Seed initial dashboard metrics for testing

### **Short Term (Next 2 Weeks):**

1. ❌ Remove all frontend mock data calculations
2. ❌ Implement competitive intelligence data generation
3. ❌ Add proper error handling for empty data states
4. ❌ Create comprehensive seed data scripts
5. ❌ Document data flow for each feature

### **Medium Term (Next Month):**

1. ❌ Implement real-time visibility score calculations
2. ❌ Add historical trend tracking
3. ❌ Enhance competitive analysis algorithms
4. ❌ Create admin dashboard for monitoring data generation
5. ❌ Add data quality checks and alerts

---

## 🎯 SUCCESS CRITERIA

**Feature is "Production Ready" when:**

1. ✅ Backend API returns real data (not empty objects)
2. ✅ Frontend displays actual data (no mock/fallback values)
3. ✅ Background jobs populate data automatically
4. ✅ Database tables have recent entries
5. ✅ Data updates regularly (per job schedule)
6. ✅ Error states are handled gracefully
7. ✅ Performance is acceptable (< 500ms API response)
8. ✅ Data accuracy is verified

---

**Document Version:** 1.0  
**Last Updated:** March 29, 2026  
**Next Review:** After Phase 2 implementation
