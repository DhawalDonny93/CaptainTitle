-- Cleanup Script for Orphaned UserRole Records
-- Run this in Supabase SQL Editor to clean up duplicate UserRole entries

-- 1. Check for duplicate UserRole entries
SELECT ur."UserId", ur."Role", COUNT(*) as count
FROM "UserRole" ur
GROUP BY ur."UserId", ur."Role"
HAVING COUNT(*) > 1;

-- 2. Check for UserRoles without corresponding Users
SELECT ur."Id", ur."UserId", ur."Role"
FROM "UserRole" ur
LEFT JOIN "User" u ON ur."UserId" = u."Id"
WHERE u."Id" IS NULL;

-- 3. Delete duplicate UserRole entries (keeps the oldest one)
DELETE FROM "UserRole"
WHERE "Id" IN (
    SELECT "Id"
    FROM (
        SELECT "Id",
               ROW_NUMBER() OVER (PARTITION BY "UserId", "Role" ORDER BY "CreatedAt" ASC) as rn
        FROM "UserRole"
    ) t
    WHERE t.rn > 1
);

-- 4. Delete orphaned UserRoles (where User doesn't exist)
DELETE FROM "UserRole"
WHERE "UserId" NOT IN (SELECT "Id" FROM "User");

-- 5. Verify cleanup - should return no rows
SELECT ur."UserId", ur."Role", COUNT(*) as count
FROM "UserRole" ur
GROUP BY ur."UserId", ur."Role"
HAVING COUNT(*) > 1;

-- 6. Show all UserRoles for verification
SELECT ur."Id", ur."UserId", ur."Role", ur."CreatedAt", u."Email"
FROM "UserRole" ur
JOIN "User" u ON ur."UserId" = u."Id"
ORDER BY ur."CreatedAt" DESC
LIMIT 20;
