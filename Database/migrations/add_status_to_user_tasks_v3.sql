-- Migration: Add Status column to UserTasks table (Version 3 - mixed case with quotes)
-- Date: 2026-02-01
-- Description: Adds Status field to support task workflow (TO-DO, IN PROGRESS, IN REVIEW, BACKLOG, DONE)

-- Try with exact Entity Framework column names (quoted to preserve case)
ALTER TABLE "UserTasks" 
ADD COLUMN "Status" character varying(20) NOT NULL DEFAULT 'TO-DO';

-- First, let's check if the column exists - if this fails, the column name is different
-- Update existing tasks - try different column name variations
DO $$
BEGIN
    -- Try with quoted PascalCase
    IF EXISTS (SELECT 1 FROM information_schema.columns 
               WHERE table_name = 'UserTasks' AND column_name = 'IsCompleted') THEN
        UPDATE "UserTasks" 
        SET "Status" = CASE 
            WHEN "IsCompleted" = true THEN 'DONE' 
            ELSE 'TO-DO' 
        END;
    -- Try with lowercase
    ELSIF EXISTS (SELECT 1 FROM information_schema.columns 
                  WHERE table_name = 'UserTasks' AND column_name = 'iscompleted') THEN
        UPDATE "UserTasks" 
        SET "Status" = CASE 
            WHEN iscompleted = true THEN 'DONE' 
            ELSE 'TO-DO' 
        END;
    END IF;
END $$;

-- Add check constraint to ensure valid status values
ALTER TABLE "UserTasks"
ADD CONSTRAINT "CK_UserTasks_Status" 
CHECK ("Status" IN ('TO-DO', 'IN PROGRESS', 'IN REVIEW', 'BACKLOG', 'DONE'));

-- Create index on Status for better query performance
CREATE INDEX "IX_UserTasks_Status" ON "UserTasks" ("Status");

-- Create composite index for common queries
CREATE INDEX "IX_UserTasks_ProjectId_Status" ON "UserTasks" ("ProjectId", "Status");

COMMENT ON COLUMN "UserTasks"."Status" IS 'Task workflow status: TO-DO, IN PROGRESS, IN REVIEW, BACKLOG, DONE';
