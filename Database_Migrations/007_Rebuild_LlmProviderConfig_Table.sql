-- ============================================
-- Rebuild LlmProviderConfig Table with Correct Schema
-- ============================================
-- Multiple columns are missing or incorrectly named
-- This script will drop and recreate the table with the correct structure

-- Step 1: Check current table structure
SELECT 
    column_name, 
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns 
WHERE table_name = 'LlmProviderConfig'
ORDER BY ordinal_position;

-- Step 2: Backup existing data (if any)
-- Uncomment if you have data to preserve:
-- CREATE TABLE "LlmProviderConfig_Backup" AS SELECT * FROM "LlmProviderConfig";

-- Step 3: Drop the existing table
DROP TABLE IF EXISTS "LlmProviderConfig" CASCADE;

-- Step 4: Recreate table with correct schema
CREATE TABLE "LlmProviderConfig" (
    "Id" SERIAL PRIMARY KEY,
    "ProviderId" INTEGER NOT NULL,
    "ConfigKey" VARCHAR(100) NOT NULL,
    "ConfigValue" TEXT,
    "IsEncrypted" BOOLEAN NOT NULL DEFAULT FALSE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_LlmProviderConfig_Provider" FOREIGN KEY ("ProviderId") 
        REFERENCES "LlmProvider"("Id") ON DELETE CASCADE
);

-- Step 5: Create indexes
CREATE INDEX "IX_LlmProviderConfig_ProviderId" ON "LlmProviderConfig"("ProviderId");
CREATE UNIQUE INDEX "IX_LlmProviderConfig_Unique" ON "LlmProviderConfig"("ProviderId", "ConfigKey");

-- Step 6: Verify the new structure
SELECT 
    column_name, 
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns 
WHERE table_name = 'LlmProviderConfig'
ORDER BY ordinal_position;

-- Step 7: Verify foreign key constraint
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
    AND tc.table_name = 'LlmProviderConfig';

-- Expected columns:
-- Id           | integer   | NO  | nextval(...)
-- ProviderId   | integer   | NO  | 
-- ConfigKey    | varchar   | NO  |
-- ConfigValue  | text      | YES |
-- IsEncrypted  | boolean   | NO  | false
-- CreatedAt    | timestamp | NO  | CURRENT_TIMESTAMP
-- UpdatedAt    | timestamp | NO  | CURRENT_TIMESTAMP
