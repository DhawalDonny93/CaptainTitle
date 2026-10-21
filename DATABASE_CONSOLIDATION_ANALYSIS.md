# Database Table Consolidation Analysis

## Overview
Analysis of database tables to identify duplicates, redundancies, and consolidation opportunities.

---

## 🔴 CRITICAL DUPLICATES - IMMEDIATE ACTION REQUIRED

### 1. **LLM Tracking Tables (4 tables doing similar things)**

#### Current Tables:
1. **LlmUsageLog** - Logs individual LLM API calls
   - Provider, Model, RequestType
   - PromptTokens, CompletionTokens, TotalTokens
   - Cost, LatencyMs
   - ProjectId, UserId
   - Success, ErrorMessage
   - CreatedAt

2. **LlmRequest** - Logs LLM requests with full details
   - ProviderId, Model, RequestType
   - PromptText, SystemPrompt, Temperature, MaxTokens
   - RequestTokens, ResponseTokens, TotalTokens
   - Cost, ResponseText, ResponseJson
   - Status, ErrorMessage, LatencyMs
   - ProjectId, UserId
   - CreatedAt

3. **LlmCostTracking** - Aggregated daily cost tracking
   - ProviderId, TrackingDate
   - TotalRequests, SuccessfulRequests, FailedRequests
   - TotalInputTokens, TotalOutputTokens
   - TotalCost
   - ProjectId, UserId
   - CreatedAt

4. **LlmMessage** - Chat conversation messages
   - ConversationId, Role
   - RawContent, ParsedContent
   - PromptTokens, CompletionTokens, TotalTokens
   - ResponseTimeMs
   - CreatedAt

#### Analysis:
- **LlmUsageLog** and **LlmRequest** have 90% overlap in functionality
- Both track individual API calls with tokens, cost, and metadata
- **LlmRequest** has more detailed fields (prompt text, response, etc.)
- **LlmUsageLog** is simpler but redundant

#### ✅ RECOMMENDATION: **MERGE LlmUsageLog INTO LlmRequest**
- **LlmRequest** is more comprehensive and already in use
- **LlmUsageLog** can be completely replaced by LlmRequest
- **LlmCostTracking** should remain as aggregated daily summaries
- **LlmMessage** should remain for chat conversations

---

### 2. **Prompt Tracking Tables (2 tables doing the same thing)**

#### Current Tables:
1. **AiTrackingPrompt**
   - ProjectId, PromptText, Topic
   - CompetitorUrlsJson
   - TrackingFrequency
   - Status (Pending, Scheduled, Active, Paused, Completed)
   - KeywordId
   - IsActive, LastCheckedAt
   - CreatedAt, UpdatedAt
   - Navigation: AiTrackingResults

2. **PromptTracking**
   - ProjectId, PromptText, Topic
   - Status (Active, Paused, Archived)
   - IsCompetitor
   - CreatedByUserId
   - IsActive, LastCheckedAt
   - AverageVisibilityScore
   - CreatedAt, UpdatedAt
   - Navigation: PlatformTrackings

#### Analysis:
- **EXACT DUPLICATE FUNCTIONALITY** - Both track prompts for visibility monitoring
- Different field names but same purpose
- Both have IsActive, LastCheckedAt, Status
- Both track prompts per project
- **AiTrackingPrompt** links to AiTrackingResults
- **PromptTracking** links to PlatformTrackings

#### ✅ RECOMMENDATION: **MERGE INTO SINGLE TABLE: PromptTracking**
- Keep **PromptTracking** (more actively used in services)
- Migrate **AiTrackingPrompt** data to PromptTracking
- Add missing fields from AiTrackingPrompt to PromptTracking:
  - CompetitorUrlsJson
  - TrackingFrequency
  - KeywordId
- Update AiTrackingResults to reference PromptTracking instead

---

### 3. **Provider Configuration (Redundant structure)**

#### Current Tables:
1. **LlmProvider** - Provider master data
   - Name, DisplayName, ApiEndpoint
   - IsActive, Priority
   - MaxTokens, CostPer1kInputTokens, CostPer1kOutputTokens
   - RateLimitPerMinute
   - Navigation: Configs, Requests

2. **LlmProviderConfig** - Key-value configs per provider
   - ProviderId, ConfigKey, ConfigValue
   - IsEncrypted
   - CreatedAt, UpdatedAt

#### Analysis:
- This is actually a **GOOD DESIGN** - not redundant
- LlmProvider = master provider data
- LlmProviderConfig = flexible key-value configs (API keys, etc.)
- Should **KEEP BOTH** tables

---

## 🟡 POTENTIALLY UNUSED TABLES

### Tables to Review for Usage:

1. **AnalyticsCohort** - User cohort analytics
2. **AnalyticsEvent** - Event tracking
3. **AnalyticsExport** - Export jobs
4. **AnalyticsFunnel** - Funnel analysis
5. **CompetitiveIntelligence** - Competitor data
6. **CompetitorAnalysis** - Competitor analysis results
7. **ContentGapOpportunity** - Content gap detection
8. **DataExport** - Generic export table
9. **DeviceRegistration** - Mobile device registration
10. **DeviceToken** - Push notification tokens
11. **OpportunityImpact** - Impact tracking
12. **OpportunityScan** - Opportunity scanning
13. **ParentalConsent** - Age verification (COPPA compliance)
14. **PaywallEvent** - Paywall interaction tracking
15. **PolicyDocument** - Legal documents
16. **Product** - Product catalog
17. **Receipt** - Purchase receipts
18. **RevenueCatWebhookEvent** - RevenueCat webhooks
19. **RevenueEvent** - Revenue tracking
20. **SystemHealthMetrics** - System monitoring
21. **UserProperty** - Custom user properties
22. **UserProvider** - OAuth provider links
23. **UserRevenueCatMapping** - RevenueCat user mapping
24. **UserTask** - User task management

---

## 📊 CONSOLIDATION PLAN

### Phase 1: Merge LLM Tables ✅ HIGH PRIORITY

**Action:** Merge LlmUsageLog into LlmRequest

**Steps:**
1. Verify LlmRequest has all fields from LlmUsageLog
2. Migrate any existing LlmUsageLog data to LlmRequest
3. Update all services using LlmUsageLog to use LlmRequest
4. Remove LlmUsageLog entity
5. Create migration to drop LlmUsageLogs table

**Impact:**
- ✅ Reduces table count by 1
- ✅ Simplifies LLM tracking logic
- ✅ No feature loss - LlmRequest is more comprehensive

---

### Phase 2: Merge Prompt Tracking Tables ✅ HIGH PRIORITY

**Action:** Merge AiTrackingPrompt into PromptTracking

**Steps:**
1. Add missing fields to PromptTracking:
   - CompetitorUrlsJson (string)
   - TrackingFrequency (string)
   - KeywordId (int?)
2. Migrate AiTrackingPrompt data to PromptTracking
3. Update AiTrackingResult to reference PromptTracking
4. Update all services using AiTrackingPrompt
5. Remove AiTrackingPrompt entity
6. Create migration to drop AiTrackingPrompts table

**Impact:**
- ✅ Reduces table count by 1
- ✅ Consolidates prompt tracking logic
- ✅ Maintains all functionality

---

### Phase 3: Remove Unused Tables 🟡 MEDIUM PRIORITY

**Action:** Identify and remove truly unused tables

**Candidates for Removal:**
1. **AnalyticsCohort** - If not using cohort analysis
2. **AnalyticsEvent** - If not using event tracking
3. **AnalyticsFunnel** - If not using funnel analysis
4. **DeviceRegistration** - If no mobile app
5. **DeviceToken** - If no push notifications
6. **ParentalConsent** - If not targeting children (COPPA)
7. **Product** - If not using product catalog
8. **Receipt** - If using RevenueCat exclusively
9. **UserProperty** - If not using custom properties
10. **UserTask** - If not using task management

**Verification Required:** Check each table for actual usage in codebase

---

## 🔍 TABLES TO KEEP (Justified)

### Core Business Logic:
- **User, UserProfile, UserRole, UserSession** - Authentication & authorization
- **Project** - Core entity
- **Subscription, SubscriptionPlan, SubscriptionTransaction** - Billing
- **BrandMention, Citation** - Core feature
- **CommunityOpportunity** - Core feature
- **ContentArticle** - Core feature
- **Keyword** - Core feature
- **PromptTracking** (consolidated) - Core feature
- **LlmRequest** (consolidated) - Core feature
- **LlmCostTracking** - Aggregated reporting
- **LlmProvider, LlmProviderConfig** - Provider management
- **LlmConversation, LlmMessage** - Chat feature

### Supporting Features:
- **Notification, NotificationTemplate, NotificationPreference** - Notifications
- **AuditLog, AdminAuditLog** - Compliance
- **JobExecutionLog** - Background jobs
- **SystemSetting** - Configuration
- **FeatureFlag** - Feature toggles

---

## 📈 EXPECTED RESULTS

### Before Consolidation:
- **Total Tables:** 87 tables

### After Phase 1 & 2:
- **Total Tables:** 85 tables (-2)
- **Reduced Complexity:** Simplified LLM and prompt tracking
- **Improved Maintainability:** Less code duplication

### After Phase 3 (if removing 10 unused):
- **Total Tables:** 75 tables (-12)
- **Cleaner Schema:** Only active, used tables

---

## ⚠️ IMPORTANT NOTES

1. **DO NOT remove tables without verification** - Check actual usage first
2. **Create data migration scripts** - Don't lose existing data
3. **Update all references** - Services, controllers, DTOs
4. **Test thoroughly** - Ensure no features break
5. **Create database backup** - Before running migrations

---

## 🎯 IMMEDIATE ACTION ITEMS

1. ✅ Merge LlmUsageLog into LlmRequest
2. ✅ Merge AiTrackingPrompt into PromptTracking
3. 🔍 Audit unused tables for actual usage
4. 📝 Create migration scripts
5. 🧪 Test all affected features
