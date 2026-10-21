#!/bin/bash

# Navigate to the API project directory
cd AppBackendBasic.API

# Check if a migration name was provided
if [ -z "$1" ]
then
    echo "Please provide a migration name"
    echo "Usage: ./migrate.sh <MigrationName>"
    exit 1
fi

# Create a new migration
echo "Creating migration: $1"
dotnet ef migrations add $1 --project ../AppBackendBasic.Infrastructure

# Apply the migration to the database
echo "Applying migration to database"
dotnet ef database update

echo "Migration complete"
