-- =====================================================
-- QUICK FIX: Add Missing User Table Columns
-- Run this immediately to fix the current error
-- =====================================================

-- Add missing columns to User table
ALTER TABLE "User" 
ADD COLUMN IF NOT EXISTS "FirstName" VARCHAR(30) NULL,
ADD COLUMN IF NOT EXISTS "PhoneNumber" VARCHAR(20) NULL,
ADD COLUMN IF NOT EXISTS "Gender" VARCHAR(100) NULL,
ADD COLUMN IF NOT EXISTS "OnboardingCompleted" BOOLEAN NOT NULL DEFAULT FALSE,
ADD COLUMN IF NOT EXISTS "ProfilePhotoUrl" VARCHAR(500) NULL,
ADD COLUMN IF NOT EXISTS "ProfilePhotoThumbnailUrl" VARCHAR(500) NULL;

-- Verify columns were added
SELECT column_name, data_type, is_nullable 
FROM information_schema.columns 
WHERE table_name = 'User' 
AND column_name IN ('FirstName', 'PhoneNumber', 'Gender', 'OnboardingCompleted', 'ProfilePhotoUrl', 'ProfilePhotoThumbnailUrl')
ORDER BY column_name;

-- Success message
SELECT 'Missing columns added successfully!' AS status;
