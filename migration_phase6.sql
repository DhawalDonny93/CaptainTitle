-- Phase 6 Migration: UserTasks and Notifications

CREATE TABLE "UserTasks" (
    "Id" SERIAL NOT NULL,
    "ProjectId" uuid NOT NULL,
    "Title" character varying(200) NOT NULL,
    "Description" text,
    "Priority" character varying(20) NOT NULL DEFAULT 'Medium',
    "IsCompleted" boolean NOT NULL DEFAULT FALSE,
    "DueDate" timestamp with time zone,
    "Source" character varying(50) DEFAULT 'Manual',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "PK_UserTasks" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_UserTasks_Projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "Projects" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_UserTasks_ProjectId" ON "UserTasks" ("ProjectId");

CREATE TABLE "Notifications" (
    "Id" SERIAL NOT NULL,
    "UserId" uuid,
    "Message" character varying(300) NOT NULL,
    "Type" character varying(20) NOT NULL DEFAULT 'Info',
    "IsRead" boolean NOT NULL DEFAULT FALSE,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "PK_Notifications" PRIMARY KEY ("Id")
);
