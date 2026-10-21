# ProviderId Column Error Fix

## 🔴 Error

```
SqlState: 42703
MessageText: column l0.ProviderId does not exist
Hint: Perhaps you meant to reference the column "l0.Provider"
```

**Root Cause:** The database column is likely named `"Provider"` instead of `"ProviderId"`, or the migration wasn't run properly.

---

## ✅ Fixes Applied

### **1. Added Explicit Column Mapping to Entity**

**File:** `@/Users/dhawal/Documents/GitHub/AISEO/AISEODevelop/AISEO/AppBackendBasic.Core/Entities/LlmProviderConfig.cs`

```csharp
[Column("ProviderId")]
public int ProviderId { get; set; }

[ForeignKey("ProviderId")]
public virtual LlmProvider Provider { get; set; } = null!;
```

This explicitly tells EF Core to use `"ProviderId"` as the column name, not `"Provider"`.

---

### **2. Added Constraint Name to DbContext**

**File:** `@/Users/dhawal/Documents/GitHub/AISEO/AISEODevelop/AISEO/AppBackendBasic.Infrastructure/Data/ApplicationDbContext.cs`

```csharp
entity.HasOne(e => e.Provider)
      .WithMany(p => p.Configs)
      .HasForeignKey(e => e.ProviderId)
      .HasConstraintName("FK_LlmProviderConfig_Provider")  // ← Added
      .OnDelete(DeleteBehavior.Cascade);
```

---

### **3. Created Database Verification Script**

**File:** `@/Users/dhawal/Documents/GitHub/AISEO/AISEODevelop/AISEO/Database_Migrations/005_Verify_And_Fix_LlmProviderConfig.sql`

This script will:
1. Check what columns actually exist in `LlmProviderConfig` table
2. Check foreign key constraints
3. Provide fix options based on what's found

---

## 🔧 Required Action

**Run the verification script to diagnose the issue:**

```bash
psql -h localhost -U postgres -d aiseo_db -f Database_Migrations/005_Verify_And_Fix_LlmProviderConfig.sql
```

**The script will show you:**
- Current column names in the table
- Whether the column is named `"Provider"` or `"ProviderId"` or missing

**Based on the results:**

### **Scenario 1: Column is named "Provider"**
Uncomment and run the rename section in the script:
```sql
ALTER TABLE "LlmProviderConfig" RENAME COLUMN "Provider" TO "ProviderId";
```

### **Scenario 2: Column doesn't exist**
Uncomment and run the add column section in the script:
```sql
ALTER TABLE "LlmProviderConfig" ADD COLUMN "ProviderId" INTEGER NOT NULL DEFAULT 1;
```

### **Scenario 3: Migration wasn't run**
Run the original migration:
```bash
psql -h localhost -U postgres -d aiseo_db -f Database_Migrations/01_add_llm_provider_tables.sql
```

---

## 🧪 After Fix - Rebuild and Test

```bash
# Rebuild the solution
dotnet build

# Run the application
dotnet run --project AppBackendBasic.API

# Test the endpoint
# Navigate to AdminUI → LLM Providers
# Should load without 401 or column errors
```

---

## 📋 Summary

**Changes Made:**
1. ✅ Added `[Column("ProviderId")]` attribute to entity
2. ✅ Added `[ForeignKey("ProviderId")]` attribute to navigation property
3. ✅ Added constraint name to DbContext configuration
4. ✅ Created verification and fix SQL script

**Next Steps:**
1. Run verification script to check database state
2. Apply appropriate fix based on results
3. Rebuild and test

The code changes ensure EF Core uses the correct column name. The database fix ensures the column exists with the correct name.
