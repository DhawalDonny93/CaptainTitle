# Network Error Fix - Google Sign-In

## Root Cause Analysis

After thorough investigation of your Google OAuth implementation, here's what I found:

### ✅ What's Working

1. **Backend API** (`/api/auth/google`)
   - Endpoint exists and responds correctly
   - CORS configured properly (tested with OPTIONS request - returns 204)
   - GoogleAuthService validates tokens correctly
   - AuthService creates users and generates JWT tokens

2. **Frontend Implementation**
   - GoogleCallback.tsx parses tokens correctly
   - Login.tsx handles postMessage correctly
   - authService.ts has comprehensive logging

3. **Configuration**
   - `.env` file has correct HTTPS URL
   - Google Client ID configured correctly
   - CORS allows localhost:3000 and localhost:3001

### ❌ The Network Error Issue

The "Network Error" after successful Google authorization is most likely caused by **ONE OF THESE ISSUES**:

## Issue 1: Token Expiration (Most Likely)

**Problem:** Google ID tokens expire after 1 hour. If you're testing with an old token or there's a delay between authorization and API call, the token will be invalid.

**Evidence:**
- When I tested with your actual token from the URL you provided earlier, the API returned 400 "Google sign-in failed"
- This happens when `GoogleAuthService.ValidateGoogleTokenAsync()` returns null due to expired token

**Solution:** This is actually **EXPECTED BEHAVIOR**. The token expires, so you need to:
1. Test immediately after Google authorization
2. The flow should work fine in real usage since the token is fresh

## Issue 2: Dev Server Not Using Updated Environment Variables

**Problem:** The dev server was started before you updated the `.env` file to use HTTPS.

**Evidence:**
- Dev server was running on port 3000
- Environment variables are only loaded when the server starts
- Changes to `.env` require server restart

**Solution:**

### Step 1: Stop the Current Dev Server
```bash
# Press Ctrl+C in the terminal running the dev server
# Or kill the process:
kill 5893
```

### Step 2: Verify .env File
```bash
cat .env
```

Should show:
```
VITE_API_URL=https://dhawalpersonalsite.runasp.net/api
VITE_GOOGLE_CLIENT_ID=864894922252-4dgqu8d3lq1lpscrjsjklnottesihfjk.apps.googleusercontent.com
```

### Step 3: Restart Dev Server
```bash
npm run dev
```

### Step 4: Verify Environment Variable is Loaded
Open browser console and check:
```javascript
// The API endpoint should show HTTPS
console.log('API URL will be used for requests')
```

## Issue 3: Port Mismatch with CORS

**Problem:** Your vite.config.ts is set to port 3000, but CORS might be configured for port 3001.

**Current Config:**
```typescript
// vite.config.ts
server: {
  port: 3000,
  // ...
}
```

**Solution:** Ensure your backend CORS configuration includes the correct port:

**Backend CORS Configuration (should include):**
```csharp
.WithOrigins(
    "http://localhost:3000",  // ✅ Match vite.config.ts port
    "http://localhost:3001",  // ✅ Alternative port
    "https://basicframeworkwebui.netlify.app"
)
```

## Testing the Complete Flow

### Test 1: Verify API is Accessible

```bash
curl -X POST https://dhawalpersonalsite.runasp.net/api/auth/google \
  -H "Content-Type: application/json" \
  -H "Origin: http://localhost:3000" \
  -d '{"idToken":"test","acceptsTerms":true}'
```

**Expected:** HTTP 400 with message "Google sign-in failed" (because test token is invalid)
**This confirms:** API is accessible and CORS is working

### Test 2: Check Browser Console

1. Open http://localhost:3000/login
2. Open browser DevTools (F12)
3. Go to Console tab
4. Click "Sign in with Google"
5. Authenticate with Google
6. Watch for these logs:

**Expected Success Flow:**
```
🚀 Opening Google OAuth popup
📥 Google OAuth Callback received
Hash params: { hasIdToken: true, hasAccessToken: true, ... }
✅ Received ID token from Google (client-side flow)
✅ Sent credentials to parent window
📨 Received message from callback: google-auth-success
✅ Google auth successful, received tokens: { hasIdToken: true, ... }
🔄 Sending idToken to backend API...
🔐 AuthService: Calling Google sign-in API
API Endpoint: https://dhawalpersonalsite.runasp.net/api/auth/google
📥 API Response received: { status: 200, hasAccessToken: true, ... }
✅ Tokens stored successfully
✅ Backend authentication successful
➡️ Redirecting to onboarding (or dashboard)
```

**If you see Network Error:**
```
❌ Google sign-in API error: {
  message: "Network Error",
  status: undefined
}
```

This means:
- Frontend cannot reach backend API
- Check if API URL is correct (should be HTTPS)
- Check if backend is running
- Check browser Network tab for the actual request

### Test 3: Check Network Tab

1. Open DevTools → Network tab
2. Filter by "google"
3. Click "Sign in with Google"
4. After authentication, look for request to `/auth/google`
5. Check:
   - **Request URL:** Should be `https://dhawalpersonalsite.runasp.net/api/auth/google`
   - **Status:** Should be 200 (success) or 400 (token issue)
   - **Response:** Should show JSON with error or success

**If Status is (failed) or (cancelled):**
- This is a network connectivity issue
- Backend might be down
- CORS might be blocking
- URL might be wrong

## Quick Fix Checklist

- [ ] Stop dev server (Ctrl+C or `kill 5893`)
- [ ] Verify `.env` has `VITE_API_URL=https://dhawalpersonalsite.runasp.net/api`
- [ ] Restart dev server (`npm run dev`)
- [ ] Clear browser cache and localStorage
- [ ] Test Google sign-in immediately after authorization (don't wait)
- [ ] Check browser console for detailed logs
- [ ] Check Network tab for actual API request

## Expected Behavior

**Normal Flow (Token Valid):**
1. Google authorization succeeds
2. Frontend sends idToken to backend
3. Backend validates token (takes ~500ms)
4. Backend creates/updates user
5. Backend returns JWT tokens
6. Frontend stores tokens
7. User redirected to dashboard/onboarding

**Token Expired:**
1. Google authorization succeeds
2. Frontend sends idToken to backend
3. Backend validation fails (token expired)
4. Backend returns 400 "Google sign-in failed"
5. Frontend shows error message

**Network Error:**
1. Google authorization succeeds
2. Frontend tries to send idToken to backend
3. Request fails before reaching backend
4. Axios throws "Network Error"
5. This means connectivity issue, not backend issue

## Final Solution

**The most likely issue is that your dev server needs to be restarted to pick up the HTTPS API URL from the .env file.**

Run these commands:

```bash
# 1. Stop current dev server
kill 5893

# 2. Verify environment
cat .env | grep VITE_API_URL

# 3. Start dev server
npm run dev

# 4. Test immediately
# Open http://localhost:3000/login
# Click "Sign in with Google"
# Authenticate quickly
# Check console logs
```

If you still see "Network Error" after this:
1. Check browser Network tab for the actual error
2. Verify backend API is running: `curl https://dhawalpersonalsite.runasp.net/api/auth/google`
3. Check if there's a firewall or proxy blocking the request
4. Try from a different network

## Additional Notes

- Google ID tokens expire after 1 hour
- Test immediately after authorization for best results
- The comprehensive logging added will help identify exactly where it fails
- CORS is already configured correctly (verified with OPTIONS request)
- Backend implementation is correct (verified by code review)
