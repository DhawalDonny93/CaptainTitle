# Google Authentication Troubleshooting Guide

## Complete Authentication Flow

### Step-by-Step Flow

1. **User clicks "Sign in with Google"** (`Login.tsx`)
   - Opens popup window with Google OAuth URL
   - Adds message event listener

2. **Google OAuth popup opens**
   - User authenticates with Google
   - Google redirects to: `https://yoursite.com/auth/google/callback#id_token=...&access_token=...`

3. **GoogleCallback.tsx processes the response**
   - Parses hash parameters (`id_token`, `access_token`, etc.)
   - Sends `postMessage` to parent window with tokens
   - Shows success message
   - Closes after 1.5 seconds

4. **Login.tsx receives the message**
   - Validates origin
   - Extracts `idToken` from message
   - Calls `authService.googleSignIn(idToken, acceptsTerms)`

5. **authService.googleSignIn sends to API**
   - POST to `/api/auth/google` with `{ idToken, acceptsTerms }`
   - API validates idToken with Google
   - API creates/updates user in database
   - API returns `{ accessToken, refreshToken, user }`

6. **authService stores tokens**
   - Stores `accessToken` in localStorage
   - Stores `refreshToken` in localStorage
   - Returns user data to Login.tsx

7. **Login.tsx redirects user**
   - If `user.onboardingCompleted === false` → `/onboarding`
   - If `user.onboardingCompleted === true` → `/dashboard`

## Console Logs to Expect

### Successful Flow

```
🚀 Opening Google OAuth popup
📥 Google OAuth Callback received
Hash params: { hasIdToken: true, hasAccessToken: true, tokenType: "Bearer", expiresIn: "3599", ... }
✅ Received ID token from Google (client-side flow)
✅ Sent credentials to parent window

📨 Received message from callback: google-auth-success
✅ Google auth successful, received tokens: { hasIdToken: true, hasAccessToken: true, ... }
🔄 Sending idToken to backend API...

🔐 AuthService: Calling Google sign-in API
API Endpoint: http://localhost:5082/api/auth/google
Request payload: { idToken: "eyJhbGciOiJSUzI1NiIsImtpZCI6IjZhOTA2ZWMxMTlkN2...", acceptsTerms: true }

📥 API Response received: { status: 200, hasAccessToken: true, hasRefreshToken: true, hasUser: true }
✅ Tokens stored successfully
User info: { email: "user@example.com", name: "User Name", onboardingCompleted: false }

✅ Backend authentication successful: { hasUser: true, onboardingCompleted: false }
➡️ Redirecting to onboarding
```

### Failed Flow - Common Issues

#### Issue 1: No idToken received

```
📥 Google OAuth Callback received
Hash params: { hasIdToken: false, hasAccessToken: true, ... }
❌ No tokens received from Google
```

**Cause:** Google OAuth response doesn't include `id_token`
**Solution:** Check OAuth URL includes `response_type=token id_token`

#### Issue 2: API returns 401 Unauthorized

```
🔐 AuthService: Calling Google sign-in API
❌ Google sign-in API error: {
  message: "Request failed with status code 401",
  status: 401,
  statusText: "Unauthorized",
  data: { message: "Invalid ID token" }
}
❌ Backend authentication failed: Error: Invalid ID token
```

**Cause:** Backend cannot validate the idToken with Google
**Solution:** 
- Verify Google Client ID matches in frontend and backend
- Check backend has correct Google Client Secret
- Ensure idToken hasn't expired (valid for 1 hour)

#### Issue 3: API returns 400 Bad Request

```
❌ Google sign-in API error: {
  status: 400,
  data: { message: "The idToken field is required." }
}
```

**Cause:** idToken not sent to API or is empty
**Solution:** Check that `event.data.idToken` exists in Login.tsx

#### Issue 4: CORS Error

```
Access to XMLHttpRequest at 'http://localhost:5082/api/auth/google' from origin 'http://localhost:3001' has been blocked by CORS policy
```

**Cause:** Backend not configured to accept requests from frontend origin
**Solution:** Configure CORS in backend to allow `http://localhost:3001` (dev) and your production domain

#### Issue 5: Network Error

```
❌ Google sign-in API error: {
  message: "Network Error",
  status: undefined
}
```

**Cause:** Backend API not running or wrong URL
**Solution:** 
- Verify backend is running
- Check `VITE_API_URL` in `.env` file
- Test API endpoint: `curl http://localhost:5082/api/auth/google`

## Testing Checklist

### Frontend Testing

1. **Open browser console** (F12)
2. **Navigate to login page**
3. **Click "Sign in with Google"**
4. **Check console logs:**
   - [ ] "🚀 Opening Google OAuth popup"
   - [ ] Popup opens successfully
   - [ ] No popup blocker warning

5. **Authenticate with Google**
6. **Check console logs in popup:**
   - [ ] "📥 Google OAuth Callback received"
   - [ ] "Hash params: { hasIdToken: true, ... }"
   - [ ] "✅ Received ID token from Google"
   - [ ] "✅ Sent credentials to parent window"

7. **Check console logs in parent window:**
   - [ ] "📨 Received message from callback: google-auth-success"
   - [ ] "✅ Google auth successful, received tokens"
   - [ ] "🔄 Sending idToken to backend API..."
   - [ ] "🔐 AuthService: Calling Google sign-in API"
   - [ ] "📥 API Response received"
   - [ ] "✅ Tokens stored successfully"
   - [ ] "✅ Backend authentication successful"
   - [ ] "➡️ Redirecting to onboarding" or "➡️ Redirecting to dashboard"

8. **Verify redirect:**
   - [ ] User redirected to correct page
   - [ ] No errors shown
   - [ ] User data loaded correctly

### Backend Testing

1. **Verify API endpoint exists:**
   ```bash
   curl -X POST http://localhost:5082/api/auth/google \
     -H "Content-Type: application/json" \
     -d '{"idToken":"test","acceptsTerms":true}'
   ```
   Should return 400 or 401 (not 404)

2. **Check backend logs** when frontend calls the API
3. **Verify backend validates idToken with Google**
4. **Verify backend creates/updates user in database**
5. **Verify backend returns correct response format:**
   ```json
   {
     "accessToken": "...",
     "refreshToken": "...",
     "user": {
       "id": "...",
       "email": "...",
       "name": "...",
       "onboardingCompleted": false
     }
   }
   ```

## Common Issues and Solutions

### Issue: "Google sign-in failed" error shown to user

**Debug Steps:**

1. **Open browser console**
2. **Look for the error log:**
   ```
   ❌ Backend authentication failed: Error: [error message]
   Error details: { message: "...", response: {...}, status: ... }
   ```

3. **Check the error details:**
   - `status: 401` → Invalid idToken or authentication failed
   - `status: 400` → Missing or invalid request data
   - `status: 500` → Backend server error
   - `status: undefined` → Network error or CORS issue

4. **Check backend logs** for detailed error information

### Issue: Popup closes but nothing happens

**Possible Causes:**

1. **Message not received by parent window**
   - Check console for "📨 Received message from callback"
   - If missing, check `window.opener` exists in popup
   - Verify origins match

2. **Event listener not set up**
   - Check "🚀 Opening Google OAuth popup" appears before popup opens
   - Verify `window.addEventListener('message', handleGoogleCallback)` is called

3. **Error occurred but not displayed**
   - Check console for error logs
   - Look for "❌" emoji in console

### Issue: User redirected but not authenticated

**Possible Causes:**

1. **Tokens not stored**
   - Check console for "✅ Tokens stored successfully"
   - Verify localStorage has `accessToken` and `refreshToken`:
     ```javascript
     console.log(localStorage.getItem('accessToken'))
     console.log(localStorage.getItem('refreshToken'))
     ```

2. **AuthContext not updated**
   - Check if `user` state is set in AuthContext
   - Verify `getCurrentUser()` is called after login

### Issue: Works locally but not on deployed site

**Possible Causes:**

1. **Environment variables not set**
   - Verify `VITE_API_URL` is set in Netlify environment variables
   - Verify `VITE_GOOGLE_CLIENT_ID` is set

2. **Google OAuth redirect URI not configured**
   - Add production URL to Google Cloud Console:
     `https://basicframeworkwebui.netlify.app/auth/google/callback`

3. **CORS not configured for production domain**
   - Backend must allow requests from production domain

4. **Netlify routing not configured**
   - Ensure `_redirects` file exists in `public/` folder
   - Ensure `netlify.toml` is in project root

## API Endpoint Requirements

Your backend API must implement:

### POST `/api/auth/google`

**Request:**
```json
{
  "idToken": "eyJhbGciOiJSUzI1NiIsImtpZCI6IjZhOTA2ZWMxMTlkN2JhNDZhNmE0M2VmMWVhODQyZTM0YThlZTA4YjQiLCJ0eXAiOiJKV1QifQ...",
  "acceptsTerms": true
}
```

**Response (Success - 200):**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "refresh_token_here",
  "user": {
    "id": "user_id",
    "email": "user@example.com",
    "name": "User Name",
    "firstName": "User",
    "lastName": "Name",
    "profilePicture": "https://...",
    "onboardingCompleted": false,
    "emailVerified": true
  }
}
```

**Response (Error - 401):**
```json
{
  "message": "Invalid ID token",
  "error": "Unauthorized"
}
```

**Backend Implementation Requirements:**

1. **Validate idToken with Google:**
   ```csharp
   // Use Google.Apis.Auth library
   var payload = await GoogleJsonWebSignature.ValidateAsync(idToken);
   ```

2. **Extract user information:**
   ```csharp
   var email = payload.Email;
   var name = payload.Name;
   var picture = payload.Picture;
   var googleId = payload.Subject;
   ```

3. **Create or update user in database:**
   - If user exists (by email or googleId), update their info
   - If user doesn't exist, create new user
   - Set `emailVerified = true` (Google verifies emails)

4. **Generate JWT tokens:**
   - Generate `accessToken` (short-lived, e.g., 15 minutes)
   - Generate `refreshToken` (long-lived, e.g., 7 days)

5. **Return response with tokens and user data**

## Environment Variables

### Frontend (.env)

```bash
VITE_API_URL=http://localhost:5082/api
VITE_GOOGLE_CLIENT_ID=864894922252-4dgqu8d3lq1lpscrjsjklnottesihfjk.apps.googleusercontent.com
```

### Backend (appsettings.json)

```json
{
  "Google": {
    "ClientId": "864894922252-4dgqu8d3lq1lpscrjsjklnottesihfjk.apps.googleusercontent.com",
    "ClientSecret": "your-client-secret-here"
  }
}
```

## Quick Debug Commands

### Check if API is running
```bash
curl http://localhost:5082/api/health
```

### Check if Google auth endpoint exists
```bash
curl -X POST http://localhost:5082/api/auth/google \
  -H "Content-Type: application/json" \
  -d '{"idToken":"test","acceptsTerms":true}'
```

### Check localStorage tokens
```javascript
// In browser console
console.log('Access Token:', localStorage.getItem('accessToken'))
console.log('Refresh Token:', localStorage.getItem('refreshToken'))
```

### Clear tokens and retry
```javascript
// In browser console
localStorage.removeItem('accessToken')
localStorage.removeItem('refreshToken')
location.reload()
```

## Support

If you're still experiencing issues after following this guide:

1. **Check all console logs** - They provide detailed information about each step
2. **Check backend logs** - Look for errors when validating idToken
3. **Verify Google Cloud Console configuration** - Ensure OAuth client is set up correctly
4. **Test with a different Google account** - Some accounts may have restrictions
5. **Check network tab** - Look for failed API requests and their responses

## Summary

The enhanced logging now provides complete visibility into the authentication flow. Every step is logged with clear emoji indicators:

- 🚀 = Action started
- 📥 = Data received
- 🔄 = Processing
- 🔐 = Security/Auth operation
- ✅ = Success
- ❌ = Error
- ➡️ = Navigation/Redirect
- ⚠️ = Warning

Follow the console logs to identify exactly where the flow is failing, then refer to the relevant section in this guide for solutions.
