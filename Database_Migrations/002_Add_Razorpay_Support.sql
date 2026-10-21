-- ============================================================================
-- RAZORPAY PAYMENT GATEWAY MIGRATION
-- Date: 2026-03-29
-- Description: Add Razorpay support and migrate from Stripe
-- ============================================================================

BEGIN;

-- Add Razorpay columns to Users table
ALTER TABLE "Users" 
ADD COLUMN IF NOT EXISTS "RazorpayCustomerId" VARCHAR(255);

-- Add Razorpay columns to Subscriptions table  
ALTER TABLE "Subscriptions"
ADD COLUMN IF NOT EXISTS "RazorpaySubscriptionId" VARCHAR(255),
ADD COLUMN IF NOT EXISTS "RazorpayCustomerId" VARCHAR(255),
ADD COLUMN IF NOT EXISTS "RazorpayPlanId" VARCHAR(255),
ADD COLUMN IF NOT EXISTS "RazorpayOrderId" VARCHAR(255);

-- Add indexes for faster lookups
CREATE INDEX IF NOT EXISTS "IX_Users_RazorpayCustomerId" 
ON "Users"("RazorpayCustomerId");

CREATE INDEX IF NOT EXISTS "IX_Subscriptions_RazorpaySubscriptionId" 
ON "Subscriptions"("RazorpaySubscriptionId");

CREATE INDEX IF NOT EXISTS "IX_Subscriptions_RazorpayOrderId" 
ON "Subscriptions"("RazorpayOrderId");

-- Create SubscriptionTransactions table for payment tracking
CREATE TABLE IF NOT EXISTS "SubscriptionTransactions" (
    "Id" SERIAL PRIMARY KEY,
    "SubscriptionId" INTEGER NOT NULL,
    "TransactionType" VARCHAR(50) NOT NULL, -- 'Payment', 'Refund', 'Failed'
    "Amount" DECIMAL(18,2) NOT NULL,
    "Currency" VARCHAR(3) NOT NULL DEFAULT 'INR',
    "Status" VARCHAR(50) NOT NULL, -- 'Pending', 'Success', 'Failed', 'Refunded'
    "TransactionDate" TIMESTAMP WITH TIME ZONE NOT NULL,
    "RazorpayPaymentId" VARCHAR(255),
    "RazorpayOrderId" VARCHAR(255),
    "RazorpayInvoiceId" VARCHAR(255),
    "RazorpaySignature" VARCHAR(500),
    "ErrorCode" VARCHAR(100),
    "ErrorDescription" TEXT,
    "Metadata" JSONB,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_SubscriptionTransactions_Subscriptions" 
        FOREIGN KEY ("SubscriptionId") REFERENCES "Subscriptions"("Id") ON DELETE CASCADE
);

-- Add indexes for SubscriptionTransactions
CREATE INDEX IF NOT EXISTS "IX_SubscriptionTransactions_SubscriptionId" 
ON "SubscriptionTransactions"("SubscriptionId");

CREATE INDEX IF NOT EXISTS "IX_SubscriptionTransactions_RazorpayPaymentId" 
ON "SubscriptionTransactions"("RazorpayPaymentId");

CREATE INDEX IF NOT EXISTS "IX_SubscriptionTransactions_RazorpayOrderId" 
ON "SubscriptionTransactions"("RazorpayOrderId");

CREATE INDEX IF NOT EXISTS "IX_SubscriptionTransactions_Status" 
ON "SubscriptionTransactions"("Status");

CREATE INDEX IF NOT EXISTS "IX_SubscriptionTransactions_TransactionDate" 
ON "SubscriptionTransactions"("TransactionDate" DESC);

-- Add trigger to update UpdatedAt timestamp
CREATE OR REPLACE FUNCTION update_subscription_transactions_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW."UpdatedAt" = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_update_subscription_transactions_updated_at
    BEFORE UPDATE ON "SubscriptionTransactions"
    FOR EACH ROW
    EXECUTE FUNCTION update_subscription_transactions_updated_at();

COMMIT;

-- Verification queries
-- Run these after migration to verify success

-- Check if columns were added
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name = 'Users' 
AND column_name = 'RazorpayCustomerId';

SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name = 'Subscriptions' 
AND column_name IN ('RazorpaySubscriptionId', 'RazorpayCustomerId', 'RazorpayPlanId', 'RazorpayOrderId');

-- Check if SubscriptionTransactions table was created
SELECT table_name 
FROM information_schema.tables 
WHERE table_name = 'SubscriptionTransactions';

-- Check indexes
SELECT indexname 
FROM pg_indexes 
WHERE tablename IN ('Users', 'Subscriptions', 'SubscriptionTransactions')
AND indexname LIKE '%Razorpay%';
