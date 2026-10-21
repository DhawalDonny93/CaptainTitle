-- Migration: Add Source column to UserTasks table
-- Date: 2026-02-01
-- Description: Adds Source field to track task origin (Manual, SystemGenerated, etc.)

-- Add Source column to UserTasks table
ALTER TABLE "UserTasks" 
ADD COLUMN "Source" character varying(50) NOT NULL DEFAULT 'Manual';

-- Create index on Source for better query performance
CREATE INDEX "IX_UserTasks_Source" ON "UserTasks" ("Source");

COMMENT ON COLUMN "UserTasks"."Source" IS 'Task origin: Manual, SystemGenerated, etc.';
