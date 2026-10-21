# AISEO Setup Scripts

This directory contains scripts to quickly set up and configure AISEO's real data generation features.

---

## 📋 **Available Scripts**

### 1. **quick_setup.sh** - Automated Setup
Complete automated setup including database migration and feature enablement.

**Usage:**
```bash
cd /Users/dhawal/Documents/GitHub/AISEO/AISEODevelop/AISEO
chmod +x scripts/quick_setup.sh
bash scripts/quick_setup.sh
```

**What it does:**
- ✅ Checks PostgreSQL installation
- ✅ Runs database migration
- ✅ Creates 9 new tables
- ✅ Enables project features (optional)
- ✅ Verifies setup

---

### 2. **enable_project_features.sql** - Manual Feature Configuration
SQL script to enable/disable features for specific projects.

**Usage:**
```bash
psql -d your_database -f scripts/enable_project_features.sql
```

**Options:**
- Enable all features for one project
- Enable all features for all projects
- Enable features selectively
- Disable features

---

## 🚀 **Quick Start (Recommended)**

### **Option A: Automated Setup**

```bash
# 1. Make script executable
chmod +x scripts/quick_setup.sh

# 2. Run setup
bash scripts/quick_setup.sh

# Follow the prompts to:
# - Enter database credentials
# - Run migration
# - Enable features
```

---

### **Option B: Manual Setup**

**Step 1: Run Migration**
```bash
psql -d aiseo_db -U postgres -f database_migrations/11_add_crawler_and_opportunities_tables.sql
```

**Step 2: Enable Features for Project ID 1**
```sql
psql -d aiseo_db -U postgres <<EOF
UPDATE "Projects" SET
    "EnableAutoCrawl" = true,
    "CrawlMaxPages" = 100,
    "CrawlMaxDepth" = 3,
    "EnableOpportunityDetection" = true,
    "CompetitorDomainsJson" = '["competitor1.com", "competitor2.com"]',
    "EnableVisibilityAlerts" = true,
    "VisibilityCheckFrequency" = 'Hourly'
WHERE "Id" = 1;
EOF
```

**Step 3: Verify**
```sql
psql -d aiseo_db -U postgres -c "
SELECT 
    \"Id\",
    \"Name\",
    \"EnableAutoCrawl\",
    \"EnableOpportunityDetection\",
    \"EnableVisibilityAlerts\"
FROM \"Projects\";
"
```

---

## 📊 **Feature Configuration**

### **Available Features**

| Feature | Column | Values | Description |
|---------|--------|--------|-------------|
| **Auto Crawl** | `EnableAutoCrawl` | true/false | Weekly website crawling |
| **Max Pages** | `CrawlMaxPages` | 50-200 | Pages to crawl per run |
| **Max Depth** | `CrawlMaxDepth` | 2-4 | Link depth from homepage |
| **Opportunity Detection** | `EnableOpportunityDetection` | true/false | Daily opportunity detection |
| **Competitors** | `CompetitorDomainsJson` | JSON array | Competitor domains |
| **Visibility Alerts** | `EnableVisibilityAlerts` | true/false | Real-time alerts |
| **Check Frequency** | `VisibilityCheckFrequency` | Hourly/Daily/Weekly | How often to check |

---

## 💡 **Examples**

### **Enable All Features for Project 1**
```sql
UPDATE "Projects" SET
    "EnableAutoCrawl" = true,
    "CrawlMaxPages" = 100,
    "CrawlMaxDepth" = 3,
    "EnableOpportunityDetection" = true,
    "CompetitorDomainsJson" = '["competitor1.com", "competitor2.com"]',
    "EnableVisibilityAlerts" = true,
    "VisibilityCheckFrequency" = 'Hourly'
WHERE "Id" = 1;
```

### **Enable Only Crawling**
```sql
UPDATE "Projects" SET
    "EnableAutoCrawl" = true,
    "CrawlMaxPages" = 50,
    "CrawlMaxDepth" = 2
WHERE "Id" = 1;
```

### **Enable Only Opportunity Detection**
```sql
UPDATE "Projects" SET
    "EnableOpportunityDetection" = true,
    "CompetitorDomainsJson" = '["example.com"]'
WHERE "Id" = 1;
```

### **Disable All Features**
```sql
UPDATE "Projects" SET
    "EnableAutoCrawl" = false,
    "EnableOpportunityDetection" = false,
    "EnableVisibilityAlerts" = false
WHERE "Id" = 1;
```

---

## 🔍 **Verification Queries**

### **Check Table Creation**
```sql
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public' 
AND table_name IN (
    'ContentGapOpportunities', 
    'SeoOpportunities', 
    'VisibilityOpportunities',
    'VisibilityTrends', 
    'VisibilityAlerts',
    'LlmProviderConfigs', 
    'LlmUsageLogs', 
    'PromptTemplates',
    'JobExecutionLogs'
)
ORDER BY table_name;
```

### **Check Project Configuration**
```sql
SELECT 
    "Id",
    "Name",
    "WebsiteUrl",
    "EnableAutoCrawl",
    "CrawlMaxPages",
    "CrawlMaxDepth",
    "EnableOpportunityDetection",
    "CompetitorDomainsJson",
    "EnableVisibilityAlerts",
    "VisibilityCheckFrequency"
FROM "Projects"
ORDER BY "Id";
```

### **Check LLM Provider Configs**
```sql
SELECT 
    "Provider",
    "Model",
    "IsEnabled",
    "Priority"
FROM "LlmProviderConfigs"
ORDER BY "Priority";
```

---

## 💰 **Cost Impact**

| Feature | Monthly Cost (per project) |
|---------|---------------------------|
| Auto Crawl | $0 (no LLM calls) |
| Opportunity Detection | ~$3 |
| Visibility Alerts | Included in hourly checks |
| **Total** | **~$16/month** |

---

## 🎯 **Next Steps After Setup**

1. **Add API Keys** - Configure in `appsettings.json`
2. **Configure Hangfire** - Add to `Program.cs`
3. **Start Application** - Run your backend
4. **Monitor Jobs** - Visit `/hangfire` dashboard
5. **Test Features** - Manually trigger jobs

---

## 📚 **Documentation**

- **READY_TO_USE_GUIDE.md** - Complete usage guide
- **DATA_GENERATION_AND_JOBS_PRD.md** - Full PRD with all details
- **TABLE_CONSOLIDATION_SUMMARY.md** - Database decisions

---

## ⚠️ **Troubleshooting**

### **Migration Fails**
```bash
# Check if tables already exist
psql -d aiseo_db -c "\dt"

# Drop and recreate if needed
psql -d aiseo_db -f database_migrations/11_add_crawler_and_opportunities_tables.sql
```

### **Features Not Working**
```sql
-- Verify columns exist
SELECT column_name 
FROM information_schema.columns 
WHERE table_name = 'Projects' 
AND column_name LIKE 'Enable%';
```

### **Jobs Not Running**
- Check Hangfire is configured in `Program.cs`
- Verify Hangfire dashboard at `/hangfire`
- Check `JobExecutionLogs` table for errors

---

## 🔒 **Security Notes**

- Never commit API keys to version control
- Use environment variables for sensitive data
- Restrict Hangfire dashboard access in production
- Review competitor domains before enabling

---

## 📞 **Support**

For issues or questions:
1. Check documentation in `/docs`
2. Review `JobExecutionLogs` table
3. Check `LlmUsageLogs` for API errors
4. Verify project configuration

---

**Last Updated:** February 2026  
**Version:** 1.0
