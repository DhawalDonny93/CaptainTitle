-- Check if there's any citation data in the database
-- Run this to diagnose why Top Citations section is empty

-- 1. Check Citations table
SELECT 
    'Citations' AS "TableName",
    COUNT(*) AS "TotalRecords",
    COUNT(DISTINCT "ProjectId") AS "UniqueProjects",
    COUNT(DISTINCT "SourceDomain") AS "UniqueDomains"
FROM "Citations";

-- 2. Check CitationAggregations table
SELECT 
    'CitationAggregations' AS "TableName",
    COUNT(*) AS "TotalRecords",
    COUNT(DISTINCT "ProjectId") AS "UniqueProjects",
    COUNT(DISTINCT "SourceDomain") AS "UniqueDomains"
FROM "CitationAggregations";

-- 3. Show sample citations if any exist
SELECT 
    "Id",
    "ProjectId",
    "SourceDomain",
    "SourceUrl",
    "Platform",
    "Position",
    "DiscoveredAt",
    "CreatedAt"
FROM "Citations"
ORDER BY "CreatedAt" DESC
LIMIT 10;

-- 4. Show sample citation aggregations if any exist
SELECT 
    "Id",
    "ProjectId",
    "SourceDomain",
    "Platform",
    "AggregationDate",
    "TotalCitations",
    "UniqueCitations",
    "AveragePosition",
    "CreatedAt"
FROM "CitationAggregations"
ORDER BY "CreatedAt" DESC
LIMIT 10;

-- 5. Check for a specific project (replace with your actual ProjectId)
-- SELECT 
--     "SourceDomain",
--     COUNT(*) AS "CitationCount",
--     AVG("Position") AS "AvgPosition"
-- FROM "Citations"
-- WHERE "ProjectId" = 1  -- Replace with your project ID
-- GROUP BY "SourceDomain"
-- ORDER BY "CitationCount" DESC
-- LIMIT 10;
