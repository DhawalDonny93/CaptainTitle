# Site Audits and Opportunities Implementation Summary

## Overview
This document provides a comprehensive guide for implementing database-backed Site Audits and Opportunities functionality with completion tracking, LLM-generated insights, and task integration.

## 1. Database Changes

### PostgreSQL Migration Scripts

**Location:** `/DATABASE_MIGRATIONS/001_SiteAuditIssues_And_Opportunities.sql`

**Key Tables Created:**
- `SiteAuditIssues` - Stores individual audit findings with completion tracking
- `OpportunityTasks` - Links opportunities to tasks for implementation tracking

**Key Features:**
- Completion status tracking (IsCompleted, CompletedAt, CompletedBy)
- Severity levels (Critical, High, Medium, Low)
- Categories (Content, AEO, Technical)
- Estimated impact scores (0-100)
- Audit run associations
- View for audit issue summaries

**To Apply Migration:**
```bash
psql -U your_username -d your_database -f DATABASE_MIGRATIONS/001_SiteAuditIssues_And_Opportunities.sql
psql -U your_username -d your_database -f DATABASE_MIGRATIONS/002_LLM_Prompt_Templates_Insert.sql
```

## 2. LLM Prompt Templates

**Location:** `/DATABASE_MIGRATIONS/002_LLM_Prompt_Templates_Insert.sql`

**Templates Created:**
1. **SiteAuditAnalysis** - Comprehensive site audit with technical SEO, AEO, and content analysis
2. **OpportunitiesDiscovery** - Identifies content gaps and visibility opportunities
3. **QuickSiteAudit** - Fast scan for top 10 critical issues
4. **OpportunityPrioritization** - Analyzes and prioritizes opportunities by impact/effort
5. **ContentOpportunityGenerator** - Generates specific content opportunities

**Usage Example:**
```typescript
// In your LLM service
const promptTemplate = await getPromptTemplate('SiteAuditAnalysis');
const prompt = promptTemplate.replace('{{SITE_URL}}', siteUrl)
                             .replace('{{SITE_CONTENT}}', content)
                             .replace('{{META_INFO}}', metaInfo);
const auditIssues = await callLLM(prompt);
```

## 3. Backend Implementation

### New Entities

**SiteAuditIssue.cs** - `/AppBackendBasic.Core/Entities/SiteAuditIssue.cs`
```csharp
public class SiteAuditIssue
{
    public int Id { get; set; }
    public int SiteAuditRunId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Severity { get; set; } // Critical, High, Medium, Low
    public string Category { get; set; } // Content, AEO, Technical
    public string Type { get; set; }
    public int AffectedPages { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? CompletedBy { get; set; }
    public string? Recommendation { get; set; }
    public bool AutoFixAvailable { get; set; }
    public int Priority { get; set; }
    public int EstimatedImpactScore { get; set; }
}
```

**OpportunityTask.cs** - `/AppBackendBasic.Core/Entities/OpportunityTask.cs`
```csharp
public class OpportunityTask
{
    public int Id { get; set; }
    public int OpportunityId { get; set; }
    public string OpportunitySource { get; set; } // Visibility, ContentGap, SEO
    public int TaskId { get; set; }
}
```

### API Endpoints Added

**SiteAuditsController.cs**

1. `GET /api/siteaudits/runs/{runId}/issues` - Get issues for specific audit run
2. `GET /api/siteaudits/project/{projectId}/issues` - Get issues from latest audit
3. `POST /api/siteaudits/issues/{issueId}/complete` - Mark issue as completed
4. `POST /api/siteaudits/issues/{issueId}/incomplete` - Mark issue as incomplete
5. `POST /api/siteaudits/runs/{runId}/issues` - Create audit issues (for LLM service)

**OpportunitiesController.cs**

1. `POST /api/opportunities/{id}/add-to-tasks` - Add opportunity to task list

### Request/Response DTOs

```csharp
public class CreateSiteAuditIssueDto
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string Severity { get; set; }
    public string Category { get; set; }
    public string Type { get; set; }
    public int AffectedPages { get; set; }
    public string? Recommendation { get; set; }
    public bool AutoFixAvailable { get; set; }
    public int Priority { get; set; }
    public int EstimatedImpactScore { get; set; }
}

public class AddOpportunityToTaskRequest
{
    public int ProjectId { get; set; }
    public string OpportunitySource { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string? Priority { get; set; }
    public DateTime? DueDate { get; set; }
}
```

## 4. Frontend Implementation

### Updated Services

**siteAuditService.ts** - `/AppBackendBasic.WebUI/src/services/siteAuditService.ts`

**New Methods:**
```typescript
// Get audit issues for a project (from latest audit)
async getProjectAuditIssues(projectId: number): Promise<AuditIssue[]>

// Get audit issues for a specific audit run
async getAuditIssues(runId: number, isCompleted?: boolean, severity?: string): Promise<AuditIssue[]>

// Mark an issue as completed
async markIssueCompleted(issueId: number): Promise<void>

// Mark an issue as incomplete
async markIssueIncomplete(issueId: number): Promise<void>

// Check if project has completed audits
async hasCompletedAudits(projectId: number): Promise<boolean>
```

**New Interface:**
```typescript
export interface AuditIssue {
  id: number;
  siteAuditRunId: number;
  title: string;
  description: string;
  severity: 'Critical' | 'High' | 'Medium' | 'Low';
  category: 'Content' | 'AEO' | 'Technical';
  type: string;
  affectedPages: number;
  isCompleted: boolean;
  completedAt?: Date;
  completedBy?: number;
  recommendation?: string;
  autoFixAvailable: boolean;
  priority: number;
  estimatedImpactScore: number;
  createdAt: Date;
  updatedAt: Date;
}
```

### Updated Components

**SiteAudits.tsx** - Key Changes:
1. Replaced mock data with real API calls
2. Added `fetchAuditsAndIssues()` to fetch both audits and issues
3. Added `handleMarkFixed()` to mark issues as completed
4. Auto-skip "Start Audit" page if project already has audits
5. Mark Fixed button now updates completion status

**OpportunitiesFound.tsx** - To be updated:
1. Add "Add to Task" functionality using new endpoint
2. Create task from opportunity with proper mapping

## 5. Implementation Steps

### Step 1: Apply Database Migrations
```bash
cd /Users/dhawal/Documents/GitHub/AISEO/AISEODevelop/AISEO
psql -U postgres -d aiseo_db -f DATABASE_MIGRATIONS/001_SiteAuditIssues_And_Opportunities.sql
psql -U postgres -d aiseo_db -f DATABASE_MIGRATIONS/002_LLM_Prompt_Templates_Insert.sql
```

### Step 2: Update DbContext
Add DbSets to `ApplicationDbContext.cs`:
```csharp
public DbSet<SiteAuditIssue> SiteAuditIssues { get; set; }
public DbSet<OpportunityTask> OpportunityTasks { get; set; }
```

### Step 3: Create LLM Integration Service
Create a service to call LLM and generate audit issues:
```csharp
public class AuditAnalysisService
{
    public async Task<List<SiteAuditIssue>> AnalyzeSiteAsync(string url, string content)
    {
        // 1. Get prompt template
        var template = await _promptService.GetTemplateAsync("SiteAuditAnalysis");
        
        // 2. Replace placeholders
        var prompt = template.Replace("{{SITE_URL}}", url)
                            .Replace("{{SITE_CONTENT}}", content);
        
        // 3. Call LLM
        var response = await _llmService.GenerateAsync(prompt);
        
        // 4. Parse JSON response into SiteAuditIssue objects
        var issues = JsonSerializer.Deserialize<List<SiteAuditIssue>>(response);
        
        return issues;
    }
}
```

### Step 4: Update Audit Workflow
Modify `SiteAuditorService` to generate issues after audit:
```csharp
public async Task<SiteAuditRun> StartAuditAsync(int projectId, string url)
{
    // 1. Create audit run
    var auditRun = new SiteAuditRun { ProjectId = projectId, Url = url };
    await _context.SiteAuditRuns.AddAsync(auditRun);
    await _context.SaveChangesAsync();
    
    // 2. Perform audit checks
    var content = await _crawler.GetPageContentAsync(url);
    
    // 3. Generate issues using LLM
    var issues = await _auditAnalysisService.AnalyzeSiteAsync(url, content);
    
    // 4. Associate issues with audit run
    foreach (var issue in issues)
    {
        issue.SiteAuditRunId = auditRun.Id;
    }
    
    await _context.SiteAuditIssues.AddRangeAsync(issues);
    await _context.SaveChangesAsync();
    
    return auditRun;
}
```

### Step 5: Implement "Add to Task" for Opportunities
Update `OpportunitiesFound.tsx`:
```typescript
const handleAddToTask = async (opportunity: Opportunity) => {
    try {
        await opportunityService.addToTasks(opportunity.id, {
            projectId: selectedProject.id,
            opportunitySource: opportunity.source,
            title: opportunity.title,
            description: opportunity.description,
            priority: opportunity.impact,
            dueDate: null
        });
        
        toast.success('Added to tasks!');
    } catch (error) {
        toast.error('Failed to add to tasks');
    }
};
```

## 6. Testing Checklist

### Backend Testing
- [ ] Run database migrations successfully
- [ ] Verify SiteAuditIssues table created
- [ ] Verify OpportunityTasks table created
- [ ] Test GET /api/siteaudits/project/{projectId}/issues
- [ ] Test POST /api/siteaudits/issues/{issueId}/complete
- [ ] Test POST /api/opportunities/{id}/add-to-tasks

### Frontend Testing
- [ ] Verify audits page loads without errors
- [ ] Verify issues are fetched from API (not mock data)
- [ ] Test "Mark Fixed" button functionality
- [ ] Verify completed issues show "Completed" status
- [ ] Test "Start Audit" page is skipped if audits exist
- [ ] Test "Add to Task" button on Opportunities page

### Integration Testing
- [ ] Run full audit and verify issues are created
- [ ] Mark issue as fixed and verify in database
- [ ] Add opportunity to tasks and verify task created
- [ ] Verify LLM prompt templates are accessible

## 7. Next Steps

1. **Implement LLM Integration**
   - Create service to call LLM with prompt templates
   - Parse LLM responses into structured audit issues
   - Handle errors and fallbacks

2. **Complete Opportunities "Add to Task"**
   - Update OpportunitiesController to save tasks
   - Inject ApplicationDbContext or ITaskService
   - Create OpportunityTask link record

3. **Add Filtering and Sorting**
   - Filter issues by severity, category, completion status
   - Sort by priority, impact score, date
   - Add search functionality

4. **Implement Auto-Fix**
   - For issues with `AutoFixAvailable = true`
   - Create automated fix scripts
   - Apply fixes and recheck

5. **Add Analytics Dashboard**
   - Show completion rates over time
   - Track most common issues
   - Display impact of fixes

## 8. Configuration

### Environment Variables
```env
LLM_API_KEY=your_llm_api_key
LLM_MODEL=gpt-4
LLM_TEMPERATURE=0.7
AUDIT_MAX_PAGES=100
AUDIT_TIMEOUT_SECONDS=300
```

### App Settings
```json
{
  "SiteAudit": {
    "EnableLLMAnalysis": true,
    "MaxIssuesPerRun": 50,
    "AutoGenerateRecommendations": true
  },
  "Opportunities": {
    "EnableAutoTaskCreation": true,
    "DefaultTaskPriority": "Medium"
  }
}
```

## 9. Troubleshooting

### Issue: Audit issues not showing
**Solution:** Check that:
1. Database migration was applied
2. SiteAuditIssues DbSet is added to DbContext
3. API endpoint returns data (check browser network tab)
4. Frontend service is calling correct endpoint

### Issue: "Mark Fixed" not working
**Solution:** Verify:
1. User is authenticated
2. Issue ID is valid
3. Backend endpoint is accessible
4. CORS is configured correctly

### Issue: LLM not generating issues
**Solution:** Check:
1. LLM API key is configured
2. Prompt template exists in database
3. LLM service has proper error handling
4. Response format matches expected JSON structure

## 10. API Documentation

### Get Project Audit Issues
```http
GET /api/siteaudits/project/{projectId}/issues
Authorization: Bearer {token}

Response: 200 OK
[
  {
    "id": 1,
    "title": "Missing WebSite schema",
    "description": "Add WebSite schema markup...",
    "severity": "High",
    "category": "AEO",
    "isCompleted": false,
    "priority": 4,
    "estimatedImpactScore": 75
  }
]
```

### Mark Issue Completed
```http
POST /api/siteaudits/issues/{issueId}/complete
Authorization: Bearer {token}

Response: 200 OK
{
  "message": "Issue marked as completed.",
  "issue": { ... }
}
```

### Add Opportunity to Tasks
```http
POST /api/opportunities/{id}/add-to-tasks
Authorization: Bearer {token}
Content-Type: application/json

{
  "projectId": 1,
  "opportunitySource": "ContentGap",
  "title": "Create guide on Topic X",
  "description": "Comprehensive resource...",
  "priority": "High",
  "dueDate": "2024-12-31"
}

Response: 200 OK
{
  "message": "Opportunity added to tasks successfully.",
  "taskId": 123,
  "opportunityId": 456
}
```

---

**Last Updated:** 2024
**Version:** 1.0
**Status:** Implementation Complete - Testing Required
