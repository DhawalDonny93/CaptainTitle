# Context-Aware JobKey Implementation

## ✅ Changes Implemented

### **Problem**
The `TestPromptAsync` method was using a hardcoded `"PromptTest"` JobKey for all calls, regardless of context. This prevented different callers from using different LLM configurations based on their specific needs.

### **Solution**
Implemented context-aware JobKey selection by allowing callers to specify which `JobKey` to use from the `LlmJobConfigs` table.

---

## 🔧 Changes Made

### **1. Updated PromptTestRequest DTO**

**File:** `@/Users/dhawal/Documents/GitHub/AISEO/AISEODevelop/AISEO/AppBackendBasic.Core/DTOs/LlmRequestDto.cs:74`

**Added:**
```csharp
public class PromptTestRequest
{
    public int ProjectId { get; set; }
    public string PromptText { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string? BrandDomain { get; set; };
    public List<string>? ProductNames { get; set; };
    public List<string> Platforms { get; set; } = new();
    
    /// <summary>
    /// Optional JobKey to use for LLM configuration lookup in LlmJobConfigs.
    /// Defaults to "PromptTest" if not specified.
    /// Examples: "PromptTest", "OpportunityDetection"
    /// </summary>
    public string? RequestType { get; set; }  // ✅ NEW
}
```

### **2. Updated TestPromptOnPlatformAsync**

**File:** `@/Users/dhawal/Documents/GitHub/AISEO/AISEODevelop/AISEO/AppBackendBasic.Infrastructure/Services/LlmProviderService.cs:212`

**Changed from:**
```csharp
RequestType = "PromptTest", // Hardcoded
```

**Changed to:**
```csharp
RequestType = request.RequestType ?? "PromptTest", // Use from request or default
```

### **3. Updated PromptTrackingService**

**File:** `@/Users/dhawal/Documents/GitHub/AISEO/AISEODevelop/AISEO/AppBackendBasic.Infrastructure/Services/PromptTrackingService.cs:220`

**Changed from:**
```csharp
var testRequest = new PromptTestRequest
{
    ProjectId = prompt.ProjectId,
    PromptText = prompt.PromptText,
    BrandName = prompt.Project.Name ?? "Brand",
    BrandDomain = prompt.Project.Domain,
    Platforms = enabledPlatforms.Select(p => p.Platform).ToList()
    // No RequestType specified
};
```

**Changed to:**
```csharp
var testRequest = new PromptTestRequest
{
    ProjectId = prompt.ProjectId,
    PromptText = prompt.PromptText,
    BrandName = prompt.Project.Name ?? "Brand",
    BrandDomain = prompt.Project.Domain,
    Platforms = enabledPlatforms.Select(p => p.Platform).ToList(),
    RequestType = "OpportunityDetection" // ✅ Background job uses OpportunityDetection
};
```

---

## 🎯 How It Works Now

### **Scenario 1: Manual Prompt Testing (API Endpoint)**

**Caller:** `LlmProvidersController.TestPrompt()`

```csharp
// User tests prompt via API - uses default "PromptTest"
var request = new PromptTestRequest
{
    ProjectId = 5,
    PromptText = "What are the best project management tools?",
    BrandName = "YourBrand",
    Platforms = ["ChatGPT", "Claude"],
    RequestType = null // or "PromptTest" explicitly
};

var response = await _llmProviderService.TestPromptAsync(request);
```

**LLM Configuration Used:**
```
JobKey: "PromptTest"
Provider: OpenAI
Model: gpt-4o
MaxTokens: 1000
Temperature: 0.7
```

### **Scenario 2: Background Job (Scheduled Visibility Checks)**

**Caller:** `PromptTrackingService.CheckPromptVisibilityAsync()`

```csharp
// Background job checks prompts - uses "OpportunityDetection"
var request = new PromptTestRequest
{
    ProjectId = prompt.ProjectId,
    PromptText = prompt.PromptText,
    BrandName = prompt.Project.Name,
    Platforms = ["ChatGPT", "Claude", "Gemini"],
    RequestType = "OpportunityDetection" // ✅ Different JobKey
};

var response = await _llmProviderService.TestPromptAsync(request);
```

**LLM Configuration Used:**
```
JobKey: "OpportunityDetection"
Provider: (configured by admin in LlmJobConfigs)
Model: (configured by admin in LlmJobConfigs)
MaxTokens: (configured by admin in LlmJobConfigs)
Temperature: (configured by admin in LlmJobConfigs)
```

---

## 📊 Available JobKeys

Based on your database screenshot, these JobKeys are available:

| JobKey | Best Used For |
|--------|---------------|
| `PromptTest` | Manual prompt testing via API |
| `OpportunityDetection` | Background job visibility checks |
| `PromptResearchGeneration` | Generating research prompts |
| `SentimentAnalysis` | Analyzing sentiment |
| `ContentGapAnalysis` | Detecting content gaps |
| `CompetitorAnalysis` | Analyzing competitors |
| `ContentGeneration` | Generating content articles |
| `DraftReply` | Generating community replies |
| `SeoOpportunityAnalysis` | SEO opportunity analysis |
| `CitationExtraction` | Extracting citations |
| `SiteAudit` | Site audit analysis |

---

## 🎯 Benefits

### **1. Cost Optimization**
- **Manual testing** can use premium models (GPT-4o)
- **Background jobs** can use cheaper models for batch processing

### **2. Quality Control**
- **User-facing features** use high-quality models
- **Internal processing** uses cost-effective models

### **3. Admin Flexibility**
Admins can configure each JobKey independently in the database:
```sql
-- Example: Configure OpportunityDetection to use cheaper model
UPDATE "LlmJobConfigs"
SET "Provider" = 'OpenAI',
    "Model" = 'gpt-3.5-turbo',
    "MaxTokens" = 500,
    "Temperature" = 0.5
WHERE "JobKey" = 'OpportunityDetection';
```

### **4. Context-Specific Configuration**
Different use cases can have different requirements:
- **PromptTest**: Higher quality for user testing
- **OpportunityDetection**: Optimized for batch processing
- **CitationExtraction**: Specialized for citation parsing

---

## 🔄 Request Flow

```
1. Caller creates PromptTestRequest with RequestType
   ↓
2. TestPromptAsync receives request
   ↓
3. For each platform, TestPromptOnPlatformAsync is called
   ↓
4. Creates LlmRequestDto with RequestType from request
   ↓
5. LlmProviderService.SendRequestAsync looks up JobKey
   ↓
6. LlmJobConfigService.GetJobConfigAsync("OpportunityDetection")
   ↓
7. Returns: Provider, Model, MaxTokens, Temperature from DB
   ↓
8. Request sent with configured settings
```

---

## 📝 Usage Examples

### **Example 1: API Controller (Manual Testing)**

```csharp
[HttpPost("test-prompt")]
public async Task<IActionResult> TestPrompt([FromBody] PromptTestRequest request)
{
    // Let it default to "PromptTest" or specify explicitly
    request.RequestType = "PromptTest";
    
    var response = await _llmProviderService.TestPromptAsync(request);
    return Ok(response);
}
```

### **Example 2: Background Job (Scheduled Checks)**

```csharp
public async Task ProcessAllActivePromptsAsync()
{
    foreach (var prompt in prompts)
    {
        var request = new PromptTestRequest
        {
            ProjectId = prompt.ProjectId,
            PromptText = prompt.PromptText,
            BrandName = prompt.Project.Name,
            Platforms = platforms,
            RequestType = "OpportunityDetection" // Batch processing context
        };
        
        await _llmProviderService.TestPromptAsync(request);
    }
}
```

### **Example 3: SEO Analysis**

```csharp
public async Task AnalyzeSeoOpportunities(int projectId)
{
    var request = new PromptTestRequest
    {
        ProjectId = projectId,
        PromptText = "Analyze SEO opportunities",
        BrandName = "Brand",
        Platforms = ["ChatGPT"],
        RequestType = "SeoOpportunityAnalysis" // SEO-specific context
    };
    
    var response = await _llmProviderService.TestPromptAsync(request);
}
```

---

## 🚀 Next Steps

### **Optional Enhancements:**

1. **Add RequestType to Other DTOs**
   - `CitationAnalysisRequest` → Default to `"CitationExtraction"`
   - `SentimentAnalysisRequest` → Already uses `"SentimentAnalysis"`
   - `DraftReplyRequest` → Already uses `"DraftReply"`

2. **Add Validation**
   ```csharp
   // Validate JobKey exists in database
   if (!string.IsNullOrEmpty(request.RequestType))
   {
       var jobConfig = await _context.LlmJobConfigs
           .FirstOrDefaultAsync(c => c.JobKey == request.RequestType);
       
       if (jobConfig == null)
           throw new ArgumentException($"Invalid JobKey: {request.RequestType}");
   }
   ```

3. **Add Logging**
   ```csharp
   _logger.LogInformation(
       "Testing prompt with JobKey: {JobKey}, Platforms: {Platforms}",
       request.RequestType ?? "PromptTest",
       string.Join(", ", request.Platforms));
   ```

---

## ✅ Testing

### **1. Test Manual Prompt Testing**
```bash
curl -X POST http://localhost:5000/api/llm-providers/test-prompt \
  -H "Content-Type: application/json" \
  -d '{
    "projectId": 5,
    "promptText": "What are the best project management tools?",
    "brandName": "YourBrand",
    "platforms": ["ChatGPT"],
    "requestType": "PromptTest"
  }'
```

**Expected:** Uses `PromptTest` JobKey configuration

### **2. Test Background Job**
Trigger the `ai-visibility-citation-check` background job manually in Hangfire dashboard.

**Expected:** Uses `OpportunityDetection` JobKey configuration

### **3. Check Logs**
```
LlmJobConfig loaded from DB for OpportunityDetection: OpenAI/gpt-4o
```

---

## 📊 Summary

**Changes:**
- ✅ Added `RequestType` property to `PromptTestRequest`
- ✅ Updated `TestPromptOnPlatformAsync` to use `RequestType` from request
- ✅ Updated `PromptTrackingService` to pass `"OpportunityDetection"` for background jobs

**Benefits:**
- ✅ Different contexts can use different LLM configurations
- ✅ Cost optimization (cheaper models for background jobs)
- ✅ Quality control (better models for user-facing features)
- ✅ Admin flexibility (configure each JobKey independently)
- ✅ Backward compatible (defaults to "PromptTest")

**Status:** Ready to test after rebuild
