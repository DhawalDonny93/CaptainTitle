# AISEO Platform - Production Readiness Audit & TODO List
**Date:** March 29, 2026  
**Audit Type:** Comprehensive Production Deployment Review  
**Payment Gateway:** Razorpay (Planned)

---

## 🚨 CRITICAL SECURITY ISSUES - FIX IMMEDIATELY

### 1. **EXPOSED CREDENTIALS IN SOURCE CODE**
**Severity:** CRITICAL ⚠️

**Current State:**
- Database credentials, API keys, and secrets are **hardcoded** in `appsettings.json`
- Sensitive data committed to Git repository
- Production secrets visible in source code

**Files with Exposed Secrets:**
```
AppBackendBasic.API/appsettings.json:
- Line 3: PostgreSQL connection string with password
- Line 7: Supabase secret key
- Line 11-12: Google OAuth credentials
- Line 15-17: Twilio credentials
- Line 20: SendGrid API key
- Line 33: JWT Secret key
- Line 127: Encryption key (weak - only 32 chars)
```

**Required Actions:**
1. ✅ **IMMEDIATELY** remove all secrets from `appsettings.json`
2. ✅ Rotate ALL exposed credentials (database, API keys, secrets)
3. ✅ Use environment variables or Azure Key Vault for secrets
4. ✅ Add `appsettings.json` to `.gitignore` (keep only `appsettings.example.json`)
5. ✅ Scan Git history and remove exposed secrets using tools like `git-secrets` or BFG Repo-Cleaner

**PostgreSQL Script Required:**
```sql
-- Rotate database password immediately
ALTER USER "postgres.ftuhzjfonajsqrfwznnt" WITH PASSWORD 'NEW_SECURE_PASSWORD_HERE';
```

---

## 💳 PAYMENT INTEGRATION - RAZORPAY IMPLEMENTATION

### Current State: ❌ **NOT IMPLEMENTED**
The application currently uses **Stripe** payment service, but you want **Razorpay**.

### Required Implementation:

#### 1. **Backend Changes**

**Create New Service:**
```
File: AppBackendBasic.Infrastructure/Services/RazorpayPaymentService.cs
```

**Add Configuration:**
```json
// appsettings.Production.json
"Razorpay": {
  "KeyId": "#{RazorpayKeyId}#",
  "KeySecret": "#{RazorpayKeySecret}#",
  "WebhookSecret": "#{RazorpayWebhookSecret}#"
}
```

**Install NuGet Package:**
```bash
dotnet add AppBackendBasic.Infrastructure package Razorpay
```

**Update Program.cs:**
```csharp
// Replace line 332:
builder.Services.AddScoped<IPaymentService, RazorpayPaymentService>();
```

#### 2. **Database Schema Updates**

**Add Razorpay-specific columns:**
```sql
-- Add to Subscriptions table
ALTER TABLE "Subscriptions" 
ADD COLUMN "RazorpaySubscriptionId" VARCHAR(255),
ADD COLUMN "RazorpayCustomerId" VARCHAR(255),
ADD COLUMN "RazorpayPlanId" VARCHAR(255);

-- Add to Users table
ALTER TABLE "Users"
ADD COLUMN "RazorpayCustomerId" VARCHAR(255);

-- Add index for faster lookups
CREATE INDEX "IX_Subscriptions_RazorpaySubscriptionId" 
ON "Subscriptions"("RazorpaySubscriptionId");
```

#### 3. **Frontend Integration**

**Install Razorpay SDK:**
```bash
cd AppBackendBasic.WebUI
npm install razorpay
```

**Create Razorpay Service:**
```
File: AppBackendBasic.WebUI/src/services/razorpayService.ts
```

#### 4. **Webhook Configuration**

**Webhook Endpoint:** `/api/webhooks/razorpay`

**Events to Handle:**
- `subscription.activated`
- `subscription.charged`
- `subscription.cancelled`
- `subscription.paused`
- `payment.failed`
- `payment.authorized`

---

## 🔧 SERVER CONFIGURATION REQUIREMENTS

### 1. **Environment Variables Setup**

**Create `.env` file on production server:**
```bash
# Database
DATABASE_CONNECTION_STRING="Host=your-db-host;Database=aiseo_prod;Username=aiseo_user;Password=SECURE_PASSWORD"

# JWT
JWT_SECRET="GENERATE_64_CHAR_RANDOM_STRING_HERE"
JWT_ISSUER="AISEO"
JWT_AUDIENCE="AISEOUsers"

# Razorpay
RAZORPAY_KEY_ID="rzp_live_XXXXX"
RAZORPAY_KEY_SECRET="YOUR_SECRET_HERE"
RAZORPAY_WEBHOOK_SECRET="YOUR_WEBHOOK_SECRET"

# Email (SendGrid)
SENDGRID_API_KEY="SG.XXXXX"
SENDGRID_FROM_EMAIL="noreply@yourdomain.com"
SENDGRID_FROM_NAME="AISEO"

# SMS (Twilio)
TWILIO_ACCOUNT_SID="ACXXXXX"
TWILIO_AUTH_TOKEN="YOUR_TOKEN"
TWILIO_FROM_PHONE="+1XXXXXXXXXX"

# Google OAuth
GOOGLE_CLIENT_ID="XXXXX.apps.googleusercontent.com"
GOOGLE_CLIENT_SECRET="GOCSPX-XXXXX"
GOOGLE_REDIRECT_URI="https://yourdomain.com/api/auth/google/callback"

# OpenAI
OPENAI_API_KEY="sk-XXXXX"

# Supabase
SUPABASE_URL="https://XXXXX.supabase.co"
SUPABASE_KEY="YOUR_ANON_KEY"

# Redis (Required for Production)
REDIS_CONNECTION_STRING="your-redis-host:6379,password=XXXXX"
REDIS_INSTANCE_NAME="AISEO_"

# Encryption
ENCRYPTION_KEY="GENERATE_32_CHAR_KEY_HERE"

# Application
APP_URL="https://yourdomain.com"
CORS_ALLOWED_ORIGINS="https://yourdomain.com,https://www.yourdomain.com"

# Logging
LOG_DIRECTORY="/var/log/aiseo"
```

### 2. **Redis Cache Configuration**

**Current State:** Redis is **disabled** in development (`"Enabled": false`)

**Production Requirement:** ✅ **MUST ENABLE**

**Installation (Ubuntu/Debian):**
```bash
sudo apt update
sudo apt install redis-server
sudo systemctl enable redis-server
sudo systemctl start redis-server

# Configure Redis for production
sudo nano /etc/redis/redis.conf
# Set: maxmemory 2gb
# Set: maxmemory-policy allkeys-lru
# Set: requirepass YOUR_SECURE_PASSWORD
```

**Update Configuration:**
```json
"Redis": {
  "ConnectionString": "localhost:6379,password=YOUR_PASSWORD",
  "InstanceName": "AISEO_",
  "Enabled": true
}
```

### 3. **Hangfire Background Jobs Configuration**

**Current State:** ✅ Already configured with PostgreSQL storage

**Production Checklist:**
- ✅ Hangfire dashboard is protected (HangfireAuthorizationFilter)
- ⚠️ Dashboard should be **disabled** in production or restricted to admin IPs
- ✅ 7 recurring jobs configured
- ✅ PostgreSQL storage configured

**Security Update Required:**
```csharp
// Program.cs - Update Hangfire dashboard configuration
if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new HangfireAuthorizationFilter() },
        IsReadOnlyFunc = (DashboardContext context) => !app.Environment.IsDevelopment()
    });
}
// DO NOT expose Hangfire dashboard in production
```

### 4. **Database Configuration**

**Current Connection Pool Settings:**
```
MinPoolSize: 1
MaxPoolSize: 10 (Conservative for Supabase)
ConnectionIdleLifetime: 60 seconds
```

**Production Recommendations:**
- ✅ Current settings are appropriate for Supabase
- ✅ Connection pooling enabled
- ✅ Keepalive configured
- ⚠️ Consider increasing `MaxPoolSize` to 20 if using dedicated database

**Required PostgreSQL Configuration:**
```sql
-- Set connection limits
ALTER DATABASE postgres SET max_connections = 100;

-- Enable query logging for monitoring
ALTER DATABASE postgres SET log_statement = 'mod';
ALTER DATABASE postgres SET log_duration = on;
ALTER DATABASE postgres SET log_min_duration_statement = 1000; -- Log queries > 1s
```

### 5. **Logging & Monitoring**

**Current State:**
- ✅ Serilog configured
- ✅ Console and File logging
- ⚠️ Application Insights placeholder exists but not configured

**Required Setup:**

**Create Log Directory:**
```bash
sudo mkdir -p /var/log/aiseo
sudo chown www-data:www-data /var/log/aiseo
sudo chmod 755 /var/log/aiseo
```

**Enable Application Insights:**
```bash
dotnet add AppBackendBasic.API package Microsoft.ApplicationInsights.AspNetCore
```

**Update appsettings.Production.json:**
```json
"ApplicationInsights": {
  "ConnectionString": "InstrumentationKey=YOUR_KEY;IngestionEndpoint=https://..."
}
```

**Alternative: Use ELK Stack or Seq**
```bash
# Install Seq for structured logging
docker run -d --name seq -e ACCEPT_EULA=Y -p 5341:80 datalust/seq:latest
```

---

## 🗄️ DATABASE MIGRATIONS & SCHEMA

### Current State Analysis:

**Schema File:** `COMPLETE_AISEO_SCHEMA.sql` (770 lines)

**Tables Created:** ✅ All 36 tables defined

**Missing Migrations:**
- ❌ No Entity Framework migrations found
- ❌ No version control for schema changes
- ⚠️ Using raw SQL script (risky for production updates)

### Required Actions:

#### 1. **Create EF Core Migrations**
```bash
cd AppBackendBasic.Infrastructure
dotnet ef migrations add InitialCreate --startup-project ../AppBackendBasic.API
dotnet ef database update --startup-project ../AppBackendBasic.API
```

#### 2. **Add Migration Scripts for Razorpay**
```sql
-- Migration: 001_Add_Razorpay_Support.sql
-- Date: 2026-03-29

BEGIN;

-- Add Razorpay columns to Users table
ALTER TABLE "Users" 
ADD COLUMN IF NOT EXISTS "RazorpayCustomerId" VARCHAR(255);

-- Add Razorpay columns to Subscriptions table  
ALTER TABLE "Subscriptions"
ADD COLUMN IF NOT EXISTS "RazorpaySubscriptionId" VARCHAR(255),
ADD COLUMN IF NOT EXISTS "RazorpayCustomerId" VARCHAR(255),
ADD COLUMN IF NOT EXISTS "RazorpayPlanId" VARCHAR(255);

-- Add indexes
CREATE INDEX IF NOT EXISTS "IX_Users_RazorpayCustomerId" 
ON "Users"("RazorpayCustomerId");

CREATE INDEX IF NOT EXISTS "IX_Subscriptions_RazorpaySubscriptionId" 
ON "Subscriptions"("RazorpaySubscriptionId");

-- Add SubscriptionTransactions table if not exists
CREATE TABLE IF NOT EXISTS "SubscriptionTransactions" (
    "Id" SERIAL PRIMARY KEY,
    "SubscriptionId" INTEGER NOT NULL,
    "TransactionType" VARCHAR(50) NOT NULL,
    "Amount" DECIMAL(18,2) NOT NULL,
    "Currency" VARCHAR(3) NOT NULL,
    "Status" VARCHAR(50) NOT NULL,
    "TransactionDate" TIMESTAMP WITH TIME ZONE NOT NULL,
    "RazorpayPaymentId" VARCHAR(255),
    "RazorpayOrderId" VARCHAR(255),
    "RazorpayInvoiceId" VARCHAR(255),
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_SubscriptionTransactions_Subscriptions" 
        FOREIGN KEY ("SubscriptionId") REFERENCES "Subscriptions"("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_SubscriptionTransactions_SubscriptionId" 
ON "SubscriptionTransactions"("SubscriptionId");

CREATE INDEX IF NOT EXISTS "IX_SubscriptionTransactions_RazorpayPaymentId" 
ON "SubscriptionTransactions"("RazorpayPaymentId");

COMMIT;
```

#### 3. **Database Backup Strategy**
```bash
# Create backup script
#!/bin/bash
# File: /opt/aiseo/backup-db.sh

BACKUP_DIR="/var/backups/aiseo"
DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="$BACKUP_DIR/aiseo_backup_$DATE.sql"

mkdir -p $BACKUP_DIR

# Backup database
pg_dump -h your-db-host -U aiseo_user -d aiseo_prod > $BACKUP_FILE

# Compress backup
gzip $BACKUP_FILE

# Delete backups older than 30 days
find $BACKUP_DIR -name "*.sql.gz" -mtime +30 -delete

echo "Backup completed: $BACKUP_FILE.gz"
```

**Setup Cron Job:**
```bash
# Run daily at 2 AM
0 2 * * * /opt/aiseo/backup-db.sh
```

---

## 🔒 SECURITY HARDENING

### 1. **SSL/TLS Configuration**

**Required:**
- ✅ HTTPS enforcement middleware already implemented
- ✅ HSTS headers configured
- ⚠️ Need valid SSL certificate

**Setup Let's Encrypt:**
```bash
sudo apt install certbot python3-certbot-nginx
sudo certbot --nginx -d yourdomain.com -d www.yourdomain.com
```

### 2. **CORS Configuration**

**Current State:**
```csharp
// Development: localhost only
// Production: Whitelist from configuration
```

**Update Production Config:**
```json
"Cors": {
  "AllowedOrigins": [
    "https://yourdomain.com",
    "https://www.yourdomain.com"
  ]
}
```

### 3. **Rate Limiting**

**Current State:** ✅ Implemented

**Production Values:** ✅ Already stricter in `appsettings.Production.json`
- Login: 3 attempts/minute (vs 5 in dev)
- Register: 2 attempts/minute (vs 3 in dev)
- General: 60 requests/minute (vs 100 in dev)

### 4. **API Key Rotation Policy**

**Create Rotation Schedule:**
```
- JWT Secret: Every 90 days
- Database Password: Every 90 days
- API Keys (SendGrid, Twilio, etc.): Every 180 days
- Razorpay Keys: Every 180 days
- Encryption Key: Every 365 days (requires data re-encryption)
```

### 5. **Security Headers**

**Current State:** ✅ SecurityHeadersMiddleware implemented

**Verify Headers:**
```
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
X-XSS-Protection: 1; mode=block
Strict-Transport-Security: max-age=31536000
Content-Security-Policy: default-src 'self'
```

---

## 🚀 DEPLOYMENT CONFIGURATION

### 1. **Web Server Setup (Nginx)**

**Install Nginx:**
```bash
sudo apt update
sudo apt install nginx
```

**Configuration:**
```nginx
# /etc/nginx/sites-available/aiseo

upstream aiseo_backend {
    server localhost:5000;
    keepalive 32;
}

server {
    listen 80;
    server_name yourdomain.com www.yourdomain.com;
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name yourdomain.com www.yourdomain.com;

    ssl_certificate /etc/letsencrypt/live/yourdomain.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/yourdomain.com/privkey.pem;
    
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;
    ssl_prefer_server_ciphers on;

    client_max_body_size 10M;

    location / {
        proxy_pass http://aiseo_backend;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
        proxy_buffering off;
    }

    # Static files for WebUI
    location /assets {
        alias /var/www/aiseo/webui/assets;
        expires 1y;
        add_header Cache-Control "public, immutable";
    }
}
```

### 2. **Systemd Service**

**Create Service File:**
```ini
# /etc/systemd/system/aiseo.service

[Unit]
Description=AISEO Platform API
After=network.target postgresql.service redis.service

[Service]
Type=notify
User=www-data
WorkingDirectory=/var/www/aiseo/api
ExecStart=/usr/bin/dotnet /var/www/aiseo/api/AppBackendBasic.API.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=aiseo-api
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
```

**Enable and Start:**
```bash
sudo systemctl daemon-reload
sudo systemctl enable aiseo
sudo systemctl start aiseo
sudo systemctl status aiseo
```

### 3. **Frontend Build & Deployment**

**Build WebUI:**
```bash
cd AppBackendBasic.WebUI

# Create production .env file
cat > .env.production << EOF
VITE_API_URL=https://yourdomain.com/api
EOF

# Build
npm run build

# Deploy to server
sudo mkdir -p /var/www/aiseo/webui
sudo cp -r dist/* /var/www/aiseo/webui/
sudo chown -R www-data:www-data /var/www/aiseo/webui
```

---

## 📝 MOCK DATA & CLEANUP

### Files with Mock/Test Data:

1. **`AppBackendBasic.WebUI/src/pages/SiteAudits.tsx`**
   - Line 144-179: `generateMockIssues()` function
   - **Action:** ✅ Already deprecated, remove function entirely

2. **`AppBackendBasic.WebUI/src/pages/OnboardingFlowNew.tsx`**
   - Line 153-156: Mock data fallback
   - **Action:** Remove fallback, show error instead

3. **`AppBackendBasic.Core/Templates/PromptTemplates.cs`**
   - Line 103-109: Example citations
   - **Action:** ✅ Keep (used as template examples)

4. **Test Email Addresses:**
   - `admin@example.com` in tests
   - **Action:** ✅ Keep in test files only

### Cleanup Script:
```bash
# Remove mock data and test files from production build
cd AppBackendBasic.WebUI
find src -name "*.test.ts" -delete
find src -name "*.test.tsx" -delete
```

---

## 🎯 PRODUCTION TODO LIST

### **PHASE 1: CRITICAL SECURITY (Week 1)** 🔴

**Priority: IMMEDIATE**

- [ ] **1.1** Remove all hardcoded secrets from `appsettings.json`
- [ ] **1.2** Rotate ALL exposed credentials (database, SendGrid, Twilio, Google OAuth, Supabase, JWT secret)
- [ ] **1.3** Setup environment variables on production server
- [ ] **1.4** Add `appsettings.json` to `.gitignore`
- [ ] **1.5** Clean Git history of exposed secrets using BFG Repo-Cleaner
- [ ] **1.6** Generate strong 64-character JWT secret
- [ ] **1.7** Generate strong 32-character encryption key
- [ ] **1.8** Setup SSL certificate (Let's Encrypt)
- [ ] **1.9** Configure firewall rules (UFW)
- [ ] **1.10** Enable fail2ban for SSH protection

**PostgreSQL Scripts:**
```sql
-- 1.2: Rotate database password
ALTER USER "postgres.ftuhzjfonajsqrfwznnt" WITH PASSWORD 'NEW_SECURE_PASSWORD';

-- 1.6: Update any database-stored secrets
-- (None currently, but verify)
```

---

### **PHASE 1.5: DATA FLOW & BACKEND IMPLEMENTATION (Week 1-2)** 🔄

**Priority: CRITICAL** (Frontend showing mock/fallback data)

#### **Data Verification & Diagnosis**

- [ ] **1.5.1** Run data verification SQL script to check existing data
  ```sql
  -- Check all critical tables for recent data
  SELECT 'VisibilityDashboardMetrics' AS "Table", COUNT(*) AS "Count", MAX("CreatedAt") AS "LatestEntry"
  FROM "VisibilityDashboardMetrics"
  UNION ALL
  SELECT 'PromptTrackings', COUNT(*), MAX("CreatedAt")
  FROM "PromptTrackings"
  UNION ALL
  SELECT 'Citations', COUNT(*), MAX("CreatedAt")
  FROM "Citations"
  UNION ALL
  SELECT 'CitationAggregations', COUNT(*), MAX("CreatedAt")
  FROM "CitationAggregations"
  UNION ALL
  SELECT 'CompetitiveIntelligence', COUNT(*), MAX("CreatedAt")
  FROM "CompetitiveIntelligence"
  UNION ALL
  SELECT 'ContentGapOpportunities', COUNT(*), MAX("CreatedAt")
  FROM "ContentGapOpportunities"
  UNION ALL
  SELECT 'VisibilityAlerts', COUNT(*), MAX("CreatedAt")
  FROM "VisibilityAlerts";
  ```
- [ ] **1.5.2** Check Hangfire dashboard - verify jobs are executing
- [ ] **1.5.3** Review application logs for job execution errors
- [ ] **1.5.4** Verify background job registration in `Program.cs`

#### **Fix Prompt Research Metrics (CRITICAL)**

**Issue:** Frontend using `Math.random()` for visibility scores, position changes, and citation metrics

- [ ] **1.5.5** Create database migration `003_Add_PromptResearch_Metrics.sql`
  ```sql
  ALTER TABLE "PromptResearchDatas"
  ADD COLUMN IF NOT EXISTS "AveragePosition" DOUBLE PRECISION DEFAULT 0,
  ADD COLUMN IF NOT EXISTS "CitationCount" INTEGER DEFAULT 0,
  ADD COLUMN IF NOT EXISTS "CitationShare" DOUBLE PRECISION DEFAULT 0,
  ADD COLUMN IF NOT EXISTS "PreviousRelevanceScore" DOUBLE PRECISION,
  ADD COLUMN IF NOT EXISTS "PreviousPosition" DOUBLE PRECISION,
  ADD COLUMN IF NOT EXISTS "PreviousCitationCount" INTEGER,
  ADD COLUMN IF NOT EXISTS "LastCalculatedAt" TIMESTAMP WITH TIME ZONE;
  
  CREATE INDEX IF NOT EXISTS "IX_PromptResearchDatas_Topic_RelevanceScore" 
  ON "PromptResearchDatas"("Topic", "RelevanceScore" DESC);
  ```
- [ ] **1.5.6** Run migration on database
- [ ] **1.5.7** Create `PromptResearchMetricsUpdateJob.cs` in `BackgroundJobs/`
- [ ] **1.5.8** Add `UpdatePromptMetricsAsync()` method to `PromptResearchService.cs`
- [ ] **1.5.9** Register job in `Program.cs` (schedule: every 12 hours)
- [ ] **1.5.10** Update `PromptResearchData` DTO to include new fields
- [ ] **1.5.11** Remove mock data calculations from `PromptResearch.tsx` (lines 80-84)
- [ ] **1.5.12** Test with real data from backend

**Files to Create:**
- `Database_Migrations/003_Add_PromptResearch_Metrics.sql`
- `AppBackendBasic.Infrastructure/BackgroundJobs/PromptResearchMetricsUpdateJob.cs`

**Files to Modify:**
- `AppBackendBasic.Infrastructure/Services/PromptResearchService.cs`
- `AppBackendBasic.Core/Interfaces/IPromptResearchService.cs`
- `AppBackendBasic.API/Program.cs`
- `AppBackendBasic.WebUI/src/pages/PromptResearch.tsx`

#### **Fix Dashboard Metrics Generation**

**Issue:** Dashboard returns empty object if no metrics exist, frontend shows hardcoded values

- [ ] **1.5.13** Verify `DashboardMetricsUpdateJob` exists and is registered
- [ ] **1.5.14** Check if job is executing (Hangfire dashboard)
- [ ] **1.5.15** Manually trigger job via Hangfire dashboard
- [ ] **1.5.16** Verify data appears in `VisibilityDashboardMetrics` table
- [ ] **1.5.17** Seed initial dashboard metrics for testing
  ```sql
  INSERT INTO "VisibilityDashboardMetrics" (
      "ProjectId", "MetricDate", "Period", "OverallVisibilityScore",
      "TotalPrompts", "TotalCitations", "TotalOpportunities", 
      "PlatformMetrics", "CreatedAt"
  ) VALUES (
      1, CURRENT_DATE, 'daily', 87.5, 45, 142, 38,
      '[{"platform":"ChatGPT","visibilityScore":92.0,"mentionCount":85,"averagePosition":1.8,"change":3.5}]',
      NOW()
  );
  ```
- [ ] **1.5.18** Remove hardcoded fallback values from `Dashboard.tsx` (lines 133-150)
- [ ] **1.5.19** Add proper "No data" state instead of fake values
- [ ] **1.5.20** Test dashboard with real data

#### **Competitive Intelligence Data Generation**

**Issue:** `CompetitiveIntelligence` table likely empty, no competitor data shown

- [ ] **1.5.21** Check if `OpportunityDetectionJob` includes competitive analysis
- [ ] **1.5.22** Verify competitor extraction from AI responses
- [ ] **1.5.23** Check `CompetitiveIntelligence` table for data
  ```sql
  SELECT COUNT(*) FROM "CompetitiveIntelligence";
  ```
- [ ] **1.5.24** Enhance `OpportunityDetectionJob` to populate competitive data
- [ ] **1.5.25** Add manual competitor tracking feature (if not exists)
- [ ] **1.5.26** Test competitive analysis display on frontend

#### **Frontend Mock Data Cleanup**

**Issue:** Multiple components using mock/random data as fallbacks

- [ ] **1.5.27** Remove `generateMockIssues()` from `SiteAudits.tsx` (lines 144-179)
- [ ] **1.5.28** Remove mock fallback from `OnboardingFlowNew.tsx` (lines 153-156)
- [ ] **1.5.29** Update all components to show loading/error states instead of fake data
- [ ] **1.5.30** Add proper error handling for empty data responses

#### **Background Jobs Verification**

- [ ] **1.5.31** Verify all 7 recurring jobs are registered:
  - AI Visibility & Citation Check (Daily 1 AM)
  - Dashboard Metrics Update (Every 6 hours)
  - Visibility Trend Aggregation (Daily 2 AM)
  - Citation Aggregation (Daily 3 AM)
  - Opportunity Detection (Daily 4 AM)
  - Prompt Research Generation (Weekly Monday 5 AM)
  - Site Crawl (Weekly Sunday 3 AM)
- [ ] **1.5.32** Check Hangfire storage (PostgreSQL) is working
- [ ] **1.5.33** Review job execution logs for errors
- [ ] **1.5.34** Set up job failure notifications

#### **Data Flow Testing**

- [ ] **1.5.35** Create end-to-end data flow test for each feature:
  - Prompt tracking → Citation detection → Dashboard metrics
  - Visibility scoring → Trend calculation → Frontend display
  - Opportunity detection → Competitive analysis → Recommendations
- [ ] **1.5.36** Document data flow for each major feature
- [ ] **1.5.37** Create data quality checks and validation

**PostgreSQL Scripts:**
```sql
-- Verify prompt research metrics are being calculated
SELECT 
    "Topic",
    COUNT(*) AS "PromptCount",
    AVG("AveragePosition") AS "AvgPosition",
    SUM("CitationCount") AS "TotalCitations"
FROM "PromptResearchDatas"
WHERE "IsActive" = true
GROUP BY "Topic"
ORDER BY "TotalCitations" DESC;

-- Check dashboard metrics generation
SELECT 
    "ProjectId",
    "MetricDate",
    "OverallVisibilityScore",
    "TotalPrompts",
    "TotalCitations",
    "TotalOpportunities"
FROM "VisibilityDashboardMetrics"
WHERE "Period" = 'daily'
ORDER BY "MetricDate" DESC
LIMIT 30;
```

**Reference Documentation:**
- See `DATA_FLOW_ANALYSIS.md` for detailed implementation guide
- Component-by-component analysis of data requirements
- Complete verification scripts and testing procedures

---

### **PHASE 2: PAYMENT INTEGRATION - RAZORPAY (Week 2-3)** 💳

**Priority: HIGH**

- [ ] **2.1** Create Razorpay account and get API keys
- [ ] **2.2** Install Razorpay NuGet package
- [ ] **2.3** Create `RazorpayPaymentService.cs`
- [ ] **2.4** Implement payment methods:
  - [ ] Create checkout session
  - [ ] Create payment link
  - [ ] Handle subscription creation
  - [ ] Handle subscription updates
  - [ ] Handle subscription cancellation
- [ ] **2.5** Create webhook endpoint `/api/webhooks/razorpay`
- [ ] **2.6** Implement webhook handlers for all events
- [ ] **2.7** Update database schema (run migration script)
- [ ] **2.8** Create Razorpay pricing plans
- [ ] **2.9** Update frontend to use Razorpay SDK
- [ ] **2.10** Create payment success/failure pages
- [ ] **2.11** Test payment flow in Razorpay test mode
- [ ] **2.12** Configure Razorpay webhook URL
- [ ] **2.13** Implement invoice generation
- [ ] **2.14** Setup payment failure notifications

**PostgreSQL Script:**
```sql
-- 2.7: Add Razorpay support
-- Run: Database_Migrations/002_Add_Razorpay_Support.sql
```

**Files to Create:**
- `AppBackendBasic.Infrastructure/Services/RazorpayPaymentService.cs`
- `AppBackendBasic.API/Controllers/RazorpayWebhookController.cs`
- `AppBackendBasic.WebUI/src/services/razorpayService.ts`
- `AppBackendBasic.WebUI/src/pages/PaymentSuccess.tsx`
- `AppBackendBasic.WebUI/src/pages/PaymentFailure.tsx`
- `Database_Migrations/002_Add_Razorpay_Support.sql`

---

### **PHASE 3: INFRASTRUCTURE SETUP (Week 3)** 🔧

**Priority: HIGH**

- [ ] **3.1** Setup production server (Ubuntu 22.04 LTS recommended)
- [ ] **3.2** Install and configure PostgreSQL
- [ ] **3.3** Install and configure Redis
- [ ] **3.4** Install and configure Nginx
- [ ] **3.5** Setup systemd service for API
- [ ] **3.6** Configure log rotation
- [ ] **3.7** Setup database backup cron job
- [ ] **3.8** Configure monitoring (Application Insights or alternative)
- [ ] **3.9** Setup health check endpoints
- [ ] **3.10** Configure CDN for static assets (optional)

**Server Specifications (Minimum):**
- CPU: 2 cores
- RAM: 4 GB
- Storage: 50 GB SSD
- OS: Ubuntu 22.04 LTS

---

### **PHASE 4: DATABASE & CACHING (Week 3-4)** 🗄️

**Priority: HIGH**

- [ ] **4.1** Create EF Core migrations from schema
- [ ] **4.2** Run database migrations on production
- [ ] **4.3** Seed initial data (admin user, system settings)
- [ ] **4.4** Enable Redis caching
- [ ] **4.5** Configure cache expiration policies
- [ ] **4.6** Implement cache warming for critical data
- [ ] **4.7** Setup database connection pooling
- [ ] **4.8** Configure database performance monitoring
- [ ] **4.9** Create database indexes for performance
- [ ] **4.10** Setup automated database backups

**PostgreSQL Scripts:**
```sql
-- 4.9: Add performance indexes
CREATE INDEX CONCURRENTLY "IX_PromptTrackings_ProjectId_CreatedAt" 
ON "PromptTrackings"("ProjectId", "CreatedAt" DESC);

CREATE INDEX CONCURRENTLY "IX_Citations_ProjectId_DiscoveredAt" 
ON "Citations"("ProjectId", "DiscoveredAt" DESC);

CREATE INDEX CONCURRENTLY "IX_VisibilityDashboardMetrics_ProjectId_Date" 
ON "VisibilityDashboardMetrics"("ProjectId", "MetricDate" DESC);
```

---

### **PHASE 5: BACKGROUND JOBS & SCHEDULING (Week 4)** ⏰

**Priority: MEDIUM**

- [ ] **5.1** Verify Hangfire dashboard security
- [ ] **5.2** Test all 7 recurring jobs
- [ ] **5.3** Configure job retry policies
- [ ] **5.4** Setup job failure notifications
- [ ] **5.5** Monitor job execution times
- [ ] **5.6** Optimize slow-running jobs
- [ ] **5.7** Configure job concurrency limits
- [ ] **5.8** Setup job execution logging
- [ ] **5.9** Create job monitoring dashboard
- [ ] **5.10** Document job schedules

**Current Jobs:**
1. AI Visibility & Citation Check (Daily 1 AM UTC)
2. Dashboard Metrics Update (Every 6 hours)
3. Visibility Trend Aggregation (Daily 2 AM UTC)
4. Citation Aggregation (Daily 3 AM UTC)
5. Opportunity Detection (Daily 4 AM UTC)
6. Prompt Research Generation (Weekly Monday 5 AM UTC)
7. Site Crawl (Weekly Sunday 3 AM UTC)

---

### **PHASE 6: API INTEGRATIONS (Week 5)** 🔌

**Priority: MEDIUM**

- [ ] **6.1** Verify OpenAI API integration
- [ ] **6.2** Test LLM provider failover
- [ ] **6.3** Configure API rate limits
- [ ] **6.4** Setup API usage monitoring
- [ ] **6.5** Implement API cost tracking
- [ ] **6.6** Configure SendGrid email templates
- [ ] **6.7** Test Twilio SMS delivery
- [ ] **6.8** Verify Google OAuth flow
- [ ] **6.9** Test Supabase storage
- [ ] **6.10** Configure API timeout policies

**API Keys Required:**
- OpenAI API Key (GPT-4)
- Anthropic API Key (Claude) - Optional
- Google Gemini API Key - Optional
- SendGrid API Key
- Twilio Account SID & Auth Token
- Google OAuth Client ID & Secret
- Supabase URL & Anon Key

---

### **PHASE 7: MONITORING & LOGGING (Week 5-6)** 📊

**Priority: MEDIUM**

- [ ] **7.1** Setup Application Insights (or alternative)
- [ ] **7.2** Configure error tracking (Sentry/Raygun)
- [ ] **7.3** Setup uptime monitoring (UptimeRobot/Pingdom)
- [ ] **7.4** Configure log aggregation (ELK/Seq)
- [ ] **7.5** Create performance dashboards
- [ ] **7.6** Setup alerting rules
- [ ] **7.7** Configure log retention policies
- [ ] **7.8** Implement custom metrics
- [ ] **7.9** Setup database query monitoring
- [ ] **7.10** Create incident response playbook

**Recommended Tools:**
- **Application Monitoring:** Application Insights or Datadog
- **Error Tracking:** Sentry
- **Uptime Monitoring:** UptimeRobot (free tier)
- **Log Management:** Seq (self-hosted) or ELK Stack

---

### **PHASE 8: TESTING & QA (Week 6-7)** 🧪

**Priority: MEDIUM**

- [ ] **8.1** Load testing (Apache JMeter/k6)
- [ ] **8.2** Security testing (OWASP ZAP)
- [ ] **8.3** API endpoint testing
- [ ] **8.4** Payment flow testing (Razorpay test mode)
- [ ] **8.5** Email delivery testing
- [ ] **8.6** SMS delivery testing
- [ ] **8.7** OAuth flow testing
- [ ] **8.8** Background job testing
- [ ] **8.9** Database backup/restore testing
- [ ] **8.10** Disaster recovery testing

**Load Testing Targets:**
- 100 concurrent users
- 1000 requests/minute
- < 500ms average response time
- < 1% error rate

---

### **PHASE 9: DOCUMENTATION (Week 7-8)** 📚

**Priority: LOW**

- [ ] **9.1** API documentation (Swagger/OpenAPI)
- [ ] **9.2** Deployment guide
- [ ] **9.3** Configuration guide
- [ ] **9.4** Database schema documentation
- [ ] **9.5** Backup/restore procedures
- [ ] **9.6** Incident response procedures
- [ ] **9.7** User guide
- [ ] **9.8** Admin guide
- [ ] **9.9** Developer onboarding guide
- [ ] **9.10** Troubleshooting guide

---

### **PHASE 10: OPTIMIZATION (Week 8+)** ⚡

**Priority: LOW**

- [ ] **10.1** Database query optimization
- [ ] **10.2** API response caching
- [ ] **10.3** Image optimization
- [ ] **10.4** Code minification
- [ ] **10.5** Bundle size reduction
- [ ] **10.6** Lazy loading implementation
- [ ] **10.7** CDN configuration
- [ ] **10.8** Gzip compression
- [ ] **10.9** HTTP/2 server push
- [ ] **10.10** Service worker implementation

---

## 🔮 FUTURE FEATURES (Post-Launch)

**Mark these for future updates:**

### **Version 2.0 Features:**
- [ ] Multi-language support (i18n)
- [ ] Advanced analytics dashboard
- [ ] AI-powered content recommendations
- [ ] Competitor benchmarking
- [ ] White-label options for enterprise
- [ ] Mobile app (React Native)
- [ ] API marketplace
- [ ] Webhook integrations
- [ ] Custom report builder
- [ ] Team collaboration features

### **Version 2.5 Features:**
- [ ] Machine learning model training
- [ ] Predictive analytics
- [ ] A/B testing framework
- [ ] Advanced SEO recommendations
- [ ] Content calendar integration
- [ ] Social media scheduling
- [ ] Influencer discovery
- [ ] Brand monitoring
- [ ] Sentiment analysis
- [ ] Custom AI model fine-tuning

---

## 📋 PRE-LAUNCH CHECKLIST

### **Security** ✅
- [ ] All secrets removed from source code
- [ ] All credentials rotated
- [ ] SSL certificate installed
- [ ] HTTPS enforced
- [ ] Security headers configured
- [ ] Rate limiting enabled
- [ ] CORS properly configured
- [ ] SQL injection prevention verified
- [ ] XSS prevention verified
- [ ] CSRF protection enabled

### **Infrastructure** ✅
- [ ] Production server provisioned
- [ ] Database configured
- [ ] Redis configured
- [ ] Nginx configured
- [ ] Systemd service created
- [ ] Firewall configured
- [ ] Backups automated
- [ ] Monitoring configured
- [ ] Logging configured
- [ ] Health checks working

### **Payment** ✅
- [ ] Razorpay account created
- [ ] Payment service implemented
- [ ] Webhooks configured
- [ ] Test payments successful
- [ ] Subscription management working
- [ ] Invoice generation working
- [ ] Payment failure handling tested
- [ ] Refund process documented

### **Testing** ✅
- [ ] Load testing completed
- [ ] Security testing completed
- [ ] Payment flow tested
- [ ] Email delivery tested
- [ ] SMS delivery tested
- [ ] OAuth flow tested
- [ ] Background jobs tested
- [ ] Backup/restore tested

### **Documentation** ✅
- [ ] API documentation complete
- [ ] Deployment guide complete
- [ ] Configuration guide complete
- [ ] User guide complete
- [ ] Admin guide complete

---

## 🚨 KNOWN ISSUES & TECHNICAL DEBT

### **Critical Issues:**
1. **Stripe Payment Service (Line 311):** TODO comment indicates incomplete Stripe SDK property mapping
   - **Action:** Complete Stripe implementation OR remove if using Razorpay only

2. **Hardcoded Pricing Plans (Line 202-263):** Pricing plans are hardcoded in `StripePaymentService.cs`
   - **Action:** Move to database or configuration file

3. **Admin Seed Disabled (Line 422-432 in Program.cs):** Admin user seeding is commented out
   - **Action:** Enable for production deployment

### **Medium Priority:**
1. **Mock Data in Frontend:** Several components have mock data fallbacks
2. **No EF Migrations:** Using raw SQL scripts instead of EF migrations
3. **Hangfire Dashboard:** Should be disabled or IP-restricted in production
4. **No Health Checks:** Basic health check exists but needs expansion

### **Low Priority:**
1. **Code Comments:** Some TODO comments in codebase
2. **Test Coverage:** No unit test coverage metrics
3. **Performance Metrics:** No baseline performance metrics established

---

## 💰 ESTIMATED COSTS (Monthly)

### **Infrastructure:**
- **VPS Server:** $20-50 (DigitalOcean/Linode/Vultr)
- **Database:** $15-25 (Managed PostgreSQL) OR $0 (Supabase free tier)
- **Redis:** $10-15 (Managed Redis) OR $0 (Self-hosted)
- **CDN:** $0-20 (Cloudflare free tier or paid)
- **SSL Certificate:** $0 (Let's Encrypt)

### **Services:**
- **SendGrid:** $0-15 (Free tier: 100 emails/day)
- **Twilio:** $1-20 (Pay-as-you-go)
- **OpenAI API:** $50-500 (Depends on usage)
- **Razorpay:** 2% transaction fee
- **Monitoring:** $0-50 (Free tiers available)

### **Total Estimated:** $100-700/month (depending on scale)

---

## 📞 SUPPORT & MAINTENANCE

### **Recommended Schedule:**
- **Daily:** Monitor logs, check error rates
- **Weekly:** Review performance metrics, check backup integrity
- **Monthly:** Security updates, dependency updates, cost review
- **Quarterly:** Security audit, performance optimization, feature planning
- **Annually:** Disaster recovery drill, architecture review

---

## ✅ FINAL RECOMMENDATIONS

### **Immediate Actions (This Week):**
1. Fix critical security issues (exposed secrets)
2. Setup environment variables
3. Rotate all credentials
4. Setup production server

### **Short Term (Next 2 Weeks):**
1. Implement Razorpay payment integration
2. Configure Redis caching
3. Setup monitoring and logging
4. Complete database migrations

### **Medium Term (Next Month):**
1. Comprehensive testing
2. Performance optimization
3. Documentation
4. Soft launch with limited users

### **Long Term (Next Quarter):**
1. Scale infrastructure based on usage
2. Implement future features
3. Expand payment options
4. International expansion

---

**Document Version:** 1.0  
**Last Updated:** March 29, 2026  
**Next Review:** April 15, 2026
