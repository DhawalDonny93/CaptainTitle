# Google OAuth Architecture - Dual Flow Support

This document describes the dual Google OAuth authentication flow architecture implemented in the WebUI project.

## Overview

The application supports **two Google OAuth flows**:

1. **Client-Side Flow (Popup)** - Direct Google OAuth with idToken
2. **Server-Side Flow (API Redirect)** - API backend handles OAuth and redirects with accessToken

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                     Google OAuth Flows                           │
└─────────────────────────────────────────────────────────────────┘

CLIENT-SIDE FLOW (Popup):
┌─────────┐     ┌────────┐     ┌────────────┐     ┌─────────┐
│ WebUI   │────>│ Google │────>│ WebUI      │────>│ API     │
│ Login   │     │ OAuth  │     │ Callback   │     │ Backend │
│         │     │        │     │ (popup)    │     │         │
└─────────┘     └────────┘     └────────────┘     └─────────┘
   Opens           Returns          Sends            Validates
   Popup           idToken          idToken          & Returns
                   in hash          to parent        accessToken

SERVER-SIDE FLOW (Redirect):
┌─────────┐     ┌─────────┐     ┌────────┐     ┌─────────────┐
│ WebUI   │────>│ API     │────>│ Google │────>│ API         │
│ Login   │     │ Backend │     │ OAuth  │     │ Backend     │
│         │     │         │     │        │     │             │
└─────────┘     └─────────┘     └────────┘     └─────────────┘
   Redirects       Initiates       Returns         Validates &
   to API          OAuth           code            Redirects to
                                                   WebUI with tokens

                                                   ↓
                                            ┌─────────────┐
                                            │ WebUI       │
                                            │ Callback    │
                                            │ (full page) │
                                            └─────────────┘
                                              Receives
                                              accessToken
                                              in query params
```

## Implementation Details

### 1. Client-Side Flow (Current Implementation)

**Flow:**
1. User clicks "Sign in with Google" button
2. WebUI opens Google OAuth popup
3. Google redirects to `/auth/google/callback` with `id_token` in hash
4. Callback page sends `idToken` to parent window via `postMessage`
5. Parent window sends `idToken` to API backend
6. API validates and returns `accessToken` + `refreshToken`
7. WebUI stores tokens and redirects to dashboard/onboarding

**Files Involved:**
- `src/pages/Login.tsx` - Initiates popup flow
- `src/pages/GoogleCallback.tsx` - Handles hash params and postMessage
- `src/services/authService.ts` - `googleSignIn(idToken)` method

**Callback URL Format:**
```
https://[domain]/auth/google/callback#id_token=[ID_TOKEN]&access_token=[GOOGLE_ACCESS_TOKEN]
```

### 2. Server-Side Flow (New Implementation)

**Flow:**
1. User clicks "Sign in with Google via API" button (or use this as primary)
2. WebUI redirects to API backend OAuth initiation endpoint
3. API backend redirects to Google OAuth
4. User authenticates with Google
5. Google redirects back to API backend with authorization code
6. API backend validates code, gets user info, creates/updates user
7. API backend redirects to WebUI callback with `accessToken` + `refreshToken`
8. WebUI stores tokens and redirects to dashboard/onboarding

**Files Involved:**
- `src/services/authService.ts` - `initiateGoogleAuthViaAPI()` method
- `src/pages/GoogleCallback.tsx` - Handles query params from API
- `src/services/authService.ts` - `handleGoogleCallbackFromAPI()` method

**Callback URL Format:**
```
https://[domain]/auth/google/callback?accessToken=[ACCESS_TOKEN]&refreshToken=[REFRESH_TOKEN]
```

**API Endpoint to Initiate:**
```
GET /api/auth/oauth/initiate?provider=google&callbackUrl=[ENCODED_CALLBACK_URL]
```

## Code Examples

### Initiating Server-Side Flow

```typescript
// In Login.tsx or any component
import { authService } from '../services/authService';

const handleGoogleSignInViaAPI = () => {
  // This will redirect to API backend
  authService.initiateGoogleAuthViaAPI();
};
```

### Callback Handler (Automatic)

The `GoogleCallback.tsx` component automatically detects which flow is being used:

```typescript
// Detects query params (server-side flow)
const accessTokenFromAPI = queryParams.get('accessToken');

// Detects hash params (client-side flow)
const idToken = hashParams.get('id_token');
```

## API Backend Requirements

### OAuth Initiation Endpoint

```
GET /api/auth/oauth/initiate
Query Parameters:
  - provider: "google"
  - callbackUrl: URL-encoded callback URL (e.g., https://yourapp.com/auth/google/callback)

Response:
  - Redirects to Google OAuth with appropriate parameters
```

### OAuth Callback Endpoint (Backend)

```
GET /api/auth/oauth/callback/google
Query Parameters:
  - code: Authorization code from Google
  - state: CSRF token

Response:
  - Validates code with Google
  - Creates/updates user in database
  - Generates accessToken and refreshToken
  - Redirects to: [callbackUrl]?accessToken=[TOKEN]&refreshToken=[TOKEN]
```

## Configuration

### Environment Variables

```bash
# .env file
VITE_API_URL=https://api.yourapp.com/api
VITE_GOOGLE_CLIENT_ID=your-google-client-id.apps.googleusercontent.com
```

### Google Cloud Console Setup

**For Client-Side Flow:**
- Authorized JavaScript origins: `https://yourapp.com`
- Authorized redirect URIs: `https://yourapp.com/auth/google/callback`

**For Server-Side Flow:**
- Authorized JavaScript origins: `https://api.yourapp.com`
- Authorized redirect URIs: `https://api.yourapp.com/api/auth/oauth/callback/google`

## Security Considerations

### Client-Side Flow
- ✅ idToken validated by API backend
- ✅ Popup prevents phishing (user sees google.com URL)
- ⚠️ Token exposed in browser (hash fragment)

### Server-Side Flow
- ✅ Tokens never exposed to browser until final redirect
- ✅ Authorization code only used once
- ✅ Backend validates with Google directly
- ✅ More secure for production use

## Choosing Between Flows

### Use Client-Side Flow When:
- Quick prototyping
- Single-page application without backend
- Need immediate user feedback in popup

### Use Server-Side Flow When:
- Production environment
- Need maximum security
- Want centralized OAuth handling
- Multiple OAuth providers (Google, Apple, GitHub, etc.)

## Migration Path

To migrate from client-side to server-side flow:

1. Implement OAuth endpoints in API backend
2. Update Google Cloud Console redirect URIs
3. Change Login.tsx to use `initiateGoogleAuthViaAPI()`
4. Test both flows work (for backward compatibility)
5. Gradually migrate users to server-side flow
6. Eventually deprecate client-side flow if desired

## Testing

### Test Client-Side Flow
1. Click "Sign in with Google" (popup button)
2. Verify popup opens with Google OAuth
3. Verify callback receives idToken in hash
4. Verify API validates and returns tokens

### Test Server-Side Flow
1. Call `authService.initiateGoogleAuthViaAPI()`
2. Verify redirect to API backend
3. Verify redirect to Google OAuth
4. Verify callback receives accessToken in query params
5. Verify user is authenticated and redirected

## Troubleshooting

### Issue: Callback receives no tokens
- Check Google Cloud Console redirect URIs
- Verify API backend OAuth endpoints are working
- Check browser console for errors

### Issue: CORS errors
- Ensure API backend has correct CORS configuration
- Verify `VITE_API_URL` is correct

### Issue: Infinite redirect loop
- Check that callback URL doesn't trigger another OAuth flow
- Verify tokens are being stored correctly
- Check AuthRedirect.tsx logic

## Future Enhancements

- [ ] Add state parameter for CSRF protection
- [ ] Implement token refresh in server-side flow
- [ ] Add support for additional OAuth providers
- [ ] Implement OAuth scope management
- [ ] Add user consent screen customization
