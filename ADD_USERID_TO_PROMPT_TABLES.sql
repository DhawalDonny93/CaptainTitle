-- ============================================
-- Add UserId to PromptPlatformTracking and PromptVisibilityMetrics Tables
-- ============================================

-- Step 1: Add UserId column to PromptPlatformTrackings table
ALTER TABLE "PromptPlatformTrackings" 
ADD COLUMN "UserId" INTEGER;

-- Step 2: Populate UserId from PromptTrackings table
UPDATE "PromptPlatformTrackings" ppt
SET "UserId" = pt."UserId"
FROM "PromptTrackings" pt
WHERE ppt."PromptId" = pt."Id";

-- Step 3: Make UserId NOT NULL after population
ALTER TABLE "PromptPlatformTrackings" 
ALTER COLUMN "UserId" SET NOT NULL;

-- Step 4: Add foreign key constraint
ALTER TABLE "PromptPlatformTrackings"
ADD CONSTRAINT "FK_PromptPlatformTrackings_User"
FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE;

-- Step 5: Create index on UserId
CREATE INDEX "IX_PromptPlatformTrackings_UserId" ON "PromptPlatformTrackings"("UserId");

-- ============================================
-- Add UserId to PromptVisibilityMetrics table
-- ============================================

-- Step 6: Add UserId column to PromptVisibilityMetrics table
ALTER TABLE "PromptVisibilityMetrics" 
ADD COLUMN "UserId" INTEGER;

-- Step 7: Populate UserId from PromptPlatformTrackings table
UPDATE "PromptVisibilityMetrics" pvm
SET "UserId" = ppt."UserId"
FROM "PromptPlatformTrackings" ppt
WHERE pvm."PromptPlatformId" = ppt."Id";

-- Step 8: Make UserId NOT NULL after population
ALTER TABLE "PromptVisibilityMetrics" 
ALTER COLUMN "UserId" SET NOT NULL;

-- Step 9: Add foreign key constraint
ALTER TABLE "PromptVisibilityMetrics"
ADD CONSTRAINT "FK_PromptVisibilityMetrics_User"
FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE;

-- Step 10: Create index on UserId
CREATE INDEX "IX_PromptVisibilityMetrics_UserId" ON "PromptVisibilityMetrics"("UserId");

-- ============================================
-- Verification
-- ============================================

-- Verify PromptPlatformTrackings
SELECT 
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns 
WHERE table_name = 'PromptPlatformTrackings'
    AND column_name = 'UserId';

-- Verify PromptVisibilityMetrics
SELECT 
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns 
WHERE table_name = 'PromptVisibilityMetrics'
    AND column_name = 'UserId';

-- Check foreign key constraints
SELECT
    tc.table_name,
    kcu.column_name,
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name
FROM information_schema.table_constraints AS tc
JOIN information_schema.key_column_usage AS kcu
    ON tc.constraint_name = kcu.constraint_name
JOIN information_schema.constraint_column_usage AS ccu
    ON ccu.constraint_name = tc.constraint_name
WHERE tc.constraint_type = 'FOREIGN KEY' 
    AND (tc.table_name = 'PromptPlatformTrackings' OR tc.table_name = 'PromptVisibilityMetrics')
    AND kcu.column_name = 'UserId';

-- Sample data check
SELECT 
    ppt."Id",
    ppt."PromptId",
    ppt."UserId",
    ppt."Platform",
    pt."UserId" as "PromptUserId"
FROM "PromptPlatformTrackings" ppt
JOIN "PromptTrackings" pt ON ppt."PromptId" = pt."Id"
LIMIT 5;

SELECT 
    pvm."Id",
    pvm."PromptPlatformId",
    pvm."UserId",
    ppt."UserId" as "PlatformUserId"
FROM "PromptVisibilityMetrics" pvm
JOIN "PromptPlatformTrackings" ppt ON pvm."PromptPlatformId" = ppt."Id"
LIMIT 5;
