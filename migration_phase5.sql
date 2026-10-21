-- Phase 5 Migration: SiteAudits

CREATE TABLE "SiteAudits" (
    "Id" SERIAL NOT NULL,
    "ProjectId" uuid NOT NULL,
    "AuditType" character varying(50) NOT NULL,
    "Status" character varying(20) NOT NULL,
    "Details" text,
    "Score" integer NOT NULL,
    "CheckDate" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "PK_SiteAudits" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_SiteAudits_Projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "Projects" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_SiteAudits_ProjectId" ON "SiteAudits" ("ProjectId");
