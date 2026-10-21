-- Migration: Add Stripe Customer ID to User table
-- Date: 2026-02-08
-- Description: Adds StripeCustomerId column to User table for payment integration

-- Add StripeCustomerId column if it doesn't exist
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name='User' AND column_name='StripeCustomerId') THEN
        ALTER TABLE "User" ADD COLUMN "StripeCustomerId" VARCHAR(100);
    END IF;
END $$;

-- Create index for faster lookups
CREATE INDEX IF NOT EXISTS "IX_User_StripeCustomerId" ON "User"("StripeCustomerId");

-- Migration Complete
