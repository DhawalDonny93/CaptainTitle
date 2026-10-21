-- Migration: Add Status column to UserTasks table
-- Date: 2026-02-01
-- Description: Adds Status field to support task workflow (TO-DO, IN PROGRESS, IN REVIEW, BACKLOG, DONE)

-- Add Status column to UserTasks table (using exact PostgreSQL column naming)
ALTER TABLE "UserTasks" 
ADD COLUMN "Status" character varying(20) NOT NULL DEFAULT 'TO-DO';

-- Update existing tasks based on IsCompleted status (use exact case-sensitive column names)
UPDATE "UserTasks" 
SET "Status" = CASE 
    WHEN "IsCompleted" = true THEN 'DONE' 
    ELSE 'TO-DO' 
END;

-- Add check constraint to ensure valid status values
ALTER TABLE "UserTasks"
ADD CONSTRAINT "CK_UserTasks_Status" 
CHECK ("Status" IN ('TO-DO', 'IN PROGRESS', 'IN REVIEW', 'BACKLOG', 'DONE'));

-- Create index on Status for better query performance
CREATE INDEX "IX_UserTasks_Status" ON "UserTasks" ("Status");

-- Create composite index for common queries
CREATE INDEX "IX_UserTasks_ProjectId_Status" ON "UserTasks" ("ProjectId", "Status");

COMMENT ON COLUMN "UserTasks"."Status" IS 'Task workflow status: TO-DO, IN PROGRESS, IN REVIEW, BACKLOG, DONE';
