#!/bin/bash

# ============================================================================
# AISEO Quick Setup Script
# Description: Automated setup for AISEO real data generation features
# Usage: bash scripts/quick_setup.sh
# ============================================================================

set -e  # Exit on error

echo "=========================================="
echo "AISEO Quick Setup"
echo "=========================================="
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# ============================================================================
# Step 1: Check Prerequisites
# ============================================================================

echo "Step 1: Checking prerequisites..."

# Check if PostgreSQL is installed
if ! command -v psql &> /dev/null; then
    echo -e "${RED}Error: PostgreSQL is not installed${NC}"
    exit 1
fi

echo -e "${GREEN}✓ PostgreSQL found${NC}"

# ============================================================================
# Step 2: Database Configuration
# ============================================================================

echo ""
echo "Step 2: Database Configuration"
echo "Enter your database connection details:"

read -p "Database name (default: aiseo_db): " DB_NAME
DB_NAME=${DB_NAME:-aiseo_db}

read -p "Database user (default: postgres): " DB_USER
DB_USER=${DB_USER:-postgres}

read -p "Database host (default: localhost): " DB_HOST
DB_HOST=${DB_HOST:-localhost}

read -p "Database port (default: 5432): " DB_PORT
DB_PORT=${DB_PORT:-5432}

echo ""
echo "Using database: $DB_NAME on $DB_HOST:$DB_PORT as $DB_USER"

# ============================================================================
# Step 3: Run Database Migration
# ============================================================================

echo ""
echo "Step 3: Running database migration..."

MIGRATION_FILE="database_migrations/11_add_crawler_and_opportunities_tables.sql"

if [ ! -f "$MIGRATION_FILE" ]; then
    echo -e "${RED}Error: Migration file not found: $MIGRATION_FILE${NC}"
    exit 1
fi

echo "Running migration: $MIGRATION_FILE"

PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -f $MIGRATION_FILE

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Migration completed successfully${NC}"
else
    echo -e "${RED}✗ Migration failed${NC}"
    exit 1
fi

# ============================================================================
# Step 4: Enable Project Features
# ============================================================================

echo ""
echo "Step 4: Enable project features"
read -p "Do you want to enable features for all projects? (y/n): " ENABLE_ALL

if [ "$ENABLE_ALL" = "y" ]; then
    echo "Enabling features for all projects..."
    
    PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME <<EOF
UPDATE "Projects" SET
    "EnableAutoCrawl" = true,
    "CrawlMaxPages" = 100,
    "CrawlMaxDepth" = 3,
    "EnableOpportunityDetection" = true,
    "EnableVisibilityAlerts" = true,
    "VisibilityCheckFrequency" = 'Hourly';
EOF
    
    echo -e "${GREEN}✓ Features enabled for all projects${NC}"
else
    echo "Skipping automatic feature enablement."
    echo "You can manually enable features using: scripts/enable_project_features.sql"
fi

# ============================================================================
# Step 5: Verify Setup
# ============================================================================

echo ""
echo "Step 5: Verifying setup..."

# Check if tables were created
TABLE_COUNT=$(PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -t -c "
SELECT COUNT(*) FROM information_schema.tables 
WHERE table_schema = 'public' 
AND table_name IN (
    'ContentGapOpportunities', 'SeoOpportunities', 'VisibilityOpportunities',
    'VisibilityTrends', 'VisibilityAlerts',
    'LlmProviderConfigs', 'LlmUsageLogs', 'PromptTemplates',
    'JobExecutionLogs'
);
" | xargs)

echo "New tables created: $TABLE_COUNT / 9"

if [ "$TABLE_COUNT" -eq 9 ]; then
    echo -e "${GREEN}✓ All tables created successfully${NC}"
else
    echo -e "${YELLOW}⚠ Warning: Expected 9 tables, found $TABLE_COUNT${NC}"
fi

# Check project configuration
PROJECT_COUNT=$(PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -t -c "
SELECT COUNT(*) FROM \"Projects\" 
WHERE \"EnableAutoCrawl\" = true 
OR \"EnableOpportunityDetection\" = true 
OR \"EnableVisibilityAlerts\" = true;
" | xargs)

echo "Projects with features enabled: $PROJECT_COUNT"

# ============================================================================
# Step 6: Summary
# ============================================================================

echo ""
echo "=========================================="
echo "Setup Complete!"
echo "=========================================="
echo ""
echo "Next steps:"
echo "1. Add API keys to appsettings.json or environment variables"
echo "2. Configure Hangfire in Program.cs (see READY_TO_USE_GUIDE.md)"
echo "3. Start your application"
echo "4. Monitor jobs at /hangfire dashboard"
echo ""
echo "Documentation:"
echo "- READY_TO_USE_GUIDE.md - Quick start guide"
echo "- DATA_GENERATION_AND_JOBS_PRD.md - Complete PRD"
echo ""
echo "Database: $DB_NAME on $DB_HOST:$DB_PORT"
echo "Tables created: $TABLE_COUNT / 9"
echo "Projects enabled: $PROJECT_COUNT"
echo ""
echo -e "${GREEN}✓ AISEO is ready to use!${NC}"
