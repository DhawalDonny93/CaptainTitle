-- Fix for LlmProviderConfig ProviderId column issue
-- The error indicates the column might be named "Provider" instead of "ProviderId"

-- First, check what columns actually exist
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name = 'LlmProviderConfig';

-- If the column is named "Provider", rename it to "ProviderId"
-- Uncomment and run this if needed:
-- ALTER TABLE "LlmProviderConfig" RENAME COLUMN "Provider" TO "ProviderId";

-- If the column doesn't exist at all, add it:
-- Uncomment and run this if needed:
-- ALTER TABLE "LlmProviderConfig" ADD COLUMN "ProviderId" INTEGER NOT NULL;
-- ALTER TABLE "LlmProviderConfig" ADD CONSTRAINT "FK_LlmProviderConfig_Provider" 
--     FOREIGN KEY ("ProviderId") REFERENCES "LlmProvider"("Id") ON DELETE CASCADE;
-- CREATE INDEX "IX_LlmProviderConfig_ProviderId" ON "LlmProviderConfig"("ProviderId");

-- Verify the fix
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name = 'LlmProviderConfig'
ORDER BY ordinal_position;
