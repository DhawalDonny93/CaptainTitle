# LLM JobKey Mapping Guide

## 📊 Available JobKeys in Database

Based on the screenshot, these JobKeys exist in `LlmJobConfigs`:

| JobKey | JobName | Use Case |
|--------|---------|----------|
| `PromptTest` | Prompt Visibility Test | On-demand test of prompts across platforms |
| `PromptResearchGeneration` | Prompt Research Generation | Generate research prompts for topics |
| `SentimentAnalysis` | Sentiment Analysis | Analyze sentiment of brand mentions |
| `ContentGapAnalysis` | Content Gap Analysis | Detect content gaps vs competitors |
| `CompetitorAnalysis` | Competitor Analysis | Analyze competitor domains |
| `ContentGeneration` | Content Article Generation | Generate SEO-optimized articles |
| `DraftReply` | Community Draft Reply | Generate draft replies for communities |
| `OpportunityDetection` | Opportunity Detection | Scan visibility metrics for opportunities |
| `SeoOpportunityAnalysis` | SEO Opportunity Analysis | Generate actionable SEO recommendations |
| `PromptResearch` | Prompt Research Generation | Generate research prompts (duplicate?) |
| `CitationExtraction` | Citation Extraction | Extract citations from LLM responses |
| `SiteAudit` | Site Audit Analysis | Analyze site for LLM optimization |

---

## 🎯 Method to JobKey Mapping

### **Current Implementation**

| Service Method | Current RequestType | Should Use JobKey | Caller Context |
|----------------|---------------------|-------------------|----------------|
| `TestPromptAsync()` | `"PromptTest"` ✅ | `PromptTest` | API endpoint + Background job |
| `GenerateContentAsync()` | `"ContentGeneration"` ✅ | `ContentGeneration` | Content generation |
| `AnalyzeCitationsAsync()` | None ❌ | `CitationExtraction` | Citation tracking |
| `AnalyzeSentimentAsync()` | `"SentimentAnalysis"` ✅ | `SentimentAnalysis` | Sentiment analysis |
| `GenerateDraftReplyAsync()` | `"DraftReply"` ✅ | `DraftReply` | Community replies |

---

## 🔧 Required Changes

### **1. TestPromptAsync - Multiple Contexts**

**Problem:** `TestPromptAsync()` is called from:
1. **API Controller** - Manual prompt testing by users
2. **PromptTrackingService** - Background job for scheduled visibility checks

**Solution:** Both should use `"PromptTest"` JobKey (already correct)

### **2. AnalyzeCitationsAsync - Missing RequestType**

**Current Code:**
```csharp
// No RequestType set - uses defaults
var response = await _llmProviderService.AnalyzeCitationsAsync(request);
```

**Should Use:** `"CitationExtraction"` JobKey

**Called From:**
- `CitationTrackingService.ExtractCitationsFromResponseAsync()`

### **3. Add Context-Aware JobKey Selection**

For methods called from multiple contexts, we should allow the caller to specify the JobKey.

---

## ✅ Recommended Implementation

### **Option 1: Add RequestType to Request DTOs (Recommended)**

Add optional `RequestType` field to all request DTOs:

```csharp
public class PromptTestRequest
{
    public int ProjectId { get; set; }
    public string PromptText { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string? BrandDomain { get; set; }
    public List<string>? ProductNames { get; set; }
    public List<string> Platforms { get; set; } = new();
    
    // NEW: Allow caller to specify JobKey
    public string? RequestType { get; set; } // Optional - defaults to "PromptTest"
}
```

**Callers can then specify:**
```csharp
// API endpoint - manual testing
var request = new PromptTestRequest
{
    ProjectId = 5,
    PromptText = "test",
    Platforms = ["ChatGPT"],
    RequestType = "PromptTest" // Explicit
};

// Background job - scheduled checks
var request = new PromptTestRequest
{
    ProjectId = 5,
    PromptText = "test",
    Platforms = ["ChatGPT"],
    RequestType = "OpportunityDetection" // Different context
};
```

### **Option 2: Separate Methods (Not Recommended)**

Create separate methods for different contexts:
- `TestPromptManualAsync()` - Uses "PromptTest"
- `TestPromptScheduledAsync()` - Uses "OpportunityDetection"

**Downside:** Code duplication

---

## 🎯 Specific Changes Needed

### **1. Update PromptTestRequest DTO**

```csharp
public class PromptTestRequest
{
    // ... existing properties ...
    
    /// <summary>
    /// Optional JobKey to use for LLM configuration.
    /// Defaults to "PromptTest" if not specified.
    /// </summary>
    public string? RequestType { get; set; }
}
```

### **2. Update TestPromptAsync Method**

```csharp
public async Task<PromptTestResponse> TestPromptAsync(PromptTestRequest request)
{
    // Use RequestType from request, default to "PromptTest"
    var jobKey = request.RequestType ?? "PromptTest";
    
    // ... rest of method
}
```

### **3. Update TestPromptOnPlatformAsync**

```csharp
private async Task<PlatformTestResult> TestPromptOnPlatformAsync(
    PromptTestRequest request, 
    string platform)
{
    var llmRequest = new LlmRequestDto
    {
        ProjectId = request.ProjectId,
        Provider = providerName,
        Model = platform,
        RequestType = request.RequestType ?? "PromptTest", // Use from request
        PromptText = request.PromptText,
        // ...
    };
}
```

### **4. Update PromptTrackingService Calls**

**For Background Job (Scheduled Checks):**
```csharp
var testRequest = new PromptTestRequest
{
    ProjectId = prompt.ProjectId,
    PromptText = prompt.PromptText,
    BrandName = prompt.Project.Name ?? "Brand",
    BrandDomain = prompt.Project.Domain,
    Platforms = enabledPlatforms.Select(p => p.Platform).ToList(),
    RequestType = "OpportunityDetection" // Background job context
};
```

**For Manual Testing:**
```csharp
var testRequest = new PromptTestRequest
{
    ProjectId = projectId,
    PromptText = promptText,
    BrandName = brandName,
    Platforms = platforms,
    RequestType = "PromptTest" // Manual testing context
};
```

### **5. Add RequestType to CitationAnalysisRequest**

```csharp
public class CitationAnalysisRequest
{
    public string ResponseText { get; set; } = string.Empty;
    public int ProjectId { get; set; }
    public string? RequestType { get; set; } // NEW - defaults to "CitationExtraction"
}
```

### **6. Update AnalyzeCitationsAsync**

```csharp
public async Task<CitationAnalysisResponse> AnalyzeCitationsAsync(CitationAnalysisRequest request)
{
    // ... existing code ...
    
    var llmRequest = new LlmRequestDto
    {
        ProjectId = request.ProjectId,
        Provider = "OpenAI",
        Model = "gpt-4",
        RequestType = request.RequestType ?? "CitationExtraction", // Use JobKey
        PromptText = request.ResponseText,
        SystemPrompt = systemPrompt,
        // ...
    };
}
```

---

## 📋 Summary of Changes

### **DTOs to Update:**
1. ✅ `PromptTestRequest` - Add `RequestType` property
2. ✅ `CitationAnalysisRequest` - Add `RequestType` property
3. ✅ `SentimentAnalysisRequest` - Add `RequestType` property (if needed)
4. ✅ `DraftReplyRequest` - Add `RequestType` property (if needed)

### **Service Methods to Update:**
1. ✅ `TestPromptAsync()` - Use `request.RequestType ?? "PromptTest"`
2. ✅ `TestPromptOnPlatformAsync()` - Pass through RequestType
3. ✅ `AnalyzeCitationsAsync()` - Use `request.RequestType ?? "CitationExtraction"`

### **Callers to Update:**
1. ✅ `PromptTrackingService.CheckPromptVisibilityAsync()` - Pass `"OpportunityDetection"`
2. ✅ `LlmProvidersController.TestPrompt()` - Pass `"PromptTest"` (or let default)
3. ✅ `CitationTrackingService` - Pass `"CitationExtraction"`

---

## 🎯 Benefits

1. **Flexibility** - Different contexts can use different LLM configurations
2. **Cost Optimization** - Background jobs can use cheaper models
3. **Quality Control** - Manual testing can use better models
4. **Admin Control** - Admins can configure each JobKey independently
5. **Backward Compatible** - Defaults maintain current behavior

---

## 🔍 Example Scenarios

### **Scenario 1: Manual Prompt Testing (API)**
```csharp
// User tests prompt via API
RequestType = "PromptTest"
→ Uses: OpenAI gpt-4o, MaxTokens=1000, Temp=0.7
```

### **Scenario 2: Background Job (Scheduled)**
```csharp
// Background job checks all prompts
RequestType = "OpportunityDetection"
→ Uses: Different model/settings optimized for batch processing
```

### **Scenario 3: Citation Extraction**
```csharp
// Extract citations from LLM response
RequestType = "CitationExtraction"
→ Uses: Model optimized for citation extraction
```

### **Scenario 4: SEO Analysis**
```csharp
// Analyze SEO opportunities
RequestType = "SeoOpportunityAnalysis"
→ Uses: Model configured for SEO analysis
```

---

## ✅ Implementation Priority

1. **High Priority:**
   - Add `RequestType` to `PromptTestRequest`
   - Update `TestPromptOnPlatformAsync` to use it
   - Update `PromptTrackingService` to pass appropriate JobKey

2. **Medium Priority:**
   - Add `RequestType` to `CitationAnalysisRequest`
   - Update `AnalyzeCitationsAsync`

3. **Low Priority:**
   - Add `RequestType` to other request DTOs if needed
   - Document all JobKey mappings
