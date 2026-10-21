# Background Jobs Documentation - Complete Overview

## 📋 Overview

This document provides a complete mapping of all background jobs, their purposes, the data they generate, and which views/URLs consume that data.

---

## 🔄 Background Jobs Summary

| Job Name | Frequency | Purpose | Data Generated | Views/URLs |
|----------|-----------|---------|----------------|------------|
| **AI Visibility & Citation Check** | Daily 1 AM | Check prompt visibility & citations | `PromptVisibilityMetrics`, `Citations`, `PromptPlatforms` | Prompts dashboard, Analytics |
| **SiteCrawlJob** | Weekly | Crawl websites for SEO audit | `SiteAuditRuns`, `SiteAuditDetails`, `CrawledPages` | Site Audit pages |
| **OpportunityDetectionJob** | Daily 4 AM | Detect SEO/content opportunities | `SeoOpportunities`, `ContentGapOpportunities`, `VisibilityOpportunities` | Opportunities dashboard |
| **DashboardMetricsUpdateJob** | Every 6 hours | Aggregate metrics for dashboard | `VisibilityDashboardMetrics` | Main dashboard |
| **VisibilityTrendAggregationJob** | Daily 2 AM | Aggregate visibility trends | `VisibilityTrends`, `VisibilityAlerts` | Trends/analytics pages |
| **CitationAggregationJob** | Daily 3 AM | Aggregate citation metrics | Logs only (no entity) | Citation reports |
| **PromptResearchGenerationJob** | Weekly Monday | Generate AI prompt suggestions | `PromptResearchData` | Prompt research page |
| **NotificationProcessorJob** | Every minute | Process scheduled notifications | `InAppNotifications` | Notification center |
| **CleanupJob** | Daily | Clean up expired/old data | N/A (deletes data) | N/A |
| **EmailQueueJob** | Scheduled | Send queued emails | Email logs | N/A |

---

## 🌟 1️⃣ AI Visibility & Citation Check Job (PRIMARY JOB)

**Job ID:** `ai-visibility-citation-check`  
**Service:** `IPromptTrackingService.ProcessAllActivePromptsAsync()`

### **Motive**
This is the **core job** that checks all active prompts for visibility across AI platforms (ChatGPT, Claude, Perplexity, Gemini) and extracts citations. It's a merged implementation that performs visibility checking and citation extraction in a single LLM call per prompt.

### **What It Does**
1. Retrieves all active prompts that haven't been checked in the last 24 hours
2. For each prompt:
   - Sends the prompt to configured AI platforms
   - Collects visibility scores, brand mentions, sentiment
   - Extracts citations and competitor mentions
   - Stores full conversation content
   - Updates prompt metrics
3. Rate-limits requests to avoid overwhelming LLM APIs (500ms delay between prompts)

### **Data Generated**

#### Tables Populated:
- **`PromptVisibilityMetrics`** - Visibility check results per prompt per platform
  - `PromptPlatformId`, `CheckDate`, `VisibilityScore` (0-100)
  - `BrandMentioned`, `Position`, `Sentiment` (Positive/Neutral/Negative)
  - `ResponseSnippet`, `FullResponse`
  - `CompetitorMentioned`, `CompetitorNames`
  - `CreatedAt`

- **`Citations`** - Citations extracted from AI responses
  - `ProjectId`, `PromptId`, `SourceUrl`, `SourceDomain`
  - `CitationType` (DirectLink/Reference/Inline)
  - `ContextText`, `Anchor`, `Position`
  - `DiscoveredAt`, `LastSeenAt`

- **`PromptPlatforms`** - Platform-specific prompt tracking
  - `PromptId`, `Platform` (ChatGPT/Claude/Perplexity/Gemini)
  - `IsActive`, `LastCheckedAt`, `CheckFrequency`
  - `AverageVisibilityScore`, `TotalChecks`

- **`PromptTrackings`** - Updated with latest check info
  - `LastCheckedAt`, `AverageVisibilityScore`
  - `TotalMentions`, `LastMentionedAt`

### **Views/URLs That Use This Data**

| View/URL | Data Used | Purpose |
|----------|-----------|---------|
| `/prompts` | `PromptTrackings` | List all tracked prompts with scores |
| `/prompts/{id}` | `PromptTrackings`, `PromptVisibilityMetrics` | Prompt details and history |
| `/prompts/{id}/visibility` | `PromptVisibilityMetrics` | Visibility timeline chart |
| `/prompts/{id}/citations` | `Citations` | Citations found for this prompt |
| `/analytics/prompts` | `PromptVisibilityMetrics` (aggregated) | Prompt performance analytics |
| `/analytics/platforms` | `PromptPlatforms` | Platform comparison |
| `/citations` | `Citations` | All citations across prompts |
| `/dashboard` | `PromptTrackings`, `Citations` (counts) | Dashboard widgets |
| `/api/prompts` | `PromptTrackings` | API endpoint |
| `/api/prompts/{id}/metrics` | `PromptVisibilityMetrics` | API endpoint |
| `/api/citations` | `Citations` | API endpoint |

### **Visibility Score Calculation**

The visibility score (0-100) is calculated based on:
- **Brand Mention**: Is the brand mentioned? (+40 points)
- **Position**: Where in the response? (Top: +30, Middle: +20, Bottom: +10)
- **Sentiment**: Positive/Neutral/Negative (+20/+10/0)
- **Citation Quality**: Direct link vs reference (+10)
- **Competitor Presence**: Are competitors mentioned? (-10)

### **Citation Extraction**

Citations are extracted using pattern matching and LLM analysis:
- **DirectLink**: Clickable URLs in response
- **Reference**: "According to [Source]" mentions
- **Inline**: Parenthetical citations

### **Manual Trigger**

Users can manually trigger a check via:
- **API:** `POST /api/prompts/{id}/check-visibility`
- **UI:** "Check Now" button on prompt detail page

### **Trigger Conditions**
- Runs daily at 1 AM UTC
- Only checks prompts where `IsActive = true` and `Status = 'Active'`
- Skips prompts checked within last 24 hours
- Respects LLM provider rate limits

### **Performance Considerations**
- Uses 500ms delay between prompts to avoid rate limiting
- Single LLM call per prompt (merged visibility + citation check)
- Processes prompts sequentially to maintain rate limits
- Logs success/failure counts for monitoring

---

## 2️⃣ SiteCrawlJob

### **Motive**
Automatically crawl websites for projects with auto-crawl enabled to perform comprehensive SEO audits.

### **What It Does**
1. Finds all projects with `EnableAutoCrawl = true`
2. For each project:
   - Crawls the website URL
   - Respects robots.txt
   - Follows internal links up to configured depth
   - Analyzes each page for SEO issues
3. Creates audit runs with detailed findings

### **Data Generated**

#### Tables Populated:
- **`SiteAuditRuns`** - Main audit run record
  - `ProjectId`, `UserId`, `Status`, `TotalPages`, `OverallScore`
  - `PassedChecks`, `FailedChecks`, `WarningChecks`
  - `StartedAt`, `CompletedAt`

- **`SiteAuditDetails`** - Individual check results
  - `SiteAuditRunId`, `PageUrl`, `CheckType`, `CheckName`
  - `Status` (Pass/Fail/Warning), `Message`, `Recommendation`
  - `Priority`, `DetailsJson`

- **`CrawledPages`** - Pages discovered during crawl
  - `SiteAuditRunId`, `Url`, `Title`, `StatusCode`
  - `ContentLength`, `LoadTime`, `Depth`

- **`JobExecutionLogs`** - Job execution tracking
  - `JobName`, `Status`, `ItemsProcessed`, `DurationSeconds`

### **Views/URLs That Use This Data**

| View/URL | Data Used | Purpose |
|----------|-----------|---------|
| `/site-audit/runs` | `SiteAuditRuns` | List all audit runs |
| `/site-audit/run/{id}` | `SiteAuditRuns`, `SiteAuditDetails` | View audit results |
| `/site-audit/details/{id}` | `SiteAuditDetails` | View specific check details |
| `/dashboard` | `SiteAuditRuns` (latest) | Show site audit score widget |
| `/api/site-audit/runs` | `SiteAuditRuns` | API endpoint for audit runs |
| `/api/site-audit/details` | `SiteAuditDetails` | API endpoint for check details |

### **Trigger Conditions**
- Project has `EnableAutoCrawl = true`
- Project has valid `WebsiteUrl`
- Runs on schedule (typically daily or weekly)

---

## 3️⃣ OpportunityDetectionJob

### **Motive**
Automatically analyze audit results and visibility data to identify actionable SEO and content opportunities.

### **What It Does**
1. Finds projects with `EnableOpportunityDetection = true`
2. For each project:
   - **Detects Content Gaps**: Analyzes competitor content vs. your content
   - **Detects SEO Opportunities**: Finds issues from site audits (missing meta, thin content, etc.)
   - **Detects Visibility Opportunities**: Identifies prompts where brand isn't mentioned but should be
3. Creates opportunity records with priority and recommendations

### **Data Generated**

#### Tables Populated:
- **`SeoOpportunities`** - SEO improvement opportunities
  - `UserId`, `ProjectId`, `SiteAuditDetailId`
  - `PageUrl`, `OpportunityType`, `Priority`
  - `CurrentState`, `RecommendedAction`, `EstimatedImpact`
  - `Status` (Open/InProgress/Fixed/Dismissed)
  - `DiscoveredAt`, `FixedAt`

- **`ContentGapOpportunities`** - Content gap opportunities
  - `ProjectId`, `Topic`, `CompetitorUrl`
  - `GapType`, `Priority`, `EstimatedImpact`
  - `RecommendedAction`, `Status`

- **`VisibilityOpportunities`** - Prompt visibility opportunities
  - `ProjectId`, `PromptText`, `Platform`
  - `CurrentPosition`, `CompetitorMentioned`
  - `Priority`, `RecommendedAction`, `Status`

- **`JobExecutionLogs`** - Job execution tracking

### **Views/URLs That Use This Data**

| View/URL | Data Used | Purpose |
|----------|-----------|---------|
| `/opportunities` | All opportunity tables | Main opportunities dashboard |
| `/opportunities/seo` | `SeoOpportunities` | SEO opportunities list |
| `/opportunities/content-gaps` | `ContentGapOpportunities` | Content gap opportunities |
| `/opportunities/visibility` | `VisibilityOpportunities` | Visibility opportunities |
| `/opportunities/{id}` | Specific opportunity | View opportunity details |
| `/dashboard` | All opportunities (count) | Show opportunities widget |
| `/api/opportunities/seo` | `SeoOpportunities` | API endpoint |
| `/api/opportunities/content-gaps` | `ContentGapOpportunities` | API endpoint |
| `/api/opportunities/visibility` | `VisibilityOpportunities` | API endpoint |

### **Opportunity Types**

**SEO Opportunities:**
- `MissingMeta` - Missing meta descriptions/titles
- `ThinContent` - Pages with insufficient content
- `PoorHeadings` - Improper heading structure
- `BrokenLinks` - Links returning 404
- `SlowPageLoad` - Performance issues
- `MobileIssues` - Mobile usability problems

**Content Gap Opportunities:**
- `MissingTopic` - Competitor covers topic you don't
- `ShallowCoverage` - Topic needs more depth
- `OutdatedContent` - Content needs updating

**Visibility Opportunities:**
- `NotMentioned` - Brand not appearing in relevant prompts
- `LowPosition` - Brand mentioned but in poor position
- `CompetitorDominance` - Competitor mentioned instead

### **Trigger Conditions**
- Project has `EnableOpportunityDetection = true`
- Latest site audit run exists (for SEO opportunities)
- Visibility metrics exist (for visibility opportunities)

---

## 4️⃣ DashboardMetricsUpdateJob

### **Motive**
Aggregate and calculate comprehensive dashboard metrics daily for quick dashboard loading.

### **What It Does**
1. For each project:
   - Calculates prompt metrics (total, active, average scores)
   - Calculates visibility metrics (mentions, mention rate)
   - Calculates citation metrics (total citations, unique domains)
   - Calculates site audit metrics (score, issues)
   - Calculates opportunity metrics (total, high priority, actioned)
   - Calculates overall visibility score (weighted formula)
2. Updates or creates daily metric record

### **Data Generated**

#### Tables Populated:
- **`VisibilityDashboardMetrics`** - Daily aggregated metrics
  - `ProjectId`, `MetricDate`, `Period` (daily)
  - **Visibility Metrics:**
    - `OverallVisibilityScore` - Weighted composite score (0-100)
  - **Prompt Metrics:**
    - `TotalPrompts`, `ActivePrompts`, `AveragePromptScore`
    - `PromptsWithMentions`, `MentionRate`
  - **Citation Metrics:**
    - `TotalCitations`, `UniqueDomainsCiting`
  - **Site Audit Metrics:**
    - `SiteAuditScore`, `CriticalIssues`, `WarningIssues`, `PassedChecks`
  - **Opportunity Metrics:**
    - `TotalOpportunities`, `HighPriorityOpportunities`, `OpportunitiesActioned`

- **`JobExecutionLogs`** - Job execution tracking

### **Views/URLs That Use This Data**

| View/URL | Data Used | Purpose |
|----------|-----------|---------|
| `/dashboard` | `VisibilityDashboardMetrics` (latest) | Main dashboard - all widgets |
| `/dashboard/overview` | `VisibilityDashboardMetrics` | Overview page |
| `/analytics/visibility` | `VisibilityDashboardMetrics` (time series) | Visibility trends chart |
| `/analytics/prompts` | `VisibilityDashboardMetrics` | Prompt performance over time |
| `/analytics/citations` | `VisibilityDashboardMetrics` | Citation trends |
| `/api/dashboard/metrics` | `VisibilityDashboardMetrics` | API endpoint for dashboard |
| `/api/dashboard/metrics/history` | `VisibilityDashboardMetrics` | Historical metrics API |

### **Metric Calculations**

**Overall Visibility Score Formula:**
```
OverallVisibilityScore = 
  (AvgVisibilityScore × 0.3) + 
  (AvgPromptScore × 0.4) + 
  (MentionRate × 0.3) + 
  (SiteAuditScore × 0.3)
```

**Data Sources:**
- Last 7 days of `PromptVisibilityMetrics` for visibility scores
- Last 30 days of `Citations` for citation metrics
- Latest `SiteAuditRuns` for audit scores
- Current `SeoOpportunities`, `ContentGapOpportunities`, `VisibilityOpportunities` for opportunity counts

### **Trigger Conditions**
- Runs daily for all projects
- Updates existing record if one exists for today, otherwise creates new

---

## 5️⃣ VisibilityTrendAggregationJob

### **Motive**
Aggregate daily visibility trends and create alerts for significant changes.

### **What It Does**
1. For each project:
   - Aggregates yesterday's visibility metrics
   - Calculates average visibility score
   - Compares with previous day to detect trends
   - Counts sentiment distribution (positive/neutral/negative)
   - Creates alerts for significant drops (>20%)

### **Data Generated**

#### Tables Populated:
- **`VisibilityTrends`** - Daily trend aggregates
  - `ProjectId`, `TrendDate`, `Platform`, `Topic`
  - `TotalPromptsChecked`, `PromptsWithMention`
  - `AverageVisibilityScore`, `AveragePosition`
  - `PositiveMentions`, `NeutralMentions`, `NegativeMentions`
  - `CompetitorMentions`, `ShareOfVoice`

- **`VisibilityAlerts`** - Alerts for significant changes
  - `ProjectId`, `AlertType` (VisibilityDrop)
  - `Severity` (Critical/High/Medium/Low)
  - `Message`, `PromptText`, `Platform`
  - `DetailsJson`, `IsRead`, `ReadAt`

- **`JobExecutionLogs`** - Job execution tracking

### **Views/URLs That Use This Data**

| View/URL | Data Used | Purpose |
|----------|-----------|---------|
| `/analytics/trends` | `VisibilityTrends` | Visibility trends over time |
| `/analytics/trends/chart` | `VisibilityTrends` | Trend visualization |
| `/alerts` | `VisibilityAlerts` | Alerts dashboard |
| `/alerts/unread` | `VisibilityAlerts` (IsRead=false) | Unread alerts |
| `/dashboard` | `VisibilityAlerts` (recent) | Alerts widget |
| `/api/analytics/trends` | `VisibilityTrends` | API endpoint |
| `/api/alerts` | `VisibilityAlerts` | API endpoint |

### **Alert Conditions**

**Visibility Drop Alert:**
- Triggered when visibility score drops >20% from previous day
- Severity: Critical
- Message includes: current value, previous value, change percentage
- Only created if project has `EnableVisibilityAlerts = true`

**Trend Directions:**
- **Improving**: Change >+5%
- **Declining**: Change <-5%
- **Stable**: Change between -5% and +5%

### **Trigger Conditions**
- Runs daily
- Processes yesterday's data
- Requires visibility metrics to exist

---

## 6️⃣ CitationAggregationJob

### **Motive**
Aggregate citation data for reporting and analytics (currently logs only, no persistent aggregates).

### **What It Does**
1. For each project:
   - Retrieves yesterday's citations
   - Calculates metrics:
     - Total citations
     - Unique domains citing
     - Citation types (DirectLink, Reference, Inline)
     - Top citing domains
   - Logs aggregated data (no database writes currently)

### **Data Generated**

#### Tables Populated:
- **`JobExecutionLogs`** - Job execution tracking only
- **Note:** Currently only logs metrics, doesn't create aggregate records

### **Data Sources Used**
- **`Citations`** table (reads only)
  - `ProjectId`, `SourceDomain`, `CitationType`
  - `DiscoveredAt`, `SourceUrl`, `ContextText`

### **Views/URLs That Use This Data**

| View/URL | Data Used | Purpose |
|----------|-----------|---------|
| `/citations` | `Citations` (raw data) | Citations list |
| `/citations/report` | `Citations` (aggregated in query) | Citation report |
| `/api/citations` | `Citations` | API endpoint |
| `/dashboard` | `Citations` (last 30 days count) | Citations widget |

**Note:** This job currently only logs aggregated data. To use aggregated citation data in views, a new entity like `CitationDailyAggregate` should be created.

### **Trigger Conditions**
- Runs daily
- Processes yesterday's citations

---

## 7️⃣ PromptResearchGenerationJob

### **Motive**
Generate AI-powered prompt suggestions to help users discover new prompts to track.

### **What It Does**
1. For each project:
   - Generates topic based on project industry
   - Determines target audience from project settings
   - Uses LLM to generate 20 new prompt suggestions
   - Saves suggestions to database for user review

### **Data Generated**

#### Tables Populated:
- **`PromptResearchData`** - AI-generated prompt suggestions
  - `ProjectId`, `Topic`, `Industry`, `Audience`
  - `SuggestedPrompt`, `Relevance`, `SearchVolume`
  - `Difficulty`, `Opportunity`, `Rationale`
  - `Status` (New/Reviewed/Approved/Rejected)
  - `CreatedAt`

- **`JobExecutionLogs`** - Job execution tracking

### **Views/URLs That Use This Data**

| View/URL | Data Used | Purpose |
|----------|-----------|---------|
| `/prompts/research` | `PromptResearchData` | Prompt research/discovery page |
| `/prompts/research/suggestions` | `PromptResearchData` (Status=New) | New suggestions |
| `/prompts/research/approved` | `PromptResearchData` (Status=Approved) | Approved prompts |
| `/prompts/add-from-research/{id}` | `PromptResearchData` | Convert suggestion to tracked prompt |
| `/api/prompts/research` | `PromptResearchData` | API endpoint |
| `/api/prompts/research/generate` | Triggers generation | Manual generation trigger |

### **Generation Logic**
- Uses `IPromptResearchService.GeneratePromptResearchAsync()`
- Leverages LLM to generate contextually relevant prompts
- Considers:
  - Project industry
  - Target audience
  - Existing tracked prompts (to avoid duplicates)
  - Current trends in the industry

### **Trigger Conditions**
- Runs on schedule for all projects
- Can be manually triggered via API
- Generates 20 prompts per project per run

---

## 8️⃣ NotificationProcessorJob

### **Motive**
Process scheduled in-app notifications every minute to ensure timely delivery.

### **What It Does**
1. Runs continuously every minute
2. Calls `INotificationSchedulerService.ProcessScheduledNotificationsAsync()`
3. Sends notifications that are due
4. Marks notifications as sent

### **Data Generated**

#### Tables Populated:
- **`InAppNotifications`** - In-app notification records
  - `UserId`, `Type`, `Title`, `Message`
  - `ActionUrl`, `IsRead`, `ReadAt`
  - `Priority`, `ExpiresAt`
  - `CreatedAt`, `ScheduledFor`

### **Views/URLs That Use This Data**

| View/URL | Data Used | Purpose |
|----------|-----------|---------|
| `/notifications` | `InAppNotifications` | Notifications center |
| `/notifications/unread` | `InAppNotifications` (IsRead=false) | Unread notifications |
| `/api/notifications` | `InAppNotifications` | API endpoint |
| `/api/notifications/unread-count` | `InAppNotifications` (count) | Badge count |
| **Header/Nav** | `InAppNotifications` (recent unread) | Notification bell dropdown |

### **Notification Types**
- `OpportunityDetected` - New opportunity found
- `VisibilityAlert` - Visibility score dropped
- `AuditCompleted` - Site audit finished
- `CrawlCompleted` - Site crawl finished
- `SubscriptionExpiring` - Subscription expiring soon
- `SystemAlert` - System-wide announcements

### **Trigger Conditions**
- Runs every 1 minute continuously
- Processes notifications where `ScheduledFor <= NOW()`

---

## 9️⃣ CleanupJob

### **Motive**
Maintain database health by removing expired and old data.

### **What It Does**
Performs multiple cleanup tasks:

1. **Expired Refresh Tokens**
   - Removes tokens where `ExpiresAt < NOW()`

2. **Old Audit Logs**
   - Removes logs older than 90 days

3. **Inactive Users**
   - Marks users inactive if not logged in for 365 days

4. **Expired Password Reset Tokens**
   - Removes tokens where `ExpiresAt < NOW()`

5. **Old Notifications**
   - Removes read notifications older than 30 days

### **Data Deleted**

#### Tables Cleaned:
- **`RefreshTokens`** - Expired tokens removed
- **`AuditLogs`** - Logs >90 days removed
- **`Users`** - Inactive users marked (not deleted)
- **`PasswordResets`** - Expired tokens removed
- **`InAppNotifications`** - Old read notifications removed

### **Views/URLs Affected**
- No direct views (cleanup improves performance across all views)
- Reduces database size
- Improves query performance

### **Trigger Conditions**
- Runs daily (typically at night)
- Can be manually triggered via admin panel

---

## 🔟 EmailQueueJob

### **Motive**
Process queued emails and send transactional emails.

### **What It Does**
1. Processes pending emails from queue
2. Sends various types of emails:
   - Welcome emails
   - Password reset emails
   - Subscription expiry reminders
   - Custom notifications

### **Data Generated**

#### Tables Populated:
- **Email logs** (if implemented)
- Uses `IEmailService` to send emails

### **Email Types**

| Email Type | Trigger | Template |
|------------|---------|----------|
| Welcome Email | User registration | Welcome message with getting started guide |
| Password Reset | Password reset request | Reset link with 1-hour expiry |
| Subscription Expiry | X days before expiry | Renewal reminder |
| Custom Notification | Various events | Event-specific content |

### **Views/URLs That Trigger Emails**
- `/register` → Welcome email
- `/forgot-password` → Password reset email
- Subscription expiry (automated) → Expiry reminder
- Various system events → Notification emails

### **Trigger Conditions**
- Runs on schedule to process queue
- Individual emails triggered by user actions or system events

---

## 📊 Data Flow Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                     Background Jobs                          │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                      Database Tables                         │
├─────────────────────────────────────────────────────────────┤
│ • SiteAuditRuns, SiteAuditDetails, CrawledPages            │
│ • SeoOpportunities, ContentGapOpportunities                 │
│ • VisibilityOpportunities                                   │
│ • VisibilityDashboardMetrics                                │
│ • VisibilityTrends, VisibilityAlerts                        │
│ • PromptResearchData                                        │
│ • InAppNotifications                                        │
│ • JobExecutionLogs                                          │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    API Endpoints & Views                     │
├─────────────────────────────────────────────────────────────┤
│ • /dashboard → VisibilityDashboardMetrics                  │
│ • /opportunities → All Opportunity tables                   │
│ • /site-audit → SiteAuditRuns, SiteAuditDetails           │
│ • /analytics/trends → VisibilityTrends                     │
│ • /alerts → VisibilityAlerts                               │
│ • /prompts/research → PromptResearchData                   │
│ • /notifications → InAppNotifications                      │
└─────────────────────────────────────────────────────────────┘
```

---

## 🔗 Job Dependencies

```
SiteCrawlJob
    ↓
OpportunityDetectionJob (needs audit data)
    ↓
DashboardMetricsUpdateJob (aggregates opportunities)
    ↓
VisibilityTrendAggregationJob (creates trends & alerts)
```

---

## ⚙️ Job Configuration

All jobs are configured in Hangfire and can be monitored at:
- **Hangfire Dashboard**: `/hangfire`
- **Job Execution Logs**: `JobExecutionLogs` table
- **API Endpoint**: `/api/admin/jobs`

---

## 📝 Summary Table

| Job | Generates Data For | Primary View/URL |
|-----|-------------------|------------------|
| **AI Visibility & Citation Check** | Prompt visibility & citations | `/prompts`, `/analytics/prompts` |
| SiteCrawlJob | Site audit results | `/site-audit` |
| OpportunityDetectionJob | Actionable opportunities | `/opportunities` |
| DashboardMetricsUpdateJob | Dashboard metrics | `/dashboard` |
| VisibilityTrendAggregationJob | Trends & alerts | `/analytics/trends`, `/alerts` |
| CitationAggregationJob | Citation analytics | `/citations/report` |
| PromptResearchGenerationJob | Prompt suggestions | `/prompts/research` |
| NotificationProcessorJob | In-app notifications | `/notifications` |
| CleanupJob | Database maintenance | N/A (performance) |
| EmailQueueJob | Email delivery | N/A (email) |

---

This documentation provides a complete overview of all background jobs, their data generation, and consumption patterns for data verification and testing purposes.
