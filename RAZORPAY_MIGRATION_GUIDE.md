# Razorpay Payment Gateway Migration Guide
**Date:** March 29, 2026  
**Migration:** Stripe → Razorpay

---

## ✅ COMPLETED CHANGES

### 1. **Database Migration**
**File:** `Database_Migrations/002_Add_Razorpay_Support.sql`

**Changes:**
- Added `RazorpayCustomerId` to `Users` table
- Added Razorpay columns to `Subscriptions` table:
  - `RazorpaySubscriptionId`
  - `RazorpayCustomerId`
  - `RazorpayPlanId`
  - `RazorpayOrderId`
- Created `SubscriptionTransactions` table for payment tracking
- Added indexes for performance
- Added update trigger for `UpdatedAt` timestamp

**Run Migration:**
```bash
psql -h your-db-host -U your-user -d your-database -f Database_Migrations/002_Add_Razorpay_Support.sql
```

---

### 2. **Backend Service Implementation**
**File:** `AppBackendBasic.Infrastructure/Services/RazorpayPaymentService.cs`

**Features Implemented:**
- ✅ Create checkout sessions for subscriptions
- ✅ Create payment intents for one-time payments
- ✅ Customer portal session (custom implementation)
- ✅ Webhook handling for all Razorpay events:
  - `subscription.activated`
  - `subscription.charged`
  - `subscription.completed`
  - `subscription.cancelled`
  - `subscription.paused`
  - `subscription.resumed`
  - `payment.authorized`
  - `payment.captured`
  - `payment.failed`
  - `order.paid`
- ✅ Webhook signature verification
- ✅ Customer creation and management
- ✅ Transaction logging

---

### 3. **Webhook Controller**
**File:** `AppBackendBasic.API/Controllers/RazorpayWebhookController.cs`

**Endpoint:** `POST /api/webhooks/razorpay`

**Features:**
- Receives Razorpay webhook events
- Verifies webhook signature from `X-Razorpay-Signature` header
- Processes events asynchronously
- Returns appropriate HTTP status codes

---

### 4. **Configuration Updates**

**Development Config:** `AppBackendBasic.API/appsettings.json`
```json
"Razorpay": {
  "KeyId": "YOUR_RAZORPAY_KEY_ID",
  "KeySecret": "YOUR_RAZORPAY_KEY_SECRET",
  "WebhookSecret": "YOUR_RAZORPAY_WEBHOOK_SECRET"
}
```

**Production Config:** `appsettings.Production.json`
```json
"Razorpay": {
  "KeyId": "#{RazorpayKeyId}#",
  "KeySecret": "#{RazorpayKeySecret}#",
  "WebhookSecret": "#{RazorpayWebhookSecret}#"
}
```

---

### 5. **Service Registration**
**File:** `AppBackendBasic.API/Program.cs`

**Changed Line 332:**
```csharp
// OLD:
builder.Services.AddScoped<IPaymentService, AppBackendBasic.Infrastructure.Services.StripePaymentService>();

// NEW:
builder.Services.AddScoped<IPaymentService, AppBackendBasic.Infrastructure.Services.RazorpayPaymentService>();
```

---

### 6. **Frontend Service**
**File:** `AppBackendBasic.WebUI/src/services/razorpayService.ts`

**Features:**
- Razorpay SDK loader
- Checkout session creation
- Subscription checkout flow
- Payment verification
- Customer portal navigation
- Subscription management

---

## 🔧 REQUIRED SETUP STEPS

### **Step 1: Install Razorpay NuGet Package**

```bash
cd AppBackendBasic.Infrastructure
dotnet add package Razorpay
```

**Package Details:**
- Package: `Razorpay`
- Version: Latest stable
- Documentation: https://razorpay.com/docs/api/

---

### **Step 2: Create Razorpay Account**

1. Go to https://razorpay.com/
2. Sign up for an account
3. Complete KYC verification (required for live mode)
4. Navigate to Settings → API Keys

**Get Your Keys:**
- **Test Mode:**
  - Key ID: `rzp_test_XXXXX`
  - Key Secret: `XXXXX`
- **Live Mode:**
  - Key ID: `rzp_live_XXXXX`
  - Key Secret: `XXXXX`

---

### **Step 3: Create Razorpay Plans**

**Via Razorpay Dashboard:**
1. Navigate to Subscriptions → Plans
2. Create plans matching your pricing:

**Premium Monthly Plan:**
- Plan Name: `Premium Monthly`
- Billing Interval: `1 month`
- Amount: `₹999`
- Currency: `INR`
- Plan ID: Copy this (e.g., `plan_XXXXX`)

**Premium Yearly Plan:**
- Plan Name: `Premium Yearly`
- Billing Interval: `1 year`
- Amount: `₹9,599`
- Currency: `INR`
- Plan ID: Copy this

**Update in Code:**
Edit `RazorpayPaymentService.cs` line 244-282 with actual plan IDs:
```csharp
RazorpayPlanId = "plan_XXXXX", // Replace with actual Razorpay plan ID
```

---

### **Step 4: Configure Webhooks**

**In Razorpay Dashboard:**
1. Navigate to Settings → Webhooks
2. Click "Add New Webhook"
3. Enter webhook URL: `https://yourdomain.com/api/webhooks/razorpay`
4. Select events to listen to:
   - ✅ `subscription.activated`
   - ✅ `subscription.charged`
   - ✅ `subscription.completed`
   - ✅ `subscription.cancelled`
   - ✅ `subscription.paused`
   - ✅ `subscription.resumed`
   - ✅ `payment.authorized`
   - ✅ `payment.captured`
   - ✅ `payment.failed`
   - ✅ `order.paid`
5. Generate webhook secret
6. Copy the secret and add to configuration

---

### **Step 5: Update Configuration Files**

**Development (`appsettings.json`):**
```json
"Razorpay": {
  "KeyId": "rzp_test_XXXXX",
  "KeySecret": "YOUR_TEST_SECRET",
  "WebhookSecret": "YOUR_WEBHOOK_SECRET"
}
```

**Production (Environment Variables):**
```bash
export RAZORPAY_KEY_ID="rzp_live_XXXXX"
export RAZORPAY_KEY_SECRET="YOUR_LIVE_SECRET"
export RAZORPAY_WEBHOOK_SECRET="YOUR_WEBHOOK_SECRET"
```

---

### **Step 6: Run Database Migration**

```bash
# Connect to your database
psql -h your-db-host -U your-user -d your-database

# Run migration
\i Database_Migrations/002_Add_Razorpay_Support.sql

# Verify tables were created
SELECT table_name FROM information_schema.tables 
WHERE table_name = 'SubscriptionTransactions';

# Verify columns were added
SELECT column_name FROM information_schema.columns 
WHERE table_name = 'Users' AND column_name = 'RazorpayCustomerId';
```

---

### **Step 7: Update Frontend Environment**

**Create/Update `.env.production`:**
```bash
VITE_RAZORPAY_KEY_ID=rzp_live_XXXXX
VITE_API_URL=https://yourdomain.com/api
```

**For Development (`.env.development`):**
```bash
VITE_RAZORPAY_KEY_ID=rzp_test_XXXXX
VITE_API_URL=http://localhost:5000/api
```

---

### **Step 8: Update Frontend Payment Flow**

**Replace Stripe references with Razorpay:**

**In `Pricing.tsx` or payment pages:**
```typescript
import razorpayService from '../services/razorpayService';

// OLD:
// import paymentService from '../services/paymentService';

// NEW:
const handleSubscribe = async (priceId: string) => {
  try {
    await razorpayService.createSubscriptionCheckout(
      priceId,
      selectedProject.id,
      {
        name: user.name,
        email: user.email,
        contact: user.phoneNumber || ''
      }
    );
  } catch (error) {
    console.error('Payment error:', error);
  }
};
```

---

## 🧪 TESTING GUIDE

### **Test Mode Setup**

1. Use test API keys (`rzp_test_XXXXX`)
2. Use test card numbers from Razorpay docs
3. Test webhook events using Razorpay dashboard

**Test Card Numbers:**
- **Success:** `4111 1111 1111 1111`
- **Failure:** `4000 0000 0000 0002`
- **CVV:** Any 3 digits
- **Expiry:** Any future date

---

### **Test Scenarios**

#### **1. Subscription Creation**
```bash
# Expected flow:
1. User clicks "Subscribe" button
2. Frontend calls createSubscriptionCheckout()
3. Razorpay checkout modal opens
4. User enters payment details
5. Payment succeeds
6. Webhook: subscription.activated received
7. Database: Subscription record created
8. User redirected to success page
```

#### **2. Recurring Payment**
```bash
# Expected flow:
1. Razorpay charges subscription automatically
2. Webhook: subscription.charged received
3. Database: SubscriptionTransaction created
4. User receives payment confirmation email
```

#### **3. Subscription Cancellation**
```bash
# Expected flow:
1. User clicks "Cancel Subscription"
2. Frontend calls cancelSubscription()
3. Razorpay cancels subscription
4. Webhook: subscription.cancelled received
5. Database: Subscription status = "Cancelled"
```

---

### **Webhook Testing**

**Using Razorpay Dashboard:**
1. Navigate to Webhooks → Your Webhook
2. Click "Test Webhook"
3. Select event type
4. Click "Send Test Webhook"
5. Check application logs for processing

**Manual Testing:**
```bash
# Send test webhook using curl
curl -X POST https://yourdomain.com/api/webhooks/razorpay \
  -H "Content-Type: application/json" \
  -H "X-Razorpay-Signature: YOUR_SIGNATURE" \
  -d '{
    "event": "subscription.activated",
    "payload": {
      "subscription": {
        "entity": {
          "id": "sub_test123",
          "status": "active",
          "notes": {
            "user_id": "1"
          }
        }
      }
    }
  }'
```

---

## 📊 PRICING CONFIGURATION

**Current Plans (Update as needed):**

| Plan | Price | Billing | Razorpay Plan ID |
|------|-------|---------|------------------|
| Free | ₹0 | - | - |
| Premium Monthly | ₹999 | Monthly | `plan_premium_monthly` |
| Premium Yearly | ₹9,599 | Yearly | `plan_premium_yearly` |

**To Update Pricing:**
1. Edit `RazorpayPaymentService.cs` → `GetPricingPlansAsync()` method
2. Update plan IDs with actual Razorpay plan IDs
3. Update prices and features as needed

---

## 🔒 SECURITY CONSIDERATIONS

### **1. API Key Security**
- ✅ Never commit API keys to Git
- ✅ Use environment variables in production
- ✅ Rotate keys every 90 days
- ✅ Use separate keys for test and live modes

### **2. Webhook Security**
- ✅ Always verify webhook signatures
- ✅ Use HTTPS for webhook URLs
- ✅ Implement rate limiting on webhook endpoint
- ✅ Log all webhook events for audit

### **3. Payment Security**
- ✅ Never store card details
- ✅ Use Razorpay's PCI-compliant checkout
- ✅ Implement 3D Secure authentication
- ✅ Monitor for fraudulent transactions

---

## 🚨 MIGRATION CHECKLIST

### **Pre-Migration**
- [ ] Backup database
- [ ] Export existing Stripe subscriptions
- [ ] Notify users of payment gateway change
- [ ] Set up Razorpay account and complete KYC

### **Migration**
- [ ] Install Razorpay NuGet package
- [ ] Run database migration
- [ ] Update configuration files
- [ ] Create Razorpay plans
- [ ] Configure webhooks
- [ ] Update frontend code
- [ ] Test in development environment

### **Post-Migration**
- [ ] Migrate existing subscriptions (manual process)
- [ ] Test all payment flows
- [ ] Monitor webhook events
- [ ] Update documentation
- [ ] Train support team
- [ ] Monitor for errors in production

---

## 📝 MANUAL SUBSCRIPTION MIGRATION

**For existing Stripe customers:**

1. **Export Stripe subscriptions:**
```sql
SELECT 
    u."Email",
    s."StripeSubscriptionId",
    s."Status",
    s."CurrentPeriodEnd"
FROM "Subscriptions" s
JOIN "Users" u ON s."UserId" = u."Id"
WHERE s."Status" = 'Active';
```

2. **Contact customers:**
   - Email them about the migration
   - Provide migration deadline
   - Offer assistance with re-subscription

3. **Cancel Stripe subscriptions:**
   - Use Stripe dashboard or API
   - Set cancellation at period end

4. **Create Razorpay subscriptions:**
   - Customers re-subscribe via new checkout
   - Or manually create subscriptions via Razorpay API

---

## 🐛 TROUBLESHOOTING

### **Issue: Webhook signature verification fails**
**Solution:**
- Verify webhook secret is correct in configuration
- Check `X-Razorpay-Signature` header is being sent
- Ensure payload is not modified before verification

### **Issue: Payment succeeds but subscription not created**
**Solution:**
- Check webhook is configured correctly
- Verify webhook endpoint is accessible (not behind firewall)
- Check application logs for errors
- Ensure database connection is working

### **Issue: Razorpay checkout not opening**
**Solution:**
- Verify Razorpay SDK is loaded (`loadRazorpayScript()`)
- Check browser console for JavaScript errors
- Ensure Razorpay key ID is correct
- Check if popup blockers are enabled

### **Issue: Subscription status not updating**
**Solution:**
- Check webhook events are being received
- Verify `RazorpaySubscriptionId` matches in database
- Check application logs for webhook processing errors
- Manually trigger webhook from Razorpay dashboard

---

## 📞 SUPPORT RESOURCES

**Razorpay Documentation:**
- API Docs: https://razorpay.com/docs/api/
- Subscriptions: https://razorpay.com/docs/subscriptions/
- Webhooks: https://razorpay.com/docs/webhooks/
- Testing: https://razorpay.com/docs/payments/payments/test-card-details/

**Razorpay Support:**
- Email: support@razorpay.com
- Dashboard: Settings → Support
- Phone: Available in dashboard

---

## 🎯 NEXT STEPS

1. **Install Razorpay package** (Step 1)
2. **Create Razorpay account** (Step 2)
3. **Run database migration** (Step 6)
4. **Configure API keys** (Step 5)
5. **Create pricing plans** (Step 3)
6. **Set up webhooks** (Step 4)
7. **Test in development** (Testing Guide)
8. **Deploy to production** (After successful testing)

---

**Migration Status:** ✅ Code Complete - Ready for Configuration & Testing  
**Estimated Setup Time:** 2-3 hours  
**Estimated Testing Time:** 4-6 hours
