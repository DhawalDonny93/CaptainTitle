# Google OAuth Implementation Examples

This document provides code examples for implementing both Google OAuth flows in your application.

## Quick Start

### Option 1: Use Server-Side Flow (Recommended for Production)

```typescript
// In your Login component
import { authService } from '../services/authService';

const handleGoogleSignIn = () => {
  // Redirects to API backend, which handles OAuth and redirects back
  authService.initiateGoogleAuthViaAPI();
};

// That's it! The callback is handled automatically by GoogleCallback.tsx
```

### Option 2: Use Client-Side Flow (Popup)

```typescript
// In your Login component
import { authService } from '../services/authService';

const handleGoogleSignIn = () => {
  // Opens popup for Google OAuth
  const clientId = import.meta.env.VITE_GOOGLE_CLIENT_ID;
  const redirectUri = `${window.location.origin}/auth/google/callback`;
  const scope = 'openid email profile';
  const responseType = 'token id_token';
  const nonce = Math.random().toString(36).substring(2);
  
  const authUrl = `https://accounts.google.com/o/oauth2/v2/auth?` +
    `client_id=${encodeURIComponent(clientId)}&` +
    `redirect_uri=${encodeURIComponent(redirectUri)}&` +
    `response_type=${encodeURIComponent(responseType)}&` +
    `scope=${encodeURIComponent(scope)}&` +
    `nonce=${nonce}&` +
    `prompt=select_account`;
  
  const popup = window.open(authUrl, 'Google Sign In', 'width=500,height=600');
  
  // Listen for callback
  window.addEventListener('message', async (event) => {
    if (event.origin !== window.location.origin) return;
    
    if (event.data.type === 'google-auth-success') {
      try {
        const response = await authService.googleSignIn(event.data.idToken, true);
        // Handle successful authentication
        if (!response.user.onboardingCompleted) {
          navigate('/onboarding');
        } else {
          navigate('/dashboard');
        }
      } catch (err) {
        console.error('Google sign-in failed:', err);
      }
    }
  });
};
```

## Complete Login Component Example

Here's a complete example showing both flows with a toggle:

```typescript
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { authService } from '../services/authService';

export default function Login() {
  const navigate = useNavigate();
  const [useServerSideFlow, setUseServerSideFlow] = useState(true);
  
  const handleGoogleSignIn = () => {
    if (useServerSideFlow) {
      // Server-side flow: Redirect to API backend
      authService.initiateGoogleAuthViaAPI();
    } else {
      // Client-side flow: Open popup
      handleGooglePopupFlow();
    }
  };
  
  const handleGooglePopupFlow = () => {
    const clientId = import.meta.env.VITE_GOOGLE_CLIENT_ID;
    const redirectUri = `${window.location.origin}/auth/google/callback`;
    const scope = 'openid email profile';
    const responseType = 'token id_token';
    const nonce = Math.random().toString(36).substring(2);
    
    const authUrl = `https://accounts.google.com/o/oauth2/v2/auth?` +
      `client_id=${encodeURIComponent(clientId)}&` +
      `redirect_uri=${encodeURIComponent(redirectUri)}&` +
      `response_type=${encodeURIComponent(responseType)}&` +
      `scope=${encodeURIComponent(scope)}&` +
      `nonce=${nonce}&` +
      `prompt=select_account`;
    
    const width = 500;
    const height = 600;
    const left = window.screen.width / 2 - width / 2;
    const top = window.screen.height / 2 - height / 2;
    
    const popup = window.open(
      authUrl,
      'Google Sign In',
      `width=${width},height=${height},left=${left},top=${top}`
    );
    
    if (!popup) {
      alert('Popup blocked! Please allow popups for this site.');
      return;
    }
    
    // Listen for callback
    const handleMessage = async (event: MessageEvent) => {
      if (event.origin !== window.location.origin) return;
      
      if (event.data.type === 'google-auth-success') {
        try {
          const response = await authService.googleSignIn(event.data.idToken, true);
          
          if (!response.user.onboardingCompleted) {
            navigate('/onboarding');
          } else {
            navigate('/dashboard');
          }
        } catch (err: any) {
          console.error('Google sign-in failed:', err);
          alert(err.message || 'Google sign-in failed');
        }
        
        window.removeEventListener('message', handleMessage);
      } else if (event.data.type === 'google-auth-error') {
        alert('Google sign-in failed: ' + event.data.error);
        window.removeEventListener('message', handleMessage);
      }
    };
    
    window.addEventListener('message', handleMessage);
  };
  
  return (
    <div>
      {/* Google Sign-In Button */}
      <button
        onClick={handleGoogleSignIn}
        className="w-full py-3 border-2 border-gray-200 rounded-full hover:border-gray-400 transition-all flex items-center justify-center gap-2"
      >
        <svg className="w-5 h-5" viewBox="0 0 24 24">
          <path fill="currentColor" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"/>
          <path fill="currentColor" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"/>
          <path fill="currentColor" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"/>
          <path fill="currentColor" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"/>
        </svg>
        Sign in with Google
      </button>
      
      {/* Developer Toggle (remove in production) */}
      <div className="mt-4 text-center text-sm">
        <label className="flex items-center justify-center gap-2 text-gray-600">
          <input
            type="checkbox"
            checked={useServerSideFlow}
            onChange={(e) => setUseServerSideFlow(e.target.checked)}
          />
          Use server-side flow (recommended)
        </label>
      </div>
    </div>
  );
}
```

## API Backend Implementation Guide

### Required Endpoints

#### 1. OAuth Initiation Endpoint

```csharp
// In your API backend (ASP.NET Core example)
[HttpGet("auth/oauth/initiate")]
public IActionResult InitiateOAuth([FromQuery] string provider, [FromQuery] string callbackUrl)
{
    if (provider != "google")
        return BadRequest("Unsupported provider");
    
    // Validate callback URL (security)
    if (!IsValidCallbackUrl(callbackUrl))
        return BadRequest("Invalid callback URL");
    
    var googleClientId = _configuration["Google:ClientId"];
    var backendCallbackUrl = $"{Request.Scheme}://{Request.Host}/api/auth/oauth/callback/google";
    
    // Generate state for CSRF protection
    var state = GenerateSecureRandomString();
    
    // Store state and original callback URL in cache/session
    _cache.Set($"oauth_state_{state}", new OAuthState 
    { 
        CallbackUrl = callbackUrl,
        Provider = provider 
    }, TimeSpan.FromMinutes(10));
    
    var googleAuthUrl = $"https://accounts.google.com/o/oauth2/v2/auth?" +
        $"client_id={Uri.EscapeDataString(googleClientId)}&" +
        $"redirect_uri={Uri.EscapeDataString(backendCallbackUrl)}&" +
        $"response_type=code&" +
        $"scope={Uri.EscapeDataString("openid email profile")}&" +
        $"state={state}&" +
        $"prompt=select_account";
    
    return Redirect(googleAuthUrl);
}
```

#### 2. OAuth Callback Endpoint

```csharp
[HttpGet("auth/oauth/callback/google")]
public async Task<IActionResult> GoogleCallback([FromQuery] string code, [FromQuery] string state)
{
    try
    {
        // Validate state (CSRF protection)
        var oauthState = _cache.Get<OAuthState>($"oauth_state_{state}");
        if (oauthState == null)
            return BadRequest("Invalid or expired state");
        
        // Exchange code for tokens with Google
        var googleTokens = await ExchangeCodeForTokens(code);
        
        // Get user info from Google
        var googleUser = await GetGoogleUserInfo(googleTokens.AccessToken);
        
        // Create or update user in your database
        var user = await _userService.CreateOrUpdateGoogleUser(googleUser);
        
        // Generate your application's access and refresh tokens
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken(user);
        
        // Redirect back to WebUI with tokens
        var redirectUrl = $"{oauthState.CallbackUrl}?" +
            $"accessToken={Uri.EscapeDataString(accessToken)}&" +
            $"refreshToken={Uri.EscapeDataString(refreshToken)}";
        
        return Redirect(redirectUrl);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Google OAuth callback failed");
        
        // Redirect with error
        var errorUrl = $"{oauthState?.CallbackUrl ?? "https://yourapp.com/auth/google/callback"}?" +
            $"error={Uri.EscapeDataString(ex.Message)}";
        
        return Redirect(errorUrl);
    }
}

private async Task<GoogleTokenResponse> ExchangeCodeForTokens(string code)
{
    var clientId = _configuration["Google:ClientId"];
    var clientSecret = _configuration["Google:ClientSecret"];
    var redirectUri = $"{Request.Scheme}://{Request.Host}/api/auth/oauth/callback/google";
    
    var tokenRequest = new Dictionary<string, string>
    {
        { "code", code },
        { "client_id", clientId },
        { "client_secret", clientSecret },
        { "redirect_uri", redirectUri },
        { "grant_type", "authorization_code" }
    };
    
    var response = await _httpClient.PostAsync(
        "https://oauth2.googleapis.com/token",
        new FormUrlEncodedContent(tokenRequest)
    );
    
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadFromJsonAsync<GoogleTokenResponse>();
}

private async Task<GoogleUserInfo> GetGoogleUserInfo(string accessToken)
{
    var request = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v2/userinfo");
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    
    var response = await _httpClient.SendAsync(request);
    response.EnsureSuccessStatusCode();
    
    return await response.Content.ReadFromJsonAsync<GoogleUserInfo>();
}
```

### Data Models

```csharp
public class OAuthState
{
    public string CallbackUrl { get; set; }
    public string Provider { get; set; }
}

public class GoogleTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }
    
    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; }
    
    [JsonPropertyName("id_token")]
    public string IdToken { get; set; }
}

public class GoogleUserInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    
    [JsonPropertyName("email")]
    public string Email { get; set; }
    
    [JsonPropertyName("verified_email")]
    public bool VerifiedEmail { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("given_name")]
    public string GivenName { get; set; }
    
    [JsonPropertyName("family_name")]
    public string FamilyName { get; set; }
    
    [JsonPropertyName("picture")]
    public string Picture { get; set; }
}
```

## Testing Checklist

### Client-Side Flow Testing
- [ ] Popup opens correctly
- [ ] Google OAuth page loads
- [ ] User can authenticate
- [ ] Callback receives idToken in hash
- [ ] Parent window receives postMessage
- [ ] API validates idToken
- [ ] User is redirected to dashboard/onboarding
- [ ] Tokens are stored correctly

### Server-Side Flow Testing
- [ ] Redirect to API backend works
- [ ] API redirects to Google OAuth
- [ ] Google OAuth page loads
- [ ] User can authenticate
- [ ] Google redirects back to API
- [ ] API exchanges code for tokens
- [ ] API creates/updates user
- [ ] API redirects to WebUI callback with tokens
- [ ] WebUI stores tokens
- [ ] User is redirected to dashboard/onboarding

## Common Issues and Solutions

### Issue: "Popup blocked"
**Solution:** Ensure the popup is opened directly from a user click event, not from an async callback.

### Issue: "Invalid redirect URI"
**Solution:** Check Google Cloud Console and ensure all redirect URIs are registered correctly.

### Issue: "CORS error"
**Solution:** 
- For client-side flow: Add your domain to Google Cloud Console authorized origins
- For server-side flow: Ensure API backend has correct CORS configuration

### Issue: "State mismatch"
**Solution:** Ensure state parameter is being stored and validated correctly in backend.

### Issue: "Tokens not stored"
**Solution:** Check browser console for errors, verify authService methods are being called.

## Best Practices

1. **Use Server-Side Flow in Production** - More secure, tokens never exposed to browser
2. **Implement CSRF Protection** - Use state parameter to prevent CSRF attacks
3. **Validate Callback URLs** - Whitelist allowed callback URLs in backend
4. **Use HTTPS** - Always use HTTPS in production for OAuth flows
5. **Handle Errors Gracefully** - Show user-friendly error messages
6. **Log OAuth Events** - Log OAuth attempts for security monitoring
7. **Implement Rate Limiting** - Prevent OAuth abuse
8. **Store Tokens Securely** - Use httpOnly cookies or secure storage
9. **Implement Token Refresh** - Handle token expiration gracefully
10. **Test Both Flows** - Ensure both flows work before deploying

## Additional Resources

- [Google OAuth 2.0 Documentation](https://developers.google.com/identity/protocols/oauth2)
- [Google Cloud Console](https://console.cloud.google.com/)
- [OAuth 2.0 Security Best Practices](https://datatracker.ietf.org/doc/html/draft-ietf-oauth-security-topics)
