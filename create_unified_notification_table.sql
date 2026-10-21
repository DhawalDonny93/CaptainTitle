-- =====================================================
-- Create Unified Notification Table
-- =====================================================
-- This script creates the unified Notification table with all fields
-- for web notifications, push notifications, and scheduled notifications

-- Drop existing table if you want a fresh start (CAREFUL - this deletes all data!)
-- DROP TABLE IF EXISTS "Notification" CASCADE;

-- Create unified Notification table
CREATE TABLE IF NOT EXISTS "Notification" (
    "Id" SERIAL PRIMARY KEY,
    
    -- User reference
    "UserId" INTEGER NOT NULL,
    
    -- Core notification fields
    "Title" VARCHAR(200) NOT NULL DEFAULT '',
    "Body" VARCHAR(1000) NOT NULL,
    "Type" VARCHAR(20) NOT NULL DEFAULT 'info', -- info, success, warning, error
    
    -- Web notification specific fields
    "Icon" VARCHAR(50),
    "IsGlobal" BOOLEAN NOT NULL DEFAULT false,
    "IsDismissible" BOOLEAN NOT NULL DEFAULT true,
    "DurationMs" INTEGER DEFAULT 5000,
    "ActionUrl" VARCHAR(500),
    "ActionText" VARCHAR(100),
    
    -- Push notification specific fields
    "Data" TEXT, -- JSON string for custom data
    "ImageUrl" TEXT,
    "IsSent" BOOLEAN NOT NULL DEFAULT false,
    "SentAt" TIMESTAMP,
    "ScheduledFor" TIMESTAMP,
    
    -- Common fields
    "IsRead" BOOLEAN NOT NULL DEFAULT false,
    "ReadAt" TIMESTAMP,
    "ExpiresAt" TIMESTAMP,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    -- Foreign key constraint
    CONSTRAINT "FK_Notification_User" FOREIGN KEY ("UserId") 
        REFERENCES "User"("Id") ON DELETE CASCADE
);

-- =====================================================
-- Create Indexes for Performance
-- =====================================================

-- Single column indexes
CREATE INDEX IF NOT EXISTS "IX_Notification_UserId" ON "Notification"("UserId");
CREATE INDEX IF NOT EXISTS "IX_Notification_IsGlobal" ON "Notification"("IsGlobal");
CREATE INDEX IF NOT EXISTS "IX_Notification_IsRead" ON "Notification"("IsRead");
CREATE INDEX IF NOT EXISTS "IX_Notification_IsSent" ON "Notification"("IsSent");
CREATE INDEX IF NOT EXISTS "IX_Notification_CreatedAt" ON "Notification"("CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_Notification_ExpiresAt" ON "Notification"("ExpiresAt");
CREATE INDEX IF NOT EXISTS "IX_Notification_ScheduledFor" ON "Notification"("ScheduledFor");

-- Composite indexes for common queries
CREATE INDEX IF NOT EXISTS "IX_Notification_UserId_IsGlobal_IsRead" 
    ON "Notification"("UserId", "IsGlobal", "IsRead");

CREATE INDEX IF NOT EXISTS "IX_Notification_IsSent_ScheduledFor" 
    ON "Notification"("IsSent", "ScheduledFor");

-- =====================================================
-- Add Comments for Documentation
-- =====================================================

COMMENT ON TABLE "Notification" IS 'Unified notification table for web, push, and scheduled notifications';
COMMENT ON COLUMN "Notification"."UserId" IS 'Reference to the user who receives the notification';
COMMENT ON COLUMN "Notification"."Title" IS 'Notification title/heading';
COMMENT ON COLUMN "Notification"."Body" IS 'Main notification message content';
COMMENT ON COLUMN "Notification"."Type" IS 'Notification type: info, success, warning, error';
COMMENT ON COLUMN "Notification"."Icon" IS 'Icon name/identifier for web notifications';
COMMENT ON COLUMN "Notification"."IsGlobal" IS 'If true, notification is shown to all users (web notifications)';
COMMENT ON COLUMN "Notification"."IsDismissible" IS 'If true, user can dismiss the notification (web notifications)';
COMMENT ON COLUMN "Notification"."DurationMs" IS 'Auto-dismiss duration in milliseconds (web notifications)';
COMMENT ON COLUMN "Notification"."ActionUrl" IS 'URL to navigate when notification is clicked';
COMMENT ON COLUMN "Notification"."ActionText" IS 'Text for action button';
COMMENT ON COLUMN "Notification"."Data" IS 'JSON string containing custom notification data (push notifications)';
COMMENT ON COLUMN "Notification"."ImageUrl" IS 'URL of image to display in notification';
COMMENT ON COLUMN "Notification"."IsSent" IS 'If true, push notification has been sent';
COMMENT ON COLUMN "Notification"."SentAt" IS 'Timestamp when push notification was sent';
COMMENT ON COLUMN "Notification"."ScheduledFor" IS 'Timestamp when notification should be sent (scheduled notifications)';
COMMENT ON COLUMN "Notification"."IsRead" IS 'If true, user has read the notification';
COMMENT ON COLUMN "Notification"."ReadAt" IS 'Timestamp when notification was marked as read';
COMMENT ON COLUMN "Notification"."ExpiresAt" IS 'Timestamp when notification expires and should no longer be shown';
COMMENT ON COLUMN "Notification"."CreatedAt" IS 'Timestamp when notification was created';

-- =====================================================
-- Sample Data (Optional - for testing)
-- =====================================================

-- Uncomment to insert sample notifications for testing

/*
-- Sample web notification (global)
INSERT INTO "Notification" ("UserId", "Title", "Body", "Type", "Icon", "IsGlobal", "IsDismissible", "DurationMs")
VALUES (1, 'Welcome!', 'Welcome to our platform. Check out the new features!', 'info', 'bell', true, true, 5000);

-- Sample push notification
INSERT INTO "Notification" ("UserId", "Title", "Body", "Type", "Data", "ImageUrl", "IsSent", "SentAt")
VALUES (1, 'New Message', 'You have a new message from John', 'info', '{"messageId": 123}', 'https://example.com/avatar.jpg', true, CURRENT_TIMESTAMP);

-- Sample scheduled notification
INSERT INTO "Notification" ("UserId", "Title", "Body", "Type", "ScheduledFor", "IsSent")
VALUES (1, 'Reminder', 'Your subscription expires in 3 days', 'warning', CURRENT_TIMESTAMP + INTERVAL '3 days', false);

-- Sample user-specific web notification
INSERT INTO "Notification" ("UserId", "Title", "Body", "Type", "ActionUrl", "ActionText", "ExpiresAt")
VALUES (1, 'Complete Your Profile', 'Add more details to your profile to get better recommendations', 'info', '/profile/edit', 'Complete Now', CURRENT_TIMESTAMP + INTERVAL '7 days');
*/

-- =====================================================
-- Verification Queries
-- =====================================================

-- Check table structure
SELECT 
    column_name, 
    data_type, 
    character_maximum_length,
    column_default,
    is_nullable
FROM information_schema.columns 
WHERE table_name = 'Notification' 
ORDER BY ordinal_position;

-- Check indexes
SELECT 
    indexname, 
    indexdef 
FROM pg_indexes 
WHERE tablename = 'Notification'
ORDER BY indexname;

-- Check foreign key constraints
SELECT
    tc.constraint_name,
    tc.table_name,
    kcu.column_name,
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name
FROM information_schema.table_constraints AS tc
JOIN information_schema.key_column_usage AS kcu
    ON tc.constraint_name = kcu.constraint_name
JOIN information_schema.constraint_column_usage AS ccu
    ON ccu.constraint_name = tc.constraint_name
WHERE tc.table_name = 'Notification' 
    AND tc.constraint_type = 'FOREIGN KEY';

-- Count records
SELECT COUNT(*) as total_notifications FROM "Notification";

-- =====================================================
-- Grant Permissions (if needed)
-- =====================================================

-- Uncomment and modify if you need to grant permissions to specific roles
-- GRANT SELECT, INSERT, UPDATE, DELETE ON "Notification" TO your_app_role;
-- GRANT USAGE, SELECT ON SEQUENCE "Notification_Id_seq" TO your_app_role;
