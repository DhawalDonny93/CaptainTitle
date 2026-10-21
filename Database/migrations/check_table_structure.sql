-- Helper script to check the actual column names in UserTasks table
-- Run this to see the exact column names PostgreSQL is using

SELECT 
    column_name,
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_name = 'UserTasks'
ORDER BY ordinal_position;

-- Alternative query to see table structure
SELECT * FROM "UserTasks" LIMIT 0;
