# Multi-User Team Implementation - Complete Guide

## ✅ What Has Been Completed

### 1. **Entity Classes** (All 3 new entities created)
- `AppBackendBasic.Core/Entities/Team.cs` - Team definition
- `AppBackendBasic.Core/Entities/UserTeamMapping.cs` - User-team associations
- `AppBackendBasic.Core/Entities/PromptEditHistory.cs` - Audit trail for prompt changes

### 2. **Entity Updates** (25+ entities updated with TeamId)
All these entities now have a `TeamId` property:
- Project
- PromptTracking
- PromptPlatformTracking
- PromptVisibilityMetrics
- Citation
- BrandMention
- ContentArticle
- SiteAuditRun
- SiteAuditIssue
- SiteAuditDetail
- SeoOpportunity
- ContentGapOpportunity
- VisibilityOpportunity
- OpportunityScan
- BrandProfile
- Competitor
- CommunityOpportunity
- VisibilityDashboardMetrics
- VisibilityAlert
- VisibilityTrend
- VisibilityGoal
- CitationMetric

### 3. **Repository Classes** (3 new repositories created)
- `TeamRepository` - Full CRUD for teams, with methods to get active teams, find by name, etc.
- `UserTeamMappingRepository` - User-team management with methods to add/remove users, check membership, get team members, etc.
- `PromptEditHistoryRepository` - Track all changes to prompts with full audit trail capabilities

### 4. **Authorization Helper** (1 utility class created)
- `TeamAuthorizationHelper` - Centralized authorization logic for:
  - Extracting UserId and TeamId from JWT claims
  - Checking team membership
  - Checking admin roles
  - Throwing consistent authorization errors

### 5. **Database Migration Scripts** (2 SQL scripts provided)
- `TeamMigration_CreateSchema.sql` - Creates Team and UserTeamMapping tables, adds TeamId columns to all 20+ tables, creates indexes and foreign keys
- `TeamMigration_PopulateData.sql` - Migrates existing user data: creates one personal team per user, associates users to teams, updates all existing data records with TeamId

### 6. **DbContext Updates** (Partially completed)
- Added DbSets for Team, UserTeamMapping, and PromptEditHistory

---

## 📋 What Still Needs to Be Done

### Phase 1: Database & Entity Framework Configuration (1-2 hours)

#### 1.1 DbContext Configuration
**File:** `AppBackendBasic.Infrastructure/Data/ApplicationDbContext.cs`

Add entity configurations in `OnModelCreating()` method. Here's the pattern:

```csharp
// Team entity configuration
modelBuilder.Entity<Team>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.ToTable("Team");
    entity.HasIndex(e => e.Name).IsUnique();
    entity.Property(e => e.Id).ValueGeneratedOnAdd();
    entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'UTC'");

    entity.HasMany(t => t.UserMappings)
          .WithOne(m => m.Team)
          .HasForeignKey(m => m.TeamId)
          .OnDelete(DeleteBehavior.Cascade);

    entity.HasMany(t => t.Projects)
          .WithOne(p => p.Team)
          .HasForeignKey(p => p.TeamId)
          .OnDelete(DeleteBehavior.Cascade);
});

// UserTeamMapping entity configuration
modelBuilder.Entity<UserTeamMapping>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.ToTable("UserTeamMapping");
    entity.HasIndex(e => new { e.UserId, e.TeamId }).IsUnique();
    entity.HasIndex(e => e.TeamId);
    entity.Property(e => e.Id).ValueGeneratedOnAdd();
    entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'UTC'");

    entity.HasOne(m => m.User)
          .WithMany()
          .HasForeignKey(m => m.UserId)
          .OnDelete(DeleteBehavior.Cascade);

    entity.HasOne(m => m.Team)
          .WithMany(t => t.UserMappings)
          .HasForeignKey(m => m.TeamId)
          .OnDelete(DeleteBehavior.Cascade);
});

// PromptEditHistory entity configuration
modelBuilder.Entity<PromptEditHistory>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.ToTable("PromptEditHistory");
    entity.HasIndex(e => e.PromptTrackingId);
    entity.HasIndex(e => e.UserId);
    entity.HasIndex(e => e.ChangedAt);
    entity.Property(e => e.Id).ValueGeneratedOnAdd();
    entity.Property(e => e.ChangedAt).HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'UTC'");
    entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'UTC'");

    entity.HasOne(h => h.PromptTracking)
          .WithMany()
          .HasForeignKey(h => h.PromptTrackingId)
          .OnDelete(DeleteBehavior.Cascade);

    entity.HasOne(h => h.User)
          .WithMany()
          .HasForeignKey(h => h.UserId)
          .OnDelete(DeleteBehavior.Cascade);
});
```

Then add configurations for all other entities that got TeamId. For each, add this pattern:
```csharp
modelBuilder.Entity<EntityName>(entity =>
{
    // ... existing configuration ...
    
    // Add this:
    entity.HasIndex(e => e.TeamId);
    entity.HasOne(e => e.Team)
          .WithMany()
          .HasForeignKey(e => e.TeamId)
          .OnDelete(DeleteBehavior.Cascade);
});
```

#### 1.2 Create EF Core Migration
```bash
cd AppBackendBasic.Infrastructure
dotnet ef migrations add AddTeamImplementation
```

**TODO List for Phase 1:**
- [ ] Add Team entity configuration
- [ ] Add UserTeamMapping entity configuration
- [ ] Add PromptEditHistory entity configuration
- [ ] Add TeamId index and FK to Project configuration
- [ ] Add TeamId index and FK to PromptTracking configuration
- [ ] Add TeamId index and FK to remaining 20+ entities
- [ ] Create and review migration
- [ ] Verify migration generates correct SQL

---

### Phase 2: Database Migration Execution (30 minutes)

#### 2.1 Run Database Migrations
```bash
# This will apply the EF Core migration to your database
dotnet ef database update

# Or manually run the SQL scripts if you prefer direct SQL:
psql -U your_user -d your_database -f TeamMigration_CreateSchema.sql
psql -U your_user -d your_database -f TeamMigration_PopulateData.sql
```

#### 2.2 Verify Data Migration
```sql
-- Check teams were created
SELECT COUNT(*) as "TeamCount" FROM "Team";

-- Check user-team mappings
SELECT COUNT(*) as "MappingCount" FROM "UserTeamMapping";

-- Check projects have TeamId set
SELECT COUNT(*) as "ProjectsWithTeam" FROM "Projects" WHERE "TeamId" > 0;

-- Check prompts have TeamId set
SELECT COUNT(*) as "PromptsWithTeam" FROM "PromptTrackings" WHERE "TeamId" > 0;
```

**TODO List for Phase 2:**
- [ ] Run migrations
- [ ] Verify all data has TeamId populated
- [ ] Backup database
- [ ] Test rollback procedure (optional but recommended)

---

### Phase 3: Repository Updates (2-3 hours)

Every existing repository that filters by `UserId` needs to be updated to filter by `TeamId` instead. Here's the pattern:

#### 3.1 Update Query Pattern - BEFORE:
```csharp
public async Task<List<PromptTracking>> GetUserPromptsAsync(int userId)
{
    return await _context.PromptTrackings
        .Where(p => p.UserId == userId)
        .ToListAsync();
}
```

#### 3.2 Update Query Pattern - AFTER:
```csharp
public async Task<List<PromptTracking>> GetTeamPromptsAsync(int teamId)
{
    return await _context.PromptTrackings
        .Where(p => p.TeamId == teamId)
        .ToListAsync();
}
```

#### 3.3 Repositories to Update:
**Files with UserId filtering that need updating:**
- `ProjectRepository.cs` - ~5 methods
- `PromptTrackingRepository.cs` - ~10 methods
- `CitationRepository.cs` - ~8 methods
- `BrandMentionRepository.cs` - ~5 methods
- `ContentArticleRepository.cs` - ~6 methods
- `SiteAuditRunRepository.cs` - ~5 methods
- `SiteAuditIssueRepository.cs` - ~4 methods
- `SeoOpportunityRepository.cs` - ~6 methods
- `ContentGapOpportunityRepository.cs` - ~5 methods
- `VisibilityOpportunityRepository.cs` - ~5 methods
- `OpportunityScanRepository.cs` - ~4 methods
- `VisibilityDashboardMetricsRepository.cs` - ~4 methods
- `VisibilityAlertRepository.cs` - ~4 methods
- `VisibilityTrendRepository.cs` - ~4 methods
- `VisibilityGoalRepository.cs` - ~4 methods
- `CommunityOpportunityRepository.cs` - ~4 methods

**General Pattern to Follow:**
1. Find all `Where(x => x.UserId == userId)` clauses
2. Change to `Where(x => x.TeamId == teamId)`
3. Change method signature from `GetByUserIdAsync()` to `GetByTeamIdAsync()`
4. Update XML documentation comments
5. Update any dependent code that calls these methods

**TODO List for Phase 3:**
- [ ] Update ProjectRepository (change GetByUserId to GetByTeamId)
- [ ] Update PromptTrackingRepository
- [ ] Update CitationRepository  
- [ ] Update BrandMentionRepository
- [ ] Update ContentArticleRepository
- [ ] Update SiteAuditRunRepository & SiteAuditIssueRepository
- [ ] Update Opportunity repositories (SEO, ContentGap, Visibility, OpportunityScan)
- [ ] Update VisibilityDashboard repositories (Metrics, Alert, Trend, Goal)
- [ ] Update CommunityOpportunityRepository
- [ ] Test all updated repository methods

---

### Phase 4: Service Layer Updates (3-4 hours)

Every service that calls repository methods needs to be updated to pass `teamId` instead of `userId`.

#### 4.1 Update Service Pattern - BEFORE:
```csharp
public async Task<List<PromptTracking>> GetPromptsAsync(int userId)
{
    return await _promptTrackingRepository.GetByUserIdAsync(userId);
}
```

#### 4.2 Update Service Pattern - AFTER:
```csharp
public async Task<List<PromptTracking>> GetPromptsAsync(int teamId)
{
    return await _promptTrackingRepository.GetByTeamIdAsync(teamId);
}
```

#### 4.3 Key Services to Update:
- `PromptTrackingService.cs` - 15+ methods
- `CitationTrackingService.cs` - 10+ methods
- `OpportunityDiscoveryService.cs` - 8+ methods
- `SiteAuditorService.cs` - 6+ methods
- `VisibilityDashboardService.cs` - 10+ methods
- `BrandProfileService.cs` - 5+ methods
- `ContentArticleService.cs` - 6+ methods
- `CommunityOpportunityService.cs` - 4+ methods
- All background job services (ProcessAllActivePromptsAsync, etc.)

#### 4.4 Important: Update Background Jobs
These job services need to iterate by TeamId:

```csharp
// BEFORE
public async Task ProcessAllActivePromptsAsync()
{
    var prompts = await _promptTrackingRepository.GetAllActiveAsync();
    foreach (var prompt in prompts)
    {
        // process
    }
}

// AFTER
public async Task ProcessAllActivePromptsAsync()
{
    var teams = await _teamRepository.GetActiveTeamsAsync();
    foreach (var team in teams)
    {
        var prompts = await _promptTrackingRepository.GetByTeamIdAsync(team.Id);
        foreach (var prompt in prompts)
        {
            // process
        }
    }
}
```

**TODO List for Phase 4:**
- [ ] Update PromptTrackingService
- [ ] Update CitationTrackingService
- [ ] Update OpportunityDiscoveryService
- [ ] Update SiteAuditorService
- [ ] Update VisibilityDashboardService
- [ ] Update BrandProfileService
- [ ] Update ContentArticleService
- [ ] Update CommunityOpportunityService
- [ ] Update ALL background job services (ProcessAllActivePromptsAsync, etc.)
- [ ] Test services with sample data

---

### Phase 5: Controller Layer Updates (2-3 hours)

Every controller that takes `userId` parameter needs to be updated to take `teamId`.

#### 5.1 Update Controller Pattern - BEFORE:
```csharp
[HttpGet("user/{userId}")]
public async Task<IActionResult> GetUserProjects(int userId)
{
    var projects = await _projectService.GetByUserIdAsync(userId);
    return Ok(projects);
}
```

#### 5.2 Update Controller Pattern - AFTER:
```csharp
[HttpGet("team/{teamId}")]
[Authorize]
public async Task<IActionResult> GetTeamProjects(int teamId)
{
    var userId = TeamAuthorizationHelper.GetUserIdFromClaims(User);
    var userTeamId = TeamAuthorizationHelper.GetTeamIdFromClaims(User);

    // Verify user is in the team
    if (!await _userTeamMappingRepository.IsUserInTeamAsync(userId.Value, teamId))
    {
        return Forbid();
    }

    var projects = await _projectService.GetByTeamIdAsync(teamId);
    return Ok(projects);
}
```

#### 5.3 Controllers to Update:
- `ProjectsController.cs` - 8+ endpoints
- `PromptsController.cs` - 12+ endpoints
- `CitationsController.cs` - 10+ endpoints
- `BrandProfileController.cs` - 6+ endpoints
- `OpportunitiesController.cs` - 10+ endpoints
- `SiteAuditsController.cs` - 8+ endpoints
- `VisibilityDashboardController.cs` - 8+ endpoints
- `ContentArticlesController.cs` - 8+ endpoints
- `CommunityOpportunitiesController.cs` - 6+ endpoints
- All 37 controllers that read team-related data

#### 5.4 Authentication Update - IMPORTANT
**File:** `AuthController.cs`

Update the login/token generation to include TeamId in JWT:

```csharp
[HttpPost("login")]
public async Task<IActionResult> Login(LoginRequest request)
{
    var user = await _authService.AuthenticateAsync(request.Email, request.Password);
    
    // Get user's primary team
    var teamId = await _userTeamMappingRepository.GetPrimaryTeamIdAsync(user.Id);
    
    // Create JWT with TeamId claim
    var token = GenerateJwtToken(user, teamId.Value);
    
    return Ok(new LoginResponse { Token = token, User = user });
}

private string GenerateJwtToken(User user, int teamId)
{
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim("team_id", teamId.ToString()), // Add this
        new Claim(ClaimTypes.Name, user.FirstName)
    };

    // ... rest of JWT generation
}
```

**TODO List for Phase 5:**
- [ ] Update AuthController to include TeamId in JWT token
- [ ] Update ProjectsController (add TeamId parameter, verify membership)
- [ ] Update PromptsController
- [ ] Update CitationsController
- [ ] Update BrandProfileController
- [ ] Update OpportunitiesController
- [ ] Update SiteAuditsController
- [ ] Update VisibilityDashboardController
- [ ] Update ContentArticlesController
- [ ] Update CommunityOpportunitiesController
- [ ] Update remaining 27+ controllers
- [ ] Test all endpoints with authorization

---

### Phase 6: Frontend Updates (1-2 hours)

**File:** `AppBackendBasic.WebUI/src/contexts/AuthContext.tsx`

Update to store TeamId:

```typescript
// BEFORE
export interface AuthContextType {
  user: User | null;
  setUser: (user: User | null) => void;
}

// AFTER
export interface AuthContextType {
  user: User | null;
  teamId: number | null;
  setUser: (user: User | null, teamId?: number | null) => void;
}
```

**File:** `AppBackendBasic.WebUI/src/pages/Login.tsx`

Update to extract and store TeamId:

```typescript
// BEFORE
const handleLogin = async () => {
  const response = await authService.login(email, password);
  setUser(response.user);
};

// AFTER
const handleLogin = async () => {
  const response = await authService.login(email, password);
  const token = parseJwt(response.token);
  const teamId = parseInt(token.team_id);
  setUser(response.user, teamId);
};
```

**TODO List for Phase 6:**
- [ ] Update AuthContext to include teamId state
- [ ] Update Login.tsx to extract and store teamId
- [ ] Update all service calls to include teamId parameter
- [ ] Update API client to extract JWT claims
- [ ] Test login flow and team data visibility

---

## 🚀 Implementation Steps (In Order)

1. **Week 1 - Database & Entities**
   - Complete DbContext configuration
   - Create EF Core migration
   - Run database migration scripts
   - Verify data migration

2. **Week 2 - Repositories & Services**
   - Update all repositories (~16 files)
   - Update all services (~10 files)
   - Test queries with sample data

3. **Week 3 - Controllers & Auth**
   - Update AuthController with JWT TeamId
   - Update all 37 controllers to use TeamId
   - Add authorization checks using TeamAuthorizationHelper
   - Test API endpoints

4. **Week 4 - Frontend & Testing**
   - Update Frontend AuthContext and services
   - Update API client
   - End-to-end testing
   - Production deployment

---

## 🔍 Quick Reference: Files Modified

### New Files Created (3)
- `Team.cs`
- `UserTeamMapping.cs`
- `PromptEditHistory.cs`
- `TeamRepository.cs`
- `UserTeamMappingRepository.cs`
- `PromptEditHistoryRepository.cs`
- `TeamAuthorizationHelper.cs`
- `TeamMigration_CreateSchema.sql`
- `TeamMigration_PopulateData.sql`

### Core Files Modified (80+)
- 25 entity classes (added TeamId property)
- 1 DbContext (added DbSets and configurations)
- 16 repository classes (update UserId → TeamId filtering)
- 10 service classes (update calls to repositories)
- 1 AuthController (add TeamId to JWT)
- 37 API controllers (add TeamId parameter)
- 2 Frontend files (AuthContext, Login)

---

## 📝 Testing Checklist

### Unit Tests
- [ ] Team repository CRUD operations
- [ ] UserTeamMapping association functions
- [ ] Authorization helper functions
- [ ] PromptEditHistory audit trail recording

### Integration Tests
- [ ] Create team and add users
- [ ] User A creates project in team
- [ ] User B (added to team) can see same project
- [ ] User C (not in team) cannot see project
- [ ] Prompt edit by User A recorded in PromptEditHistory
- [ ] Background jobs process only team data

### E2E Tests
- [ ] User registration → automatic team creation
- [ ] Login → JWT includes TeamId
- [ ] Frontend displays correct team data
- [ ] Adding user to team → they see team data
- [ ] Team members see each other's changes

---

## ⚠️ Important Notes

1. **Backward Compatibility:** All existing single-user projects are migrated to personal teams. Users can add others to their teams later.

2. **JWT Token:** Include `team_id` claim in authentication token for optimal performance (avoids DB lookup in every request).

3. **Subscription:** With team-level subscription (as requested), ensure billing is recalculated if needed when users are added/removed from teams.

4. **Permissions:** Currently all team members have equal access. Role-based access control (Admin/Manager/Member) is built into the schema but not enforced yet. Can be added in Phase 7 if needed.

5. **Audit Trail:** PromptEditHistory automatically tracks all prompt changes with user attribution. Enable this in services when creating/updating prompts.

6. **Performance:** All TeamId columns have indexes created in migration script. Queries should be optimized by filtering on TeamId first.

---

## 📞 Support & Troubleshooting

### Common Issues

**Issue:** TeamId = 0 after migration
- **Solution:** Check that `TeamMigration_PopulateData.sql` completed successfully. Verify team creation with query: `SELECT COUNT(*) FROM "Team";`

**Issue:** Unauthorized errors on all requests
- **Solution:** Verify JWT includes `team_id` claim. Check AuthController is populating the claim correctly.

**Issue:** "User is not in team" errors
- **Solution:** Verify UserTeamMapping was created correctly. Check: `SELECT * FROM "UserTeamMapping" WHERE "UserId" = <your_id>;`

**Issue:** Background jobs not running
- **Solution:** Ensure background job services are updated to iterate by TeamId. Check job logs for errors.

---

**This implementation supports the goal of multi-user team collaboration while maintaining backward compatibility with existing single-user projects.**
