import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { authService } from '../services/authService';
import { CheckCircle, XCircle } from 'lucide-react';

export default function GoogleCallback() {
  const navigate = useNavigate();
  const [status, setStatus] = useState<'processing' | 'success' | 'error'>('processing');
  const [message, setMessage] = useState<string>('Processing authentication...');
  const [countdown, setCountdown] = useState<number>(3);

  useEffect(() => {
    const handleCallback = async () => {
      try {
        // Check for query parameters (server-side flow from API)
        const queryParams = new URLSearchParams(window.location.search);
        const accessTokenFromAPI = queryParams.get('accessToken');
        const refreshTokenFromAPI = queryParams.get('refreshToken');
        const queryError = queryParams.get('error');

        // Check for hash parameters (client-side flow from Google directly)
        const hash = window.location.hash.substring(1);
        const hashParams = new URLSearchParams(hash);
        const idToken = hashParams.get('id_token');
        const googleAccessToken = hashParams.get('access_token');
        const tokenType = hashParams.get('token_type');
        const expiresIn = hashParams.get('expires_in');
        const scope = hashParams.get('scope');
        const hashError = hashParams.get('error');

        console.log('📥 Google OAuth Callback received');
        console.log('Hash params:', { 
          hasIdToken: !!idToken, 
          hasAccessToken: !!googleAccessToken,
          tokenType,
          expiresIn,
          scope: scope?.split('%20').join(' ')
        });

        // Handle errors from either flow
        if (queryError || hashError) {
          const errorMsg = queryError || hashError || 'Authentication failed';
          console.error('❌ Google OAuth error:', errorMsg);
          
          setStatus('error');
          setMessage(errorMsg);
          
          // If opened as popup, send error to parent
          if (window.opener) {
            window.opener.postMessage(
              { type: 'google-auth-error', error: errorMsg },
              window.location.origin
            );
            setTimeout(() => window.close(), 5000);
            return;
          }
          
          // Otherwise, redirect to login
          setTimeout(() => navigate('/login?error=' + encodeURIComponent(errorMsg)), 3000);
          return;
        }

        // SERVER-SIDE FLOW: Handle accessToken from API backend
        if (accessTokenFromAPI) {
          console.log('✅ Received accessToken from API backend (server-side flow)');
          setMessage('Validating credentials...');
          
          // Store tokens directly (already authenticated by backend)
          await authService.handleGoogleCallbackFromAPI(accessTokenFromAPI, refreshTokenFromAPI);
          
          setStatus('success');
          setMessage('Successfully authenticated!');
          
          // Get user info and redirect
          const user = await authService.getCurrentUser();
          if (user) {
            setTimeout(() => {
              if (!user.onboardingCompleted) {
                navigate('/onboarding', { replace: true });
              } else {
                navigate('/dashboard', { replace: true });
              }
            }, 2000);
          } else {
            throw new Error('Failed to get user information');
          }
          return;
        }

        // CLIENT-SIDE FLOW: Handle idToken from Google (popup flow)
        if (idToken) {
          console.log('✅ Received ID token from Google (client-side flow)');
          setMessage('Verifying with server...');
          
          // If opened as popup, send token to parent window
          if (window.opener) {
            setStatus('success');
            setMessage('Successfully authenticated!');
            
            window.opener.postMessage(
              { 
                type: 'google-auth-success', 
                idToken,
                accessToken: googleAccessToken,
                tokenType,
                expiresIn: expiresIn ? parseInt(expiresIn) : null,
                scope
              },
              window.location.origin
            );
            
            console.log('✅ Sent credentials to parent window');
            
            // Close popup after showing success message
            setTimeout(() => {
              window.close();
            }, 1500);
          } else {
            // If not a popup, this shouldn't happen in client-side flow
            console.error('❌ Client-side flow requires popup window');
            setStatus('error');
            setMessage('Invalid authentication flow - popup window required');
            setTimeout(() => navigate('/login'), 3000);
          }
          return;
        }

        // No valid tokens received
        console.error('❌ No tokens received from Google');
        setStatus('error');
        setMessage('No authentication token received');
        
        if (window.opener) {
          window.opener.postMessage(
            { type: 'google-auth-error', error: 'No token received' },
            window.location.origin
          );
          setTimeout(() => window.close(), 2000);
        } else {
          setTimeout(() => navigate('/login'), 3000);
        }
      } catch (err: any) {
        console.error('❌ Google callback error:', err);
        setStatus('error');
        setMessage(err.message || 'Authentication failed');
        
        if (window.opener) {
          window.opener.postMessage(
            { type: 'google-auth-error', error: err.message },
            window.location.origin
          );
          setTimeout(() => window.close(), 2000);
        } else {
          setTimeout(() => navigate('/login?error=' + encodeURIComponent(err.message)), 3000);
        }
      }
    };

    handleCallback();
  }, [navigate]);

  // Countdown timer for success/error states
  useEffect(() => {
    if (status === 'success' || status === 'error') {
      const timer = setInterval(() => {
        setCountdown((prev) => {
          if (prev <= 1) {
            clearInterval(timer);
            return 0;
          }
          return prev - 1;
        });
      }, 1000);

      return () => clearInterval(timer);
    }
  }, [status]);

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 via-white to-purple-50 flex items-center justify-center p-4">
      <div className="max-w-md w-full bg-white rounded-3xl shadow-xl p-8 text-center">
        {status === 'processing' && (
          <>
            <div className="inline-block animate-spin rounded-full h-16 w-16 border-4 border-blue-200 border-t-blue-600 mb-6"></div>
            <h2 className="text-2xl font-semibold text-gray-900 mb-2">
              Processing Authentication
            </h2>
            <p className="text-gray-600">{message}</p>
            <div className="mt-6 flex items-center justify-center gap-2">
              <div className="w-2 h-2 bg-blue-600 rounded-full animate-bounce"></div>
              <div className="w-2 h-2 bg-blue-600 rounded-full animate-bounce" style={{ animationDelay: '0.1s' }}></div>
              <div className="w-2 h-2 bg-blue-600 rounded-full animate-bounce" style={{ animationDelay: '0.2s' }}></div>
            </div>
          </>
        )}

        {status === 'success' && (
          <>
            <div className="mb-6 inline-flex items-center justify-center w-20 h-20 bg-green-100 rounded-full">
              <CheckCircle className="w-12 h-12 text-green-600" />
            </div>
            <h2 className="text-2xl font-semibold text-gray-900 mb-2">
              Successfully Authenticated!
            </h2>
            <p className="text-gray-600 mb-6">{message}</p>
            <div className="flex items-center justify-center gap-2 text-sm text-gray-500">
              <div className="inline-flex items-center justify-center w-8 h-8 bg-gray-100 rounded-full">
                <span className="text-lg font-semibold text-gray-700">{countdown}</span>
              </div>
              <span>{window.opener ? 'Closing window...' : 'Redirecting...'}</span>
            </div>
          </>
        )}

        {status === 'error' && (
          <>
            <div className="mb-6 inline-flex items-center justify-center w-20 h-20 bg-red-100 rounded-full">
              <XCircle className="w-12 h-12 text-red-600" />
            </div>
            <h2 className="text-2xl font-semibold text-gray-900 mb-2">
              Authentication Failed
            </h2>
            <p className="text-gray-600 mb-6">{message}</p>
            <div className="flex items-center justify-center gap-2 text-sm text-gray-500">
              <div className="inline-flex items-center justify-center w-8 h-8 bg-gray-100 rounded-full">
                <span className="text-lg font-semibold text-gray-700">{countdown}</span>
              </div>
              <span>{window.opener ? 'Closing window...' : 'Redirecting to login...'}</span>
            </div>
          </>
        )}
      </div>
    </div>
  );
}
