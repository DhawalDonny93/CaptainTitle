# Controller Update Guide - Multi-User Team Implementation

This guide provides step-by-step instructions for updating all 25+ API controllers to support team-based authorization and data filtering.

## Controller Update Checklist

### Priority 1: CRITICAL (Update First - Blocks Everything)
- [ ] ProjectsController.cs
- [ ] AuthController.cs (partially done)

### Priority 2: CORE FEATURES (Update Second)
- [ ] PromptsController.cs
- [ ] CitationsController.cs
- [ ] BrandProfileController.cs
- [ ] OpportunitiesController.cs

### Priority 3: SUPPORTING FEATURES (Update Third)
- [ ] SiteAuditsController.cs
- [ ] ContentArticlesController.cs
- [ ] VisibilityDashboardController.cs
- [ ] CommunityOpportunitiesController.cs

### Priority 4: SECONDARY (Update Last)
- [ ] AiTrackingPromptsController.cs
- [ ] UserTasksController.cs
- [ ] And 15+ other controllers

---

## Step-by-Step Update Process

### Step 1: Update Controller Constructor

**BEFORE:**
```csharp
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(
        IProjectService projectService,
        ILogger<ProjectsController> logger)
    {
        _projectService = projectService;
        _logger = logger;
    }
}
```

**AFTER:**
```csharp
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly IUserTeamMappingRepository _userTeamMappingRepository;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(
        IProjectService projectService,
        IUserTeamMappingRepository userTeamMappingRepository,
        ILogger<ProjectsController> logger)
    {
        _projectService = projectService;
        _userTeamMappingRepository = userTeamMappingRepository;
        _logger = logger;
    }
}
```

### Step 2: Add Authorization Helper Method

Add this method to the class (right after the constructor):

```csharp
/// <summary>
/// Extracts and validates user and team IDs from JWT claims
/// </summary>
private async Task<(int userId, int teamId)> GetUserAndTeamAsync()
{
    var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    var teamIdClaim = User.FindFirst("team_id")?.Value;
    
    if (!int.TryParse(userIdClaim, out var userId) || !int.TryParse(teamIdClaim, out var teamId))
    {
        _logger.LogWarning("Invalid user or team ID in claims");
        return (0, 0);
    }
    
    // Verify user is actually in the team
    var isInTeam = await _userTeamMappingRepository.IsUserInTeamAsync(userId, teamId);
    if (!isInTeam)
    {
        _logger.LogWarning("User {UserId} is not in team {TeamId}", userId, teamId);
        return (0, 0);
    }
    
    return (userId, teamId);
}
```

### Step 3: Update Each Endpoint

#### Endpoint Type A: GET List (No Parameters)

**BEFORE:**
```csharp
[HttpGet]
[Authorize]
public async Task<IActionResult> GetAll()
{
    var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
    var items = await _service.GetByUserIdAsync(userId);
    return Ok(items);
}
```

**AFTER:**
```csharp
[HttpGet]
[Authorize]
public async Task<IActionResult> GetAll()
{
    var (userId, teamId) = await GetUserAndTeamAsync();
    if (teamId == 0)
        return Forbid("User does not have access to this resource");
    
    var items = await _service.GetByTeamIdAsync(teamId);
    return Ok(items);
}
```

#### Endpoint Type B: GET by ID

**BEFORE:**
```csharp
[HttpGet("{id}")]
[Authorize]
public async Task<IActionResult> GetById(int id)
{
    var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
    var item = await _service.GetByIdAsync(userId, id);
    if (item == null)
        return NotFound();
    return Ok(item);
}
```

**AFTER:**
```csharp
[HttpGet("{id}")]
[Authorize]
public async Task<IActionResult> GetById(int id)
{
    var (userId, teamId) = await GetUserAndTeamAsync();
    if (teamId == 0)
        return Forbid();
    
    var item = await _service.GetByIdAsync(teamId, id);
    if (item == null)
        return NotFound();
    return Ok(item);
}
```

#### Endpoint Type C: POST Create

**BEFORE:**
```csharp
[HttpPost]
[Authorize]
public async Task<IActionResult> Create([FromBody] CreateRequest request)
{
    var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
    var result = await _service.CreateAsync(userId, request);
    return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
}
```

**AFTER:**
```csharp
[HttpPost]
[Authorize]
public async Task<IActionResult> Create([FromBody] CreateRequest request)
{
    var (userId, teamId) = await GetUserAndTeamAsync();
    if (teamId == 0)
        return Forbid();
    
    var result = await _service.CreateAsync(teamId, userId, request);
    return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
}
```

#### Endpoint Type D: PUT Update

**BEFORE:**
```csharp
[HttpPut("{id}")]
[Authorize]
public async Task<IActionResult> Update(int id, [FromBody] UpdateRequest request)
{
    var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
    var updated = await _service.UpdateAsync(userId, id, request);
    if (updated == null)
        return NotFound();
    return Ok(updated);
}
```

**AFTER:**
```csharp
[HttpPut("{id}")]
[Authorize]
public async Task<IActionResult> Update(int id, [FromBody] UpdateRequest request)
{
    var (userId, teamId) = await GetUserAndTeamAsync();
    if (teamId == 0)
        return Forbid();
    
    var updated = await _service.UpdateAsync(teamId, userId, id, request);
    if (updated == null)
        return NotFound();
    return Ok(updated);
}
```

#### Endpoint Type E: DELETE

**BEFORE:**
```csharp
[HttpDelete("{id}")]
[Authorize]
public async Task<IActionResult> Delete(int id)
{
    var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
    var success = await _service.DeleteAsync(userId, id);
    if (!success)
        return NotFound();
    return NoContent();
}
```

**AFTER:**
```csharp
[HttpDelete("{id}")]
[Authorize]
public async Task<IActionResult> Delete(int id)
{
    var (userId, teamId) = await GetUserAndTeamAsync();
    if (teamId == 0)
        return Forbid();
    
    var success = await _service.DeleteAsync(teamId, userId, id);
    if (!success)
        return NotFound();
    return NoContent();
}
```

---

## Common Update Patterns by Controller Type

### Pattern 1: Controllers That Filter by ProjectId
**Example:** BrandProfileController, CompetitorController

These controllers often receive `projectId` in the route. You still need to:
1. Add team authorization check
2. Verify project belongs to the team (ProjectId filtering is team-scoped)
3. Example route: `[HttpGet("project/{projectId}")]`

```csharp
[HttpGet("project/{projectId}")]
[Authorize]
public async Task<IActionResult> GetByProject(int projectId)
{
    var (userId, teamId) = await GetUserAndTeamAsync();
    if (teamId == 0)
        return Forbid();
    
    // The service should verify projectId belongs to teamId
    var items = await _service.GetByProjectIdAsync(projectId, teamId);
    return Ok(items);
}
```

### Pattern 2: Controllers with Route Parameters
**Example:** PromptsController with `{projectId}/{promptId}`

```csharp
[HttpGet("{projectId}/{promptId}")]
[Authorize]
public async Task<IActionResult> GetPrompt(int projectId, int promptId)
{
    var (userId, teamId) = await GetUserAndTeamAsync();
    if (teamId == 0)
        return Forbid();
    
    var prompt = await _service.GetByIdAsync(teamId, promptId);
    if (prompt == null || prompt.ProjectId != projectId)
        return NotFound();
    return Ok(prompt);
}
```

---

## Controllers Priority & Files

### Update in This Order:

#### First: ProjectsController
- File: `/AppBackendBasic.API/Controllers/ProjectsController.cs`
- **Why First**: All other resources depend on projects
- Methods: GetAll, GetById, Create, Update, Delete, GetProjectMetrics, etc.
- Changes: Add teamId parameter to all service calls, verify team membership

#### Second: PromptsController  
- File: `/AppBackendBasic.API/Controllers/PromptsController.cs`
- Methods: GetAll, GetById, Create, Update, Delete, CheckVisibility, etc.
- Changes: Update all GetBy* calls to GetByTeam*

#### Third: CitationsController
- File: `/AppBackendBasic.API/Controllers/CitationsController.cs`
- Methods: GetAll, GetById, GetByProject, GetByPrompt, etc.
- Changes: Filter by TeamId in all queries

#### Fourth: OpportunitiesController
- File: `/AppBackendBasic.API/Controllers/OpportunitiesController.cs`
- Includes: SeoOpportunities, ContentGapOpportunities, VisibilityOpportunities
- Changes: Update all opportunity queries to filter by TeamId

#### Fifth-Ninth: Other Core Controllers
1. SiteAuditsController
2. ContentArticlesController
3. VisibilityDashboardController
4. BrandProfileController
5. CommunityOpportunitiesController

#### Tenth+: Supporting Controllers
- AiTrackingPromptsController
- UserTasksController
- All others that read team data

---

## Service Method Updates Required

### Pattern: Method Signature Changes

For each controller endpoint, the service it calls must be updated:

**BEFORE:**
```csharp
public async Task<List<Item>> GetByUserIdAsync(int userId)
public async Task<Item> GetByIdAsync(int userId, int itemId)
public async Task<Item> CreateAsync(int userId, CreateRequest request)
public async Task<Item> UpdateAsync(int userId, int itemId, UpdateRequest request)
public async Task<bool> DeleteAsync(int userId, int itemId)
```

**AFTER:**
```csharp
public async Task<List<Item>> GetByTeamIdAsync(int teamId)
public async Task<Item> GetByIdAsync(int teamId, int itemId)
public async Task<Item> CreateAsync(int teamId, int userId, CreateRequest request)
public async Task<Item> UpdateAsync(int teamId, int userId, int itemId, UpdateRequest request)
public async Task<bool> DeleteAsync(int teamId, int userId, int itemId)
```

---

## Verification Checklist

After updating each controller, verify:
- [ ] Controller imports `IUserTeamMappingRepository`
- [ ] Constructor includes the repository
- [ ] `GetUserAndTeamAsync()` helper method exists
- [ ] All endpoints call `GetUserAndTeamAsync()`
- [ ] All endpoints return `Forbid()` if teamId == 0
- [ ] All service calls pass `teamId` parameter
- [ ] All `[Authorize]` attributes are present on protected endpoints
- [ ] Compilation succeeds
- [ ] No warnings about unused imports

---

## Testing Each Controller

After updating, test with:

1. **No Token**: Should return 401 Unauthorized
   ```bash
   curl -X GET http://localhost:5000/api/projects
   ```

2. **Invalid Token**: Should return 403 Forbidden
   ```bash
   curl -X GET -H "Authorization: Bearer invalid" http://localhost:5000/api/projects
   ```

3. **Valid Token, Wrong Team**: Should return 403 Forbidden
   ```bash
   # Login as user from Team A
   # Try to access Team B resource
   ```

4. **Valid Token, Correct Team**: Should return 200 OK with data
   ```bash
   # Login as user from Team A
   # Access Team A resource
   ```

---

## Automation Tip

If you're comfortable with regex replacements, you can use VS Code Find & Replace:

**Find:**
```
GetByUserIdAsync\(userId\)
```

**Replace:**
```
GetByTeamIdAsync(teamId)
```

But **verify each match manually** to ensure context is correct.

---

## Need Help?

Refer to `TEAM_CODE_PATTERNS.md` for complete copy-paste examples of:
- Authorization helper patterns
- Service call patterns  
- Error handling patterns
- Common endpoint types

All patterns are ready to copy and paste into your controllers.
