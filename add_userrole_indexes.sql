-- Add indexes to UserRole table for better query performance
-- Run this in Supabase SQL Editor to improve performance

-- Add index on UserId for faster lookups
CREATE INDEX IF NOT EXISTS "IX_UserRole_UserId" ON "UserRole" ("UserId");

-- Add index on Role for faster role-based queries
CREATE INDEX IF NOT EXISTS "IX_UserRole_Role" ON "UserRole" ("Role");

-- Verify indexes were created
SELECT 
    tablename,
    indexname,
    indexdef
FROM pg_indexes
WHERE tablename = 'UserRole'
ORDER BY indexname;
