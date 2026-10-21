-- Migration script to standardize notification tables
-- This script renames the 'Message' column to 'Body' in WebNotification table
-- and ensures all notification tables have consistent column names

-- =====================================================
-- 1. WebNotification: Rename Message to Body
-- =====================================================
DO $$ 
BEGIN
    -- Check if Message column exists and Body doesn't
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'WebNotification' AND column_name = 'Message'
    ) AND NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'WebNotification' AND column_name = 'Body'
    ) THEN
        ALTER TABLE "WebNotification" RENAME COLUMN "Message" TO "Body";
        RAISE NOTICE 'Renamed WebNotification.Message to Body';
    ELSE
        RAISE NOTICE 'WebNotification.Message column does not exist or Body already exists';
    END IF;
END $$;

-- =====================================================
-- 2. Ensure Notification table has correct schema
-- =====================================================
-- Notification table should have: Id, UserId, Title, Body, Type, Data, ImageUrl, 
-- IsRead, ReadAt, IsSent, SentAt, ScheduledFor, CreatedAt

-- Check if Notification table exists
DO $$ 
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Notification') THEN
        -- Ensure Body column exists (not Message)
        IF NOT EXISTS (
            SELECT 1 FROM information_schema.columns 
            WHERE table_name = 'Notification' AND column_name = 'Body'
        ) THEN
            -- If Message exists, rename it
            IF EXISTS (
                SELECT 1 FROM information_schema.columns 
                WHERE table_name = 'Notification' AND column_name = 'Message'
            ) THEN
                ALTER TABLE "Notification" RENAME COLUMN "Message" TO "Body";
                RAISE NOTICE 'Renamed Notification.Message to Body';
            ELSE
                -- Add Body column if neither exists
                ALTER TABLE "Notification" ADD COLUMN "Body" VARCHAR(300) NOT NULL DEFAULT '';
                RAISE NOTICE 'Added Body column to Notification table';
            END IF;
        END IF;
    ELSE
        RAISE NOTICE 'Notification table does not exist';
    END IF;
END $$;

-- =====================================================
-- 3. Add indexes for performance (if not exist)
-- =====================================================

-- WebNotification indexes
CREATE INDEX IF NOT EXISTS "IX_WebNotification_UserId" ON "WebNotification"("UserId");
CREATE INDEX IF NOT EXISTS "IX_WebNotification_IsGlobal" ON "WebNotification"("IsGlobal");
CREATE INDEX IF NOT EXISTS "IX_WebNotification_IsRead" ON "WebNotification"("IsRead");
CREATE INDEX IF NOT EXISTS "IX_WebNotification_CreatedAt" ON "WebNotification"("CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_WebNotification_ExpiresAt" ON "WebNotification"("ExpiresAt");

-- InAppNotification indexes (should already exist, but ensure)
CREATE INDEX IF NOT EXISTS "IX_InAppNotification_UserId" ON "InAppNotification"("UserId");
CREATE INDEX IF NOT EXISTS "IX_InAppNotification_IsRead" ON "InAppNotification"("IsRead");
CREATE INDEX IF NOT EXISTS "IX_InAppNotification_IsArchived" ON "InAppNotification"("IsArchived");
CREATE INDEX IF NOT EXISTS "IX_InAppNotification_CreatedAt" ON "InAppNotification"("CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_InAppNotification_UserId_IsRead_IsArchived" 
    ON "InAppNotification"("UserId", "IsRead", "IsArchived");

-- Notification indexes
CREATE INDEX IF NOT EXISTS "IX_Notification_UserId" ON "Notification"("UserId");
CREATE INDEX IF NOT EXISTS "IX_Notification_IsRead" ON "Notification"("IsRead");
CREATE INDEX IF NOT EXISTS "IX_Notification_IsSent" ON "Notification"("IsSent");
CREATE INDEX IF NOT EXISTS "IX_Notification_CreatedAt" ON "Notification"("CreatedAt");

-- =====================================================
-- 4. Verification queries
-- =====================================================
-- Run these to verify the migration

-- Check WebNotification schema
SELECT column_name, data_type, character_maximum_length 
FROM information_schema.columns 
WHERE table_name = 'WebNotification' 
ORDER BY ordinal_position;

-- Check InAppNotification schema
SELECT column_name, data_type, character_maximum_length 
FROM information_schema.columns 
WHERE table_name = 'InAppNotification' 
ORDER BY ordinal_position;

-- Check Notification schema
SELECT column_name, data_type, character_maximum_length 
FROM information_schema.columns 
WHERE table_name = 'Notification' 
ORDER BY ordinal_position;

-- Check indexes
SELECT tablename, indexname 
FROM pg_indexes 
WHERE tablename IN ('WebNotification', 'InAppNotification', 'Notification')
ORDER BY tablename, indexname;
