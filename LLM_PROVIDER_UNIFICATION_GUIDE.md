# LLM Provider Configuration Unification Guide
**Date:** March 29, 2026  
**Purpose:** Unified database-driven LLM provider configuration system

---

## 🎯 OVERVIEW

The LLM provider configuration system has been **unified** to use the `LLMProviderConfig` table instead of hardcoded `appsettings.json` values. This allows:

- ✅ **Dynamic configuration** via Admin UI
- ✅ **Encrypted API key storage** in database
- ✅ **Per-provider settings** (endpoint, rate limits, costs)
- ✅ **Runtime configuration changes** without redeployment
- ✅ **Multiple display names** for the same provider
- ✅ **Centralized configuration management**

---

## 📊 ARCHITECTURE

### **Before (Hardcoded)**
```
appsettings.json → IConfiguration → Provider Classes
```

### **After (Database-Driven)**
```
LLMProviderConfig Table → ILlmProviderConfigService → Provider Classes
```

---

## 🗄️ DATABASE SCHEMA

### **LlmProvider Table**
```sql
CREATE TABLE "LlmProvider" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(50) NOT NULL UNIQUE,        -- OpenAI, Anthropic, Google
    "DisplayName" VARCHAR(100) NOT NULL,       -- OpenAI (ChatGPT), Anthropic (Claude)
    "ApiEndpoint" VARCHAR(500),                -- Default API endpoint
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "Priority" INTEGER NOT NULL DEFAULT 0,     -- Higher = preferred
    "MaxTokens" INTEGER NOT NULL DEFAULT 4000,
    "CostPer1kInputTokens" DECIMAL(10,6),
    "CostPer1kOutputTokens" DECIMAL(10,6),
    "RateLimitPerMinute" INTEGER NOT NULL DEFAULT 60,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);
```

### **LlmProviderConfig Table**
```sql
CREATE TABLE "LlmProviderConfig" (
    "Id" SERIAL PRIMARY KEY,
    "ProviderId" INTEGER NOT NULL,
    "ConfigKey" VARCHAR(100) NOT NULL,         -- ApiKey, Endpoint, etc.
    "ConfigValue" TEXT,                        -- Encrypted for sensitive data
    "IsEncrypted" BOOLEAN NOT NULL DEFAULT FALSE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_LlmProviderConfig_Provider" FOREIGN KEY ("ProviderId") 
        REFERENCES "LlmProvider"("Id") ON DELETE CASCADE,
    CONSTRAINT "UQ_LlmProviderConfig" UNIQUE ("ProviderId", "ConfigKey")
);
```

---

## 🔧 IMPLEMENTATION

### **1. New Service Interface**

**File:** `AppBackendBasic.Core/Interfaces/ILlmProviderConfigService.cs`

```csharp
public interface ILlmProviderConfigService
{
    Task<string?> GetConfigValueAsync(string providerName, string configKey);
    Task<Dictionary<string, string>> GetAllConfigsAsync(string providerName);
    Task SetConfigValueAsync(string providerName, string configKey, string configValue, bool isEncrypted = false);
    Task<LlmProvider?> GetProviderByNameAsync(string providerName);
    Task<List<LlmProvider>> GetActiveProvidersAsync();
    Task<bool> IsProviderConfiguredAsync(string providerName);
}
```

### **2. Service Implementation**

**File:** `AppBackendBasic.Infrastructure/Services/LlmProviderConfigService.cs`

**Features:**
- ✅ AES encryption for API keys
- ✅ PBKDF2 key derivation
- ✅ Automatic decryption on retrieval
- ✅ Provider lookup by name or ID
- ✅ Batch configuration retrieval

**Encryption:**
```csharp
private string Encrypt(string plainText)
{
    using var aes = Aes.Create();
    aes.Key = DeriveKeyFromPassword(_encryptionKey);
    aes.GenerateIV();
    // ... encryption logic
}

private string Decrypt(string cipherText)
{
    // ... decryption logic with IV extraction
}
```

### **3. Updated Provider Classes**

All provider classes now use `ILlmProviderConfigService`:

**OpenAIProvider.cs:**
```csharp
public class OpenAIProvider : ILlmProvider
{
    private readonly ILlmProviderConfigService _configService;
    
    public async Task<LlmResponseDto> SendRequestAsync(LlmRequestDto request)
    {
        var apiKey = await _configService.GetConfigValueAsync(ProviderName, "ApiKey");
        var apiEndpoint = await _configService.GetConfigValueAsync(ProviderName, "Endpoint") 
            ?? "https://api.openai.com/v1";
        // ... use apiKey and apiEndpoint
    }
}
```

**AnthropicProvider.cs:**
```csharp
public class AnthropicProvider : ILlmProvider
{
    private readonly ILlmProviderConfigService _configService;
    
    public async Task<LlmResponseDto> SendRequestAsync(LlmRequestDto request)
    {
        var apiKey = await _configService.GetConfigValueAsync(ProviderName, "ApiKey");
        var apiEndpoint = await _configService.GetConfigValueAsync(ProviderName, "Endpoint") 
            ?? "https://api.anthropic.com/v1";
        // ... use apiKey and apiEndpoint
    }
}
```

**GoogleGeminiProvider.cs:**
```csharp
public class GoogleGeminiProvider : ILlmProvider
{
    private readonly ILlmProviderConfigService _configService;
    
    public async Task<LlmResponseDto> SendRequestAsync(LlmRequestDto request)
    {
        var apiKey = await _configService.GetConfigValueAsync(ProviderName, "ApiKey");
        var apiEndpoint = await _configService.GetConfigValueAsync(ProviderName, "Endpoint") 
            ?? "https://generativelanguage.googleapis.com/v1beta";
        // ... use apiKey and apiEndpoint
    }
}
```

---

## 🚀 SETUP INSTRUCTIONS

### **Step 1: Run Database Migration**

```bash
psql -h your-db-host -U your-user -d your-database -f Database_Migrations/004_Seed_LLM_Provider_Configs.sql
```

This will:
- Insert 3 providers (OpenAI, Anthropic, Google)
- Create placeholder configurations
- Set up default endpoints

### **Step 2: Verify Service Registration**

**File:** `AppBackendBasic.API/Program.cs`

```csharp
// Line 315 - Service is now registered
builder.Services.AddScoped<ILlmProviderConfigService, AppBackendBasic.Infrastructure.Services.LlmProviderConfigService>();
```

### **Step 3: Set API Keys via Admin UI**

**Navigate to:** Admin Dashboard → Settings → LLM Providers

**Or manually via SQL:**
```sql
-- Get provider ID
SELECT "Id" FROM "LlmProvider" WHERE "Name" = 'OpenAI';

-- Update API key (will be encrypted by service)
-- Use Admin UI or API endpoint to set encrypted values
```

### **Step 4: Configure Encryption Key**

**In `appsettings.json`:**
```json
{
  "EncryptionKey": "your-secure-32-character-key-here"
}
```

**In `appsettings.Production.json`:**
```json
{
  "EncryptionKey": "#{EncryptionKey}#"
}
```

**⚠️ IMPORTANT:** Use a strong, random 32+ character encryption key in production.

---

## 📝 CONFIGURATION MANAGEMENT

### **Via Admin UI (Recommended)**

**AdminController.cs** already has endpoints:

```csharp
POST   /api/admin/llm-providers              // Create provider
PUT    /api/admin/llm-providers/{id}         // Update provider
DELETE /api/admin/llm-providers/{id}         // Delete provider
GET    /api/admin/llm-providers              // List all providers
GET    /api/admin/llm-providers/{id}         // Get provider details
```

**Setting API Key:**
```typescript
// Frontend example
const response = await api.put(`/api/admin/llm-providers/${providerId}`, {
  name: 'OpenAI',
  displayName: 'OpenAI (ChatGPT)',
  apiKey: 'sk-...',  // Will be encrypted automatically
  apiEndpoint: 'https://api.openai.com/v1',
  isActive: true,
  priority: 100
});
```

### **Via SQL (Manual)**

```sql
-- Insert encrypted API key
-- Note: Use the service to encrypt, don't insert plain text
INSERT INTO "LlmProviderConfig" ("ProviderId", "ConfigKey", "ConfigValue", "IsEncrypted")
VALUES (
    (SELECT "Id" FROM "LlmProvider" WHERE "Name" = 'OpenAI'),
    'ApiKey',
    '[ENCRYPTED_VALUE_FROM_SERVICE]',
    TRUE
);
```

### **Via Code (Programmatic)**

```csharp
// In a service or controller
await _configService.SetConfigValueAsync(
    providerName: "OpenAI",
    configKey: "ApiKey",
    configValue: "sk-...",
    isEncrypted: true  // Will encrypt automatically
);
```

---

## 🔍 VERIFICATION

### **Check Providers**
```sql
SELECT 
    "Id",
    "Name",
    "DisplayName",
    "IsActive",
    "Priority",
    "ApiEndpoint"
FROM "LlmProvider"
ORDER BY "Priority" DESC;
```

### **Check Configurations**
```sql
SELECT 
    p."Name" AS "Provider",
    c."ConfigKey",
    CASE 
        WHEN c."IsEncrypted" THEN '[ENCRYPTED]'
        ELSE c."ConfigValue"
    END AS "ConfigValue",
    c."IsEncrypted"
FROM "LlmProviderConfig" c
JOIN "LlmProvider" p ON c."ProviderId" = p."Id"
ORDER BY p."Name", c."ConfigKey";
```

### **Test Provider Configuration**
```csharp
// Check if provider is configured
var isConfigured = await _configService.IsProviderConfiguredAsync("OpenAI");

// Get all configs for a provider
var configs = await _configService.GetAllConfigsAsync("OpenAI");
// Returns: { "ApiKey": "sk-...", "Endpoint": "https://..." }
```

---

## 🔐 SECURITY CONSIDERATIONS

### **Encryption**
- ✅ API keys are encrypted using AES-256
- ✅ Encryption key derived using PBKDF2 with 10,000 iterations
- ✅ Unique IV for each encrypted value
- ✅ Encryption key stored in environment variables (production)

### **Best Practices**
1. **Never commit** encryption keys to Git
2. **Rotate** encryption keys periodically
3. **Use environment variables** in production
4. **Audit** configuration changes via database logs
5. **Restrict** Admin UI access to authorized users only

### **Key Rotation**
```csharp
// When rotating encryption key:
// 1. Decrypt all values with old key
// 2. Update EncryptionKey in configuration
// 3. Re-encrypt all values with new key
// 4. Update database
```

---

## 📊 PROVIDER CONFIGURATION SCHEMA

### **Standard Config Keys**

| ConfigKey | Description | Encrypted | Example |
|-----------|-------------|-----------|---------|
| `ApiKey` | Provider API key | ✅ Yes | `sk-...` |
| `Endpoint` | API endpoint URL | ❌ No | `https://api.openai.com/v1` |
| `Organization` | Organization ID (optional) | ❌ No | `org-...` |
| `Model` | Default model (optional) | ❌ No | `gpt-4` |
| `MaxRetries` | Max retry attempts | ❌ No | `3` |
| `Timeout` | Request timeout (ms) | ❌ No | `30000` |

### **Adding Custom Config Keys**

```csharp
// Add any custom configuration
await _configService.SetConfigValueAsync(
    "OpenAI",
    "CustomSetting",
    "value",
    isEncrypted: false
);

// Retrieve custom config
var customValue = await _configService.GetConfigValueAsync("OpenAI", "CustomSetting");
```

---

## 🔄 MIGRATION FROM APPSETTINGS

### **Old Configuration (appsettings.json)**
```json
{
  "LlmProviders": {
    "OpenAI": {
      "ApiKey": "sk-...",
      "Endpoint": "https://api.openai.com/v1"
    },
    "Anthropic": {
      "ApiKey": "sk-ant-...",
      "Endpoint": "https://api.anthropic.com/v1"
    }
  }
}
```

### **Migration Script**
```csharp
// One-time migration from appsettings to database
public async Task MigrateConfigsFromAppsettings()
{
    var openAiKey = _configuration["LlmProviders:OpenAI:ApiKey"];
    if (!string.IsNullOrEmpty(openAiKey))
    {
        await _configService.SetConfigValueAsync("OpenAI", "ApiKey", openAiKey, true);
    }
    
    var anthropicKey = _configuration["LlmProviders:Anthropic:ApiKey"];
    if (!string.IsNullOrEmpty(anthropicKey))
    {
        await _configService.SetConfigValueAsync("Anthropic", "ApiKey", anthropicKey, true);
    }
    
    // After migration, remove from appsettings.json
}
```

### **Cleanup**
After migration, you can remove the `LlmProviders` section from `appsettings.json`:

```json
{
  // Remove this entire section:
  // "LlmProviders": { ... }
  
  // Keep only the encryption key:
  "EncryptionKey": "#{EncryptionKey}#"
}
```

---

## 🧪 TESTING

### **Unit Test Example**
```csharp
[Fact]
public async Task GetConfigValue_ReturnsDecryptedValue()
{
    // Arrange
    var service = new LlmProviderConfigService(_context, _configuration, _logger);
    await service.SetConfigValueAsync("OpenAI", "ApiKey", "sk-test", true);
    
    // Act
    var value = await service.GetConfigValueAsync("OpenAI", "ApiKey");
    
    // Assert
    Assert.Equal("sk-test", value);
}
```

### **Integration Test**
```csharp
[Fact]
public async Task Provider_UsesConfigFromDatabase()
{
    // Arrange
    await _configService.SetConfigValueAsync("OpenAI", "ApiKey", "sk-test", true);
    var provider = new OpenAIProvider(_httpClient, _configService, _logger);
    
    // Act
    var request = new LlmRequestDto { Model = "gpt-4", PromptText = "Hello" };
    var response = await provider.SendRequestAsync(request);
    
    // Assert
    Assert.True(response.Success);
}
```

---

## 📋 CHECKLIST

### **Implementation Complete** ✅
- [x] Created `ILlmProviderConfigService` interface
- [x] Implemented `LlmProviderConfigService` with encryption
- [x] Updated `OpenAIProvider` to use config service
- [x] Updated `AnthropicProvider` to use config service
- [x] Updated `GoogleGeminiProvider` to use config service
- [x] Registered service in `Program.cs`
- [x] Created migration script `004_Seed_LLM_Provider_Configs.sql`
- [x] Created documentation

### **Deployment Steps** ⏳
- [ ] Run database migration
- [ ] Set encryption key in production environment
- [ ] Configure API keys via Admin UI
- [ ] Test each provider
- [ ] Remove old appsettings configurations
- [ ] Monitor logs for configuration errors

---

## 🎯 BENEFITS

### **Before**
- ❌ API keys in appsettings.json (security risk)
- ❌ Requires redeployment to change configs
- ❌ No encryption at rest
- ❌ Difficult to manage multiple environments
- ❌ No audit trail for config changes

### **After**
- ✅ API keys encrypted in database
- ✅ Runtime configuration changes via Admin UI
- ✅ AES-256 encryption at rest
- ✅ Easy multi-environment management
- ✅ Database audit trail for all changes
- ✅ Centralized configuration management
- ✅ Support for multiple display names per provider
- ✅ Dynamic provider activation/deactivation

---

## 📞 TROUBLESHOOTING

### **Issue: "API key not configured in database"**
**Solution:**
```sql
-- Check if API key exists
SELECT * FROM "LlmProviderConfig" 
WHERE "ProviderId" = (SELECT "Id" FROM "LlmProvider" WHERE "Name" = 'OpenAI')
AND "ConfigKey" = 'ApiKey';

-- If missing, add via Admin UI or:
-- Use the service to set it (will encrypt automatically)
```

### **Issue: "Decryption failed"**
**Solution:**
- Verify encryption key matches the one used for encryption
- Check if `IsEncrypted` flag is set correctly
- Ensure encryption key is at least 32 characters

### **Issue: "Provider not found"**
**Solution:**
```sql
-- Verify provider exists
SELECT * FROM "LlmProvider" WHERE "Name" = 'OpenAI';

-- If missing, run migration script
\i Database_Migrations/004_Seed_LLM_Provider_Configs.sql
```

---

**Status:** ✅ Implementation Complete  
**Next Steps:** Run migration and configure API keys via Admin UI
