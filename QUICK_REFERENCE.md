# Team Implementation - Quick Reference Card

## ✅ What's Done

### Database & Entities (100%)
```
✅ Team.cs, UserTeamMapping.cs, PromptEditHistory.cs
✅ 25+ entities updated with TeamId property
✅ DbContext fully configured
✅ SQL migration scripts ready
```

### Repositories (100%)
```
✅ TeamRepository - CRUD + helpers
✅ UserTeamMappingRepository - Association management
✅ PromptEditHistoryRepository - Audit trail
```

### Services (PromptTracking - 100%)
```
✅ CreatePromptAsync(teamId, userId, ...)
✅ UpdatePromptAsync(teamId, ...)
✅ DeletePromptAsync(teamId, ...)
✅ CheckPromptVisibilityAsync(teamId, ...)
✅ ProcessCitationsAndCompetitorsAsync(teamId, ...)
✅ TrackOwnBrandMentionAsync(teamId, ...)
✅ ProcessAllActivePromptsAsync() - uses TeamId
```

### Auth (100%)
```
✅ AuthController - All endpoints return teamId
✅ Register, Login, Google, Apple updated
✅ TeamAuthorizationHelper utility created
```

---

## 🚧 What's Left

### Controllers (0/25+)
```
CRITICAL:
- [ ] ProjectsController
- [ ] PromptsController
- [ ] CitationsController
- [ ] OpportunitiesController

Then: 20+ more controllers
```

### Frontend (0/2)
```
- [ ] AuthContext.tsx - Add teamId state
- [ ] Login.tsx - Extract teamId from JWT
```

### Database
```
- [ ] dotnet ef migrations add AddTeamImplementation
- [ ] dotnet ef database update
```

---

## 📋 Controller Update Template

### For EACH controller, apply this pattern:

```csharp
// 1. Add to constructor
private readonly IUserTeamMappingRepository _userTeamMappingRepository;

// 2. Add this method
private async Task<(int userId, int teamId)> GetUserAndTeamAsync()
{
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var teamIdClaim = User.FindFirst("team_id")?.Value;
    
    if (!int.TryParse(userIdClaim, out var userId) || 
        !int.TryParse(teamIdClaim, out var teamId))
        return (0, 0);
    
    var isInTeam = await _userTeamMappingRepository
        .IsUserInTeamAsync(userId, teamId);
    return isInTeam ? (userId, teamId) : (0, 0);
}

// 3. In each endpoint
var (userId, teamId) = await GetUserAndTeamAsync();
if (teamId == 0) return Forbid();

// 4. Update service calls
// FROM: _service.GetByUserIdAsync(userId)
// TO:   _service.GetByTeamIdAsync(teamId)
```

---

## 🔍 What to Change in Each Endpoint

### Method Parameter Updates:
```csharp
// FROM
GetByUserIdAsync(userId)
GetByIdAsync(userId, itemId)
CreateAsync(userId, request)
UpdateAsync(userId, itemId, request)
DeleteAsync(userId, itemId)

// TO
GetByTeamIdAsync(teamId)
GetByIdAsync(teamId, itemId)
CreateAsync(teamId, userId, request)
UpdateAsync(teamId, userId, itemId, request)
DeleteAsync(teamId, userId, itemId)
```

---

## 📊 Progress Tracker

| Area | Status | Files | Time |
|------|--------|-------|------|
| Entities | ✅ | 28 | Done |
| Repos | ✅ | 3 | Done |
| Services | ✅ | 1 | Done |
| Auth | ✅ | 1 | Done |
| Controllers | 🔄 | 25+ | 4-6h |
| Frontend | ⏳ | 2 | 1-2h |
| Database | ⏳ | SQL | 0.5h |
| Testing | ⏳ | - | 2-3h |

**Total: 40% done | ~8 hours remaining**

---

## 🎯 Updated Service Signatures

Check these services for what parameters your controllers should pass:

```csharp
// PromptTrackingService
public async Task<PromptTracking> CreatePromptAsync(
    int projectId, int teamId, int userId, string promptText, ...)
public async Task<PromptTracking> UpdatePromptAsync(
    int promptId, int teamId, string promptText, ...)
public async Task<bool> DeletePromptAsync(
    int promptId, int teamId)

// Other services need similar updates
// Check TEAM_CODE_PATTERNS.md for examples
```

---

## ⚡ Fast Path to MVP (4.5 hours)

1. **Update 4 controllers** (2h)
   - ProjectsController
   - PromptsController
   - CitationsController
   - OpportunitiesController

2. **Update Frontend** (1h)
   - AuthContext.tsx
   - Login.tsx

3. **Database Migration** (30m)
   - Create migration
   - Run update

4. **Test** (1h)
   - Team auth works
   - Data filtering works

---

## 📁 Key Files to Edit

### Controllers (25+ files):
```
AppBackendBasic.API/Controllers/
  ├── ProjectsController.cs ⚠️ FIRST
  ├── PromptsController.cs ⚠️ SECOND
  ├── CitationsController.cs ⚠️ THIRD
  ├── OpportunitiesController.cs ⚠️ FOURTH
  └── ... (20+ more)
```

### Frontend (2 files):
```
AppBackendBasic.WebUI/src/
  ├── contexts/AuthContext.tsx
  └── pages/Login.tsx
```

### Services (to reference):
```
AppBackendBasic.Infrastructure/Services/
  └── PromptTrackingService.cs (✅ Done)
```

---

## 🔗 Reference Documents

When stuck, check:
- **CONTROLLER_UPDATE_GUIDE.md** - Detailed steps
- **TEAM_CODE_PATTERNS.md** - Copy-paste examples
- **COMPLETION_STATUS.md** - What's done/pending
- **PromptTrackingService.cs** - Completed example
- **AuthController.cs** - Completed example

---

## ⚠️ Critical Reminders

1. **Always** add `IUserTeamMappingRepository` to constructor
2. **Always** call `GetUserAndTeamAsync()` in protected endpoints
3. **Always** return `Forbid()` if teamId == 0
4. **Always** pass teamId as first parameter to service methods
5. **Keep** UserId for audit trail, use TeamId for filtering
6. **Check** SQL migration scripts are in the project root

---

## ✨ Done When:

- [x] All entities + repos + services updated
- [x] Auth returns teamId in response
- [x] AuthController complete
- [ ] All 25+ controllers have team authorization pattern
- [ ] Frontend stores and uses teamId
- [ ] Database migration runs successfully
- [ ] Team members see same data
- [ ] Non-team members get Forbid responses
- [ ] All tests pass

---

**Current Status: 40% Complete - Controllers in Progress**
