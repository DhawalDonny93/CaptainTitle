-- ============================================
-- Add IsFirstLogin column to Users table
-- ============================================

-- Step 1: Add IsFirstLogin column (default TRUE for new users)
ALTER TABLE "Users" 
ADD COLUMN "IsFirstLogin" BOOLEAN NOT NULL DEFAULT TRUE;

-- Step 2: Set existing users to FALSE (they've already logged in)
UPDATE "Users" 
SET "IsFirstLogin" = FALSE 
WHERE "CreatedAt" < NOW();

-- Step 3: Verify the column was added
SELECT 
    column_name,
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns 
WHERE table_name = 'Users'
    AND column_name = 'IsFirstLogin';

-- Step 4: Sample data check
SELECT 
    "Id",
    "Email",
    "IsFirstLogin",
    "OnboardingCompleted",
    "CreatedAt"
FROM "Users"
LIMIT 10;
