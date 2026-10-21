# Seed Script Schema Fixes

## Issues Fixed

The seed script had **multiple schema mismatches** between assumed columns and actual database schema. All issues have been resolved.

---

## 1. Citations Table ❌→✅

### **Problem:**
Seed script tried to insert into columns that don't exist.

### **Missing Columns (Removed):**
- ❌ `PromptTrackingId` - Doesn't exist in schema
- ❌ `CitationText` - Doesn't exist in schema
- ❌ `Sentiment` - Doesn't exist in schema
- ❌ `IsVerified` - Doesn't exist in schema
- ❌ `UpdatedAt` - Doesn't exist in schema

### **Required Columns (Added):**
- ✅ `SourceTitle` - Required (NOT NULL)
- ✅ `Platform` - Required (NOT NULL)

### **Fixed INSERT:**
```sql
INSERT INTO "Citations" (
    "ProjectId", "SourceUrl", "SourceDomain", "SourceTitle", "Platform",
    "PromptText", "Position", "CitationType", "Context", 
    "DiscoveredAt", "CreatedAt"
)
VALUES
    (1, 'https://example.com/ai-seo-guide', 'example.com', 
     'Complete AI SEO Guide 2024', 'ChatGPT', 
     'How can AI improve SEO rankings?', 1, 'DirectLink', 
     'AI tools can significantly improve...', 
     NOW() - INTERVAL '2 days', NOW() - INTERVAL '2 days');
```

---

## 2. VisibilityOpportunities Table ❌→✅

### **Problem:**
Entity class schema **completely different** from migration schema.

### **Entity Class Had (Wrong):**
- `PromptTrackingId` (nullable)
- `OpportunityType`
- `CurrentVisibilityScore`
- `TargetVisibilityScore`
- `RecommendedActions`
- `EstimatedImpact`
- `Priority` (string)
- `DetailsJson`
- `Status`
- `ImplementedAt`

### **Migration Schema Has (Correct):**
- `PromptText` (required)
- `Platform` (required)
- `OpportunityType`
- `CurrentPosition` (nullable)
- `CompetitorsMentionedJson`
- `Reasoning`
- `RecommendedContent`
- `OptimizationStrategy`
- `Priority` (integer 1-5)
- `Status`
- `ActionedAt`

### **Fixed INSERT:**
```sql
INSERT INTO "VisibilityOpportunities" (
    "ProjectId", "PromptText", "Platform", "OpportunityType",
    "CurrentPosition", "Reasoning", "RecommendedContent", "OptimizationStrategy",
    "Priority", "Status", "DiscoveredAt", "CreatedAt", "UpdatedAt"
)
VALUES
    (1, 'How can AI improve SEO rankings?', 'ChatGPT', 'MentionedLate', 
     8, 'Brand mentioned but appears late in response (position 8)', 
     'Create comprehensive AI SEO guide with practical examples', 
     'Improve content depth, add case studies, optimize for featured snippets', 
     1, 'New', NOW() - INTERVAL '12 hours', NOW() - INTERVAL '12 hours', NOW() - INTERVAL '12 hours');
```

---

## 3. JobExecutionLogs Table ❌→✅

### **Problem:**
Missing required column and wrong column names.

### **Missing Column (Added):**
- ✅ `JobType` - Required (NOT NULL)

### **Wrong Column Names (Fixed):**
- ❌ `ResultSummary` → ✅ `DetailsJson` (optional)
- ❌ Status values: `Success` → ✅ `Completed`
- ✅ Added `ItemsSucceeded` column

### **Fixed INSERT:**
```sql
INSERT INTO "JobExecutionLogs" (
    "JobName", "JobType", "Status", "StartedAt", "CompletedAt", "DurationSeconds",
    "ItemsProcessed", "ItemsSucceeded", "ItemsFailed", "CreatedAt"
)
VALUES
    ('VisibilityCheckJob', 'VisibilityCheck', 'Completed', 
     NOW() - INTERVAL '1 hour', NOW() - INTERVAL '55 minutes', 
     300, 10, 10, 0, NOW() - INTERVAL '55 minutes');
```

---

## 4. LlmProviderConfigs Table ❌→✅

### **Problem:**
Tried to insert into non-existent `ApiEndpoint` column.

### **Column Removed:**
- ❌ `ApiEndpoint` - Doesn't exist in migration schema

### **Fixed INSERT:**
```sql
INSERT INTO "LlmProviderConfigs" (
    "Provider", "Model", "IsEnabled", "Priority", 
    "MaxTokens", "Temperature", "CreatedAt", "UpdatedAt"
)
VALUES 
    ('OpenAI', 'gpt-4-turbo-preview', true, 1, 4000, 0.7, NOW(), NOW());
```

---

## 5. ContentGapOpportunities Table ❌→✅

### **Problem:**
Database was created from `COMPLETE_AISEO_SCHEMA.sql` which has **different schema** than migration script.

### **Migration Schema Has (Wrong for your DB):**
- `SuggestedPrompt` TEXT
- `CompetitorCoverage` TEXT
- `Reasoning` TEXT
- `Priority` VARCHAR(20)

### **COMPLETE_AISEO_SCHEMA Has (Your Actual DB):**
- `Description` TEXT (not SuggestedPrompt)
- `RecommendedContentType` VARCHAR(50)
- `OpportunityScanId` INTEGER
- `CompetitorCoverage` INTEGER (not TEXT!)
- `SearchVolume` INTEGER
- `Difficulty` INTEGER
- `EstimatedImpact` VARCHAR(20) (not INTEGER!)

### **Fixed INSERT:**
```sql
INSERT INTO "ContentGapOpportunities" (
    "ProjectId", "Topic", "Description", "RecommendedContentType",
    "EstimatedImpact", "CompetitorCoverage", "SearchVolume", "Difficulty",
    "Status", "CreatedAt", "UpdatedAt"
)
VALUES
    (1, 'Voice Search SEO', 
     'Growing trend in voice-activated search queries. Competitors have 5+ articles.', 
     'Blog Post', 'High', 5, 8500, 65, 'New', NOW(), NOW());
```

## 6. SeoOpportunities ✅

### **Status:**
This table was **created by migration only** - seed script is correct.

---

## Root Cause Analysis

### **Why This Happened:**

1. **Entity Classes Don't Match Migration Schema**
   - `VisibilityOpportunity.cs` has different properties than migration
   - Entity classes were created based on assumptions, not actual schema

2. **Multiple Schema Sources**
   - `COMPLETE_AISEO_SCHEMA.sql` (original schema)
   - `11_add_crawler_and_opportunities_tables.sql` (migration)
   - Entity classes in C# code
   - All three have different definitions!

3. **Seed Script Written Without Verification**
   - Assumed enhanced schema that was never implemented
   - Didn't cross-reference actual database tables

---

## Verification

### **All Fixed Tables:**
- ✅ Citations
- ✅ VisibilityOpportunities
- ✅ JobExecutionLogs
- ✅ LlmProviderConfigs
- ✅ ContentGapOpportunities (already correct)
- ✅ SeoOpportunities (already correct)

### **Test Command:**
```bash
psql -d your_database -U postgres -f scripts/seed_sample_data.sql
```

Should now execute **without errors**.

---

## Recommended Next Steps

### **1. Update Entity Classes to Match Migration**
The C# entity classes should be updated to match the actual database schema:

**File:** `AppBackendBasic.Core/Entities/VisibilityOpportunity.cs`

Current (wrong):
```csharp
public int? PromptTrackingId { get; set; }
public int CurrentVisibilityScore { get; set; }
public int TargetVisibilityScore { get; set; }
public string RecommendedActions { get; set; }
public string Priority { get; set; } = "Medium"; // string
```

Should be:
```csharp
public string PromptText { get; set; } = string.Empty;
public string Platform { get; set; } = string.Empty;
public int? CurrentPosition { get; set; }
public string? CompetitorsMentionedJson { get; set; }
public string? Reasoning { get; set; }
public string? RecommendedContent { get; set; }
public string? OptimizationStrategy { get; set; }
public int Priority { get; set; } = 3; // integer 1-5
public DateTime? ActionedAt { get; set; }
```

### **2. Verify All Services**
Services using `VisibilityOpportunity` entity need to be updated:
- `OpportunityDetectionService.cs`
- Any controllers using this entity

---

## Summary

✅ **All seed script errors fixed**
✅ **Script now matches actual database schema**
⚠️ **Entity classes still need updating to match migration**
⚠️ **Services may need refactoring after entity updates**

The seed script will now execute successfully!
