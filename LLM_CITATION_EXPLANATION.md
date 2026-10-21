# Why LLM Responses Don't Contain Citations by Default

## 🔍 The Issue

LLM responses from ChatGPT, Claude, and Gemini **do not include URLs or citations by default** unless explicitly requested.

---

## 📊 Why This Happens

### **1. LLM Behavior**

**Standard LLM models (GPT-4, Claude, Gemini) generate text based on training data, not web searches:**

- ❌ They don't browse the web
- ❌ They don't have access to real-time URLs
- ❌ They don't automatically cite sources
- ✅ They generate responses from learned patterns

**Example:**
```
User: "What are the best project management tools?"

Standard Response (No Citations):
"Some popular project management tools include Asana, Trello, Monday.com, and Jira. 
These tools offer features like task tracking, team collaboration, and workflow automation."
```

**No URLs, no citations, no sources.**

---

### **2. Search-Enabled Models**

Only **specific search-enabled models** provide citations:

| Model | Citations by Default? | Notes |
|-------|----------------------|-------|
| GPT-4 (standard) | ❌ No | Text generation only |
| GPT-4 with browsing | ✅ Yes | Requires browsing mode |
| Claude (standard) | ❌ No | Text generation only |
| Gemini (standard) | ❌ No | Text generation only |
| Perplexity AI | ✅ Yes | Search-focused, always cites |
| ChatGPT with web search | ✅ Yes | When web search is enabled |

---

## ✅ Solution: Request Citations Explicitly

### **Updated System Prompt**

**File:** `@/Users/dhawal/Documents/GitHub/AISEO/AISEODevelop/AISEO/AppBackendBasic.Infrastructure/Services/LlmProviderService.cs:214`

**Before:**
```csharp
SystemPrompt = "Respond naturally as you would to a user query."
```

**After:**
```csharp
SystemPrompt = "Respond naturally as you would to a user query. Include relevant citations and source URLs in your response when providing factual information or recommendations. Format URLs as clickable links."
```

---

## 🎯 Expected Behavior After Fix

### **With Updated Prompt:**

```
User: "What are the best project management tools?"

Response (With Citations):
"Some popular project management tools include:

1. **Asana** (https://asana.com) - Great for team collaboration and task tracking
2. **Trello** (https://trello.com) - Visual kanban-style project management
3. **Monday.com** (https://monday.com) - Customizable workflow automation
4. **Jira** (https://atlassian.com/software/jira) - Best for software development teams

According to recent reviews on https://capterra.com and https://g2.com, these tools 
are highly rated for their ease of use and feature sets."
```

**Now includes:**
- ✅ URLs to tool websites
- ✅ Citations to review sites
- ✅ Source references

---

## 🔄 How Citation Extraction Works Now

### **Flow:**

```
1. User tests prompt via API
   ↓
2. LlmProviderService.TestPromptAsync()
   ↓
3. System prompt requests citations
   ↓
4. LLM generates response WITH URLs
   ↓
5. CitationParser.ParseCitations(responseText)
   ↓
6. Extracts URLs using regex: https?://[^\s<>"']+
   ↓
7. For each URL:
   - Extract domain (e.g., "asana.com")
   - Extract path (e.g., "/features")
   - Determine citation type (DirectLink, Reference, etc.)
   - Find position in response
   - Extract context (surrounding sentence)
   ↓
8. Returns List<DetectedCitation> with full metadata
   ↓
9. ProcessCitationsAndCompetitorsAsync()
   ↓
10. Creates Citation records in database
11. Detects SERP competitors from citation domains
```

---

## 📝 Important Notes

### **1. Not All LLMs Will Comply**

Even with the updated prompt:
- Some models may still not include URLs
- Depends on model capabilities and training
- Search-enabled models work best

### **2. Alternative: Use Search-Enabled APIs**

For guaranteed citations, consider:
- **Perplexity API** - Always includes citations
- **ChatGPT with browsing** - Requires specific API mode
- **Bing Chat API** - Search-integrated responses

### **3. Fallback Strategy**

If LLM doesn't provide citations:
- CitationParser will return empty list
- No citations will be tracked
- No SERP competitors detected
- This is expected behavior for standard models

---

## 🚀 Testing

### **1. Test with Updated Prompt**

```bash
curl -X POST http://localhost:5000/api/llm-providers/test-prompt \
  -H "Content-Type: application/json" \
  -d '{
    "projectId": 5,
    "promptText": "What are the best project management tools? Include sources.",
    "brandName": "YourBrand",
    "platforms": ["ChatGPT"]
  }'
```

**Expected:** Response should include URLs if LLM complies with system prompt.

### **2. Check Response**

```json
{
  "success": true,
  "results": [
    {
      "platform": "ChatGPT",
      "responseText": "... https://asana.com ... https://trello.com ...",
      "citationsFound": [
        "https://asana.com",
        "https://trello.com"
      ]
    }
  ]
}
```

### **3. Verify Database**

```sql
-- Check if citations were extracted
SELECT * FROM "Citations" 
WHERE "CreatedAt" > NOW() - INTERVAL '1 hour'
ORDER BY "CreatedAt" DESC;

-- Check SERP competitors detected
SELECT * FROM "Competitors" 
WHERE "Type" = 'SERP' 
AND "CreatedAt" > NOW() - INTERVAL '1 hour';
```

---

## ✅ Summary

**Problem:** LLMs don't provide citations by default  
**Cause:** Standard LLM models generate text, not web searches  
**Solution:** Updated system prompt to explicitly request citations  
**Result:** LLMs will attempt to include URLs when possible  
**Caveat:** Not guaranteed - depends on model capabilities  

**Alternative:** Use search-enabled LLM APIs (Perplexity, ChatGPT with browsing) for guaranteed citations.
