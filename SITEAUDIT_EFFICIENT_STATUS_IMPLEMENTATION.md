# SiteAudit Efficient Status Implementation

## Overview
Implemented an efficient system to track SiteAudit run status using UserProfile and session management, minimizing database calls by storing audit status in sessionStorage and UserProfile.ExtendedProperties.

## Implementation Strategy

### 1. Backend Changes

#### A. Updated DTOs (`AppBackendBasic.Core/DTOs/AuthResponseDto.cs`)
- Added `UserProfilePreferencesDto` to `UserDto` in auth response
- Contains: `Language`, `Theme`, `Timezone`, and `ExtendedProperties` (JSONB)
- ExtendedProperties stores SiteAudit status per project

```csharp
public class UserProfilePreferencesDto
{
    public string Language { get; set; } = "en";
    public string Theme { get; set; } = "light";
    public string Timezone { get; set; } = "UTC";
    public string ExtendedProperties { get; set; } = "{}";
}
```

#### B. Enhanced AuthService (`AppBackendBasic.Services/Authentication/AuthService.cs`)
- Modified `GenerateAuthResponseAsync` to load UserProfile data during login
- Single DB query to fetch UserProfile with ExtendedProperties
- Includes SiteAudit status in login response

**Benefits:**
- User profile loaded once at login
- No additional DB calls needed for SiteAudit status checks
- Data cached in session for entire user session

#### C. Updated SiteAuditsController (`AppBackendBasic.API/Controllers/SiteAuditsController.cs`)
- Added `UpdateUserProfileAuditStatusAsync` helper method
- Automatically updates UserProfile.ExtendedProperties when audit completes
- Stores audit status as: `{ "siteAuditStatus": { "projectId": true/false } }`

**Key Features:**
- Creates UserProfile if doesn't exist
- Parses and updates JSONB ExtendedProperties
- Non-blocking - doesn't fail request if profile update fails

### 2. Frontend Changes

#### A. Created UserProfileContext (`src/contexts/UserProfileContext.tsx`)
- Manages UserProfile data in sessionStorage
- Provides methods: `hasSiteAudit`, `updateSiteAuditStatus`, `clearProfile`
- Auto-clears profile when user logs out (monitors localStorage.accessToken)

**Key Methods:**
```typescript
hasSiteAudit(projectId: number): boolean
updateSiteAuditStatus(projectId: number, hasAudit: boolean): void
clearProfile(): void
```

#### B. Updated App.tsx
- Wrapped app with `UserProfileProvider`
- Makes profile context available throughout application

#### C. Enhanced Login.tsx
- Stores UserProfile in sessionStorage on successful login
- Profile data includes SiteAudit status from backend

#### D. Optimized SiteAudits.tsx
- **Smart Loading Strategy:**
  1. Check sessionStorage first (instant, no DB call)
  2. If audit status found in session → fetch data directly
  3. If not in session → single DB call to check + update session
  
- **New Method:** `checkAndFetchAudits()`
  - Single DB call to check if audits exist
  - Updates session to avoid future checks
  - Only fetches issues if audits exist

**Flow:**
```
User visits SiteAudits page
  ↓
Check hasSiteAudit(projectId) in session
  ↓
If TRUE → Fetch audits & issues (user has run audit before)
  ↓
If FALSE → Single check query → Update session → Show "Start Scan" or fetch data
```

## Database Call Reduction

### Before Implementation:
- Every page load: 2 DB calls (audits + issues)
- Total: **2 DB calls per visit**

### After Implementation:
- **First visit after login:** 1 DB call (check audits) + session update
- **Subsequent visits:** 0 DB calls if status in session, direct fetch if has audits
- **After running audit:** Status automatically updated in session + UserProfile

### Efficiency Gains:
- **~50-100% reduction** in DB calls for SiteAudit status checks
- Session data persists across page refreshes
- UserProfile.ExtendedProperties synced on login and audit completion

## Data Flow

### Login Flow:
```
User logs in
  ↓
Backend: Load UserProfile with ExtendedProperties
  ↓
Response includes: { user: { profile: { extendedProperties: {...} } } }
  ↓
Frontend: Store profile in sessionStorage
  ↓
Profile available for entire session
```

### Audit Run Flow:
```
User runs audit
  ↓
Backend: Save audit to database
  ↓
Backend: Update UserProfile.ExtendedProperties { siteAuditStatus: { projectId: true } }
  ↓
Frontend: Update sessionStorage immediately
  ↓
Future visits: Instant status check from session
```

### Logout Flow:
```
User logs out
  ↓
localStorage.accessToken removed
  ↓
UserProfileContext detects token removal
  ↓
Auto-clear sessionStorage profile
  ↓
Clean state for next login
```

## Technical Details

### UserProfile.ExtendedProperties Structure:
```json
{
  "siteAuditStatus": {
    "1": true,    // Project 1 has audit
    "2": false,   // Project 2 no audit
    "5": true     // Project 5 has audit
  }
}
```

### Session Storage Key:
- Key: `userProfile`
- Value: JSON string of UserProfilePreferencesDto
- Cleared on logout automatically

## Benefits

1. **Performance:**
   - Minimal DB queries
   - Instant status checks from session
   - Reduced server load

2. **User Experience:**
   - Faster page loads
   - Immediate UI state (Start Scan vs Show Results)
   - No loading delays for status checks

3. **Scalability:**
   - Session-based caching reduces DB pressure
   - Works efficiently with multiple projects
   - Easy to extend for other status flags

4. **Maintainability:**
   - Centralized profile management
   - Single source of truth (UserProfile.ExtendedProperties)
   - Clean separation of concerns

## Files Modified

### Backend:
1. `AppBackendBasic.Core/DTOs/AuthResponseDto.cs` - Added UserProfilePreferencesDto
2. `AppBackendBasic.Services/Authentication/AuthService.cs` - Load profile on login
3. `AppBackendBasic.API/Controllers/SiteAuditsController.cs` - Update profile on audit completion

### Frontend:
1. `src/contexts/UserProfileContext.tsx` - New context for profile management
2. `src/App.tsx` - Added UserProfileProvider
3. `src/pages/Login.tsx` - Store profile on login
4. `src/pages/SiteAudits.tsx` - Smart loading with session checks
5. `src/services/authService.ts` - Updated types for profile

## Testing Recommendations

1. **Login Flow:**
   - Verify profile stored in sessionStorage after login
   - Check ExtendedProperties contains siteAuditStatus

2. **SiteAudit Status:**
   - First visit: Should check DB once
   - Subsequent visits: Should use session data
   - After running audit: Status updated immediately

3. **Logout Flow:**
   - Verify sessionStorage cleared on logout
   - Confirm clean state for next login

4. **Multi-Project:**
   - Test with multiple projects
   - Verify each project status tracked independently

## Future Enhancements

1. Add more status flags to ExtendedProperties (e.g., onboarding steps, feature flags)
2. Implement profile sync across tabs using BroadcastChannel
3. Add profile version control for cache invalidation
4. Extend to track other resource statuses (content, prompts, etc.)

## Notes

- UserProfile.ExtendedProperties is JSONB in PostgreSQL - efficient for flexible data storage
- Session data automatically cleared on logout via token monitoring
- No additional API endpoints required - uses existing infrastructure
- Backward compatible - works even if UserProfile doesn't exist initially
