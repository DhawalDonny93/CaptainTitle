-- Master Migration Script: Add all missing columns to UserTasks table
-- Date: 2026-02-01
-- Description: Adds Source, IsCompleted, and Status columns to UserTasks table

-- Step 1: Add Source column
\echo '========================================='
\echo 'Step 1: Adding Source column...'
\echo '========================================='
ALTER TABLE "UserTasks" 
ADD COLUMN IF NOT EXISTS "Source" character varying(50) NOT NULL DEFAULT 'Manual';

CREATE INDEX IF NOT EXISTS "IX_UserTasks_Source" ON "UserTasks" ("Source");
COMMENT ON COLUMN "UserTasks"."Source" IS 'Task origin: Manual, SystemGenerated, etc.';
\echo 'Source column added successfully!'

-- Step 2: Add IsCompleted column
\echo ''
\echo '========================================='
\echo 'Step 2: Adding IsCompleted column...'
\echo '========================================='
ALTER TABLE "UserTasks" 
ADD COLUMN IF NOT EXISTS "IsCompleted" boolean NOT NULL DEFAULT false;

CREATE INDEX IF NOT EXISTS "IX_UserTasks_IsCompleted" ON "UserTasks" ("IsCompleted");
CREATE INDEX IF NOT EXISTS "IX_UserTasks_ProjectId_IsCompleted" ON "UserTasks" ("ProjectId", "IsCompleted");
COMMENT ON COLUMN "UserTasks"."IsCompleted" IS 'Indicates whether the task has been completed';
\echo 'IsCompleted column added successfully!'

-- Step 3: Add Status column
\echo ''
\echo '========================================='
\echo 'Step 3: Adding Status column...'
\echo '========================================='
ALTER TABLE "UserTasks" 
ADD COLUMN IF NOT EXISTS "Status" character varying(20) NOT NULL DEFAULT 'TO-DO';

-- Update existing tasks based on IsCompleted status (only if Status was just added)
DO $$
BEGIN
    -- Check if we need to update Status values
    IF EXISTS (SELECT 1 FROM "UserTasks" WHERE "Status" = 'TO-DO' AND "IsCompleted" = true) THEN
        UPDATE "UserTasks" 
        SET "Status" = CASE 
            WHEN "IsCompleted" = true THEN 'DONE' 
            ELSE 'TO-DO' 
        END;
    END IF;
END $$;

-- Add check constraint (drop first if exists to avoid errors)
DO $$
BEGIN
    ALTER TABLE "UserTasks" DROP CONSTRAINT IF EXISTS "CK_UserTasks_Status";
    ALTER TABLE "UserTasks"
    ADD CONSTRAINT "CK_UserTasks_Status" 
    CHECK ("Status" IN ('TO-DO', 'IN PROGRESS', 'IN REVIEW', 'BACKLOG', 'DONE'));
END $$;

CREATE INDEX IF NOT EXISTS "IX_UserTasks_Status" ON "UserTasks" ("Status");
CREATE INDEX IF NOT EXISTS "IX_UserTasks_ProjectId_Status" ON "UserTasks" ("ProjectId", "Status");
COMMENT ON COLUMN "UserTasks"."Status" IS 'Task workflow status: TO-DO, IN PROGRESS, IN REVIEW, BACKLOG, DONE';
\echo 'Status column added successfully!'

-- Verify the migration
\echo ''
\echo '========================================='
\echo 'Migration Summary'
\echo '========================================='
SELECT 
    column_name,
    data_type,
    character_maximum_length,
    column_default,
    is_nullable
FROM information_schema.columns
WHERE table_name = 'UserTasks'
    AND column_name IN ('Source', 'IsCompleted', 'Status')
ORDER BY column_name;

\echo ''
\echo '========================================='
\echo 'All migrations completed successfully!'
\echo 'You can now run the seed data script.'
\echo '========================================='
