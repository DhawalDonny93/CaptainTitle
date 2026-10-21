-- Helper script to get your Project UUID
-- Run this first to find your project's UUID, then use it in the seed data script

\echo '========================================='
\echo 'Available Projects'
\echo '========================================='

SELECT 
    "Id" as project_uuid,
    "Name" as project_name,
    "Domain" as domain,
    "CreatedAt" as created_at
FROM "Project"
ORDER BY "CreatedAt" DESC
LIMIT 10;

\echo ''
\echo '========================================='
\echo 'Instructions:'
\echo '1. Copy the UUID from the project you want to add tasks to'
\echo '2. Replace YOUR_PROJECT_UUID_HERE in user_tasks_seed.sql with that UUID'
\echo '3. Run: psql -U username -d dbname -f user_tasks_seed.sql'
\echo '========================================='
