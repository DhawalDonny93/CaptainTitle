-- Seed test citation data for debugging Top Citations section
-- This script adds sample citations for testing purposes
-- Replace ProjectId = 1 with your actual project ID

-- First, let's insert some test citations
INSERT INTO "Citations" (
    "ProjectId",
    "SourceUrl",
    "SourceDomain",
    "SourceTitle",
    "Platform",
    "PromptText",
    "Position",
    "CitationType",
    "Context",
    "DiscoveredAt",
    "CreatedAt"
) VALUES
    (1, 'https://example.com/article1', 'example.com', 'Example Article 1', 'ChatGPT', 'What is AI?', 1, 'DirectLink', 'AI is artificial intelligence...', NOW() - INTERVAL '5 days', NOW() - INTERVAL '5 days'),
    (1, 'https://example.com/article2', 'example.com', 'Example Article 2', 'ChatGPT', 'How does AI work?', 2, 'DirectLink', 'AI works by...', NOW() - INTERVAL '4 days', NOW() - INTERVAL '4 days'),
    (1, 'https://wikipedia.org/wiki/ai', 'wikipedia.org', 'Artificial Intelligence - Wikipedia', 'ChatGPT', 'What is AI?', 1, 'DirectLink', 'From Wikipedia...', NOW() - INTERVAL '5 days', NOW() - INTERVAL '5 days'),
    (1, 'https://wikipedia.org/wiki/ml', 'wikipedia.org', 'Machine Learning - Wikipedia', 'Perplexity', 'What is machine learning?', 2, 'DirectLink', 'Machine learning is...', NOW() - INTERVAL '3 days', NOW() - INTERVAL '3 days'),
    (1, 'https://stackoverflow.com/questions/123', 'stackoverflow.com', 'How to implement AI', 'ChatGPT', 'How to code AI?', 3, 'DirectLink', 'You can implement AI by...', NOW() - INTERVAL '2 days', NOW() - INTERVAL '2 days'),
    (1, 'https://github.com/ai-project', 'github.com', 'AI Project Repository', 'Perplexity', 'AI code examples', 1, 'DirectLink', 'Here is an AI project...', NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 day'),
    (1, 'https://medium.com/ai-article', 'medium.com', 'Understanding AI', 'ChatGPT', 'Explain AI', 2, 'DirectLink', 'AI explained...', NOW() - INTERVAL '6 days', NOW() - INTERVAL '6 days'),
    (1, 'https://towardsdatascience.com/ai', 'towardsdatascience.com', 'AI Deep Dive', 'Perplexity', 'Deep learning basics', 1, 'DirectLink', 'Deep learning is...', NOW() - INTERVAL '4 days', NOW() - INTERVAL '4 days'),
    (1, 'https://arxiv.org/ai-paper', 'arxiv.org', 'AI Research Paper', 'ChatGPT', 'Latest AI research', 3, 'DirectLink', 'Recent research shows...', NOW() - INTERVAL '3 days', NOW() - INTERVAL '3 days'),
    (1, 'https://openai.com/blog/ai', 'openai.com', 'OpenAI Blog Post', 'ChatGPT', 'OpenAI updates', 1, 'DirectLink', 'OpenAI has released...', NOW() - INTERVAL '2 days', NOW() - INTERVAL '2 days')
ON CONFLICT DO NOTHING;

-- Now create citation aggregations for the past 7 days
-- This aggregates the citations by domain and date
INSERT INTO "CitationAggregations" (
    "ProjectId",
    "SourceDomain",
    "Platform",
    "AggregationDate",
    "TotalCitations",
    "UniqueCitations",
    "AveragePosition",
    "TopPosition",
    "CreatedAt"
)
SELECT 
    "ProjectId",
    "SourceDomain",
    "Platform",
    DATE("DiscoveredAt") AS "AggregationDate",
    COUNT(*) AS "TotalCitations",
    COUNT(DISTINCT "SourceUrl") AS "UniqueCitations",
    AVG("Position") AS "AveragePosition",
    MIN("Position") AS "TopPosition",
    NOW() AS "CreatedAt"
FROM "Citations"
WHERE "ProjectId" = 1  -- Replace with your project ID
    AND "DiscoveredAt" >= NOW() - INTERVAL '7 days'
GROUP BY "ProjectId", "SourceDomain", "Platform", DATE("DiscoveredAt")
ON CONFLICT ("ProjectId", "SourceDomain", "Platform", "AggregationDate") DO NOTHING;

-- Verify the data was inserted
SELECT 
    'Citations Inserted' AS "Status",
    COUNT(*) AS "Count"
FROM "Citations"
WHERE "ProjectId" = 1;

SELECT 
    'Citation Aggregations Created' AS "Status",
    COUNT(*) AS "Count"
FROM "CitationAggregations"
WHERE "ProjectId" = 1;

-- Show top domains
SELECT 
    "SourceDomain",
    COUNT(*) AS "CitationCount",
    AVG("Position") AS "AvgPosition"
FROM "Citations"
WHERE "ProjectId" = 1
GROUP BY "SourceDomain"
ORDER BY "CitationCount" DESC
LIMIT 10;
