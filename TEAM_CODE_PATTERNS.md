# Team Implementation - Code Patterns Reference

## Quick Copy-Paste Patterns

Use these patterns to update repositories, services, and controllers systematically.

---

## 📦 Repository Pattern Updates

### Pattern 1: Simple Query by TeamId

**BEFORE:**
```csharp
public async Task<List<PromptTracking>> GetByUserIdAsync(int userId)
{
    return await _context.PromptTrackings
        .Where(p => p.UserId == userId)
        .ToListAsync();
}
```

**AFTER:**
```csharp
public async Task<List<PromptTracking>> GetByTeamIdAsync(int teamId)
{
    return await _context.PromptTrackings
        .Where(p => p.TeamId == teamId)
        .ToListAsync();
}
```

---

### Pattern 2: Query with Includes

**BEFORE:**
```csharp
public async Task<List<Citation>> GetUserCitationsAsync(int userId)
{
    return await _context.Citations
        .Where(c => c.UserId == userId)
        .Include(c => c.Project)
        .Include(c => c.Competitor)
        .OrderByDescending(c => c.DiscoveredAt)
        .ToListAsync();
}
```

**AFTER:**
```csharp
public async Task<List<Citation>> GetTeamCitationsAsync(int teamId)
{
    return await _context.Citations
        .Where(c => c.TeamId == teamId)
        .Include(c => c.Project)
        .Include(c => c.Competitor)
        .OrderByDescending(c => c.DiscoveredAt)
        .ToListAsync();
}
```

---

### Pattern 3: Query with Multiple Conditions

**BEFORE:**
```csharp
public async Task<PromptTracking?> GetByIdAsync(int userId, int promptId)
{
    return await _context.PromptTrackings
        .Where(p => p.UserId == userId && p.Id == promptId)
        .Include(p => p.PlatformTrackings)
        .FirstOrDefaultAsync();
}
```

**AFTER:**
```csharp
public async Task<PromptTracking?> GetByIdAsync(int teamId, int promptId)
{
    return await _context.PromptTrackings
        .Where(p => p.TeamId == teamId && p.Id == promptId)
        .Include(p => p.PlatformTrackings)
        .FirstOrDefaultAsync();
}
```

---

### Pattern 4: Paged Query

**BEFORE:**
```csharp
public async Task<PagedResult<ContentArticle>> GetPagedAsync(int userId, int pageNumber, int pageSize)
{
    var query = _context.ContentArticles
        .Where(a => a.UserId == userId);

    var total = await query.CountAsync();
    var items = await query
        .OrderByDescending(a => a.CreatedAt)
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return new PagedResult<ContentArticle> { Items = items, Total = total };
}
```

**AFTER:**
```csharp
public async Task<PagedResult<ContentArticle>> GetPagedAsync(int teamId, int pageNumber, int pageSize)
{
    var query = _context.ContentArticles
        .Where(a => a.TeamId == teamId);

    var total = await query.CountAsync();
    var items = await query
        .OrderByDescending(a => a.CreatedAt)
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return new PagedResult<ContentArticle> { Items = items, Total = total };
}
```

---

### Pattern 5: Existence Check

**BEFORE:**
```csharp
public async Task<bool> UserHasProjectAsync(int userId, int projectId)
{
    return await _context.Projects
        .AnyAsync(p => p.UserId == userId && p.Id == projectId);
}
```

**AFTER:**
```csharp
public async Task<bool> TeamHasProjectAsync(int teamId, int projectId)
{
    return await _context.Projects
        .AnyAsync(p => p.TeamId == teamId && p.Id == projectId);
}
```

---

### Pattern 6: Count Query

**BEFORE:**
```csharp
public async Task<int> GetUserPromptsCountAsync(int userId)
{
    return await _context.PromptTrackings
        .CountAsync(p => p.UserId == userId && p.IsActive);
}
```

**AFTER:**
```csharp
public async Task<int> GetTeamPromptsCountAsync(int teamId)
{
    return await _context.PromptTrackings
        .CountAsync(p => p.TeamId == teamId && p.IsActive);
}
```

---

### Pattern 7: Create/Insert (Keep UserId for audit)

**BEFORE:**
```csharp
public async Task<PromptTracking> CreateAsync(int userId, PromptTracking prompt)
{
    prompt.UserId = userId;  // Still set UserId for audit
    _context.PromptTrackings.Add(prompt);
    await _context.SaveChangesAsync();
    return prompt;
}
```

**AFTER:**
```csharp
public async Task<PromptTracking> CreateAsync(int teamId, int userId, PromptTracking prompt)
{
    prompt.TeamId = teamId;  // Set TeamId for filtering
    prompt.UserId = userId;  // Still set UserId for audit trail
    _context.PromptTrackings.Add(prompt);
    await _context.SaveChangesAsync();
    return prompt;
}
```

---

## 🔧 Service Pattern Updates

### Service Pattern 1: Call Updated Repository

**BEFORE:**
```csharp
public async Task<List<PromptTracking>> GetUserPromptsAsync(int userId)
{
    return await _promptTrackingRepository.GetByUserIdAsync(userId);
}
```

**AFTER:**
```csharp
public async Task<List<PromptTracking>> GetTeamPromptsAsync(int teamId)
{
    return await _promptTrackingRepository.GetByTeamIdAsync(teamId);
}
```

---

### Service Pattern 2: Service with Logic

**BEFORE:**
```csharp
public async Task<PromptVisibilityAnalysis> AnalyzeUserPromptsAsync(int userId)
{
    var prompts = await _promptTrackingRepository.GetByUserIdAsync(userId);
    var citations = await _citationRepository.GetByUserIdAsync(userId);
    
    return new PromptVisibilityAnalysis 
    { 
        TotalPrompts = prompts.Count,
        TotalCitations = citations.Count
    };
}
```

**AFTER:**
```csharp
public async Task<PromptVisibilityAnalysis> AnalyzeTeamPromptsAsync(int teamId)
{
    var prompts = await _promptTrackingRepository.GetByTeamIdAsync(teamId);
    var citations = await _citationRepository.GetByTeamIdAsync(teamId);
    
    return new PromptVisibilityAnalysis 
    { 
        TotalPrompts = prompts.Count,
        TotalCitations = citations.Count
    };
}
```

---

### Service Pattern 3: Background Job (Iterate by Team)

**BEFORE:**
```csharp
public async Task ProcessAllActivePromptsAsync()
{
    var prompts = await _promptTrackingRepository.GetAllActiveAsync();
    foreach (var prompt in prompts)
    {
        await ProcessPromptAsync(prompt);
    }
}
```

**AFTER:**
```csharp
public async Task ProcessAllActivePromptsAsync()
{
    var teams = await _teamRepository.GetActiveTeamsAsync();
    
    foreach (var team in teams)
    {
        var prompts = await _promptTrackingRepository.GetByTeamIdAsync(team.Id);
        var activePrompts = prompts.Where(p => p.IsActive).ToList();
        
        foreach (var prompt in activePrompts)
        {
            await ProcessPromptAsync(prompt);
        }
    }
}
```

---

### Service Pattern 4: Create with Audit Trail

**BEFORE:**
```csharp
public async Task<PromptTracking> CreatePromptAsync(int userId, CreatePromptRequest request)
{
    var prompt = new PromptTracking
    {
        UserId = userId,
        PromptText = request.PromptText,
        IsActive = true
    };
    
    return await _promptTrackingRepository.CreateAsync(userId, prompt);
}
```

**AFTER:**
```csharp
public async Task<PromptTracking> CreatePromptAsync(int teamId, int userId, CreatePromptRequest request)
{
    var prompt = new PromptTracking
    {
        TeamId = teamId,        // Set for filtering
        UserId = userId,        // Keep for audit
        PromptText = request.PromptText,
        IsActive = true
    };
    
    var result = await _promptTrackingRepository.CreateAsync(teamId, userId, prompt);
    
    // Record in audit trail
    await _promptEditHistoryRepository.RecordChangeAsync(
        promptTrackingId: result.Id,
        userId: userId,
        fieldName: "created",
        oldValue: null,
        newValue: request.PromptText,
        changeType: "Created"
    );
    
    return result;
}
```

---

## 🎮 Controller Pattern Updates

### Controller Pattern 1: Get with Authorization

**BEFORE:**
```csharp
[HttpGet("{userId}")]
public async Task<IActionResult> GetUserProjects(int userId)
{
    var projects = await _projectService.GetByUserIdAsync(userId);
    return Ok(projects);
}
```

**AFTER:**
```csharp
[HttpGet("team/{teamId}")]
[Authorize]
public async Task<IActionResult> GetTeamProjects(int teamId)
{
    var userId = TeamAuthorizationHelper.GetUserIdFromClaims(User);
    if (!userId.HasValue)
        return Unauthorized("User ID not found in token");
    
    // Verify user is in the team
    var isInTeam = await _userTeamMappingRepository.IsUserInTeamAsync(userId.Value, teamId);
    if (!isInTeam)
        return Forbid("User is not a member of this team");
    
    var projects = await _projectService.GetByTeamIdAsync(teamId);
    return Ok(projects);
}
```

---

### Controller Pattern 2: Create with Team

**BEFORE:**
```csharp
[HttpPost]
[Authorize]
public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequest request)
{
    var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
    var project = await _projectService.CreateAsync(userId, request);
    return CreatedAtAction(nameof(GetProjectById), new { id = project.Id }, project);
}
```

**AFTER:**
```csharp
[HttpPost]
[Authorize]
public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequest request)
{
    var userId = TeamAuthorizationHelper.GetUserIdFromClaims(User);
    if (!userId.HasValue)
        return Unauthorized("User ID not found in token");
    
    // Get user's primary team (or from request if team selection supported)
    var teamId = await _userTeamMappingRepository.GetPrimaryTeamIdAsync(userId.Value);
    if (!teamId.HasValue)
        return BadRequest("User is not part of any team");
    
    var project = await _projectService.CreateAsync(teamId.Value, userId.Value, request);
    return CreatedAtAction(nameof(GetProjectById), new { id = project.Id }, project);
}
```

---

### Controller Pattern 3: Update with Verification

**BEFORE:**
```csharp
[HttpPut("{id}")]
[Authorize]
public async Task<IActionResult> UpdatePrompt(int id, [FromBody] UpdatePromptRequest request)
{
    var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
    var prompt = await _promptService.GetByIdAsync(userId, id);
    
    if (prompt == null)
        return NotFound();
    
    var updated = await _promptService.UpdateAsync(id, request);
    return Ok(updated);
}
```

**AFTER:**
```csharp
[HttpPut("{teamId}/{id}")]
[Authorize]
public async Task<IActionResult> UpdatePrompt(int teamId, int id, [FromBody] UpdatePromptRequest request)
{
    var userId = TeamAuthorizationHelper.GetUserIdFromClaims(User);
    if (!userId.HasValue)
        return Unauthorized("User ID not found in token");
    
    // Verify user is in team
    var isInTeam = await _userTeamMappingRepository.IsUserInTeamAsync(userId.Value, teamId);
    if (!isInTeam)
        return Forbid("User is not a member of this team");
    
    // Verify prompt belongs to team
    var prompt = await _promptService.GetByIdAsync(teamId, id);
    if (prompt == null)
        return NotFound();
    
    var updated = await _promptService.UpdateAsync(teamId, userId.Value, id, request);
    
    // Record edit in history
    await _promptEditHistoryRepository.RecordChangeAsync(
        promptTrackingId: id,
        userId: userId.Value,
        fieldName: "prompt_text",
        oldValue: prompt.PromptText,
        newValue: request.PromptText,
        changeType: "Updated",
        ipAddress: TeamAuthorizationHelper.GetIpAddressFromContext(HttpContext)
    );
    
    return Ok(updated);
}
```

---

### Controller Pattern 4: List with Pagination

**BEFORE:**
```csharp
[HttpGet("user/{userId}/paged")]
public async Task<IActionResult> GetPagedPrompts(int userId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
{
    var result = await _promptService.GetPagedPromptsAsync(userId, pageNumber, pageSize);
    return Ok(result);
}
```

**AFTER:**
```csharp
[HttpGet("team/{teamId}/paged")]
[Authorize]
public async Task<IActionResult> GetPagedPrompts(int teamId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
{
    var userId = TeamAuthorizationHelper.GetUserIdFromClaims(User);
    if (!userId.HasValue)
        return Unauthorized("User ID not found in token");
    
    // Verify team membership
    if (!await _userTeamMappingRepository.IsUserInTeamAsync(userId.Value, teamId))
        return Forbid();
    
    var result = await _promptService.GetPagedPromptsAsync(teamId, pageNumber, pageSize);
    return Ok(result);
}
```

---

### Controller Pattern 5: Delete with Audit

**BEFORE:**
```csharp
[HttpDelete("{id}")]
[Authorize]
public async Task<IActionResult> DeletePrompt(int id)
{
    var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
    await _promptService.DeleteAsync(userId, id);
    return NoContent();
}
```

**AFTER:**
```csharp
[HttpDelete("team/{teamId}/{id}")]
[Authorize]
public async Task<IActionResult> DeletePrompt(int teamId, int id)
{
    var userId = TeamAuthorizationHelper.GetUserIdFromClaims(User);
    if (!userId.HasValue)
        return Unauthorized("User ID not found in token");
    
    // Verify team membership
    if (!await _userTeamMappingRepository.IsUserInTeamAsync(userId.Value, teamId))
        return Forbid();
    
    // Get prompt for audit
    var prompt = await _promptService.GetByIdAsync(teamId, id);
    if (prompt == null)
        return NotFound();
    
    await _promptService.DeleteAsync(teamId, id);
    
    // Record in history
    await _promptEditHistoryRepository.RecordChangeAsync(
        promptTrackingId: id,
        userId: userId.Value,
        fieldName: "status",
        oldValue: prompt.Status,
        newValue: "Deleted",
        changeType: "Deleted",
        reason: "User deleted",
        ipAddress: TeamAuthorizationHelper.GetIpAddressFromContext(HttpContext)
    );
    
    return NoContent();
}
```

---

## 🔐 Authentication Pattern

### Update AuthController - Add TeamId to JWT

**BEFORE:**
```csharp
[HttpPost("login")]
public async Task<IActionResult> Login([FromBody] LoginRequest request)
{
    var user = await _authService.AuthenticateAsync(request.Email, request.Password);
    if (user == null)
        return Unauthorized();
    
    var token = GenerateJwtToken(user);
    return Ok(new { Token = token, User = user });
}

private string GenerateJwtToken(User user)
{
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Name, user.FirstName)
    };
    
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var token = new JwtSecurityToken(
        issuer: _configuration["Jwt:Issuer"],
        audience: _configuration["Jwt:Audience"],
        claims: claims,
        expires: DateTime.UtcNow.AddHours(24),
        signingCredentials: credentials
    );
    
    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

**AFTER:**
```csharp
[HttpPost("login")]
public async Task<IActionResult> Login([FromBody] LoginRequest request)
{
    var user = await _authService.AuthenticateAsync(request.Email, request.Password);
    if (user == null)
        return Unauthorized();
    
    // Get user's primary team
    var teamId = await _userTeamMappingRepository.GetPrimaryTeamIdAsync(user.Id);
    if (!teamId.HasValue)
        return BadRequest("User is not associated with any team");
    
    var token = GenerateJwtToken(user, teamId.Value);
    return Ok(new { Token = token, User = user, TeamId = teamId });
}

private string GenerateJwtToken(User user, int teamId)
{
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Name, user.FirstName),
        new Claim("team_id", teamId.ToString())  // ADD THIS
    };
    
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var token = new JwtSecurityToken(
        issuer: _configuration["Jwt:Issuer"],
        audience: _configuration["Jwt:Audience"],
        claims: claims,
        expires: DateTime.UtcNow.AddHours(24),
        signingCredentials: credentials
    );
    
    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

---

## 📝 Frontend Pattern

### AuthContext Update

**BEFORE:**
```typescript
export interface AuthContextType {
  user: User | null;
  setUser: (user: User | null) => void;
}

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  
  return (
    <AuthContext.Provider value={{ user, setUser }}>
      {children}
    </AuthContext.Provider>
  );
};
```

**AFTER:**
```typescript
export interface AuthContextType {
  user: User | null;
  teamId: number | null;
  setUser: (user: User | null, teamId?: number | null) => void;
}

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [teamId, setTeamId] = useState<number | null>(null);
  
  const handleSetUser = (newUser: User | null, newTeamId?: number | null) => {
    setUser(newUser);
    if (newTeamId !== undefined) {
      setTeamId(newTeamId);
    }
  };
  
  return (
    <AuthContext.Provider value={{ user, teamId, setUser: handleSetUser }}>
      {children}
    </AuthContext.Provider>
  );
};
```

---

## ✨ Summary

Use these patterns when:
- **Repositories**: Replace `UserId` filtering with `TeamId` filtering
- **Services**: Update method signatures and repository calls
- **Controllers**: Add team authorization checks before operations
- **Auth**: Include `team_id` in JWT token
- **Frontend**: Store and use `teamId` from context

**Key Rule**: Always verify user is in the team before allowing any operation on team data.
