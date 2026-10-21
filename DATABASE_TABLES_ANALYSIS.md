# Database Tables Analysis - AISEO Platform

## 1. Tables Populated by Background Jobs

### 1.1 Prompt Visibility Check Job
**Schedule:** Daily at 10 AM  
**Service:** `PromptTrackingService.ProcessAllActivePromptsAsync()`  
**Tables Written:**
- `PromptTrackings` - Updates `LastCheckedAt`, `AverageVisibilityScore`
- `PromptPlatformTrackings` - Updates `LastCheckDate`, `CurrentVisibilityScore`
- `PromptVisibilityMetrics` - Creates new visibility check records
- `BrandMentions` - Creates brand mention records (own brand and competitors)
- `Citations` - Creates citation records from AI responses
- `CitationMetrics` - Creates citation tracking metrics
- `LlmRequests` - Logs LLM API calls
- `LlmCostTracking` - Tracks LLM usage costs

### 1.2 Citation Aggregation Job
**Schedule:** Daily (time not specified in JobScheduler)  
**Tables Written:**
- `JobExecutionLogs` - Logs job execution details
- **Note:** Currently only reads from `Citations` table for aggregation but doesn't write aggregated data to a dedicated table

### 1.3 Opportunity Detection Job
**Schedule:** Daily (time not specified in JobScheduler)  
**Service:** `OpportunityDetectionService`  
**Tables Written:**
- `ContentGapOpportunities` - Content gap opportunities detected
- `SeoOpportunities` - SEO improvement opportunities
- `VisibilityOpportunities` - Visibility enhancement opportunities
- `JobExecutionLogs` - Job execution tracking

### 1.4 Dashboard Metrics Update Job
**Schedule:** Daily (time not specified in JobScheduler)  
**Tables Written:**
- `VisibilityDashboardMetrics` - Daily dashboard metrics aggregation
- `JobExecutionLogs` - Job execution tracking

**Tables Read:**
- `Projects`, `PromptTrackings`, `PromptVisibilityMetrics`, `PromptPlatformTrackings`
- `Citations`, `SiteAuditRuns`, `ContentGapOpportunities`, `SeoOpportunities`, `VisibilityOpportunities`

### 1.5 Visibility Trend Aggregation Job
**Schedule:** Daily (time not specified in JobScheduler)  
**Tables Written:**
- `VisibilityTrends` - Daily visibility trend aggregation
- `VisibilityAlerts` - Alerts for significant visibility drops
- `JobExecutionLogs` - Job execution tracking

**Tables Read:**
- `Projects`, `PromptVisibilityMetrics`, `PromptPlatformTrackings`, `PromptTrackings`

### 1.6 Prompt Research Generation Job
**Schedule:** Daily (time not specified in JobScheduler)  
**Service:** `PromptResearchService`  
**Tables Written:**
- `PromptResearchData` - AI-generated prompt suggestions
- `JobExecutionLogs` - Job execution tracking

### 1.7 Site Crawl Job
**Schedule:** Weekly on Monday at 9 AM  
**Service:** `SiteAuditorService`  
**Tables Written:**
- `SiteCrawlRuns` - Crawl execution records
- `CrawledPages` - Individual page crawl data
- `JobExecutionLogs` - Job execution tracking

### 1.8 Email Queue Job
**Schedule:** Every 5 minutes  
**Tables Written:**
- `EmailLogs` - Email sending logs
- Updates `Notifications` status

### 1.9 Cleanup Jobs
**Schedule:** Various (daily/weekly/monthly)  
**Tables Modified:**
- `RefreshTokens` - Deletes expired tokens (Daily at 2 AM)
- `AuditLogs` - Deletes old logs (Weekly Sunday at 3 AM)
- `Users` - Marks inactive users (Monthly 1st at 4 AM)
- `PasswordResetTokens` - Deletes expired tokens (Daily at 1 AM)
- `Notifications` - Deletes old notifications (Daily at 5 AM)

### 1.10 Auto-Blog Publishing Job
**Schedule:** Daily at 9 AM  
**Service:** `AutoBlogService`  
**Tables Written:**
- `ContentArticles` - Updates published articles
- `PublishingLogs` - Publishing activity logs

### 1.11 Social Monitoring Scan Job
**Schedule:** Daily at 11 AM  
**Service:** `SocialMonitoringService`  
**Tables Written:**
- `CommunityOpportunities` - Social media opportunities (Reddit, Quora, etc.)
- `OpportunityScans` - Scan execution records

---

## 2. Tables Queried by Frontend Views

### 2.1 Visibility Dashboard (`/projects/:id/visibility`)
**Services Used:** `visibilityService`  
**Tables Queried:**
- `VisibilityDashboardMetrics` - Overall metrics
- `PromptTrackings` - Prompt data
- `Competitors` - Competitor information
- `Projects` - Project details

### 2.2 Sources Page (`/projects/:id/sources`)
**Services Used:** `sourcesService`, `citationService`  
**Tables Queried:**
- `Citations` - Citation data
- `CitationAggregations` - Aggregated citation metrics
- `Projects` - Project details
- `Competitors` - Competitor data

### 2.3 Mentions Page (`/projects/:id/mentions`)
**Services Used:** `mentionsService`  
**Tables Queried:**
- `BrandMentions` - Brand mention records
- `Citations` - Citation counts
- `PromptTrackings` - Prompt information
- `Competitors` - Competitor mentions
- `Projects` - Project details

### 2.4 Your Prompts Page (`/projects/:id/prompts`)
**Services Used:** `promptService`  
**Tables Queried:**
- `PromptTrackings` - All prompt data
- `PromptPlatformTrackings` - Platform-specific tracking
- `PromptVisibilityMetrics` - Visibility metrics
- `Keywords` - Associated keywords

### 2.5 Competitors Page (`/projects/:id/competitors`)
**Services Used:** `brandProfileService`  
**Tables Queried:**
- `Competitors` - Competitor list
- `Projects` - Project details

### 2.6 Brand Profile Page (`/projects/:id/brand-profile`)
**Services Used:** `brandProfileService`  
**Tables Queried:**
- `BrandProfiles` - Brand profile data
- `Projects` - Project details

### 2.7 Community Opportunities (`/projects/:id/community-opportunities`)
**Services Used:** `communityOpportunityService`  
**Tables Queried:**
- `CommunityOpportunities` - Social opportunities
- `Projects` - Project details

### 2.8 Content Writer (`/projects/:id/content-writer`)
**Services Used:** `aiSeoService`  
**Tables Queried:**
- `ContentArticles` - Article content
- `Projects` - Project details

### 2.9 Onboarding Flow (`/onboarding`)
**Services Used:** `onboardingService`  
**Tables Queried/Written:**
- `Users` - User data
- `Projects` - Creates new project
- `OnboardingSteps` - Onboarding progress
- `PromptTrackings` - Creates initial prompts
- `Competitors` - Adds competitors

---

## 3. Core Active Tables (Fully Implemented)

### 3.1 User & Authentication
- `Users` - User accounts
- `RefreshTokens` - JWT refresh tokens
- `PasswordResetTokens` - Password reset tokens
- `DeviceTokens` - Push notification tokens
- `UserSubscriptions` - Subscription data

### 3.2 Projects & Branding
- `Projects` - Project/brand management
- `BrandProfiles` - Detailed brand information
- `Competitors` - Competitor tracking

### 3.3 Prompt Tracking & Visibility
- `PromptTrackings` - Prompt definitions
- `PromptPlatformTrackings` - Platform-specific tracking
- `PromptVisibilityMetrics` - Visibility check results
- `AiTrackingResults` - AI tracking results

### 3.4 Citations & Mentions
- `Citations` - Citation records
- `CitationMetrics` - Citation tracking metrics
- `CitationAggregations` - Daily aggregated data
- `BrandMentions` - Brand mention records

### 3.5 Opportunities
- `ContentGapOpportunities` - Content gaps
- `SeoOpportunities` - SEO improvements
- `VisibilityOpportunities` - Visibility enhancements
- `CommunityOpportunities` - Social opportunities
- `OpportunityScans` - Scan execution records

### 3.6 Analytics & Metrics
- `VisibilityDashboardMetrics` - Dashboard aggregations
- `VisibilityTrends` - Trend analysis
- `VisibilityAlerts` - Alert notifications

### 3.7 Content Management
- `ContentArticles` - Auto-blog articles
- `PromptResearchData` - Prompt suggestions

### 3.8 Site Auditing
- `SiteAuditRuns` - Audit executions
- `SiteAuditChecks` - Individual check results
- `SiteCrawlRuns` - Crawl executions
- `CrawledPages` - Crawled page data

### 3.9 LLM & AI
- `LlmProviders` - LLM provider configs
- `LlmProviderConfigs` - Provider settings
- `LlmJobConfigs` - Job-specific LLM configs
- `LlmRequests` - API request logs
- `LlmCostTracking` - Cost tracking
- `LlmConversations` - Conversation threads
- `LlmMessages` - Individual messages
- `LlmPromptTemplates` - Prompt templates

### 3.10 System & Logging
- `JobExecutionLogs` - Background job logs
- `AuditLogs` - User activity logs
- `AdminAuditLogs` - Admin activity logs
- `Notifications` - User notifications
- `InAppNotifications` - In-app notifications
- `EmailLogs` - Email sending logs

---

## 4. Partially Implemented / Unused Tables

### 4.1 Partially Implemented
**These tables exist but have limited or incomplete functionality:**

- **`CompetitorAnalysis`** - Exists but not actively populated by jobs
- **`CompetitiveIntelligence`** - Exists but no service implementation found
- **`OpportunityImpact`** - Exists but not used in opportunity detection
- **`OpportunityTask`** - Exists but task management not implemented
- **`DataExport`** - Export functionality not implemented
- **`SystemHealthMetrics`** - Mentioned in code but not actively used

### 4.2 Likely Unused / Deprecated
**These tables may be legacy or planned features not yet implemented:**

- **`Keywords`** - Referenced in entities but minimal usage
- **`EmailTemplate`** - Email templates not actively used
- **`NotificationTemplate`** - Template system not implemented
- **`NotificationPreference`** - User preferences not implemented
- **`DeviceRegistration`** - Device registration not fully implemented
- **`FeatureFlag`** - Feature flag system not implemented
- **`OnboardingQuestion`** - Not used in current onboarding flow

### 4.3 Tables Without Clear Usage
**These tables exist in entities but have no clear implementation:**

- **`LlmUsageQuota`** - Quota system not implemented
- **`ContentGapOpportunity.OpportunityImpactId`** - Foreign key but OpportunityImpact not used

---

## 5. Recommendations

### 5.1 Tables to Keep
All tables in Section 3 (Core Active Tables) are actively used and should be maintained.

### 5.2 Tables to Review
Tables in Section 4.1 (Partially Implemented) should be either:
- Fully implemented if the feature is planned
- Removed if the feature is not needed

### 5.3 Tables to Consider Removing
Tables in Section 4.2 and 4.3 should be evaluated:
- If not planned for implementation, consider removing to reduce database complexity
- If planned, add to roadmap with clear implementation timeline

### 5.4 Missing Aggregation Tables
Consider adding:
- **`CitationDailyAggregates`** - Currently CitationAggregationJob doesn't write to a dedicated table
- **`MentionDailyAggregates`** - For better performance on mentions analytics

---

## 6. Data Flow Summary

### 6.1 Visibility Tracking Flow
```
User Creates Prompt → PromptTrackings
    ↓
Background Job Runs → Calls LLM APIs → LlmRequests, LlmCostTracking
    ↓
Analyzes Response → BrandMentions, Citations, CitationMetrics
    ↓
Updates Metrics → PromptVisibilityMetrics, PromptPlatformTrackings
    ↓
Daily Aggregation → VisibilityTrends, VisibilityDashboardMetrics
    ↓
Frontend Displays → Visibility Dashboard, Sources, Mentions pages
```

### 6.2 Opportunity Detection Flow
```
Background Job Scans → Analyzes Data
    ↓
Creates Opportunities → ContentGapOpportunities, SeoOpportunities, VisibilityOpportunities
    ↓
User Reviews → Community Opportunities page
    ↓
User Actions → Updates opportunity status
```

### 6.3 Content Generation Flow
```
User Requests Content → ContentArticles (Draft)
    ↓
LLM Generates → LlmRequests, LlmCostTracking
    ↓
Auto-Blog Job → Publishes scheduled articles
    ↓
Updates Status → ContentArticles (Published)
```

---

**Document Generated:** April 6, 2026  
**Version:** 1.0  
**Last Updated:** Based on current codebase analysis
