# LLM Service Lifetime Fix

## 🔴 Error

```
System.InvalidOperationException: Cannot resolve scoped service 'AppBackendBasic.Infrastructure.Services.LlmProviders.OpenAIProvider' from root provider.
   at AppBackendBasic.Infrastructure.Services.LlmProviders.LlmProviderFactory.GetProvider(String providerName)
   at AppBackendBasic.Infrastructure.Services.LlmProviderService.SendRequestAsync(LlmRequestDto request)
```

---

## 🔍 Root Cause

**Service Lifetime Mismatch**

The `LlmProviderFactory` was registered as **Singleton**, but it was trying to resolve **Scoped** services (`OpenAIProvider`, `AnthropicProvider`, `GoogleGeminiProvider`).

### **The Problem:**

```csharp
// Program.cs - BEFORE (INCORRECT)
builder.Services.AddScoped<OpenAIProvider>();           // Scoped
builder.Services.AddScoped<AnthropicProvider>();        // Scoped
builder.Services.AddScoped<GoogleGeminiProvider>();     // Scoped
builder.Services.AddSingleton<LlmProviderFactory>();    // ❌ Singleton trying to resolve Scoped
```

### **Why This Fails:**

In .NET dependency injection:
- **Singleton** services live for the entire application lifetime
- **Scoped** services live for the duration of a single request
- A **Singleton** service **cannot** resolve **Scoped** services because the Singleton outlives the Scoped service's lifetime

When `LlmProviderFactory` (Singleton) tried to call:
```csharp
_serviceProvider.GetRequiredService<OpenAIProvider>()  // Scoped service
```

.NET threw an exception because it would create a memory leak - the Singleton would hold a reference to a Scoped service that should be disposed after each request.

---

## ✅ Solution

**Change `LlmProviderFactory` from Singleton to Scoped**

### **File:** `@/Users/dhawal/Documents/GitHub/AISEO/AISEODevelop/AISEO/AppBackendBasic.API/Program.cs:316`

**Before:**
```csharp
builder.Services.AddSingleton<AppBackendBasic.Infrastructure.Services.LlmProviders.LlmProviderFactory>();
```

**After:**
```csharp
builder.Services.AddScoped<AppBackendBasic.Infrastructure.Services.LlmProviders.LlmProviderFactory>();
```

---

## 🎯 Service Lifetime Rules

### **Singleton**
- Lives for the entire application lifetime
- Created once and reused for all requests
- **Can resolve:** Singleton services only
- **Cannot resolve:** Scoped or Transient services
- **Use for:** Stateless services, caches, factories that don't depend on request-scoped data

### **Scoped**
- Lives for the duration of a single HTTP request
- Created once per request
- **Can resolve:** Singleton, Scoped, and Transient services
- **Use for:** Services that need request-specific data (DbContext, HttpContext, etc.)

### **Transient**
- Created every time it's requested
- **Can resolve:** Singleton, Scoped, and Transient services
- **Use for:** Lightweight, stateless services

---

## 📊 Current LLM Service Registrations

After the fix, all LLM services are now **Scoped**:

```csharp
// HTTP Clients for LLM providers
builder.Services.AddHttpClient<OpenAIProvider>();
builder.Services.AddHttpClient<AnthropicProvider>();
builder.Services.AddHttpClient<GoogleGeminiProvider>();

// LLM Provider implementations (Scoped)
builder.Services.AddScoped<OpenAIProvider>();
builder.Services.AddScoped<AnthropicProvider>();
builder.Services.AddScoped<GoogleGeminiProvider>();

// LLM Provider Factory (Scoped) ✅ FIXED
builder.Services.AddScoped<LlmProviderFactory>();

// LLM Services (Scoped)
builder.Services.AddScoped<ILlmProviderConfigService, LlmProviderConfigService>();
builder.Services.AddScoped<ILlmProviderService, LlmProviderService>();
builder.Services.AddScoped<ILlmJobConfigService, LlmJobConfigService>();
```

---

## 🔄 Request Flow (After Fix)

```
1. HTTP Request arrives
   ↓
2. ASP.NET Core creates a new Scoped service container
   ↓
3. LlmProviderService (Scoped) is created
   ↓
4. LlmProviderFactory (Scoped) is created
   ↓
5. LlmProviderFactory.GetProvider("OpenAI") is called
   ↓
6. OpenAIProvider (Scoped) is resolved from the SAME scope
   ↓
7. Request is processed
   ↓
8. Request ends, all Scoped services are disposed
```

**Before the fix:** Step 4 would fail because LlmProviderFactory was Singleton trying to resolve Scoped OpenAIProvider.

---

## ✅ Why Scoped is Correct for LlmProviderFactory

1. **Resolves Scoped Services**
   - `OpenAIProvider`, `AnthropicProvider`, `GoogleGeminiProvider` are all Scoped
   - Factory needs to be Scoped to resolve them

2. **Request-Scoped Data**
   - LLM providers may use `ApplicationDbContext` (Scoped)
   - LLM providers may use `HttpContext` (Scoped)

3. **No State to Preserve**
   - Factory doesn't maintain state between requests
   - No benefit to making it Singleton

4. **Proper Disposal**
   - Scoped services are properly disposed after each request
   - Prevents memory leaks

---

## 🚀 Testing

### **1. Rebuild Application**
```bash
dotnet build
```

### **2. Start Application**
```bash
dotnet run --project AppBackendBasic.API
```

### **3. Test LLM Request**
```bash
curl -X POST http://localhost:5000/api/llm-providers/test-prompt \
  -H "Content-Type: application/json" \
  -d '{
    "projectId": 5,
    "promptText": "What are the best project management tools?",
    "brandName": "YourBrand",
    "platforms": ["ChatGPT"]
  }'
```

**Expected:** No more "Cannot resolve scoped service" errors

### **4. Check Logs**
Should see:
```
LlmJobConfig loaded from DB for PromptTest: OpenAI/gpt-4o
```

Should NOT see:
```
Cannot resolve scoped service 'OpenAIProvider' from root provider
```

---

## 📝 Related Issues

This error commonly occurs when:

1. **Singleton resolves Scoped** (this case)
   - Fix: Change Singleton to Scoped

2. **Background job resolves Scoped without scope**
   - Fix: Create a scope in the background job:
   ```csharp
   using (var scope = _serviceProvider.CreateScope())
   {
       var service = scope.ServiceProvider.GetRequiredService<IScopedService>();
   }
   ```

3. **Static method resolves Scoped**
   - Fix: Don't use static methods for DI, or create a scope

---

## ✅ Summary

**Problem:** `LlmProviderFactory` was Singleton trying to resolve Scoped services  
**Solution:** Changed `LlmProviderFactory` to Scoped  
**Result:** All LLM services now have compatible lifetimes  
**Status:** Fixed and ready to test

The error was a classic .NET dependency injection lifetime mismatch. Now all services are properly aligned.
