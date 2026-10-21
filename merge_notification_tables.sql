-- =====================================================
-- Migration Script: Merge WebNotification into Notification
-- =====================================================
-- This script merges the WebNotification table into the Notification table
-- to create a unified notification system

-- =====================================================
-- Step 1: Add new columns to Notification table
-- =====================================================

-- Add web notification specific columns
ALTER TABLE "Notification" ADD COLUMN IF NOT EXISTS "Icon" VARCHAR(50);
ALTER TABLE "Notification" ADD COLUMN IF NOT EXISTS "IsGlobal" BOOLEAN DEFAULT false;
ALTER TABLE "Notification" ADD COLUMN IF NOT EXISTS "IsDismissible" BOOLEAN DEFAULT true;
ALTER TABLE "Notification" ADD COLUMN IF NOT EXISTS "DurationMs" INTEGER DEFAULT 5000;
ALTER TABLE "Notification" ADD COLUMN IF NOT EXISTS "ExpiresAt" TIMESTAMP;

-- Ensure ActionUrl and ActionText have appropriate lengths
DO $$ 
BEGIN
    -- Check if ActionUrl exists and update its length
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Notification' AND column_name = 'ActionUrl'
    ) THEN
        ALTER TABLE "Notification" ALTER COLUMN "ActionUrl" TYPE VARCHAR(500);
    ELSE
        ALTER TABLE "Notification" ADD COLUMN "ActionUrl" VARCHAR(500);
    END IF;
    
    -- Check if ActionText exists and update its length
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Notification' AND column_name = 'ActionText'
    ) THEN
        ALTER TABLE "Notification" ALTER COLUMN "ActionText" TYPE VARCHAR(100);
    ELSE
        ALTER TABLE "Notification" ADD COLUMN "ActionText" VARCHAR(100);
    END IF;
END $$;

-- Ensure Title column has appropriate length
ALTER TABLE "Notification" ALTER COLUMN "Title" TYPE VARCHAR(200);

-- Ensure Body column has appropriate length
ALTER TABLE "Notification" ALTER COLUMN "Body" TYPE VARCHAR(1000);

-- =====================================================
-- Step 2: Migrate data from WebNotification to Notification
-- =====================================================

DO $$ 
BEGIN
    -- Check if WebNotification table exists
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'WebNotification') THEN
        
        -- Insert WebNotification records into Notification
        INSERT INTO "Notification" (
            "UserId",
            "Title",
            "Body",
            "Type",
            "Icon",
            "IsGlobal",
            "IsDismissible",
            "DurationMs",
            "ActionUrl",
            "ActionText",
            "IsRead",
            "ReadAt",
            "ExpiresAt",
            "CreatedAt"
        )
        SELECT 
            COALESCE("UserId", 0) as "UserId",
            "Title",
            "Body",
            "Type",
            "Icon",
            "IsGlobal",
            "IsDismissible",
            "DurationMs",
            "ActionUrl",
            "ActionText",
            "IsRead",
            "ReadAt",
            "ExpiresAt",
            "CreatedAt"
        FROM "WebNotification"
        WHERE NOT EXISTS (
            -- Avoid duplicates if migration is run multiple times
            SELECT 1 FROM "Notification" n 
            WHERE n."Title" = "WebNotification"."Title" 
            AND n."Body" = "WebNotification"."Body"
            AND n."CreatedAt" = "WebNotification"."CreatedAt"
        );
        
        RAISE NOTICE 'Migrated WebNotification records to Notification table';
    ELSE
        RAISE NOTICE 'WebNotification table does not exist, skipping migration';
    END IF;
END $$;

-- =====================================================
-- Step 3: Add indexes for performance
-- =====================================================

CREATE INDEX IF NOT EXISTS "IX_Notification_UserId" ON "Notification"("UserId");
CREATE INDEX IF NOT EXISTS "IX_Notification_IsGlobal" ON "Notification"("IsGlobal");
CREATE INDEX IF NOT EXISTS "IX_Notification_IsRead" ON "Notification"("IsRead");
CREATE INDEX IF NOT EXISTS "IX_Notification_IsSent" ON "Notification"("IsSent");
CREATE INDEX IF NOT EXISTS "IX_Notification_CreatedAt" ON "Notification"("CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_Notification_ExpiresAt" ON "Notification"("ExpiresAt");
CREATE INDEX IF NOT EXISTS "IX_Notification_UserId_IsGlobal_IsRead" 
    ON "Notification"("UserId", "IsGlobal", "IsRead");

-- =====================================================
-- Step 4: Drop WebNotification table (OPTIONAL - uncomment when ready)
-- =====================================================

-- WARNING: This will permanently delete the WebNotification table
-- Only run this after verifying the migration was successful

-- DROP TABLE IF EXISTS "WebNotification";
-- RAISE NOTICE 'Dropped WebNotification table';

-- =====================================================
-- Step 5: Verification queries
-- =====================================================

-- Check Notification table schema
SELECT column_name, data_type, character_maximum_length, is_nullable
FROM information_schema.columns 
WHERE table_name = 'Notification' 
ORDER BY ordinal_position;

-- Count records in Notification table
SELECT COUNT(*) as notification_count FROM "Notification";

-- Check if WebNotification table still exists
SELECT EXISTS (
    SELECT 1 FROM information_schema.tables 
    WHERE table_name = 'WebNotification'
) as webnotification_exists;

-- Check indexes
SELECT indexname, indexdef 
FROM pg_indexes 
WHERE tablename = 'Notification'
ORDER BY indexname;

-- =====================================================
-- Rollback Script (if needed)
-- =====================================================

-- To rollback this migration:
-- 1. Restore WebNotification table from backup
-- 2. Remove the added columns from Notification:
--    ALTER TABLE "Notification" DROP COLUMN IF EXISTS "Icon";
--    ALTER TABLE "Notification" DROP COLUMN IF EXISTS "IsGlobal";
--    ALTER TABLE "Notification" DROP COLUMN IF EXISTS "IsDismissible";
--    ALTER TABLE "Notification" DROP COLUMN IF EXISTS "DurationMs";
--    ALTER TABLE "Notification" DROP COLUMN IF EXISTS "ExpiresAt";
