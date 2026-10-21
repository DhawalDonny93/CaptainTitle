-- Migration: Add IsCompleted column to UserTasks table
-- Date: 2026-02-01
-- Description: Adds IsCompleted boolean field to track task completion status

-- Add IsCompleted column to UserTasks table
ALTER TABLE "UserTasks" 
ADD COLUMN "IsCompleted" boolean NOT NULL DEFAULT false;

-- Create index on IsCompleted for better query performance
CREATE INDEX "IX_UserTasks_IsCompleted" ON "UserTasks" ("IsCompleted");

-- Create composite index for common queries (ProjectId + IsCompleted)
CREATE INDEX "IX_UserTasks_ProjectId_IsCompleted" ON "UserTasks" ("ProjectId", "IsCompleted");

COMMENT ON COLUMN "UserTasks"."IsCompleted" IS 'Indicates whether the task has been completed';
