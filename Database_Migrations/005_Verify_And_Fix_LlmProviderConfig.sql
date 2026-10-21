-- ============================================
-- Verify and Fix LlmProviderConfig Column Issue
-- ============================================
-- Error: column l0.ProviderId does not exist
-- Hint: Perhaps you meant to reference the column "l0.Provider"
-- This suggests the column might be named "Provider" instead of "ProviderId"

-- Step 1: Check current column structure
SELECT 
    column_name, 
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns 
WHERE table_name = 'LlmProviderConfig'
ORDER BY ordinal_position;

-- Step 2: Check if foreign key constraint exists
SELECT
    tc.constraint_name,
    tc.table_name,
    kcu.column_name,
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name
FROM information_schema.table_constraints AS tc
JOIN information_schema.key_column_usage AS kcu
    ON tc.constraint_name = kcu.constraint_name
    AND tc.table_schema = kcu.table_schema
JOIN information_schema.constraint_column_usage AS ccu
    ON ccu.constraint_name = tc.constraint_name
    AND ccu.table_schema = tc.table_schema
WHERE tc.constraint_type = 'FOREIGN KEY' 
    AND tc.table_name = 'LlmProviderConfig';

-- Step 3: If column is named "Provider", rename it to "ProviderId"
-- ONLY RUN THIS IF THE COLUMN IS NAMED "Provider"
-- Uncomment the following lines if needed:

-- DROP INDEX IF EXISTS "IX_LlmProviderConfig_ProviderId";
-- DROP INDEX IF EXISTS "IX_LlmProviderConfig_Unique";
-- ALTER TABLE "LlmProviderConfig" DROP CONSTRAINT IF EXISTS "FK_LlmProviderConfig_Provider";
-- 
-- ALTER TABLE "LlmProviderConfig" RENAME COLUMN "Provider" TO "ProviderId";
-- 
-- ALTER TABLE "LlmProviderConfig" 
--     ADD CONSTRAINT "FK_LlmProviderConfig_Provider" 
--     FOREIGN KEY ("ProviderId") REFERENCES "LlmProvider"("Id") ON DELETE CASCADE;
-- 
-- CREATE INDEX "IX_LlmProviderConfig_ProviderId" ON "LlmProviderConfig"("ProviderId");
-- CREATE UNIQUE INDEX "IX_LlmProviderConfig_Unique" ON "LlmProviderConfig"("ProviderId", "ConfigKey");

-- Step 4: If column doesn't exist at all, add it
-- ONLY RUN THIS IF THE COLUMN DOESN'T EXIST
-- Uncomment the following lines if needed:

-- ALTER TABLE "LlmProviderConfig" ADD COLUMN "ProviderId" INTEGER NOT NULL DEFAULT 1;
-- 
-- ALTER TABLE "LlmProviderConfig" 
--     ADD CONSTRAINT "FK_LlmProviderConfig_Provider" 
--     FOREIGN KEY ("ProviderId") REFERENCES "LlmProvider"("Id") ON DELETE CASCADE;
-- 
-- CREATE INDEX "IX_LlmProviderConfig_ProviderId" ON "LlmProviderConfig"("ProviderId");
-- CREATE UNIQUE INDEX "IX_LlmProviderConfig_Unique" ON "LlmProviderConfig"("ProviderId", "ConfigKey");

-- Step 5: Verify the fix
SELECT 
    column_name, 
    data_type,
    is_nullable
FROM information_schema.columns 
WHERE table_name = 'LlmProviderConfig'
    AND column_name IN ('ProviderId', 'Provider')
ORDER BY ordinal_position;

-- Expected result: Should see "ProviderId" column, not "Provider"
