# LLM Providers Setup Guide

## 🎯 Overview

Your application now supports **5 LLM providers** for prompt testing, citation extraction, and AI visibility tracking:

1. **OpenAI (ChatGPT)** - GPT-4, GPT-3.5-turbo
2. **Anthropic (Claude)** - Claude 3 Opus, Sonnet, Haiku
3. **Google (Gemini)** - Gemini Pro, Gemini Pro Vision
4. **Perplexity** - Llama 3.1 Sonar models with built-in search and citations
5. **Google Search (AI Overview/SGE)** - Gemini with Google Search grounding

---

## ✅ Implementation Status

### **Backend Providers**

| Provider | Status | File | Features |
|----------|--------|------|----------|
| OpenAI | ✅ Implemented | `OpenAIProvider.cs` | GPT-4, GPT-3.5, JSON mode |
| Anthropic | ✅ Implemented | `AnthropicProvider.cs` | Claude 3 models |
| Google Gemini | ✅ Implemented | `GoogleGeminiProvider.cs` | Gemini Pro, Vision |
| Perplexity | ✅ Implemented | `PerplexityProvider.cs` | Search + Citations |
| Google Search | ✅ **NEW** | `GoogleSearchProvider.cs` | AI Overview/SGE + Grounding |

### **Frontend Integration**

| Component | Platforms Supported |
|-----------|-------------------|
| YourPrompts.tsx | ChatGPT, Claude, Gemini, Perplexity, Copilot |
| ContentCreation.tsx | ChatGPT, Claude, Gemini, Perplexity, SearchGPT |
| PromptResearch.tsx | ChatGPT, Claude, Gemini, Perplexity |
| OnboardingPromptsModal.tsx | ChatGPT, Claude, Gemini |

---

## 🔑 API Keys Required

You need to obtain API keys from each provider you want to use:

### **1. OpenAI (ChatGPT)**

**Get API Key:**
1. Go to https://platform.openai.com/api-keys
2. Sign in or create an account
3. Click "Create new secret key"
4. Copy the key (starts with `sk-`)

**Models Available:**
- `gpt-4` - Most capable, higher cost
- `gpt-4-turbo-preview` - Faster, cheaper than GPT-4
- `gpt-3.5-turbo` - Fast, affordable

**Pricing:**
- GPT-4: $0.03/1K input tokens, $0.06/1K output tokens
- GPT-3.5-turbo: $0.0005/1K input tokens, $0.0015/1K output tokens

---

### **2. Anthropic (Claude)**

**Get API Key:**
1. Go to https://console.anthropic.com/
2. Sign up for an account
3. Navigate to "API Keys"
4. Click "Create Key"
5. Copy the key (starts with `sk-ant-`)

**Models Available:**
- `claude-3-opus-20240229` - Most capable
- `claude-3-sonnet-20240229` - Balanced performance
- `claude-3-haiku-20240307` - Fast and affordable

**Pricing:**
- Opus: $0.015/1K input tokens, $0.075/1K output tokens
- Sonnet: $0.003/1K input tokens, $0.015/1K output tokens
- Haiku: $0.00025/1K input tokens, $0.00125/1K output tokens

---

### **3. Google AI (Gemini)**

**Get API Key:**
1. Go to https://makersuite.google.com/app/apikey
2. Sign in with your Google account
3. Click "Create API Key"
4. Select or create a Google Cloud project
5. Copy the API key

**Models Available:**
- `gemini-pro` - Text generation
- `gemini-pro-vision` - Multimodal (text + images)

**Pricing:**
- Gemini Pro: $0.00025/1K input tokens, $0.0005/1K output tokens

**Note:** Free tier available with rate limits

---

### **4. Perplexity AI** ⭐ **NEW**

**Get API Key:**
1. Go to https://www.perplexity.ai/settings/api
2. Sign in or create an account
3. Navigate to API settings
4. Click "Generate API Key"
5. Copy the key (starts with `pplx-`)

**Models Available:**
- `llama-3.1-sonar-small-128k-online` - Fast, with search
- `llama-3.1-sonar-large-128k-online` - More capable, with search
- `llama-3.1-sonar-huge-128k-online` - Most capable, with search
- `llama-3.1-sonar-small-128k-chat` - Chat without search
- `llama-3.1-sonar-large-128k-chat` - Chat without search

**Pricing:**
- Small: $0.0002/1K tokens (input + output)
- Large: $0.001/1K tokens (input + output)
- Huge: $0.005/1K tokens (input + output)

**Special Features:**
- ✅ Built-in web search
- ✅ Automatic citations
- ✅ Real-time information
- ✅ `return_citations` parameter for structured citations

---

### **5. Google AI Search Overview (SGE)** ⭐ **NEW**

**Get API Key:**
Uses the same Google API key as Gemini (see section 3 above)

**Models Available:**
- `gemini-pro` - Standard model with search grounding
- `gemini-1.5-pro` - More capable with longer context
- `gemini-1.5-flash` - Fast and efficient

**Pricing:**
Same as Gemini (uses same API):
- Gemini Pro: $0.00025/1K input tokens, $0.0005/1K output tokens
- Gemini 1.5 Pro: $0.00125/1K input tokens, $0.00375/1K output tokens
- Gemini 1.5 Flash: $0.000075/1K input tokens, $0.0003/1K output tokens

**Special Features:**
- ✅ Google Search grounding (web search integration)
- ✅ Automatic source citations from search results
- ✅ Real-time, up-to-date information
- ✅ Simulates Google AI Overview/SGE experience
- ✅ `groundingMetadata` with web sources
- ✅ Dynamic retrieval mode for optimal search usage

**How It Works:**
This provider uses Gemini API with the `googleSearchRetrieval` tool enabled, which:
1. Analyzes the query to determine if search is needed
2. Performs Google Search queries automatically
3. Grounds the response in search results
4. Returns citations from web sources
5. Provides metadata about search queries used

**Use Cases:**
1. **AI Overview Simulation** - Test how your brand appears in Google's AI-generated search summaries
2. **Search Visibility** - Track which sources Google's AI cites for relevant queries
3. **Competitive Analysis** - See which competitors get cited in AI search results
4. **Content Verification** - Ensure your content is discoverable by Google's AI

**Note:** No separate API key needed - uses your existing Google/Gemini API key!

---

## 📊 Database Configuration

### **Step 1: Add API Keys to Database**

Run these SQL commands to configure each provider:

```sql
-- OpenAI Configuration
INSERT INTO "LlmProviderConfigs" ("Provider", "ConfigKey", "ConfigValue", "IsEncrypted", "CreatedAt", "UpdatedAt")
VALUES 
('OpenAI', 'ApiKey', 'YOUR_OPENAI_API_KEY_HERE', TRUE, NOW(), NOW()),
('OpenAI', 'Endpoint', 'https://api.openai.com/v1', FALSE, NOW(), NOW())
ON CONFLICT ("Provider", "ConfigKey") DO UPDATE 
SET "ConfigValue" = EXCLUDED."ConfigValue", "UpdatedAt" = NOW();

-- Anthropic Configuration
INSERT INTO "LlmProviderConfigs" ("Provider", "ConfigKey", "ConfigValue", "IsEncrypted", "CreatedAt", "UpdatedAt")
VALUES 
('Anthropic', 'ApiKey', 'YOUR_ANTHROPIC_API_KEY_HERE', TRUE, NOW(), NOW()),
('Anthropic', 'Endpoint', 'https://api.anthropic.com/v1', FALSE, NOW(), NOW())
ON CONFLICT ("Provider", "ConfigKey") DO UPDATE 
SET "ConfigValue" = EXCLUDED."ConfigValue", "UpdatedAt" = NOW();

-- Google Gemini Configuration
INSERT INTO "LlmProviderConfigs" ("Provider", "ConfigKey", "ConfigValue", "IsEncrypted", "CreatedAt", "UpdatedAt")
VALUES 
('Google', 'ApiKey', 'YOUR_GOOGLE_API_KEY_HERE', TRUE, NOW(), NOW()),
('Google', 'Endpoint', 'https://generativelanguage.googleapis.com/v1beta', FALSE, NOW(), NOW())
ON CONFLICT ("Provider", "ConfigKey") DO UPDATE 
SET "ConfigValue" = EXCLUDED."ConfigValue", "UpdatedAt" = NOW();

-- Perplexity Configuration (NEW)
INSERT INTO "LlmProviderConfigs" ("Provider", "ConfigKey", "ConfigValue", "IsEncrypted", "CreatedAt", "UpdatedAt")
VALUES 
('Perplexity', 'ApiKey', 'YOUR_PERPLEXITY_API_KEY_HERE', TRUE, NOW(), NOW()),
('Perplexity', 'Endpoint', 'https://api.perplexity.ai', FALSE, NOW(), NOW())
ON CONFLICT ("Provider", "ConfigKey") DO UPDATE 
SET "ConfigValue" = EXCLUDED."ConfigValue", "UpdatedAt" = NOW();
```

**Replace:**
- `YOUR_OPENAI_API_KEY_HERE` with your actual OpenAI key
- `YOUR_ANTHROPIC_API_KEY_HERE` with your actual Anthropic key
- `YOUR_GOOGLE_API_KEY_HERE` with your actual Google key
- `YOUR_PERPLEXITY_API_KEY_HERE` with your actual Perplexity key

---

### **Step 2: Configure Job-Specific Models (Optional)**

Set which model to use for each job type:

```sql
-- Prompt Testing Jobs
INSERT INTO "LlmJobConfigs" ("JobKey", "JobName", "Provider", "Model", "MaxTokens", "Temperature", "Category", "IsActive")
VALUES 
('PromptTest', 'Prompt Visibility Test', 'OpenAI', 'gpt-4', 1000, 0.7, 'Testing', TRUE),
('PromptTest', 'Prompt Visibility Test - Claude', 'Anthropic', 'claude-3-sonnet-20240229', 1000, 0.7, 'Testing', TRUE),
('PromptTest', 'Prompt Visibility Test - Gemini', 'Google', 'gemini-pro', 1000, 0.7, 'Testing', TRUE),
('PromptTest', 'Prompt Visibility Test - Perplexity', 'Perplexity', 'llama-3.1-sonar-large-128k-online', 1000, 0.7, 'Testing', TRUE),
('PromptTest', 'Prompt Visibility Test - Google Search', 'GoogleSearch', 'gemini-pro', 1000, 0.7, 'Testing', TRUE)
ON CONFLICT ("JobKey", "Provider") DO UPDATE 
SET "Model" = EXCLUDED."Model", "UpdatedAt" = NOW();

-- Content Generation Jobs
INSERT INTO "LlmJobConfigs" ("JobKey", "JobName", "Provider", "Model", "MaxTokens", "Temperature", "Category", "IsActive")
VALUES 
('ContentGeneration', 'Content Generation', 'OpenAI', 'gpt-4-turbo-preview', 4000, 0.7, 'Content', TRUE),
('ContentGeneration', 'Content Generation - Claude', 'Anthropic', 'claude-3-opus-20240229', 4000, 0.7, 'Content', TRUE),
('ContentGeneration', 'Content Generation - Perplexity', 'Perplexity', 'llama-3.1-sonar-large-128k-online', 4000, 0.7, 'Content', TRUE)
ON CONFLICT ("JobKey", "Provider") DO UPDATE 
SET "Model" = EXCLUDED."Model", "UpdatedAt" = NOW();
```

---

## 🧪 Testing the Integration

### **1. Test API Connection**

```bash
# Test OpenAI
curl -X POST http://localhost:5000/api/llm-providers/test-connection \
  -H "Content-Type: application/json" \
  -d '{"provider": "OpenAI"}'

# Test Anthropic
curl -X POST http://localhost:5000/api/llm-providers/test-connection \
  -H "Content-Type: application/json" \
  -d '{"provider": "Anthropic"}'

# Test Google
curl -X POST http://localhost:5000/api/llm-providers/test-connection \
  -H "Content-Type: application/json" \
  -d '{"provider": "Google"}'

# Test Perplexity
curl -X POST http://localhost:5000/api/llm-providers/test-connection \
  -H "Content-Type: application/json" \
  -d '{"provider": "Perplexity"}'
```

---

### **2. Test Prompt Across All Platforms**

```bash
curl -X POST http://localhost:5000/api/llm-providers/test-prompt \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "projectId": 1,
    "promptText": "What are the best project management tools for remote teams?",
    "brandName": "YourBrand",
    "platforms": ["ChatGPT", "Claude", "Gemini", "Perplexity", "SearchGPT"]
  }'
```

**Expected Response:**
```json
{
  "success": true,
  "results": [
    {
      "platform": "ChatGPT",
      "responseText": "...",
      "citationsFound": ["https://asana.com", "https://trello.com"],
      "brandMentioned": false,
      "visibilityScore": 0
    },
    {
      "platform": "Claude",
      "responseText": "...",
      "citationsFound": ["https://monday.com"],
      "brandMentioned": false,
      "visibilityScore": 0
    },
    {
      "platform": "Gemini",
      "responseText": "...",
      "citationsFound": [],
      "brandMentioned": false,
      "visibilityScore": 0
    },
    {
      "platform": "Perplexity",
      "responseText": "...",
      "citationsFound": ["https://clickup.com", "https://notion.so"],
      "brandMentioned": false,
      "visibilityScore": 0
    },
    {
      "platform": "SearchGPT",
      "responseText": "...",
      "citationsFound": ["https://slack.com", "https://basecamp.com"],
      "brandMentioned": false,
      "visibilityScore": 0
    }
  ],
  "averageVisibilityScore": 0,
  "totalCost": 0.045
}
```

---

## 🎨 Frontend Usage

### **Prompt Testing**

Users can select platforms in the UI:

**File:** `YourPrompts.tsx`
```tsx
{['ChatGPT', 'Claude', 'Gemini', 'Perplexity', 'Copilot'].map(platform => (
  <button onClick={() => togglePlatform(platform)}>
    {platform}
  </button>
))}
```

### **Content Creation**

**File:** `ContentCreation.tsx`
```tsx
const platformOptions = ['ChatGPT', 'Claude', 'Gemini', 'Perplexity', 'SearchGPT'];
```

---

## 🔄 Platform Mapping

**Backend Mapping:**

| Frontend Platform | Backend Provider | Model Used |
|------------------|------------------|------------|
| ChatGPT | OpenAI | gpt-4 |
| Claude | Anthropic | claude-3-sonnet-20240229 |
| Gemini | Google | gemini-pro |
| Perplexity | Perplexity | llama-3.1-sonar-large-128k-online |
| SearchGPT | GoogleSearch | gemini-pro (with search grounding) |

**Code:** `LlmProviderService.cs:202-211`
```csharp
var providerName = platform.ToLower() switch
{
    "chatgpt" => "OpenAI",
    "claude" => "Anthropic",
    "gemini" => "Google",
    "perplexity" => "Perplexity",
    "searchgpt" => "GoogleSearch",
    "googlesearch" => "GoogleSearch",
    _ => "OpenAI"
};
```

---

## 💡 Why Perplexity is Special

### **Built-in Citations**

Perplexity automatically includes citations in responses:

```json
{
  "choices": [{
    "message": {
      "content": "The best project management tools include Asana, Trello, and Monday.com..."
    }
  }],
  "citations": [
    "https://asana.com",
    "https://trello.com",
    "https://monday.com"
  ]
}
```

### **Real-Time Search**

- Uses `*-online` models for web search
- Returns up-to-date information
- Perfect for competitive analysis

### **Use Cases**

1. **Citation Extraction** - Get sources automatically
2. **Competitor Research** - Find what tools are being recommended
3. **Content Verification** - Check facts with sources
4. **SERP Analysis** - See which domains appear in AI responses

---

## 📝 Configuration Checklist

- [ ] Obtain OpenAI API key from https://platform.openai.com/api-keys
- [ ] Obtain Anthropic API key from https://console.anthropic.com/
- [ ] Obtain Google API key from https://makersuite.google.com/app/apikey
- [ ] Obtain Perplexity API key from https://www.perplexity.ai/settings/api
- [ ] Run SQL script to add API keys to `LlmProviderConfigs` table
- [ ] (Optional) Configure job-specific models in `LlmJobConfigs` table
- [ ] Test each provider connection
- [ ] Test prompt across all platforms
- [ ] Verify citations are being extracted

---

## 🚀 Build and Deploy

```bash
# Build backend
dotnet build

# Run backend
dotnet run --project AppBackendBasic.API

# Build frontend
cd AppBackendBasic.WebUI
npm run build

# Run frontend
npm run dev
```

---

## ⚠️ Important Notes

### **API Key Security**

- API keys are stored encrypted in the database (`IsEncrypted = TRUE`)
- Never commit API keys to version control
- Use environment variables for local development if preferred

### **Rate Limits**

Each provider has different rate limits:
- **OpenAI**: 3,500 requests/min (GPT-4), 10,000 requests/min (GPT-3.5)
- **Anthropic**: 5 requests/sec
- **Google**: 60 requests/min (free tier)
- **Perplexity**: Varies by plan

### **Cost Management**

Monitor usage to avoid unexpected costs:
- Set up billing alerts in each provider's dashboard
- Use cheaper models for testing (GPT-3.5, Haiku, Gemini Pro)
- Use expensive models (GPT-4, Opus) only for production

---

## 🎯 Summary

**What's Implemented:**
- ✅ 5 LLM providers (OpenAI, Anthropic, Google Gemini, Perplexity, Google Search)
- ✅ Automatic citation extraction
- ✅ Multi-platform prompt testing
- ✅ Google Search grounding for AI Overview simulation
- ✅ Cost tracking per request
- ✅ Token usage monitoring
- ✅ Database-driven configuration
- ✅ Frontend integration complete

**What You Need to Do:**
1. Get API keys from each provider (Google Search uses same key as Gemini!)
2. Run SQL script to add keys to database
3. Test each provider
4. Start tracking AI visibility!

**Files Created/Modified:**
- ✅ Created `PerplexityProvider.cs`
- ✅ Created `GoogleSearchProvider.cs`
- ✅ Updated `LlmProviderFactory.cs`
- ✅ Updated `LlmProviderService.cs`
- ✅ Updated `Program.cs` (DI registration)

**Ready to use!** 🎉
