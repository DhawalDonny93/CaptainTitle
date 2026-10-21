# First-Time User Onboarding Prompts Feature

## 🎯 Overview

This feature automatically generates 5 custom prompts for new users on their first login using ChatGPT, displays them in a popup modal for selection, and triggers AI visibility tracking jobs for approved prompts.

---

## 📊 Implementation Summary

### **1. Database Changes**

**File:** `@/Users/dhawal/Documents/GitHub/AISEO/AISEODevelop/AISEO/ADD_ISFIRSTLOGIN_COLUMN.sql`

Added `IsFirstLogin` column to `Users` table:
```sql
ALTER TABLE "Users" 
ADD COLUMN "IsFirstLogin" BOOLEAN NOT NULL DEFAULT TRUE;
```

**Run migration:**
```bash
psql "$DATABASE_URL" -f ADD_ISFIRSTLOGIN_COLUMN.sql
```

---

### **2. Backend Changes**

#### **A. User Entity**

**File:** `@/Users/dhawal/Documents/GitHub/AISEO/AISEODevelop/AISEO/AppBackendBasic.Core/Entities/User.cs:41`

Added property:
```csharp
public bool IsFirstLogin { get; set; } = true;
```

#### **B. DTOs**

**File:** `@/Users/dhawal/Documents/GitHub/AISEO/AISEODevelop/AISEO/AppBackendBasic.Core/DTOs/OnboardingPromptDto.cs`

Created DTOs for:
- `GenerateOnboardingPromptsRequest` - Request to generate prompts
- `GeneratedPrompt` - Individual prompt with text, topic, rationale, platforms
- `GenerateOnboardingPromptsResponse` - Response with list of prompts
- `SaveOnboardingPromptsRequest` - Request to save selected prompts
- `SaveOnboardingPromptsResponse` - Response with created prompt IDs

#### **C. API Endpoints**

**File:** `@/Users/dhawal/Documents/GitHub/AISEO/AISEODevelop/AISEO/AppBackendBasic.API/Controllers/OnboardingController.cs`

**1. Generate Prompts Endpoint:**
```
POST /api/onboarding/generate-prompts
```

**Request:**
```json
{
  "projectId": 1,
  "industry": "SaaS",
  "companyName": "MyCompany",
  "targetAudience": "B2B enterprises"
}
```

**Process:**
1. Builds system prompt for ChatGPT
2. Calls ChatGPT (GPT-4) with temperature 0.8
3. Parses JSON response with 5 prompts
4. Returns prompts with topic, rationale, suggested platforms

**Response:**
```json
{
  "success": true,
  "prompts": [
    {
      "promptText": "What are the best project management tools for remote teams?",
      "topic": "Project Management",
      "rationale": "Helps track visibility in competitive landscape",
      "suggestedPlatforms": ["ChatGPT", "Claude", "Gemini"],
      "isSelected": false
    }
  ]
}
```

**2. Save Prompts Endpoint:**
```
POST /api/onboarding/save-prompts
```

**Request:**
```json
{
  "projectId": 1,
  "selectedPrompts": [
    {
      "promptText": "What are the best project management tools?",
      "topic": "Project Management",
      "platforms": ["ChatGPT", "Claude"]
    }
  ]
}
```

**Process:**
1. Creates PromptTracking records for each selected prompt
2. Triggers Hangfire background jobs for visibility checks
3. Marks user as `IsFirstLogin = false`
4. Returns created prompt IDs

**Response:**
```json
{
  "success": true,
  "promptsCreated": 3,
  "promptIds": [101, 102, 103]
}
```

#### **D. Auth Controller Update**

**File:** `@/Users/dhawal/Documents/GitHub/AISEO/AISEODevelop/AISEO/AppBackendBasic.API/Controllers/AuthController.cs:74`

Updated login response to include `isFirstLogin` flag:
```csharp
return Ok(new { 
    success = true, 
    accessToken = result.AccessToken, 
    refreshToken = result.RefreshToken, 
    requiresSubscription = result.RequiresSubscription, 
    isFirstLogin = result.User?.IsFirstLogin ?? false, 
    user = result.User 
});
```

---

### **3. Frontend Changes**

#### **A. Onboarding Prompts Modal Component**

**File:** `@/Users/dhawal/Documents/GitHub/AISEO/AISEODevelop/AISEO/AppBackendBasic.WebUI/src/components/OnboardingPromptsModal.tsx`

**Features:**
- **Generating Step:** Shows loading animation while ChatGPT generates prompts
- **Selecting Step:** Displays 5 prompts with checkboxes for selection
- **Each Prompt Shows:**
  - Prompt text (question)
  - Topic category badge
  - Rationale (why it's valuable)
  - Suggested platforms (ChatGPT, Claude, Gemini)
- **Actions:**
  - Select/deselect prompts (all selected by default)
  - Skip for now (closes modal)
  - Start Tracking (saves selected prompts and triggers jobs)

**UI Design:**
- Gradient blue header with sparkles icon
- Card-based prompt display
- Blue highlight for selected prompts
- Footer shows selection count
- Redirects to `/prompts` page after saving

#### **B. Login Page Integration**

**File:** `@/Users/dhawal/Documents/GitHub/AISEO/AISEODevelop/AISEO/AppBackendBasic.WebUI/src/pages/Login.tsx`

**Changes:**
1. Import `OnboardingPromptsModal`
2. Add state for modal visibility and project ID
3. Check `isFirstLogin` flag in login response
4. Fetch user's first project
5. Show modal if first login and project exists
6. Modal closes → navigate to dashboard

**Flow:**
```
User logs in
  ↓
Check isFirstLogin flag
  ↓
If true → Fetch projects
  ↓
If projects exist → Show OnboardingPromptsModal
  ↓
User selects prompts → Save & trigger jobs
  ↓
Navigate to /prompts page
```

#### **C. Auth Service Type Update**

**File:** `@/Users/dhawal/Documents/GitHub/AISEO/AISEODevelop/AISEO/AppBackendBasic.WebUI/src/services/authService.ts`

Added `isFirstLogin?: boolean` to `AuthResponse` interface.

---

## 🔄 Complete User Flow

### **First-Time Login:**

1. **User logs in** → Backend returns `isFirstLogin: true`
2. **Frontend fetches projects** → Gets first project ID
3. **Modal opens** with "Generating..." screen
4. **API calls ChatGPT** → Generates 5 custom prompts based on:
   - Company name
   - Industry
   - Target audience
5. **Modal shows prompts** → All selected by default
6. **User reviews prompts** → Can deselect unwanted ones
7. **User clicks "Start Tracking"** → Frontend calls save endpoint
8. **Backend:**
   - Creates PromptTracking records
   - Triggers visibility check jobs for each prompt
   - Sets `IsFirstLogin = false`
9. **User redirected** to `/prompts` page
10. **Background jobs run** → Check visibility on ChatGPT, Claude, Gemini

### **Subsequent Logins:**

- `isFirstLogin: false` → Normal login flow
- No modal shown
- User goes directly to dashboard or return URL

---

## 🚀 Testing

### **1. Run Database Migration**
```bash
psql "$DATABASE_URL" -f ADD_ISFIRSTLOGIN_COLUMN.sql
```

### **2. Create Test User**
```sql
INSERT INTO "Users" ("Email", "PasswordHash", "IsFirstLogin", "CreatedAt", "UpdatedAt")
VALUES ('testuser@example.com', 'hashed_password', TRUE, NOW(), NOW());
```

### **3. Test Login Flow**

**Login:**
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "testuser@example.com",
    "password": "password123"
  }'
```

**Expected Response:**
```json
{
  "success": true,
  "accessToken": "...",
  "isFirstLogin": true,
  "user": { ... }
}
```

### **4. Test Generate Prompts**
```bash
curl -X POST http://localhost:5000/api/onboarding/generate-prompts \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "projectId": 1,
    "industry": "SaaS",
    "companyName": "MyCompany"
  }'
```

### **5. Test Save Prompts**
```bash
curl -X POST http://localhost:5000/api/onboarding/save-prompts \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "projectId": 1,
    "selectedPrompts": [
      {
        "promptText": "What are the best tools?",
        "topic": "Tools",
        "platforms": ["ChatGPT"]
      }
    ]
  }'
```

### **6. Verify Database**
```sql
-- Check IsFirstLogin was set to false
SELECT "Id", "Email", "IsFirstLogin" FROM "Users" WHERE "Email" = 'testuser@example.com';

-- Check prompts were created
SELECT * FROM "PromptTrackings" WHERE "UserId" = (SELECT "Id" FROM "Users" WHERE "Email" = 'testuser@example.com');

-- Check visibility jobs were triggered
SELECT * FROM "PromptPlatformTrackings" WHERE "PromptId" IN (
  SELECT "Id" FROM "PromptTrackings" WHERE "UserId" = (SELECT "Id" FROM "Users" WHERE "Email" = 'testuser@example.com')
);
```

---

## 📝 Key Features

✅ **Automatic Prompt Generation** - ChatGPT creates 5 custom prompts  
✅ **Industry-Specific** - Prompts tailored to user's business  
✅ **User Selection** - Users choose which prompts to track  
✅ **Instant Visibility Tracking** - Jobs triggered immediately  
✅ **One-Time Experience** - Only shown on first login  
✅ **Skip Option** - Users can skip and set up later  
✅ **Beautiful UI** - Modern modal with gradient header  
✅ **Smart Defaults** - All prompts selected by default  

---

## 🔧 Configuration

### **LLM Job Config**

Add to `LlmJobConfigs` table:
```sql
INSERT INTO "LlmJobConfigs" ("JobKey", "JobName", "Provider", "Model", "MaxTokens", "Temperature", "Category")
VALUES ('OnboardingPromptGeneration', 'Onboarding Prompt Generation', 'OpenAI', 'gpt-4', 2000, 0.8, 'Onboarding');
```

### **System Prompt Customization**

Edit in `OnboardingController.cs:322` to customize prompt generation instructions.

---

## ⚠️ Important Notes

1. **Requires Project:** User must have at least one project created
2. **ChatGPT API:** Requires OpenAI API key configured
3. **Background Jobs:** Requires Hangfire configured
4. **First Login Only:** Modal only shows once per user
5. **JSON Parsing:** LLM response must be valid JSON array

---

## 🎨 UI Screenshots

**Modal States:**
1. **Generating:** Loading spinner with "Generating Your Custom Prompts"
2. **Selecting:** List of 5 prompts with checkboxes
3. **Footer:** Shows count of selected prompts

**Design:**
- Gradient blue header (blue-600 to indigo-600)
- White cards with blue borders for selected prompts
- Topic badges in indigo
- Platform tags in gray
- Sparkles icon for AI generation theme

---

## ✅ Completion Checklist

- [x] Database migration created
- [x] User entity updated
- [x] DTOs created
- [x] Generate prompts endpoint
- [x] Save prompts endpoint
- [x] Auth controller updated
- [x] Frontend modal component
- [x] Login page integration
- [x] Auth service types updated
- [x] Documentation created

**Status:** ✅ **Ready for Testing**

Run the database migration and test the feature!
