-- Migration 17: Brand Profile and Competitors
-- Adds BrandProfiles and Competitors tables for project branding and competitive tracking

-- BrandProfiles table
CREATE TABLE IF NOT EXISTS "BrandProfiles" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" INTEGER NOT NULL,
    "Overview" VARCHAR(120),
    "Description" TEXT,
    "Topics" TEXT, -- JSON array of selected topics
    "LogoUrl" VARCHAR(500),
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP,
    
    CONSTRAINT "FK_BrandProfiles_Projects" 
        FOREIGN KEY ("ProjectId") 
        REFERENCES "Projects"("Id") 
        ON DELETE CASCADE,
    
    CONSTRAINT "UQ_BrandProfiles_ProjectId" 
        UNIQUE ("ProjectId")
);

CREATE INDEX "IX_BrandProfiles_ProjectId" ON "BrandProfiles"("ProjectId");

-- Competitors table
CREATE TABLE IF NOT EXISTS "Competitors" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" INTEGER NOT NULL,
    "Name" VARCHAR(200) NOT NULL,
    "Domain" VARCHAR(255) NOT NULL,
    "LogoUrl" VARCHAR(500),
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP,
    
    CONSTRAINT "FK_Competitors_Projects" 
        FOREIGN KEY ("ProjectId") 
        REFERENCES "Projects"("Id") 
        ON DELETE CASCADE
);

CREATE INDEX "IX_Competitors_ProjectId" ON "Competitors"("ProjectId");
CREATE INDEX "IX_Competitors_Domain" ON "Competitors"("Domain");

-- Add comment for documentation
COMMENT ON TABLE "BrandProfiles" IS 'Stores brand profile information including overview, description, and topics for each project';
COMMENT ON TABLE "Competitors" IS 'Stores competitor information with logos for competitive tracking';
