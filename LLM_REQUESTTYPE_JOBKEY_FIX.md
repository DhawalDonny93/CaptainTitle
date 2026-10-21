# LLM RequestType JobKey Fix

## 🔴 Problem

**Error:** `LlmJobConfigService` cannot find JobKey "OpenAI_ChatGPT"

**Root Cause:** The `TestPromptOnPlatformAsync` method was creating invalid `RequestType` values by concatenating provider name and platform:

```csharp
RequestType = providerName + "_" + platform  // "OpenAI_ChatGPT", "Anthropic_Claude", etc.
```

These concatenated values don't exist in the `LlmJobConfigs` table, causing the lookup to fail.

---

## 🔍 Analysis

### **How RequestType Works**

1. When `LlmProviderService.SendRequestAsync()` receives a request with a `RequestType`:
   ```csharp
   if (!string.IsNullOrEmpty(request.RequestType))
   {
       var resolved = await _jobConfigService.GetJobConfigAsync(
           request.RequestType,  // Looks up this JobKey in LlmJobConfigs
           defaultProvider: request.Provider,
           defaultModel: request.Model);
   }
   ```

2. `LlmJobConfigService.GetJobConfigAsync()` queries the database:
   ```csharp
   var config = await _context.LlmJobConfigs
       .FirstOrDefaultAsync(c => c.JobKey == jobKey && c.IsActive);
   ```

3. If JobKey doesn't exist in `LlmJobConfigs`, it falls back to defaults but logs an error.

### **Valid JobKeys in Database**

From `@/Users/dhawal/Documents/GitHub/AISEO/AISEODevelop/AISEO/Database_Migrations/16_llm_job_configs.sql`:

| JobKey | Purpose |
|--------|---------|
| `AiTracking` | AI Prompt Tracking |
| `PromptTest` | Prompt Visibility Test |
| `PromptResearchGeneration` | Prompt Research Generation |
| `SentimentAnalysis` | Sentiment Analysis |
| `ContentGapAnalysis` | Content Gap Analysis |
| `CompetitorAnalysis` | Competitor Analysis |
| `ContentGeneration` | Content Article Generation |
| `DraftReply` | Community Draft Reply |

---

## ✅ Solution

### **Fixed Code**

**File:** `@/Users/dhawal/Documents/GitHub/AISEO/AISEODevelop/AISEO/AppBackendBasic.Infrastructure/Services/LlmProviderService.cs:212`

**Before:**
```csharp
var llmRequest = new LlmRequestDto
{
    ProjectId = request.ProjectId,
    Provider = providerName,
    Model = platform,
    RequestType = providerName+"_"+platform,  // ❌ Invalid: "OpenAI_ChatGPT"
    PromptText = request.PromptText,
    SystemPrompt = "Respond naturally as you would to a user query.",
    Temperature = 0.7m,
    MaxTokens = 1000
};
```

**After:**
```csharp
var llmRequest = new LlmRequestDto
{
    ProjectId = request.ProjectId,
    Provider = providerName,
    Model = platform,
    RequestType = "PromptTest",  // ✅ Valid JobKey from LlmJobConfigs
    PromptText = request.PromptText,
    SystemPrompt = "Respond naturally as you would to a user query.",
    Temperature = 0.7m,
    MaxTokens = 1000
};
```

---

## ✅ Verification: All RequestType Assignments

### **Checked All RequestType Assignments in Codebase**

| File | Line | RequestType | Status |
|------|------|-------------|--------|
| `LlmProviderService.cs` | 212 | `"PromptTest"` | ✅ Fixed |
| `LlmProviderService.cs` | 384 | `"SentimentAnalysis"` | ✅ Valid |
| `LlmProviderService.cs` | 483 | `"DraftReply"` | ✅ Valid |
| `LlmProviderService.cs` | 591 | `request.RequestType` | ✅ Pass-through |
| `OpenAIProvider.cs` | 205 | `"ContentGeneration"` | ✅ Valid |
| `AnthropicProvider.cs` | 180 | `"ContentGeneration"` | ✅ Valid |
| `GoogleGeminiProvider.cs` | 196 | `"ContentGeneration"` | ✅ Valid |

**All other RequestType assignments are using valid JobKeys!**

---

## 🎯 Why "PromptTest" is the Correct JobKey

The `TestPromptOnPlatformAsync` method is called by:
1. `TestPromptAsync()` - Direct API endpoint for testing prompts
2. `CheckPromptVisibilityAsync()` - Background job for scheduled visibility checks

Both use cases are **prompt visibility testing**, which matches the `PromptTest` JobKey:

```sql
JobKey: 'PromptTest'
JobName: 'Prompt Visibility Test'
Description: 'On-demand test of a single prompt across selected platforms (ChatGPT, Claude, Gemini, Perplexity).'
Provider: 'OpenAI'
Model: 'gpt-4o'
MaxTokens: 1000
Temperature: 0.70
```

---

## 🔧 How It Works Now

### **Request Flow:**

1. **User/Background Job** calls `TestPromptAsync()` with platforms: `["ChatGPT", "Claude", "Gemini"]`

2. **For each platform**, `TestPromptOnPlatformAsync()` creates request:
   ```csharp
   RequestType = "PromptTest"  // Valid JobKey
   Provider = "OpenAI"         // Determined by platform
   Model = "ChatGPT"           // Platform name
   ```

3. **LlmProviderService.SendRequestAsync()** looks up `"PromptTest"` in `LlmJobConfigs`:
   ```csharp
   var resolved = await _jobConfigService.GetJobConfigAsync("PromptTest", ...);
   // Returns: Provider="OpenAI", Model="gpt-4o", MaxTokens=1000, Temperature=0.7
   ```

4. **Provider and Model are overridden** with job config values:
   ```csharp
   request.Provider = resolved.Provider;  // "OpenAI" (from job config)
   request.Model = resolved.Model;        // "gpt-4o" (from job config)
   ```

5. **Request is sent** to OpenAI with the configured model

---

## 📝 Important Notes

### **Why Not Use Platform-Specific JobKeys?**

You might think: "Why not create `ChatGPT_Test`, `Claude_Test`, etc. in LlmJobConfigs?"

**Answer:** Not necessary because:

1. **Single JobKey for all platforms** - All prompt testing uses the same LLM configuration
2. **Platform is just the target** - We're using one LLM (e.g., GPT-4o) to simulate responses from different platforms
3. **Simpler configuration** - Admin only needs to configure one "PromptTest" job, not 4 separate ones
4. **Consistent behavior** - All platforms tested with same model/settings for fair comparison

### **When to Create New JobKeys**

Create a new JobKey in `LlmJobConfigs` when:
- ✅ Different **business purpose** (e.g., "ContentGeneration" vs "SentimentAnalysis")
- ✅ Different **model requirements** (e.g., needs higher temperature or more tokens)
- ✅ Different **provider preference** (e.g., Claude is better for sentiment analysis)

Don't create new JobKeys for:
- ❌ Different platforms (ChatGPT, Claude, etc.) - use same JobKey
- ❌ Different users/projects - use same JobKey
- ❌ Different times of day - use same JobKey

---

## 🚀 Testing

### **1. Restart Application**
```bash
dotnet build
dotnet run --project AppBackendBasic.API
```

### **2. Test Prompt Visibility**
```bash
curl -X POST http://localhost:5000/api/llm-providers/test-prompt \
  -H "Content-Type: application/json" \
  -d '{
    "projectId": 5,
    "promptText": "What are the best project management tools?",
    "brandName": "YourBrand",
    "platforms": ["ChatGPT", "Claude", "Gemini"]
  }'
```

### **3. Check Logs**
Should see:
```
LlmJobConfig loaded from DB for PromptTest: OpenAI/gpt-4o
```

Should NOT see:
```
No LlmJobConfig found for OpenAI_ChatGPT
```

### **4. Trigger Background Job**
The `ai-visibility-citation-check` job will now work correctly when it calls `CheckPromptVisibilityAsync()`.

---

## ✅ Summary

**Problem:** Invalid RequestType "OpenAI_ChatGPT" created by string concatenation  
**Solution:** Use valid JobKey "PromptTest" from LlmJobConfigs table  
**Result:** Prompt visibility testing now uses correct job configuration  
**Status:** Fixed and ready to test

All RequestType assignments in the codebase are now using valid JobKeys from the `LlmJobConfigs` table.
