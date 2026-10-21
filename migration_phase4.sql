-- Phase 4 Migration: CommunityOpportunities

CREATE TABLE "CommunityOpportunities" (
    "Id" SERIAL NOT NULL,
    "ProjectId" uuid NOT NULL,
    "KeywordId" integer,
    "Platform" character varying(50) NOT NULL,
    "Title" character varying(300) NOT NULL,
    "Url" text NOT NULL,
    "Summary" text,
    "Sentiment" character varying(50),
    "Status" character varying(20) NOT NULL DEFAULT 'New',
    "DraftReply" text,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "PK_CommunityOpportunities" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CommunityOpportunities_Projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "Projects" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_CommunityOpportunities_Keywords_KeywordId" FOREIGN KEY ("KeywordId") REFERENCES "Keywords" ("Id") ON DELETE SET NULL
);

CREATE INDEX "IX_CommunityOpportunities_ProjectId" ON "CommunityOpportunities" ("ProjectId");
CREATE INDEX "IX_CommunityOpportunities_KeywordId" ON "CommunityOpportunities" ("KeywordId");
