-- Rollback Script: Remove Status column from UserTasks table
-- Date: 2026-02-01
-- Description: Rollback migration that added Status field

-- Drop indexes
DROP INDEX IF EXISTS "IX_UserTasks_ProjectId_Status";
DROP INDEX IF EXISTS "IX_UserTasks_Status";

-- Drop check constraint
ALTER TABLE "UserTasks" DROP CONSTRAINT IF EXISTS "CK_UserTasks_Status";

-- Drop Status column
ALTER TABLE "UserTasks" DROP COLUMN IF EXISTS "Status";
