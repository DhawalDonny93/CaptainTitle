-- ============================================
-- Fix ProviderId Data Type Mismatch
-- ============================================
-- Error: operator does not exist: integer = character varying
-- Root Cause: ProviderId column is VARCHAR but should be INTEGER

-- Step 1: Check current data type
SELECT 
    column_name, 
    data_type,
    character_maximum_length,
    is_nullable
FROM information_schema.columns 
WHERE table_name = 'LlmProviderConfig'
    AND column_name = 'ProviderId';

-- Step 2: Check if there's data in the table
SELECT COUNT(*) as row_count FROM "LlmProviderConfig";

-- Step 3: Drop foreign key constraint (will be recreated)
ALTER TABLE "LlmProviderConfig" 
    DROP CONSTRAINT IF EXISTS "FK_LlmProviderConfig_Provider";

-- Step 4: Drop indexes
DROP INDEX IF EXISTS "IX_LlmProviderConfig_ProviderId";
DROP INDEX IF EXISTS "IX_LlmProviderConfig_Unique";

-- Step 5: Convert ProviderId from VARCHAR to INTEGER
-- If table has data, this will try to cast existing values
ALTER TABLE "LlmProviderConfig" 
    ALTER COLUMN "ProviderId" TYPE INTEGER USING "ProviderId"::INTEGER;

-- Step 6: Recreate foreign key constraint
ALTER TABLE "LlmProviderConfig" 
    ADD CONSTRAINT "FK_LlmProviderConfig_Provider" 
    FOREIGN KEY ("ProviderId") REFERENCES "LlmProvider"("Id") ON DELETE CASCADE;

-- Step 7: Recreate indexes
CREATE INDEX "IX_LlmProviderConfig_ProviderId" ON "LlmProviderConfig"("ProviderId");
CREATE UNIQUE INDEX "IX_LlmProviderConfig_Unique" ON "LlmProviderConfig"("ProviderId", "ConfigKey");

-- Step 8: Verify the fix
SELECT 
    column_name, 
    data_type,
    is_nullable
FROM information_schema.columns 
WHERE table_name = 'LlmProviderConfig'
    AND column_name = 'ProviderId';

-- Expected result: data_type should be 'integer', not 'character varying'

-- Step 9: Verify foreign key exists
SELECT
    tc.constraint_name,
    kcu.column_name,
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name
FROM information_schema.table_constraints AS tc
JOIN information_schema.key_column_usage AS kcu
    ON tc.constraint_name = kcu.constraint_name
JOIN information_schema.constraint_column_usage AS ccu
    ON ccu.constraint_name = tc.constraint_name
WHERE tc.constraint_type = 'FOREIGN KEY' 
    AND tc.table_name = 'LlmProviderConfig'
    AND kcu.column_name = 'ProviderId';
