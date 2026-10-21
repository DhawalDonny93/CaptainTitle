-- ============================================================================
-- Fix PromptResearchData Table Name
-- Date: 2026-02-09
-- Description: Rename PromptResearchData to PromptResearchDatas to match EF Core
-- ============================================================================

BEGIN;

-- Check if old singular table exists and rename it
DO $$ 
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'PromptResearchData') THEN
        IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'PromptResearchDatas') THEN
            ALTER TABLE "PromptResearchData" RENAME TO "PromptResearchDatas";
            RAISE NOTICE 'Renamed PromptResearchData to PromptResearchDatas';
        ELSE
            RAISE WARNING 'Both PromptResearchData and PromptResearchDatas exist - manual intervention required';
        END IF;
    ELSIF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'PromptResearchDatas') THEN
        -- Create the table if it doesn't exist
        CREATE TABLE "PromptResearchDatas" (
            "Id" SERIAL PRIMARY KEY,
            "ProjectId" INTEGER NOT NULL,
            "Topic" VARCHAR(255) NOT NULL,
            "PromptText" TEXT NOT NULL,
            "EstimatedVolume" INTEGER,
            "Difficulty" VARCHAR(50),
            "RelevanceScore" DOUBLE PRECISION,
            "DiscoveredAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
            "Status" VARCHAR(50) NOT NULL DEFAULT 'New',
            "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
            CONSTRAINT "FK_PromptResearchDatas_Projects_ProjectId" FOREIGN KEY ("ProjectId") 
                REFERENCES "Projects"("Id") ON DELETE CASCADE
        );
        
        CREATE INDEX "IX_PromptResearchDatas_ProjectId" ON "PromptResearchDatas"("ProjectId");
        CREATE INDEX "IX_PromptResearchDatas_Topic" ON "PromptResearchDatas"("Topic");
        CREATE INDEX "IX_PromptResearchDatas_Status" ON "PromptResearchDatas"("Status");
        
        RAISE NOTICE 'Created PromptResearchDatas table';
    ELSE
        RAISE NOTICE 'PromptResearchDatas table already exists';
    END IF;
END $$;

-- Drop old singular table indexes if they exist
DROP INDEX IF EXISTS "IX_PromptResearchData_ProjectId";
DROP INDEX IF EXISTS "IX_PromptResearchData_Topic";
DROP INDEX IF EXISTS "IX_PromptResearchData_Status";

COMMIT;

-- Verification
SELECT 'PromptResearchDatas Table:' as info;
SELECT column_name, data_type, is_nullable
FROM information_schema.columns 
WHERE table_name = 'PromptResearchDatas'
ORDER BY ordinal_position;
