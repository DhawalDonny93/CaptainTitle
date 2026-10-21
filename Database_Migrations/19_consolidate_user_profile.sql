-- Migration: Consolidate UserPreference and UserProperty into UserProfile
-- Date: 2026-02-21
-- Description: Merges UserPreference and UserProperty tables into a single UserProfile table
--              for storing all user-related information outside the main User table

-- ============================================
-- 1. Create new UserProfile table
-- ============================================
CREATE TABLE IF NOT EXISTS "UserProfile" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    
    -- UI Preferences (from UserPreference)
    "Language" VARCHAR(10) NOT NULL DEFAULT 'en',
    "Theme" VARCHAR(20) NOT NULL DEFAULT 'light',
    "Timezone" VARCHAR(50) NOT NULL DEFAULT 'UTC',
    
    -- Additional Profile Information
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
    "EmailNotifications" BOOLEAN NOT NULL DEFAULT TRUE,
    "PushNotifications" BOOLEAN NOT NULL DEFAULT TRUE,
    "MarketingEmails" BOOLEAN NOT NULL DEFAULT FALSE,
    "WeeklyDigest" BOOLEAN NOT NULL DEFAULT TRUE,
    
    -- Display Preferences
    "DateFormat" VARCHAR(20) DEFAULT 'MM/DD/YYYY',
    "TimeFormat" VARCHAR(10) DEFAULT '12h',
    "Currency" VARCHAR(10) DEFAULT 'USD',
    
    -- Privacy Settings
    "ProfileVisibility" VARCHAR(20) DEFAULT 'private',
    "ShowEmail" BOOLEAN NOT NULL DEFAULT FALSE,
    "ShowPhone" BOOLEAN NOT NULL DEFAULT FALSE,
    
    -- Extended properties as JSONB for flexibility
    "CustomPreferences" JSONB DEFAULT '{}',
    "ExtendedProperties" JSONB DEFAULT '{}',
    
    -- Metadata
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    -- Foreign key
    CONSTRAINT "FK_UserProfile_User" FOREIGN KEY ("UserId") 
        REFERENCES "User"("Id") ON DELETE CASCADE
);

-- ============================================
-- 2. Create indexes
-- ============================================
CREATE UNIQUE INDEX "IX_UserProfile_UserId" ON "UserProfile"("UserId");
CREATE INDEX "IX_UserProfile_Language" ON "UserProfile"("Language");
CREATE INDEX "IX_UserProfile_Country" ON "UserProfile"("Country");
CREATE INDEX "IX_UserProfile_CreatedAt" ON "UserProfile"("CreatedAt");

-- GIN index for JSONB columns for efficient querying
CREATE INDEX "IX_UserProfile_CustomPreferences" ON "UserProfile" USING GIN ("CustomPreferences");
CREATE INDEX "IX_UserProfile_ExtendedProperties" ON "UserProfile" USING GIN ("ExtendedProperties");

-- ============================================
-- 3. Migrate data from UserPreference if it exists
-- ============================================
DO $$
BEGIN
    -- Check if UserPreference table exists
    IF EXISTS (SELECT FROM information_schema.tables 
               WHERE table_schema = 'public' 
               AND table_name = 'UserPreference') THEN
        
        -- Migrate data from UserPreference to UserProfile
        INSERT INTO "UserProfile" (
            "UserId",
            "Language",
            "Theme",
            "Timezone",
            "CustomPreferences",
            "CreatedAt",
            "UpdatedAt"
        )
        SELECT 
            "UserId",
            COALESCE("Language", 'en'),
            COALESCE("Theme", 'light'),
            COALESCE("Timezone", 'UTC'),
            CASE 
                WHEN "CustomPreferences" IS NOT NULL AND "CustomPreferences" != '' 
                THEN "CustomPreferences"::JSONB
                ELSE '{}'::JSONB
            END,
            "CreatedAt",
            "UpdatedAt"
        FROM "UserPreference"
        ON CONFLICT ("UserId") DO NOTHING;
        
        RAISE NOTICE 'Migrated data from UserPreference table';
    ELSE
        RAISE NOTICE 'UserPreference table does not exist, skipping migration';
    END IF;
END $$;

-- ============================================
-- 4. Migrate data from UserProperty if it exists
-- ============================================
DO $$
BEGIN
    -- Check if UserProperty table exists
    IF EXISTS (SELECT FROM information_schema.tables 
               WHERE table_schema = 'public' 
               AND table_name = 'UserProperty') THEN
        
        -- Merge UserProperty data into UserProfile ExtendedProperties
        -- If UserProfile record exists, merge the properties; otherwise insert
        INSERT INTO "UserProfile" (
            "UserId",
            "ExtendedProperties",
            "CreatedAt",
            "UpdatedAt"
        )
        SELECT 
            CAST("UserId" AS INTEGER),
            CASE 
                WHEN "Properties" IS NOT NULL AND "Properties" != '' 
                THEN "Properties"::JSONB
                ELSE '{}'::JSONB
            END,
            "CreatedAt",
            "UpdatedAt"
        FROM "UserProperty"
        WHERE "UserId" ~ '^[0-9]+$' -- Only migrate if UserId is numeric
        ON CONFLICT ("UserId") DO UPDATE
        SET 
            "ExtendedProperties" = "UserProfile"."ExtendedProperties" || EXCLUDED."ExtendedProperties",
            "UpdatedAt" = GREATEST("UserProfile"."UpdatedAt", EXCLUDED."UpdatedAt");
        
        RAISE NOTICE 'Migrated data from UserProperty table';
    ELSE
        RAISE NOTICE 'UserProperty table does not exist, skipping migration';
    END IF;
END $$;

-- ============================================
-- 5. Create trigger for UpdatedAt
-- ============================================
CREATE OR REPLACE FUNCTION update_user_profile_timestamp()
RETURNS TRIGGER AS $$
BEGIN
    NEW."UpdatedAt" = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_user_profile_updated_at
    BEFORE UPDATE ON "UserProfile"
    FOR EACH ROW
    EXECUTE FUNCTION update_user_profile_timestamp();

-- ============================================
-- 6. Create default UserProfile for existing users
-- ============================================
-- Insert default UserProfile for users who don't have one
INSERT INTO "UserProfile" ("UserId", "CreatedAt", "UpdatedAt")
SELECT 
    "Id",
    CURRENT_TIMESTAMP,
    CURRENT_TIMESTAMP
FROM "User"
WHERE "Id" NOT IN (SELECT "UserId" FROM "UserProfile")
ON CONFLICT ("UserId") DO NOTHING;

-- ============================================
-- 7. Optional: Drop old tables (commented out for safety)
-- ============================================
-- Uncomment these lines after verifying data migration is successful

-- DROP TABLE IF EXISTS "UserPreference" CASCADE;
-- DROP TABLE IF EXISTS "UserProperty" CASCADE;

-- ============================================
-- 8. Add comments for documentation
-- ============================================
COMMENT ON TABLE "UserProfile" IS 'Consolidated user profile and preferences table. Stores all user-related information outside the main User table.';
COMMENT ON COLUMN "UserProfile"."CustomPreferences" IS 'JSONB field for flexible UI and app preferences';
COMMENT ON COLUMN "UserProfile"."ExtendedProperties" IS 'JSONB field for additional user properties and metadata';
COMMENT ON COLUMN "UserProfile"."ProfileVisibility" IS 'Values: public, private, connections';
COMMENT ON COLUMN "UserProfile"."DateFormat" IS 'Examples: MM/DD/YYYY, DD/MM/YYYY, YYYY-MM-DD';
COMMENT ON COLUMN "UserProfile"."TimeFormat" IS 'Values: 12h, 24h';

-- ============================================
-- Migration Complete
-- ============================================
-- Summary:
-- - Created UserProfile table with comprehensive fields
-- - Migrated data from UserPreference (if exists)
-- - Migrated data from UserProperty (if exists)
-- - Created indexes for performance
-- - Created trigger for automatic UpdatedAt
-- - Created default profiles for existing users
-- - Old tables preserved for safety (can be dropped manually after verification)
