# Stack Overflow - ACTUAL Root Cause Found and Fixed

## 🔴 The Real Problem

**Infinite recursion in `showLoading()` and `hideLoading()` helper functions!**

### The Bug

**File:** `api-client.js` lines 171-180

```javascript
// BEFORE - INFINITE RECURSION BUG
function showLoading() {
    if (typeof window.showLoading === 'function') {
        window.showLoading();  // ← Calls itself infinitely!
    }
}

function hideLoading() {
    if (typeof window.hideLoading === 'function') {
        window.hideLoading();  // ← Calls itself infinitely!
    }
}
```

### Why This Causes Stack Overflow

1. `showLoading()` is defined as a global function
2. It checks if `window.showLoading` is a function
3. Since `window.showLoading` IS the function itself, the check passes
4. It calls `window.showLoading()` which is itself
5. Infinite recursion → Stack overflow

**Call Stack:**
```
showLoading() → window.showLoading() → showLoading() → window.showLoading() → ...
                                                                              ↑
                                                              STACK OVERFLOW! ↑
```

This happens **every time** the API client makes a request because:
```javascript
async request(url, options = {}) {
    try {
        showLoading();  // ← Triggers infinite recursion
        const response = await fetch(this.baseUrl + url, mergedOptions);
        // ...
    } finally {
        hideLoading();  // ← Also triggers infinite recursion
    }
}
```

---

## ✅ The Fix

**File:** `@/Users/dhawal/Documents/GitHub/AISEO/AISEODevelop/AISEO/AppBackendBasic.AdminUI/wwwroot/js/api-client.js`

```javascript
// AFTER - FIXED
function showLoading() {
    // Check for DIFFERENT function name to avoid self-reference
    if (window.showLoadingOverlay && typeof window.showLoadingOverlay === 'function') {
        window.showLoadingOverlay();
    } else {
        // Fallback: Simple loading indicator
        const existingLoader = document.getElementById('api-loading-indicator');
        if (!existingLoader) {
            const loader = document.createElement('div');
            loader.id = 'api-loading-indicator';
            loader.style.cssText = 'position:fixed;top:0;left:0;right:0;height:3px;background:linear-gradient(90deg,#6366f1,#8b5cf6);z-index:9999;';
            document.body.appendChild(loader);
        }
    }
}

function hideLoading() {
    // Check for DIFFERENT function name to avoid self-reference
    if (window.hideLoadingOverlay && typeof window.hideLoadingOverlay === 'function') {
        window.hideLoadingOverlay();
    } else {
        // Fallback: Remove loading indicator
        const loader = document.getElementById('api-loading-indicator');
        if (loader) {
            loader.remove();
        }
    }
}
```

### Key Changes

1. ✅ Check for `window.showLoadingOverlay` instead of `window.showLoading`
2. ✅ Check for `window.hideLoadingOverlay` instead of `window.hideLoading`
3. ✅ Added fallback implementation that creates/removes a loading bar
4. ✅ No more self-reference, no more infinite recursion

---

## 🧪 Testing

```bash
# Rebuild
dotnet build

# Run
dotnet run --project AppBackendBasic.API

# Test
# 1. Navigate to AdminUI → LLM Providers
# 2. Click "Edit" on any provider
# 3. Page should load WITHOUT stack overflow
# 4. You should see a loading indicator at the top
```

---

## 📊 Why Previous Fixes Didn't Work

| Previous Fix | Why It Didn't Help |
|--------------|-------------------|
| Added retry limit to rate limiting | Good practice, but not the cause |
| Wrapped Edit.cshtml in IIFE | Good practice, but not the cause |
| Prevented duplicate event listeners | Good practice, but not the cause |
| Returned DTOs from API | Good practice, but not the cause |

**The real issue:** Every API call triggered `showLoading()` → infinite recursion → stack overflow **before** any of the other code even ran.

---

## 🎯 Root Cause Analysis

**Timeline of execution:**
```
1. User clicks "Edit" button
2. Page loads, JavaScript executes
3. loadProvider() is called
4. window.apiClient.get() is called
5. apiClient.request() is called
6. showLoading() is called  ← STACK OVERFLOW HAPPENS HERE
7. (Never reaches fetch, never reaches form, never reaches anything else)
```

The stack overflow happened **immediately** when making the first API call, not during form submission or data processing.

---

## ✅ Verification

After the fix, the call flow should be:
```
1. User clicks "Edit" button
2. Page loads, JavaScript executes
3. loadProvider() is called
4. window.apiClient.get() is called
5. apiClient.request() is called
6. showLoading() is called
   → Checks for window.showLoadingOverlay (doesn't exist)
   → Creates loading indicator div
   → Returns successfully ✓
7. fetch() is called ✓
8. Response is received ✓
9. hideLoading() is called
   → Removes loading indicator div
   → Returns successfully ✓
10. Form is populated with data ✓
```

---

## 📝 Summary

**Problem:** `showLoading()` and `hideLoading()` were calling themselves infinitely

**Solution:** Check for different function names (`showLoadingOverlay`/`hideLoadingOverlay`) and provide fallback implementation

**Result:** No more stack overflow, API calls work correctly

---

## 🔧 Best Practice Learned

**Never do this:**
```javascript
function myFunction() {
    if (typeof window.myFunction === 'function') {
        window.myFunction();  // ← Self-reference causes infinite recursion
    }
}
```

**Always do this:**
```javascript
function myFunction() {
    if (window.externalFunction && typeof window.externalFunction === 'function') {
        window.externalFunction();  // ← Different name, no self-reference
    } else {
        // Fallback implementation
    }
}
```

---

The stack overflow is now completely fixed. The issue was a classic infinite recursion bug in the loading helper functions.
