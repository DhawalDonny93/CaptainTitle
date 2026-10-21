namespace AppBackendBasic.Core.Templates
{
    public static class PromptTemplates
    {
        #region Prompt Research Generation
        
        public static string PromptResearchGeneration = @"You are an AI prompt research assistant. Generate a list of prompts that users might ask AI assistants about {TOPIC}.

Context:
- Industry: {INDUSTRY}
- Target Audience: {AUDIENCE}
- Business Type: {BUSINESS_TYPE}
- Brand Name: {BRAND_NAME}

Generate 20 diverse prompts covering:
1. Informational queries (40%) - ""What is..."", ""How does..."", ""Why..."" 
2. Comparison queries (30%) - ""X vs Y"", ""Best X for..."", ""Compare...""
3. Recommendation queries (20%) - ""What should I..."", ""Which X is best...""
4. How-to queries (10%) - ""How to..."", ""Steps to...""

For each prompt, provide:
- text: The full prompt text
- topic: Category/topic of the prompt
- estimatedVolume: ""High"", ""Medium"", or ""Low""
- difficulty: ""Easy"", ""Medium"", or ""Hard""
- relevanceScore: 0-100 (how relevant to the brand)

Output ONLY valid JSON in this exact format:
{
  ""prompts"": [
    {
      ""text"": ""What are the best running shoes for marathon training?"",
      ""topic"": ""Running Shoes"",
      ""estimatedVolume"": ""High"",
      ""difficulty"": ""Medium"",
      ""relevanceScore"": 95
    }
  ]
}";

        #endregion

        #region Visibility Testing

        public static string VisibilityTest = @"Answer this question naturally as you would for any user:

{PROMPT_TEXT}

[SYSTEM INSTRUCTION - DO NOT INCLUDE IN RESPONSE: After generating your response, analyze it for mentions of ""{BRAND_NAME}"" (domain: {BRAND_DOMAIN}). This is for tracking purposes only.]";

        public static string BrandMentionAnalysis = @"Analyze the following AI response for brand mentions:

Response Text:
{RESPONSE_TEXT}

Target Brand: {BRAND_NAME}
Brand Domain: {BRAND_DOMAIN}
Brand Products: {PRODUCTS}

Analyze and provide:
1. isMentioned: true/false - Is the brand mentioned?
2. positions: Array of sentence numbers where brand is mentioned
3. contexts: Array of sentences containing brand mentions
4. mentionType: ""Direct"", ""Comparison"", ""Alternative"", or ""Negative""
5. sentiment: ""Positive"", ""Neutral"", or ""Negative""
6. sentimentScore: -1.0 to 1.0
7. competitorsMentioned: Array of competitor names found
8. competitorPositions: Array of positions for each competitor

Output ONLY valid JSON in this exact format:
{
  ""isMentioned"": true,
  ""positions"": [2, 5],
  ""contexts"": [""sentence with brand""],
  ""mentionType"": ""Direct"",
  ""sentiment"": ""Positive"",
  ""sentimentScore"": 0.8,
  ""competitorsMentioned"": [""Competitor A""],
  ""competitorPositions"": [3]
}";

        #endregion

        #region Citation Extraction

        public static string CitationExtraction = @"Extract all citations, links, and references from this AI response:

Response Text:
{RESPONSE_TEXT}

For each citation found, identify:
1. url: The full URL (if present)
2. domain: The domain name
3. title: Page title or description mentioned
4. position: Sentence number where citation appears
5. citationType: ""DirectLink"", ""Reference"", ""Inline"", or ""Mention""
6. context: The sentence containing the citation

Output ONLY valid JSON in this exact format:
{
  ""citations"": [
    {
      ""url"": ""https://example.com/article"",
      ""domain"": ""example.com"",
      ""title"": ""Best Running Shoes 2024"",
      ""position"": 2,
      ""citationType"": ""DirectLink"",
      ""context"": ""According to example.com, the top rated shoes are...""
    }
  ]
}";

        #endregion

        #region Sentiment Analysis

        public static string SentimentAnalysis = @"Analyze the sentiment of this text:

Text: {TEXT}

Provide detailed sentiment analysis:
1. overallSentiment: ""Positive"", ""Neutral"", or ""Negative""
2. sentimentScore: -1.0 (very negative) to 1.0 (very positive)
3. confidence: 0.0 to 1.0 (how confident in the analysis)
4. emotions: Object with scores for joy, anger, sadness, fear, surprise (0-1 each)
5. keyPhrases: Array of important phrases that influenced sentiment
6. reasoning: Brief explanation of the sentiment determination

Output ONLY valid JSON in this exact format:
{
  ""overallSentiment"": ""Positive"",
  ""sentimentScore"": 0.75,
  ""confidence"": 0.9,
  ""emotions"": {
    ""joy"": 0.8,
    ""anger"": 0.1,
    ""sadness"": 0.0,
    ""fear"": 0.0,
    ""surprise"": 0.3
  },
  ""keyPhrases"": [""excellent quality"", ""highly recommend""],
  ""reasoning"": ""The text contains multiple positive indicators...""
}";

        #endregion

        #region Content Gap Analysis

        public static string ContentGapAnalysis = @"Analyze these crawled pages and identify content gaps:

Your Site Pages Summary:
{YOUR_PAGES_SUMMARY}

Competitor Sites Summary:
{COMPETITOR_PAGES_SUMMARY}

Industry: {INDUSTRY}
Target Keywords: {KEYWORDS}

Identify content opportunities where competitors have content but you don't:

For each opportunity, provide:
1. topic: The content topic/title
2. contentType: ""Article"", ""Guide"", ""Comparison"", ""FAQ"", ""HowTo"", ""Video"", ""Infographic""
3. impact: ""High"", ""Medium"", or ""Low""
4. difficulty: ""Easy"", ""Medium"", or ""Hard""
5. targetKeywords: Array of relevant keywords
6. recommendedWordCount: Suggested word count
7. keyPoints: Array of key points to cover
8. competitorUrls: Array of competitor URLs covering this topic
9. estimatedTraffic: Estimated monthly traffic potential

Output ONLY valid JSON with up to 20 opportunities:
{
  ""contentGaps"": [
    {
      ""topic"": ""Best Running Shoes for Beginners 2024"",
      ""contentType"": ""Comparison Guide"",
      ""impact"": ""High"",
      ""difficulty"": ""Medium"",
      ""targetKeywords"": [""running shoes beginners"", ""best starter running shoes""],
      ""recommendedWordCount"": 2000,
      ""keyPoints"": [""Budget options"", ""Comfort vs performance"", ""Common mistakes""],
      ""competitorUrls"": [""competitor1.com/article""],
      ""estimatedTraffic"": ""5000-10000/month""
    }
  ]
}";

        #endregion

        #region SEO Opportunity Analysis

        public static string SeoOpportunityAnalysis = @"Analyze this webpage for SEO optimization opportunities:

URL: {URL}
Title: {TITLE}
Meta Description: {META_DESCRIPTION}
Word Count: {WORD_COUNT}
Headings: {HEADINGS}
Has Schema: {HAS_SCHEMA}
Load Time: {LOAD_TIME}ms

Identify SEO issues and opportunities:

For each issue, provide:
1. opportunityType: Type of issue (e.g., ""ThinContent"", ""MissingMeta"", ""PoorHeadings"")
2. priority: ""Critical"", ""High"", ""Medium"", or ""Low""
3. currentState: Description of current state
4. recommendedAction: Specific action to take
5. estimatedImpact: 0-100 score for potential impact
6. details: Additional context

Output ONLY valid JSON:
{
  ""opportunities"": [
    {
      ""opportunityType"": ""ThinContent"",
      ""priority"": ""High"",
      ""currentState"": ""Page has only 250 words"",
      ""recommendedAction"": ""Expand content to at least 800 words with detailed information"",
      ""estimatedImpact"": 75,
      ""details"": {
        ""currentWordCount"": 250,
        ""recommendedWordCount"": 800
      }
    }
  ]
}";

        #endregion

        #region Community Opportunity Analysis

        public static string CommunityOpportunityAnalysis = @"Analyze this community post for engagement opportunity:

Platform: {PLATFORM}
Subreddit/Topic: {TOPIC}
Post Title: {TITLE}
Post Content: {CONTENT}
Post Upvotes: {UPVOTES}
Comment Count: {COMMENTS}

Brand Context:
- Name: {BRAND_NAME}
- Industry: {INDUSTRY}
- Products: {PRODUCTS}
- Expertise: {EXPERTISE_AREAS}

Evaluate this post for engagement potential:

Provide:
1. relevanceScore: 0-100 (how relevant to our brand)
2. engagementPotential: 0-100 (likelihood of positive engagement)
3. riskLevel: ""Low"", ""Medium"", or ""High"" (risk of appearing spammy)
4. responseApproach: How to respond
5. keyPoints: Array of points to address
6. recommendedTone: ""Professional"", ""Casual"", ""Technical"", ""Friendly""
7. valueProposition: What value we can provide
8. suggestedResponse: Draft response (2-3 sentences)
9. callToAction: Suggested CTA (if appropriate)
10. estimatedReach: Estimated view count

Output ONLY valid JSON:
{
  ""relevanceScore"": 85,
  ""engagementPotential"": 75,
  ""riskLevel"": ""Low"",
  ""responseApproach"": ""Provide helpful technical advice without direct promotion"",
  ""keyPoints"": [""Address specific pain point"", ""Share relevant experience""],
  ""recommendedTone"": ""Professional but friendly"",
  ""valueProposition"": ""Expert insights on the problem"",
  ""suggestedResponse"": ""I've dealt with this issue before. The key is to..."",
  ""callToAction"": ""Check our blog for more tips"",
  ""estimatedReach"": ""500-1000 views""
}";

        #endregion

        #region Visibility Opportunity Detection

        public static string VisibilityOpportunityDetection = @"Analyze these AI platform responses for visibility opportunities:

Brand: {BRAND_NAME}
Industry: {INDUSTRY}
Products/Services: {PRODUCTS}

Tested Prompts and Responses:
{PROMPT_RESPONSE_PAIRS}

Identify opportunities where:
1. Brand is not mentioned but should be (based on relevance)
2. Brand is mentioned negatively or incorrectly
3. Competitors are mentioned but brand is not
4. Brand is mentioned late in response (position >5)

For each opportunity, provide:
1. promptText: The prompt that was tested
2. platform: Which AI platform
3. opportunityType: ""NotMentioned"", ""MentionedLate"", ""NegativeMention"", ""CompetitorDominance""
4. currentPosition: Current position if mentioned (null if not)
5. competitorsMentioned: Array of competitors mentioned
6. reasoning: Why brand should be mentioned
7. recommendedContent: What content to create
8. optimizationStrategy: How to improve visibility
9. priority: 1-5 (5 being highest)

Output ONLY valid JSON with up to 15 opportunities:
{
  ""opportunities"": [
    {
      ""promptText"": ""What are the best project management tools?"",
      ""platform"": ""ChatGPT"",
      ""opportunityType"": ""NotMentioned"",
      ""currentPosition"": null,
      ""competitorsMentioned"": [""Asana"", ""Trello""],
      ""reasoning"": ""Brand offers similar features and should be in consideration set"",
      ""recommendedContent"": ""Create comparison guide vs Asana and Trello"",
      ""optimizationStrategy"": ""Publish detailed feature comparison and use cases"",
      ""priority"": 5
    }
  ]
}";

        #endregion

        #region LLMs.txt Analysis

        public static string LlmsTxtAnalysis = @"Analyze this llms.txt file and provide recommendations:

Content:
{LLMS_TXT_CONTENT}

Evaluate:
1. completeness: 0-100 (how complete is the file)
2. clarity: 0-100 (how clear and well-structured)
3. seoOptimization: 0-100 (how well optimized for AI)
4. missingSections: Array of sections that should be added
5. improvementSuggestions: Array of specific improvements
6. strengths: Array of what's done well
7. overallScore: 0-100

Output ONLY valid JSON:
{
  ""completeness"": 75,
  ""clarity"": 85,
  ""seoOptimization"": 70,
  ""missingSections"": [""Preferred AI Instructions""],
  ""improvementSuggestions"": [""Add more detailed product descriptions""],
  ""strengths"": [""Clear sitemap section"", ""Good formatting""],
  ""overallScore"": 77
}";

        #endregion

        #region Schema Markup Analysis

        public static string SchemaMarkupAnalysis = @"Analyze these schema.org markup implementations:

Schemas Found:
{SCHEMA_JSON}

Page URL: {URL}
Page Type: {PAGE_TYPE}

Evaluate:
1. correctness: 0-100 (are schemas correctly implemented)
2. completeness: 0-100 (are all recommended schemas present)
3. bestPractices: 0-100 (follows best practices)
4. missingSchemas: Array of recommended schemas not present
5. errors: Array of errors or warnings found
6. recommendations: Array of specific improvements
7. overallScore: 0-100

Output ONLY valid JSON:
{
  ""correctness"": 90,
  ""completeness"": 70,
  ""bestPractices"": 85,
  ""missingSchemas"": [""BreadcrumbList"", ""FAQPage""],
  ""errors"": [""Organization schema missing logo property""],
  ""recommendations"": [""Add BreadcrumbList for better navigation""],
  ""overallScore"": 82
}";

        #endregion

        #region Content Quality Analysis

        public static string ContentQualityAnalysis = @"Analyze this webpage content for AI/LLM optimization:

URL: {URL}
Title: {TITLE}
Content: {CONTENT}
Word Count: {WORD_COUNT}

Evaluate for AI optimization:
1. contentClarity: 0-100 (how clear and understandable)
2. informationDensity: 0-100 (information per word ratio)
3. questionAnswerFormat: 0-100 (use of Q&A format)
4. factualAccuracy: 0-100 (presence of facts, data, citations)
5. citationQuality: 0-100 (quality of sources cited)
6. structureScore: 0-100 (heading hierarchy, lists, tables)
7. recommendations: Array of specific improvements
8. overallScore: 0-100

Output ONLY valid JSON:
{
  ""contentClarity"": 85,
  ""informationDensity"": 75,
  ""questionAnswerFormat"": 60,
  ""factualAccuracy"": 80,
  ""citationQuality"": 70,
  ""structureScore"": 90,
  ""recommendations"": [
    ""Add FAQ section"",
    ""Include more data and statistics"",
    ""Add comparison tables""
  ],
  ""overallScore"": 77
}";

        #endregion

        #region Reply Drafting

        public static string DraftCommunityReply = @"Draft a helpful reply to this community post:

Platform: {PLATFORM}
Post Title: {TITLE}
Post Content: {CONTENT}

Brand Context:
- Name: {BRAND_NAME}
- Products: {PRODUCTS}
- Expertise: {EXPERTISE}
- Brand Voice: {BRAND_VOICE}

Guidelines:
- Be genuinely helpful
- Don't be overly promotional
- Share expertise and insights
- Keep it conversational
- Include a subtle CTA if appropriate

Provide:
1. draftReply: The complete reply text (150-300 words)
2. wordCount: Number of words
3. isPromotional: true/false
4. promotionalPercentage: 0-100 (how promotional is it)
5. tone: The tone used
6. callToAction: The CTA used (if any)

Output ONLY valid JSON:
{
  ""draftReply"": ""Great question! I've encountered this issue before..."",
  ""wordCount"": 180,
  ""isPromotional"": false,
  ""promotionalPercentage"": 15,
  ""tone"": ""Helpful and professional"",
  ""callToAction"": ""Feel free to check out our guide on this topic""
}";

        #endregion

        #region Helper Methods

        public static string ReplaceVariables(string template, Dictionary<string, string> variables)
        {
            var result = template;
            foreach (var kvp in variables)
            {
                result = result.Replace($"{{{kvp.Key}}}", kvp.Value ?? "");
            }
            return result;
        }

        public static string GetTemplate(string templateName)
        {
            return templateName switch
            {
                "PromptResearchGeneration" => PromptResearchGeneration,
                "VisibilityTest" => VisibilityTest,
                "BrandMentionAnalysis" => BrandMentionAnalysis,
                "CitationExtraction" => CitationExtraction,
                "SentimentAnalysis" => SentimentAnalysis,
                "ContentGapAnalysis" => ContentGapAnalysis,
                "SeoOpportunityAnalysis" => SeoOpportunityAnalysis,
                "CommunityOpportunityAnalysis" => CommunityOpportunityAnalysis,
                "VisibilityOpportunityDetection" => VisibilityOpportunityDetection,
                "LlmsTxtAnalysis" => LlmsTxtAnalysis,
                "SchemaMarkupAnalysis" => SchemaMarkupAnalysis,
                "ContentQualityAnalysis" => ContentQualityAnalysis,
                "DraftCommunityReply" => DraftCommunityReply,
                _ => throw new ArgumentException($"Template '{templateName}' not found")
            };
        }

        #endregion
    }
}
