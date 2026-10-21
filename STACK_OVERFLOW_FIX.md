# Maximum Call Stack Size Exceeded - Complete Fix

## 🔴 Error
```
Maximum call stack size exceeded
Page: LlmProviders/Edit/11
```

## 🔍 Root Causes Found

### **1. Infinite Recursion in api-client.js**
The `handleRateLimit` method could recursively call `request()` indefinitely without a retry limit.

```javascript
// BEFORE (Potential infinite loop)
async handleRateLimit(response, url, options) {
    await this.sleep(seconds * 1000);
    return await this.request(url, options); // ← Could loop forever
}
```

### **2. Event Listener Timing Issues**
Event listener was attached before DOM was fully ready, potentially causing multiple attachments or timing issues.

### **3. Circular References in JSON**
API was returning entities with navigation properties causing circular references (already fixed by returning DTOs).

---

## ✅ Fixes Applied

### **Fix 1: Add Retry Limit to api-client.js**

**File:** `@/Users/dhawal/Documents/GitHub/AISEO/AISEODevelop/AISEO/AppBackendBasic.AdminUI/wwwroot/js/api-client.js`

```javascript
class ApiClient {
    constructor(baseUrl) {
        this.baseUrl = baseUrl || '';
        this.maxRetries = 3; // ← Added retry limit
    }

    async request(url, options = {}) {
        // Initialize retry count
        if (!mergedOptions._retryCount) {
            mergedOptions._retryCount = 0;
        }

        // Check retry limit before handling rate limit
        if (response.status === 429) {
            if (mergedOptions._retryCount >= this.maxRetries) {
                throw new Error('Maximum retry attempts exceeded');
            }
            return await this.handleRateLimit(response, url, mergedOptions);
        }
    }

    async handleRateLimit(response, url, options) {
        options._retryCount = (options._retryCount || 0) + 1; // ← Increment
        await this.sleep(seconds * 1000);
        return await this.request(url, options); // ← Now safe with limit
    }
}
```

---

### **Fix 2: Proper Script Initialization in Edit.cshtml**

**File:** `@/Users/dhawal/Documents/GitHub/AISEO/AISEODevelop/AISEO/AppBackendBasic.AdminUI/Pages/LlmProviders/Edit.cshtml`

```javascript
(function() {
    'use strict';
    
    const providerId = @Model.Id;
    let formSubmitHandlerAttached = false; // ← Prevent duplicate attachment

    function attachFormHandler() {
        const form = document.getElementById('providerForm');
        if (!form || formSubmitHandlerAttached) {
            return; // ← Exit if already attached
        }
        formSubmitHandlerAttached = true;
        
        form.addEventListener('submit', async function(e) {
            // ... form submission logic
        });
    }

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function() {
            attachFormHandler();
            loadProvider();
        });
    } else {
        attachFormHandler();
        loadProvider();
    }
})(); // ← IIFE to avoid global scope pollution
```

**Key improvements:**
- ✅ Wrapped in IIFE (Immediately Invoked Function Expression)
- ✅ Checks if event listener already attached
- ✅ Waits for DOM ready before attaching listeners
- ✅ Handles both loading and loaded states

---

### **Fix 3: Return DTOs from API (Already Applied)**

**File:** `@/Users/dhawal/Documents/GitHub/AISEO/AISEODevelop/AISEO/AppBackendBasic.API/Controllers/AdminController.cs`

```csharp
[HttpGet("llm-providers/{id}")]
public async Task<IActionResult> GetLLMProvider(int id)
{
    var provider = await _context.LlmProviders
        .AsNoTracking() // ← No tracking
        .FirstOrDefaultAsync(p => p.Id == id);
    
    // Return DTO instead of entity
    var dto = new
    {
        provider.Id,
        provider.Name,
        provider.DisplayName,
        // ... other properties (no navigation properties)
    };
    
    return Ok(dto); // ← No circular references
}
```

---

## 🧪 Testing

```bash
# Rebuild application
dotnet build

# Run application
dotnet run --project AppBackendBasic.API

# Test Edit page
# 1. Navigate to AdminUI → LLM Providers
# 2. Click "Edit" on any provider
# 3. Page should load without stack overflow error
# 4. Form should populate with provider data
# 5. Submit form to test update functionality
```

---

## 📊 What Was Causing the Stack Overflow

**Scenario 1: Infinite Rate Limit Retry**
```
request() → 429 error → handleRateLimit() → request() → 429 error → handleRateLimit() → ...
                                                                                        ↑
                                                                        Stack overflow! ↑
```

**Scenario 2: Multiple Event Listeners**
```
Page load → attach listener
Page reload → attach listener again
Script re-execution → attach listener again
...
Submit form → all listeners fire → infinite loop
```

**Scenario 3: Circular JSON**
```
Provider object → Configs collection → Each config has Provider property → ...
                                                                           ↑
                                                           Stack overflow! ↑
```

---

## ✅ Summary

| Issue | Status |
|-------|--------|
| Infinite recursion in rate limit handler | ✅ Fixed - Added retry limit |
| Event listener duplication | ✅ Fixed - Added attachment check |
| DOM timing issues | ✅ Fixed - Proper ready state handling |
| Circular JSON references | ✅ Fixed - Return DTOs from API |
| Script scope pollution | ✅ Fixed - Wrapped in IIFE |

---

## 📝 Files Modified

1. **api-client.js** - Added retry limit and counter
2. **Edit.cshtml** - Improved script initialization
3. **AdminController.cs** - Return DTOs instead of entities

The stack overflow error should now be completely resolved. The application has proper safeguards against infinite recursion and circular references.
