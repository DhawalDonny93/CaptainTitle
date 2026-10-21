-- =====================================================
-- LLM Prompt Templates for Site Audits and Opportunities
-- =====================================================
-- Insert prompt templates for generating audit issues and opportunities

-- 1. Site Audit Analysis Prompt Template
INSERT INTO "LlmPromptTemplates" ("Name", "Description", "PromptText", "Category", "IsActive", "CreatedAt", "UpdatedAt")
VALUES (
    'SiteAuditAnalysis',
    'Generates comprehensive site audit issues including technical SEO, AEO, and content analysis',
    'You are an expert SEO and AEO (Answer Engine Optimization) auditor. Analyze the following website and provide a comprehensive audit report.

Website URL: {{SITE_URL}}
Website Content: {{SITE_CONTENT}}
Meta Information: {{META_INFO}}
Technical Details: {{TECHNICAL_DETAILS}}

Please analyze the website and identify issues in the following categories:

1. **Content Issues** - Problems with content quality, structure, clarity, and user engagement
2. **AEO (Answer Engine Optimization) Issues** - Missing or incorrect schema markup, structured data, and AI-readability
3. **Technical SEO Issues** - Meta tags, headings, images, links, performance, and accessibility

For each issue found, provide:
- **Title**: Clear, concise issue title (max 100 characters)
- **Description**: Detailed explanation of the issue and why it matters (2-3 sentences)
- **Severity**: Critical, High, Medium, or Low
- **Category**: Content, AEO, or Technical
- **Type**: Specific type (e.g., Schema, Meta, Links, Navigation, Structure, Performance, Accessibility, Social)
- **AffectedPages**: Estimated number of pages affected (if applicable)
- **Recommendation**: Specific actionable steps to fix the issue
- **EstimatedImpactScore**: Score from 0-100 indicating potential impact of fixing this issue

Focus on issues that will:
- Improve visibility in AI-powered search engines (ChatGPT, Perplexity, Claude, Gemini)
- Enhance traditional SEO performance
- Improve user experience and engagement
- Increase conversion rates

Return the results as a JSON array of issues. Example format:
[
  {
    "title": "Missing WebSite schema markup",
    "description": "The website lacks WebSite schema markup which helps search engines understand your site structure and can enable sitelinks in search results.",
    "severity": "High",
    "category": "AEO",
    "type": "Schema",
    "affectedPages": 1,
    "recommendation": "Add WebSite schema markup to your homepage including name, url, and potentialAction for search functionality.",
    "estimatedImpactScore": 75
  }
]',
    'SiteAudit',
    TRUE,
    NOW(),
    NOW()
) ON CONFLICT ("Name") DO UPDATE SET
    "Description" = EXCLUDED."Description",
    "PromptText" = EXCLUDED."PromptText",
    "UpdatedAt" = NOW();

-- 2. Opportunities Discovery Prompt Template
INSERT INTO "LlmPromptTemplates" ("Name", "Description", "PromptText", "Category", "IsActive", "CreatedAt", "UpdatedAt")
VALUES (
    'OpportunitiesDiscovery',
    'Identifies opportunities for brand visibility and content creation based on site analysis',
    'You are an expert digital marketing strategist specializing in AI-powered search optimization and content opportunities.

Analyze the following information and identify opportunities for the brand to improve visibility and engagement:

Brand/Website: {{SITE_URL}}
Industry/Niche: {{INDUSTRY}}
Current Content: {{CURRENT_CONTENT}}
Competitor Analysis: {{COMPETITOR_DATA}}
Search Trends: {{SEARCH_TRENDS}}
AI Platform Data: {{AI_PLATFORM_DATA}}

Identify opportunities in the following areas:

1. **Content Gap Opportunities** - Topics or questions the brand should cover but currently doesn''t
2. **AI Visibility Opportunities** - Ways to improve mentions in AI-generated responses
3. **Technical Optimization Opportunities** - Technical improvements that will boost visibility
4. **Competitor Advantage Opportunities** - Areas where competitors are dominating that can be captured

For each opportunity, provide:
- **Title**: Clear, actionable opportunity title (max 100 characters)
- **Description**: Detailed explanation of the opportunity and potential benefits (2-4 sentences)
- **Impact**: Critical, High, Medium, or Low
- **Type**: ContentGap, Visibility, Technical, Competitive, Schema, Social, or Links
- **Platform**: Specific platform or "All" (e.g., ChatGPT, Perplexity, Google, All)
- **EstimatedImpact**: Score from 0-100 indicating potential impact
- **RecommendedAction**: Specific steps to capitalize on this opportunity
- **Priority**: Urgency level (1-5, where 1 is highest priority)

Focus on opportunities that will:
- Increase brand mentions in AI-generated responses
- Fill content gaps that competitors are exploiting
- Improve rankings for high-intent queries
- Enhance brand authority and trust signals
- Drive qualified traffic and conversions

Return the results as a JSON array of opportunities. Example format:
[
  {
    "title": "Create comprehensive guide on [Topic X]",
    "description": "Competitors are dominating AI responses for queries about [Topic X]. Creating an authoritative guide with proper schema markup could capture 30%+ of these mentions.",
    "impact": "High",
    "type": "ContentGap",
    "platform": "All",
    "estimatedImpact": 85,
    "recommendedAction": "Write a 2000+ word guide covering [specific aspects]. Include FAQ schema, HowTo schema, and cite authoritative sources. Optimize for question-based queries.",
    "priority": 1
  }
]',
    'Opportunities',
    TRUE,
    NOW(),
    NOW()
) ON CONFLICT ("Name") DO UPDATE SET
    "Description" = EXCLUDED."Description",
    "PromptText" = EXCLUDED."PromptText",
    "UpdatedAt" = NOW();

-- 3. Quick Audit Issues Prompt (for faster scans)
INSERT INTO "LlmPromptTemplates" ("Name", "Description", "PromptText", "Category", "IsActive", "CreatedAt", "UpdatedAt")
VALUES (
    'QuickSiteAudit',
    'Generates top priority audit issues for quick scans',
    'You are an SEO and AEO expert. Quickly analyze this website and identify the TOP 10 most critical issues.

Website URL: {{SITE_URL}}
Homepage Content: {{HOMEPAGE_CONTENT}}
Meta Tags: {{META_TAGS}}

Focus ONLY on the most impactful issues that:
1. Are blocking AI engines from citing the site
2. Have critical technical SEO problems
3. Are missing essential schema markup
4. Have severe content or UX issues

For each issue, provide:
- **title**: Issue name (max 80 chars)
- **description**: Why it matters (1-2 sentences)
- **severity**: Critical or High only
- **category**: Content, AEO, or Technical
- **type**: Specific type
- **recommendation**: How to fix (1 sentence)
- **estimatedImpactScore**: 60-100

Return as JSON array with exactly 10 issues, sorted by impact (highest first).

Example:
[
  {
    "title": "No Organization schema",
    "description": "Missing Organization schema prevents AI engines from understanding your company identity and authority.",
    "severity": "Critical",
    "category": "AEO",
    "type": "Schema",
    "recommendation": "Add Organization schema with name, logo, url, and sameAs social links.",
    "estimatedImpactScore": 90
  }
]',
    'SiteAudit',
    TRUE,
    NOW(),
    NOW()
) ON CONFLICT ("Name") DO UPDATE SET
    "Description" = EXCLUDED."Description",
    "PromptText" = EXCLUDED."PromptText",
    "UpdatedAt" = NOW();

-- 4. Opportunity Prioritization Prompt
INSERT INTO "LlmPromptTemplates" ("Name", "Description", "PromptText", "Category", "IsActive", "CreatedAt", "UpdatedAt")
VALUES (
    'OpportunityPrioritization',
    'Analyzes and prioritizes opportunities based on impact and effort',
    'You are a strategic advisor helping prioritize digital marketing opportunities.

Given these opportunities:
{{OPPORTUNITIES_LIST}}

And these constraints:
- Available resources: {{RESOURCES}}
- Timeline: {{TIMELINE}}
- Business goals: {{BUSINESS_GOALS}}

Analyze each opportunity and provide:
1. **Effort Score** (1-10): How much work is required
2. **Impact Score** (1-10): Expected business impact
3. **Priority Ranking**: Based on impact/effort ratio
4. **Quick Wins**: Opportunities that are high impact, low effort
5. **Strategic Bets**: High impact, high effort opportunities worth investing in
6. **Implementation Order**: Recommended sequence to tackle these

Return as JSON with:
{
  "quickWins": [list of opportunity IDs],
  "strategicBets": [list of opportunity IDs],
  "lowPriority": [list of opportunity IDs],
  "recommendedOrder": [ordered list of opportunity IDs],
  "reasoning": "Brief explanation of prioritization strategy"
}',
    'Opportunities',
    TRUE,
    NOW(),
    NOW()
) ON CONFLICT ("Name") DO UPDATE SET
    "Description" = EXCLUDED."Description",
    "PromptText" = EXCLUDED."PromptText",
    "UpdatedAt" = NOW();

-- 5. Content Opportunity Generator
INSERT INTO "LlmPromptTemplates" ("Name", "Description", "PromptText", "Category", "IsActive", "CreatedAt", "UpdatedAt")
VALUES (
    'ContentOpportunityGenerator',
    'Generates specific content opportunities based on search trends and AI platform analysis',
    'You are a content strategist specializing in AI-optimized content creation.

Analyze the following data and generate specific content opportunities:

Brand: {{BRAND_NAME}}
Industry: {{INDUSTRY}}
Target Audience: {{TARGET_AUDIENCE}}
Current Top Pages: {{TOP_PAGES}}
Competitor Content: {{COMPETITOR_CONTENT}}
Trending Topics: {{TRENDING_TOPICS}}
AI Platform Queries: {{AI_QUERIES}}

Generate 15-20 content opportunities that will:
- Answer questions AI platforms are being asked
- Fill gaps in current content coverage
- Target high-intent search queries
- Establish thought leadership
- Drive conversions

For each opportunity:
- **title**: Content piece title/topic
- **description**: What to cover and why it matters
- **impact**: Critical, High, Medium, or Low
- **type**: Article, Guide, FAQ, HowTo, Comparison, Case Study, etc.
- **targetKeywords**: Primary keywords/questions to target
- **estimatedTraffic**: Potential monthly visitors (0-10000+)
- **aiPlatformPotential**: Likelihood of being cited by AI (0-100)
- **recommendedLength**: Suggested word count
- **schemaTypes**: Recommended schema markup types
- **priority**: 1-5 (1 is highest)

Return as JSON array sorted by priority.

Example:
[
  {
    "title": "Complete Guide to [Topic]",
    "description": "Comprehensive resource answering the top 20 questions about [Topic]. High search volume with low competition.",
    "impact": "High",
    "type": "Guide",
    "targetKeywords": ["how to [topic]", "best practices [topic]", "[topic] guide"],
    "estimatedTraffic": 5000,
    "aiPlatformPotential": 85,
    "recommendedLength": 3000,
    "schemaTypes": ["Article", "HowTo", "FAQPage"],
    "priority": 1
  }
]',
    'Opportunities',
    TRUE,
    NOW(),
    NOW()
) ON CONFLICT ("Name") DO UPDATE SET
    "Description" = EXCLUDED."Description",
    "PromptText" = EXCLUDED."PromptText",
    "UpdatedAt" = NOW();

COMMENT ON TABLE "LlmPromptTemplates" IS 'Stores reusable prompt templates for LLM interactions';
