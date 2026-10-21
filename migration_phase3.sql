-- Phase 3 Migration: Keywords and AiTrackingResult Updates

-- 1. Create Keywords table
CREATE TABLE "Keywords" (
    "Id" SERIAL NOT NULL,
    "ProjectId" uuid NOT NULL,
    "Text" character varying(200) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "PK_Keywords" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Keywords_Projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "Projects" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_Keywords_ProjectId" ON "Keywords" ("ProjectId");

-- 2. Update AiTrackingPrompts table
ALTER TABLE "AiTrackingPrompts" ADD COLUMN "KeywordId" integer;
ALTER TABLE "AiTrackingPrompts" ADD CONSTRAINT "FK_AiTrackingPrompts_Keywords_KeywordId" FOREIGN KEY ("KeywordId") REFERENCES "Keywords" ("Id") ON DELETE SET NULL;
CREATE INDEX "IX_AiTrackingPrompts_KeywordId" ON "AiTrackingPrompts" ("KeywordId");

-- 3. Update AiTrackingResults table
ALTER TABLE "AiTrackingResults" ADD COLUMN "VisibilityScore" integer NOT NULL DEFAULT 0;
ALTER TABLE "AiTrackingResults" ADD COLUMN "Sentiment" character varying(20);
