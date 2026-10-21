# PromptTracking CreatedByUserId Column Fix

## 🔴 Error

```
Npgsql.PostgresException: 42703: column p.CreatedByUserId does not exist
```

**Location:** `PromptTrackings` table query  
**Cause:** EF Core entity still references `CreatedByUserId` property but database column was removed

---

## 🔍 Root Cause

The `PromptTracking` entity had **both** `UserId` and `CreatedByUserId` columns:
- **`UserId`** - The user who owns/created the prompt tracking
- **`CreatedByUserId`** - Redundant duplicate of UserId

The database column was removed, but the EF Core entity still had the navigation property `CreatedByUser` which caused EF Core to look for the `CreatedByUserId` foreign key column.

---

## ✅ Solution

### **1. Removed Navigation Property from Entity**

**File:** `@/Users/dhawal/Documents/GitHub/AISEO/AISEODevelop/AISEO/AppBackendBasic.Core/Entities/PromptTracking.cs`

**Before:**
```csharp
// Navigation properties
public virtual User User { get; set; } = null!;
public virtual Project Project { get; set; } = null!;
public virtual User CreatedByUser { get; set; } = null!;  // ❌ Removed
public virtual Keyword? Keyword { get; set; }
```

**After:**
```csharp
// Navigation properties
public virtual User User { get; set; } = null!;
public virtual Project Project { get; set; } = null!;
public virtual Keyword? Keyword { get; set; }
```

### **2. Database Cleanup Script**

**File:** `@/Users/dhawal/Documents/GitHub/AISEO/AISEODevelop/AISEO/DATABASE_CLEANUP_DUPLICATE_COLUMNS.sql`

```sql
-- Drop CreatedByUserId column from PromptTrackings
ALTER TABLE "PromptTrackings" 
DROP COLUMN IF EXISTS "CreatedByUserId";
```

---

## 📊 Analysis: All Entities Checked

### **Entities with BOTH UserId and CreatedByUserId:**
- ❌ **PromptTracking** - Had duplicate (FIXED)

### **Entities with CreatedByUserId ONLY (No UserId):**
- ✅ **LlmPromptTemplate** - Admin-created templates (CORRECT - no user ownership)

### **Entities with UserId ONLY:**
- ✅ All other 40+ entities use `UserId` correctly

**Conclusion:** Only `PromptTracking` had the duplicate column issue. All other entities follow the correct pattern.

---

## 🗄️ Database Pattern

### **Standard Pattern (User-Owned Data):**
```csharp
public int UserId { get; set; }
public virtual User User { get; set; } = null!;
```

**Used by:** Projects, Citations, SiteAudits, Opportunities, Keywords, etc.

### **Admin-Created Pattern (No User Ownership):**
```csharp
public int? CreatedByUserId { get; set; }  // Admin who created it
// No UserId - not owned by regular users
```

**Used by:** LlmPromptTemplates (admin templates)

---

## 🔧 Steps to Apply Fix

### **1. Run Database Cleanup Script**
```bash
psql "$DATABASE_URL" -f DATABASE_CLEANUP_DUPLICATE_COLUMNS.sql
```

Or run manually:
```sql
ALTER TABLE "PromptTrackings" DROP COLUMN IF EXISTS "CreatedByUserId";
```

### **2. Rebuild Application**
```bash
dotnet clean
dotnet build
```

### **3. Restart Application**
```bash
dotnet run --project AppBackendBasic.API
```

### **4. Test Prompts Endpoint**
```bash
curl http://localhost:5000/api/prompts?projectId=5
```

Should return prompts without the `CreatedByUserId` error.

---

## ✅ Verification

### **Check Entity:**
```csharp
// PromptTracking.cs should NOT have:
public virtual User CreatedByUser { get; set; }
```

### **Check Database:**
```sql
-- Should return 0 rows
SELECT column_name 
FROM information_schema.columns
WHERE table_name = 'PromptTrackings' 
AND column_name = 'CreatedByUserId';
```

### **Check for Other Duplicates:**
```sql
-- Should return 0 rows (no tables with both columns)
SELECT table_name, COUNT(*) as column_count
FROM information_schema.columns
WHERE table_schema = 'public'
AND column_name IN ('UserId', 'CreatedByUserId')
GROUP BY table_name
HAVING COUNT(*) > 1;
```

---

## 📝 Summary

**Problem:** `PromptTracking` entity had navigation property for deleted `CreatedByUserId` column  
**Solution:** Removed `CreatedByUser` navigation property from entity  
**Database:** Run cleanup script to ensure column is dropped  
**Result:** No more duplicate columns, consistent UserId pattern across all entities  
**Status:** Ready to test after rebuild
