# UserProfile Consolidation Migration

## Overview
This migration consolidates the `UserPreference` and `UserProperty` tables into a single comprehensive `UserProfile` table. This provides a centralized location for storing all user-related information outside the main `User` table.

## What This Migration Does

### 1. Creates New UserProfile Table
- **UI Preferences**: Language, Theme, Timezone
- **Profile Information**: Bio, Company, JobTitle, Website, Location, Country, City
- **Social Links**: LinkedIn, Twitter, GitHub URLs
- **Communication Preferences**: Email/Push notifications, Marketing emails, Weekly digest
- **Display Preferences**: Date format, Time format, Currency
- **Privacy Settings**: Profile visibility, Show email/phone flags
- **Flexible Storage**: Two JSONB fields (`CustomPreferences` and `ExtendedProperties`) for unlimited extensibility

### 2. Data Migration
- **From UserPreference**: Migrates Language, Theme, Timezone, and CustomPreferences
- **From UserProperty**: Merges Properties into ExtendedProperties JSONB field
- **Conflict Resolution**: Uses `ON CONFLICT DO UPDATE` to merge data if both tables have records for the same user
- **Default Profiles**: Creates default UserProfile records for all existing users who don't have one

### 3. Safety Features
- **Non-destructive**: Old tables are preserved (commented DROP statements)
- **Conditional Migration**: Only migrates if source tables exist
- **Data Validation**: Checks for numeric UserIds before migration
- **Automatic Timestamps**: Trigger maintains UpdatedAt on every update

## Database Schema

```sql
CREATE TABLE "UserProfile" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL UNIQUE,
    
    -- UI Preferences
    "Language" VARCHAR(10) DEFAULT 'en',
    "Theme" VARCHAR(20) DEFAULT 'light',
    "Timezone" VARCHAR(50) DEFAULT 'UTC',
    
    -- Profile Information
    "Bio" TEXT,
    "Company" VARCHAR(200),
    "JobTitle" VARCHAR(100),
    "Website" VARCHAR(500),
    "Location" VARCHAR(200),
    "Country" VARCHAR(100),
    "City" VARCHAR(100),
    
    -- Social Links
    "LinkedInUrl" VARCHAR(500),
    "TwitterUrl" VARCHAR(500),
    "GitHubUrl" VARCHAR(500),
    
    -- Communication Preferences
    "EmailNotifications" BOOLEAN DEFAULT TRUE,
    "PushNotifications" BOOLEAN DEFAULT TRUE,
    "MarketingEmails" BOOLEAN DEFAULT FALSE,
    "WeeklyDigest" BOOLEAN DEFAULT TRUE,
    
    -- Display Preferences
    "DateFormat" VARCHAR(20) DEFAULT 'MM/DD/YYYY',
    "TimeFormat" VARCHAR(10) DEFAULT '12h',
    "Currency" VARCHAR(10) DEFAULT 'USD',
    
    -- Privacy Settings
    "ProfileVisibility" VARCHAR(20) DEFAULT 'private',
    "ShowEmail" BOOLEAN DEFAULT FALSE,
    "ShowPhone" BOOLEAN DEFAULT FALSE,
    
    -- Flexible JSONB fields
    "CustomPreferences" JSONB DEFAULT '{}',
    "ExtendedProperties" JSONB DEFAULT '{}',
    
    -- Timestamps
    "CreatedAt" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

## Indexes Created
- `IX_UserProfile_UserId` (UNIQUE) - Fast user lookup
- `IX_UserProfile_Language` - Filter by language
- `IX_UserProfile_Country` - Filter by country
- `IX_UserProfile_CreatedAt` - Sort by creation date
- `IX_UserProfile_CustomPreferences` (GIN) - JSONB querying
- `IX_UserProfile_ExtendedProperties` (GIN) - JSONB querying

## Running the Migration

### Step 1: Backup Your Database
```bash
pg_dump -U your_user -d your_database > backup_before_userprofile_migration.sql
```

### Step 2: Run the Migration
```bash
psql -U your_user -d your_database -f Database_Migrations/19_consolidate_user_profile.sql
```

### Step 3: Verify Data Migration
```sql
-- Check record counts
SELECT COUNT(*) FROM "UserProfile";
SELECT COUNT(*) FROM "User";

-- Verify data was migrated
SELECT 
    u."Email",
    up."Language",
    up."Theme",
    up."Timezone",
    up."CustomPreferences",
    up."ExtendedProperties"
FROM "User" u
LEFT JOIN "UserProfile" up ON u."Id" = up."UserId"
LIMIT 10;

-- Check for users without profiles
SELECT COUNT(*) 
FROM "User" u
LEFT JOIN "UserProfile" up ON u."Id" = up."UserId"
WHERE up."Id" IS NULL;
```

### Step 4: Drop Old Tables (Optional)
After verifying the migration is successful, you can drop the old tables:

```sql
DROP TABLE IF EXISTS "UserPreference" CASCADE;
DROP TABLE IF EXISTS "UserProperty" CASCADE;
```

## Backend Changes

### Entity Created
- `AppBackendBasic.Core/Entities/UserProfile.cs`

### DbContext Updated
- Replaced `DbSet<UserPreference>` with `DbSet<UserProfile>`
- Updated entity configuration with all new fields
- Added JSONB column type for CustomPreferences and ExtendedProperties

### Old Entities (Can be removed after migration)
- `UserPreference.cs` - Can be deleted
- `UserProperty.cs` - Can be deleted

## Usage Examples

### C# - Get User Profile
```csharp
var userProfile = await _context.UserProfiles
    .FirstOrDefaultAsync(up => up.UserId == userId);
```

### C# - Update Preferences
```csharp
userProfile.Theme = "dark";
userProfile.Language = "es";
userProfile.CustomPreferences = JsonSerializer.Serialize(new {
    sidebarCollapsed = true,
    notificationSound = "chime"
});
await _context.SaveChangesAsync();
```

### SQL - Query JSONB Fields
```sql
-- Find users with specific custom preference
SELECT * FROM "UserProfile"
WHERE "CustomPreferences" @> '{"sidebarCollapsed": true}';

-- Update JSONB field
UPDATE "UserProfile"
SET "CustomPreferences" = "CustomPreferences" || '{"newSetting": "value"}'::jsonb
WHERE "UserId" = 123;
```

## Benefits

1. **Single Source of Truth**: All user preferences and profile data in one table
2. **Better Performance**: Single join instead of multiple tables
3. **Flexibility**: JSONB fields allow unlimited extensibility without schema changes
4. **Type Safety**: Strongly-typed fields for common preferences
5. **Efficient Querying**: GIN indexes on JSONB fields for fast searches
6. **Clean Architecture**: Separates user authentication (User table) from profile/preferences

## Rollback Plan

If you need to rollback:

1. The old tables are preserved by default
2. Simply update your code to use the old entities
3. Optionally drop the UserProfile table:
```sql
DROP TABLE IF EXISTS "UserProfile" CASCADE;
```

## Future Enhancements

The JSONB fields (`CustomPreferences` and `ExtendedProperties`) can store:
- Feature flags per user
- A/B test assignments
- User experiment data
- Custom dashboard layouts
- Saved filters and views
- Any other user-specific metadata

Example:
```json
{
  "featureFlags": {
    "betaFeatures": true,
    "darkModeV2": false
  },
  "dashboardLayout": {
    "widgets": ["analytics", "recent-activity", "notifications"]
  },
  "savedFilters": {
    "prompts": {
      "status": "Active",
      "isCompetitor": false
    }
  }
}
```
