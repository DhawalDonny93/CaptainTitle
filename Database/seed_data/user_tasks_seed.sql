-- Seed Data: UserTasks for Project ID 1
-- Date: 2026-02-01
-- Description: Sample tasks with various statuses, priorities, and sources

-- IMPORTANT: Replace 'YOUR_PROJECT_UUID_HERE' with your actual Project UUID from the Project table
-- You can find it by running: SELECT "Id", "Name" FROM "Project" LIMIT 5;

-- Clear existing tasks for this project (optional - comment out if you want to keep existing data)
-- DELETE FROM "UserTasks" WHERE "ProjectId" = 'YOUR_PROJECT_UUID_HERE';

-- Insert sample tasks for your project
-- Replace 'YOUR_PROJECT_UUID_HERE' with the actual UUID of your project
INSERT INTO "UserTasks" ("ProjectId", "Title", "Description", "Priority", "Status", "IsCompleted", "DueDate", "Source", "CreatedAt")
VALUES
-- TO-DO Tasks (High Priority)
('YOUR_PROJECT_UUID_HERE', 'Review your first opportunities', 'Head to the Opportunities page to see potential content topics and engagement opportunities', 'High', 'TO-DO', false, CURRENT_DATE + INTERVAL '3 days', 'SystemGenerated', CURRENT_TIMESTAMP),
('YOUR_PROJECT_UUID_HERE', 'Optimize meta descriptions for top 10 pages', 'Update meta descriptions to improve click-through rates from search results', 'High', 'TO-DO', false, CURRENT_DATE + INTERVAL '5 days', 'Manual', CURRENT_TIMESTAMP),
('YOUR_PROJECT_UUID_HERE', 'Conduct keyword research for Q1 content', 'Identify high-value keywords for upcoming content calendar', 'High', 'TO-DO', false, CURRENT_DATE + INTERVAL '7 days', 'Manual', CURRENT_TIMESTAMP),

-- TO-DO Tasks (Medium Priority)
('YOUR_PROJECT_UUID_HERE', 'Update internal linking structure', 'Review and improve internal links to boost page authority', 'Medium', 'TO-DO', false, CURRENT_DATE + INTERVAL '10 days', 'Manual', CURRENT_TIMESTAMP),
('YOUR_PROJECT_UUID_HERE', 'Create FAQ schema markup', 'Add structured data for frequently asked questions', 'Medium', 'TO-DO', false, CURRENT_DATE + INTERVAL '14 days', 'SystemGenerated', CURRENT_TIMESTAMP),

-- TO-DO Tasks (Low Priority)
('YOUR_PROJECT_UUID_HERE', 'Unlock automated opportunities', 'Get AI-powered tasks delivered automatically', 'Low', 'TO-DO', false, NULL, 'SystemGenerated', CURRENT_TIMESTAMP),
('YOUR_PROJECT_UUID_HERE', 'Review competitor backlink profiles', 'Analyze competitor link building strategies', 'Low', 'TO-DO', false, NULL, 'Manual', CURRENT_TIMESTAMP),

-- IN PROGRESS Tasks
('YOUR_PROJECT_UUID_HERE', 'Write blog post: "10 SEO Tips for 2026"', 'Create comprehensive guide for modern SEO practices', 'High', 'IN PROGRESS', false, CURRENT_DATE + INTERVAL '2 days', 'Manual', CURRENT_TIMESTAMP - INTERVAL '2 days'),
('YOUR_PROJECT_UUID_HERE', 'Implement Core Web Vitals improvements', 'Optimize LCP, FID, and CLS metrics for better performance', 'High', 'IN PROGRESS', false, CURRENT_DATE + INTERVAL '5 days', 'SystemGenerated', CURRENT_TIMESTAMP - INTERVAL '3 days'),
('YOUR_PROJECT_UUID_HERE', 'Update product page images', 'Replace low-quality images with optimized versions', 'Medium', 'IN PROGRESS', false, CURRENT_DATE + INTERVAL '4 days', 'Manual', CURRENT_TIMESTAMP - INTERVAL '1 day'),

-- IN REVIEW Tasks
('YOUR_PROJECT_UUID_HERE', 'Content audit for outdated articles', 'Review and update or remove outdated content pieces', 'Medium', 'IN REVIEW', false, CURRENT_DATE + INTERVAL '3 days', 'Manual', CURRENT_TIMESTAMP - INTERVAL '5 days'),
('YOUR_PROJECT_UUID_HERE', 'Mobile usability improvements', 'Fix mobile-specific UX issues identified in audit', 'High', 'IN REVIEW', false, CURRENT_DATE + INTERVAL '2 days', 'SystemGenerated', CURRENT_TIMESTAMP - INTERVAL '4 days'),

-- BACKLOG Tasks
('YOUR_PROJECT_UUID_HERE', 'Create video content for YouTube', 'Expand content strategy to include video marketing', 'Medium', 'BACKLOG', false, NULL, 'Manual', CURRENT_TIMESTAMP - INTERVAL '10 days'),
('YOUR_PROJECT_UUID_HERE', 'Implement voice search optimization', 'Optimize content for voice search queries', 'Low', 'BACKLOG', false, NULL, 'Manual', CURRENT_TIMESTAMP - INTERVAL '15 days'),
('YOUR_PROJECT_UUID_HERE', 'Set up Google Analytics 4 events', 'Configure custom events for better tracking', 'Medium', 'BACKLOG', false, NULL, 'SystemGenerated', CURRENT_TIMESTAMP - INTERVAL '8 days'),
('YOUR_PROJECT_UUID_HERE', 'Create social media content calendar', 'Plan and schedule social media posts for next quarter', 'Low', 'BACKLOG', false, NULL, 'Manual', CURRENT_TIMESTAMP - INTERVAL '12 days'),

-- DONE Tasks
('YOUR_PROJECT_UUID_HERE', 'Set up Google Search Console', 'Connect website to GSC for monitoring search performance', 'High', 'DONE', true, NULL, 'SystemGenerated', CURRENT_TIMESTAMP - INTERVAL '30 days'),
('YOUR_PROJECT_UUID_HERE', 'Install SSL certificate', 'Secure website with HTTPS protocol', 'High', 'DONE', true, NULL, 'Manual', CURRENT_TIMESTAMP - INTERVAL '25 days'),
('YOUR_PROJECT_UUID_HERE', 'Create XML sitemap', 'Generate and submit sitemap to search engines', 'High', 'DONE', true, NULL, 'SystemGenerated', CURRENT_TIMESTAMP - INTERVAL '28 days'),
('YOUR_PROJECT_UUID_HERE', 'Fix broken links on homepage', 'Identify and repair all broken links', 'Medium', 'DONE', true, NULL, 'Manual', CURRENT_TIMESTAMP - INTERVAL '20 days'),
('YOUR_PROJECT_UUID_HERE', 'Optimize homepage load time', 'Reduce page load time to under 2 seconds', 'High', 'DONE', true, NULL, 'SystemGenerated', CURRENT_TIMESTAMP - INTERVAL '22 days'),
('YOUR_PROJECT_UUID_HERE', 'Add alt text to all images', 'Improve accessibility and SEO with descriptive alt text', 'Medium', 'DONE', true, NULL, 'Manual', CURRENT_TIMESTAMP - INTERVAL '18 days'),
('YOUR_PROJECT_UUID_HERE', 'Create robots.txt file', 'Configure crawling rules for search engines', 'Medium', 'DONE', true, NULL, 'SystemGenerated', CURRENT_TIMESTAMP - INTERVAL '26 days');

-- Verify the inserted data
SELECT 
    "Status",
    "Priority",
    COUNT(*) as task_count
FROM "UserTasks"
WHERE "ProjectId" = 'YOUR_PROJECT_UUID_HERE'
GROUP BY "Status", "Priority"
ORDER BY 
    CASE "Status"
        WHEN 'TO-DO' THEN 1
        WHEN 'IN PROGRESS' THEN 2
        WHEN 'IN REVIEW' THEN 3
        WHEN 'BACKLOG' THEN 4
        WHEN 'DONE' THEN 5
    END,
    CASE "Priority"
        WHEN 'High' THEN 1
        WHEN 'Medium' THEN 2
        WHEN 'Low' THEN 3
    END;
