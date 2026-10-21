# UserId Segregation Implementation Guide

## Overview
This document outlines the implementation of UserId-based data segregation across all multi-user tables to ensure proper data isolation and prevent unauthorized access to other users' data.

## Database Changes

### Migration Script
**Location:** `/DATABASE_MIGRATIONS/003_Add_UserId_Segregation.sql`

### Tables Updated with UserId Column

1. **SiteAudits** - Site audit records
2. **SiteAuditRuns** - Audit execution runs
3. **SiteAuditIssues** - Individual audit findings
4. **OpportunityScans** - Opportunity discovery scans
5. **VisibilityOpportunities** - Visibility tracking opportunities
6. **ContentGapOpportunities** - Content gap analysis
7. **SeoOpportunities** - SEO improvement opportunities
8. **Citations** - Citation tracking data
9. **BrandMentions** - Brand mention tracking
10. **ContentArticles** - Generated content articles
11. **Keywords** - Keyword tracking
12. **PromptTrackings** - Prompt tracking (added UserId alongside CreatedByUserId)

### Migration Features
- Adds `UserId` column to all tables
- Populates existing records with UserId from related Projects table
- Sets `NOT NULL` constraint after population
- Adds foreign key constraints with CASCADE delete
- Creates performance indexes on UserId
- Creates composite indexes for common query patterns (UserId + ProjectId, UserId + Status, etc.)

## Backend Entity Updates

All entities have been updated with:
```csharp
public int UserId { get; set; }

[ForeignKey("UserId")]
public virtual User User { get; set; } = null!;
```

## Controller Updates Required

### Security Pattern to Implement

**All controllers must:**
1. Get the current UserId from claims
2. Validate that the user owns the project/resource
3. Filter all queries by UserId
4. Set UserId when creating new records

### Example Implementation Pattern

```csharp
private int GetUserId()
{
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    return int.TryParse(userIdClaim, out var userId) ? userId : 0;
}

// Validate project ownership
private async Task<bool> ValidateProjectOwnership(int projectId)
{
    var userId = GetUserId();
    return await _context.Projects
        .AnyAsync(p => p.Id == projectId && p.UserId == userId);
}

// GET endpoint example
[HttpGet("project/{projectId}")]
public async Task<ActionResult<IEnumerable<SiteAudit>>> GetByProject(int projectId)
{
    var userId = GetUserId();
    
    // Validate project ownership
    if (!await ValidateProjectOwnership(projectId))
    {
        return Forbid();
    }
    
    // Filter by UserId
    return await _context.SiteAudits
        .Where(a => a.ProjectId == projectId && a.UserId == userId)
        .OrderByDescending(a => a.AuditDate)
        .ToListAsync();
}

// POST endpoint example
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateRequest request)
{
    var userId = GetUserId();
    
    // Validate project ownership
    if (!await ValidateProjectOwnership(request.ProjectId))
    {
        return Forbid();
    }
    
    var entity = new SiteAudit
    {
        UserId = userId, // Set UserId
        ProjectId = request.ProjectId,
        // ... other properties
    };
    
    _context.SiteAudits.Add(entity);
    await _context.SaveChangesAsync();
    
    return Ok(entity);
}
```

## Controllers Requiring Updates

### 1. SiteAuditsController
**File:** `/AppBackendBasic.API/Controllers/SiteAuditsController.cs`

**Endpoints to Update:**
- `GET /api/siteaudits/project/{projectId}` - Add UserId filter
- `POST /api/siteaudits/run` - Set UserId on creation, validate project ownership
- `GET /api/siteaudits/runs/{id}` - Validate UserId ownership
- `GET /api/siteaudits/runs` - Add UserId filter
- `GET /api/siteaudits/project/{projectId}/latest` - Add UserId filter
- `GET /api/siteaudits/project/{projectId}/summary` - Add UserId filter
- `GET /api/siteaudits/project/{projectId}/history` - Add UserId filter
- `POST /api/siteaudits/{detailId}/recheck` - Validate UserId ownership
- `POST /api/siteaudits/{detailId}/mark-fixed` - Validate UserId ownership
- `GET /api/siteaudits/export/{runId}` - Validate UserId ownership
- `GET /api/siteaudits/runs/{runId}/issues` - Add UserId filter
- `GET /api/siteaudits/project/{projectId}/issues` - Add UserId filter
- `POST /api/siteaudits/issues/{issueId}/complete` - Validate UserId ownership
- `POST /api/siteaudits/issues/{issueId}/incomplete` - Validate UserId ownership
- `POST /api/siteaudits/runs/{runId}/issues` - Set UserId, validate project ownership

### 2. OpportunitiesController
**File:** `/AppBackendBasic.API/Controllers/OpportunitiesController.cs`

**Endpoints to Update:**
- `GET /api/opportunities/project/{projectId}` - Add UserId filter
- `POST /api/opportunities/scan` - Set UserId, validate project ownership
- `GET /api/opportunities/scans/{scanId}` - Validate UserId ownership
- `GET /api/opportunities/scans` - Add UserId filter
- `GET /api/opportunities/community/{projectId}` - Add UserId filter
- `GET /api/opportunities/content-gaps/{projectId}` - Add UserId filter
- `GET /api/opportunities/competitor-analysis/{projectId}` - Add UserId filter
- `PUT /api/opportunities/{id}/status` - Validate UserId ownership
- `POST /api/opportunities/{id}/draft-reply` - Validate UserId ownership
- `DELETE /api/opportunities/{id}` - Validate UserId ownership
- `GET /api/opportunities/dashboard/{projectId}` - Add UserId filter
- `GET /api/opportunities/trends/{projectId}` - Add UserId filter
- `POST /api/opportunities/{id}/add-to-tasks` - Validate UserId ownership, set UserId on task

### 3. CitationsController
**File:** `/AppBackendBasic.API/Controllers/CitationsController.cs`

**All endpoints must:**
- Add UserId filter to queries
- Set UserId when creating citations
- Validate UserId ownership before updates/deletes

### 4. SourcesController / BrandMentionsController
**Similar pattern for all brand mention and citation endpoints**

### 5. ContentArticlesController
**All endpoints must:**
- Add UserId filter
- Set UserId on creation
- Validate ownership

### 6. KeywordsController / PromptTrackingsController
**All endpoints must:**
- Add UserId filter
- Set UserId on creation
- Validate ownership

## Service Layer Updates

### ISiteAuditService / SiteAuditService
Update all methods to:
- Accept `userId` parameter
- Filter by UserId in all queries
- Set UserId when creating records

### IOpportunityService / OpportunityService
Update all methods to:
- Accept `userId` parameter
- Filter by UserId in all queries
- Set UserId when creating records

## Testing Checklist

### Database Migration
- [ ] Run migration script on development database
- [ ] Verify all tables have UserId column
- [ ] Verify existing data has UserId populated
- [ ] Verify foreign key constraints are in place
- [ ] Verify indexes are created

### Backend Testing
- [ ] Test that users can only see their own audits
- [ ] Test that users cannot access other users' audits by changing IDs in URL
- [ ] Test that creating new records sets UserId correctly
- [ ] Test that project ownership validation works
- [ ] Test all GET endpoints filter by UserId
- [ ] Test all POST endpoints set UserId
- [ ] Test all PUT/DELETE endpoints validate ownership

### Security Testing
- [ ] Attempt to access another user's audit by ID - should fail
- [ ] Attempt to access another user's project data - should fail
- [ ] Attempt to create audit for another user's project - should fail
- [ ] Attempt to update another user's audit issue - should fail
- [ ] Verify 403 Forbidden is returned for unauthorized access

## Implementation Steps

### Step 1: Apply Database Migration
```bash
cd /Users/dhawal/Documents/GitHub/AISEO/AISEODevelop/AISEO
psql -U postgres -d your_database_name -f DATABASE_MIGRATIONS/003_Add_UserId_Segregation.sql
```

### Step 2: Update DbContext (if needed)
Verify that Entity Framework recognizes the new UserId properties.

### Step 3: Update SiteAuditsController
1. Add `ValidateProjectOwnership` helper method
2. Update all GET endpoints to filter by UserId
3. Update all POST endpoints to set UserId
4. Update all PUT/DELETE endpoints to validate ownership
5. Add proper error handling (403 Forbidden for unauthorized access)

### Step 4: Update OpportunitiesController
Same pattern as SiteAuditsController

### Step 5: Update Other Controllers
Apply the same pattern to:
- CitationsController
- BrandMentionsController
- ContentArticlesController
- KeywordsController
- PromptTrackingsController

### Step 6: Update Services
Update service layer methods to accept and use userId parameter

### Step 7: Testing
- Unit tests for UserId validation
- Integration tests for data segregation
- Security tests for unauthorized access attempts

## Code Examples

### Complete Controller Method with UserId Validation

```csharp
/// <summary>
/// Get audit issues for a project (from latest audit)
/// </summary>
[HttpGet("project/{projectId}/issues")]
public async Task<IActionResult> GetProjectAuditIssues(int projectId)
{
    try
    {
        var userId = GetUserId();
        
        // Validate project ownership
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId);
            
        if (project == null)
        {
            return Forbid(); // User doesn't own this project
        }
        
        // Get the latest audit run for the project (filtered by UserId)
        var latestRun = await _context.SiteAuditRuns
            .Where(r => r.ProjectId == projectId && r.UserId == userId && r.Status == "Completed")
            .OrderByDescending(r => r.CompletedAt)
            .FirstOrDefaultAsync();

        if (latestRun == null)
        {
            return Ok(new List<object>()); // Return empty list if no completed audits
        }

        // Get issues (filtered by UserId)
        var issues = await _context.SiteAuditIssues
            .Where(i => i.SiteAuditRunId == latestRun.Id && i.UserId == userId)
            .OrderByDescending(i => i.Priority)
            .ThenBy(i => i.IsCompleted)
            .ThenByDescending(i => i.CreatedAt)
            .ToListAsync();

        return Ok(issues);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting audit issues for project {ProjectId}", projectId);
        return Ok(new List<object>()); // Return empty list on error to prevent frontend crash
    }
}

/// <summary>
/// Create audit issues (typically called by LLM service after analysis)
/// </summary>
[HttpPost("runs/{runId}/issues")]
public async Task<IActionResult> CreateAuditIssues(int runId, [FromBody] List<CreateSiteAuditIssueDto> issues)
{
    try
    {
        var userId = GetUserId();
        
        // Validate audit run ownership
        var auditRun = await _context.SiteAuditRuns
            .FirstOrDefaultAsync(r => r.Id == runId && r.UserId == userId);
            
        if (auditRun == null)
        {
            return Forbid(); // User doesn't own this audit run
        }

        var newIssues = issues.Select(dto => new SiteAuditIssue
        {
            UserId = userId, // Set UserId
            SiteAuditRunId = runId,
            Title = dto.Title,
            Description = dto.Description,
            Severity = dto.Severity,
            Category = dto.Category,
            Type = dto.Type,
            AffectedPages = dto.AffectedPages,
            Recommendation = dto.Recommendation,
            AutoFixAvailable = dto.AutoFixAvailable,
            Priority = dto.Priority,
            EstimatedImpactScore = dto.EstimatedImpactScore,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }).ToList();

        _context.SiteAuditIssues.AddRange(newIssues);
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Created {newIssues.Count} audit issues.", issues = newIssues });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error creating audit issues for run {RunId}", runId);
        return StatusCode(500, new { message = "An error occurred while creating audit issues." });
    }
}
```

## Security Best Practices

1. **Always validate project ownership** before performing operations
2. **Always filter by UserId** in all queries
3. **Always set UserId** when creating new records
4. **Return 403 Forbidden** for unauthorized access (not 404)
5. **Log security violations** for monitoring
6. **Use parameterized queries** to prevent SQL injection
7. **Validate all input** before processing
8. **Use transactions** for multi-step operations

## Common Pitfalls to Avoid

1. ❌ **Don't** trust projectId from request without validation
2. ❌ **Don't** use only projectId for authorization (validate UserId too)
3. ❌ **Don't** forget to set UserId when creating records
4. ❌ **Don't** return detailed error messages that reveal data existence
5. ❌ **Don't** skip UserId validation in "internal" endpoints
6. ✅ **Do** validate ownership at the controller level
7. ✅ **Do** use consistent error handling
8. ✅ **Do** log all authorization failures
9. ✅ **Do** test with multiple user accounts

## Verification Queries

After applying migration, run these queries to verify:

```sql
-- Check all tables have UserId populated
SELECT 'SiteAudits' as table_name, COUNT(*) as total, COUNT("UserId") as with_userid 
FROM "SiteAudits"
UNION ALL
SELECT 'SiteAuditRuns', COUNT(*), COUNT("UserId") FROM "SiteAuditRuns"
UNION ALL
SELECT 'SiteAuditIssues', COUNT(*), COUNT("UserId") FROM "SiteAuditIssues"
-- ... etc for all tables

-- Verify foreign key constraints exist
SELECT
    tc.table_name, 
    kcu.column_name, 
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name 
FROM information_schema.table_constraints AS tc 
JOIN information_schema.key_column_usage AS kcu
  ON tc.constraint_name = kcu.constraint_name
JOIN information_schema.constraint_column_usage AS ccu
  ON ccu.constraint_name = tc.constraint_name
WHERE tc.constraint_type = 'FOREIGN KEY' 
  AND kcu.column_name = 'UserId';

-- Verify indexes exist
SELECT
    tablename,
    indexname,
    indexdef
FROM pg_indexes
WHERE indexname LIKE '%UserId%'
ORDER BY tablename, indexname;
```

---

**Last Updated:** 2024
**Version:** 1.0
**Status:** Entities Updated - Controllers Pending
