import { useState, useEffect, useCallback, useRef } from 'react'
import { useNavigate } from 'react-router-dom'
import { Mail, Lock, ArrowRight, User, AlertCircle } from 'lucide-react'
import { authService } from '../services/authService'
import { useAuth } from '../contexts/AuthContext'
import { useUserProfile } from '../contexts/UserProfileContext'
import { toast } from '../utils/toast'

export default function Login() {
  const navigate = useNavigate()
  const { setUser } = useAuth()
  const { setProfile } = useUserProfile()
  const [isLogin, setIsLogin] = useState(true)
  const [formData, setFormData] = useState({
    email: '',
    password: '',
    name: '',
  })
  const [acceptsTerms, setAcceptsTerms] = useState(false)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  
  // Check for session expiry message and scan URL
  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    const reason = params.get('reason');
    if (params.get('mode') === 'signup') setIsLogin(false);

    if (reason === 'session_expired') {
      toast.error('Your session has expired. Please log in again.');
    }

    // Clear the query parameters but keep the page clean
    if (reason || params.get('mode')) {
      window.history.replaceState({}, '', '/login');
    }
  }, [])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    setLoading(true)

    try {
      if (isLogin) {
        // Login using authService directly to get full response
        const response = await authService.login({
          email: formData.email,
          password: formData.password
        })

        if(response.success) {
          // UPDATE AUTHCONTEXT WITH USER - This is CRITICAL for dashboard redirect to work!
          // The AuthContext needs to have the user set, otherwise ProtectedRoute will redirect back to login
          setUser({ ...response.user, isSubscribed: response.user.isSubscribed || false }, response.teamId);

          // Store user profile in session to minimize DB calls
          if (response.user?.profile) {
            setProfile(response.user.profile);
          }

          toast.success('Welcome back!');

          // Captain Title: every new or returning user lands in the studio (a free video is included)
          navigate('/studio', { replace: true });
        }
      } else {
        // Register
        if (!acceptsTerms) {
          setError('You must accept the Terms of Service and Privacy Policy')
          setLoading(false)
          return
        }
        // For registration, still use authService directly to handle email verification flow
        const response = await authService.register({
          email: formData.email,
          password: formData.password,
          firstName: formData.name,
          acceptsTerms,
        })

        toast.success('Account created successfully!');

        // Check if email verification is required
        if (response.requiresEmailVerification) {
          // Store session ID and redirect to email verification page
          localStorage.setItem('emailVerificationSessionId', response.sessionId || '');
          localStorage.setItem('verificationEmail', formData.email);
          toast.info('Please check your email to verify your account');
          navigate('/verify-email');
        } else {
          // Direct login without verification
          navigate('/studio');
        }
      }
    } catch (err: any) {
      const errorMsg = err.message || 'Authentication failed';
      setError(errorMsg);
      toast.error(errorMsg)
    } finally {
      setLoading(false)
    }
  }

  // Handle Google OAuth callback - use ref to avoid recreating
  const acceptsTermsRef = useRef(acceptsTerms);

  useEffect(() => {
    acceptsTermsRef.current = acceptsTerms;
  }, [acceptsTerms]);

  const handleGoogleCallback = useCallback(async (event: MessageEvent) => {
    // Verify origin
    if (event.origin !== window.location.origin) {
      console.warn('⚠️ Received message from different origin:', event.origin);
      return;
    }

    console.log('📨 Received message from callback:', event.data.type);

    // Handle success
    if (event.data.type === 'google-auth-success') {
      console.log('✅ Google auth successful, received tokens:', {
        hasIdToken: !!event.data.idToken,
        hasAccessToken: !!event.data.accessToken,
        tokenType: event.data.tokenType,
        expiresIn: event.data.expiresIn
      });
      if (!event.data.idToken) {
        console.error('❌ No idToken received from callback');
        setError('Authentication failed: No ID token received');
        window.removeEventListener('message', handleGoogleCallback);
        return;
      }

      setLoading(true);
      setError('');

      try {
        console.log('🔄 Sending idToken to backend API...');
        const response = await authService.googleSignIn(event.data.idToken, acceptsTermsRef.current);

        console.log('✅ Backend authentication successful:', {
          hasUser: !!response.user,
          onboardingCompleted: response.user?.onboardingCompleted,
          isSubscribed: response.user?.isSubscribed,
          requiresSubscription: response.requiresSubscription
        });

        // UPDATE AUTHCONTEXT - Critical for proper redirect
        setUser({ ...response.user, isSubscribed: response.user.isSubscribed || false }, response.teamId);

        navigate('/studio', { replace: true });
      } catch (err: any) {
        console.error('❌ Backend authentication failed:', err);
        console.error('Error details:', {
          message: err.message,
          response: err.response?.data,
          status: err.response?.status
        });
        setError(err.message || 'Google sign-in failed. Please try again.');
      } finally {
        setLoading(false);
      }

      // Clean up listener
      window.removeEventListener('message', handleGoogleCallback);
    }

    // Handle error
    if (event.data.type === 'google-auth-error') {
      console.error('❌ Google auth error from callback:', event.data.error);
      setError(event.data.error || 'Google authentication failed');
      setLoading(false);
      // Clean up listener
      window.removeEventListener('message', handleGoogleCallback);
    }
  }, [navigate]);

  const handleGoogleButtonClick = () => {
    if (!isLogin && !acceptsTerms) {
      setError('You must accept the Terms of Service and Privacy Policy')
      return
    }

    console.log('🚀 Opening Google OAuth popup');

    // Google OAuth URL (client id comes from VITE_GOOGLE_CLIENT_ID)
    const clientId = import.meta.env.VITE_GOOGLE_CLIENT_ID;
    if (!clientId) {
      setError('Google sign-in is not configured yet. Please use email and password.');
      return;
    }
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
    // Open popup window
    const width = 500;
    const height = 600;
    const left = window.screen.width / 2 - width / 2;
    const top = window.screen.height / 2 - height / 2;
    const popup = window.open(
      authUrl,
      'Google Sign In',
      `width=${width},height=${height},left=${left},top=${top},toolbar=no,menubar=no,scrollbars=yes,resizable=yes`
    );
    if (!popup) {
      setError('Popup blocked! Please allow popups for this site.');
      return;
    }
    // Listen for the callback
    window.addEventListener('message', handleGoogleCallback);
  }

  const handleAppleSignIn = () => {
    if (!isLogin && !acceptsTerms) {
      setError('You must accept the Terms of Service and Privacy Policy')
      return
    }
    authService.initiateAppleAuth()
  }

  // Clean up message listener on unmount
  useEffect(() => {
    return () => {
      window.removeEventListener('message', handleGoogleCallback);
    };
  }, [handleGoogleCallback]);

  return (
    <div className="min-h-screen bg-paper text-ink grid lg:grid-cols-2">
      {/* Form side */}
      <div className="flex items-center justify-center p-6 sm:p-10">
        <div className="w-full max-w-md">
          <button onClick={() => navigate('/')} className="flex items-center gap-2.5 mb-8" aria-label="Captain Title home">
            <span className="grid h-10 w-10 place-items-center rounded-lg border-2 border-ink bg-cherry text-white font-display text-lg font-extrabold shadow-hard-sm">C</span>
            <span className="font-display text-xl font-extrabold tracking-tight">Captain Title</span>
          </button>

          <h1 className="font-display text-4xl font-extrabold tracking-tight mb-2">
            {isLogin ? 'Welcome back' : 'Make your first captions'}
          </h1>
          <p className="text-gray-700 mb-8">
            {isLogin ? 'Sign in to open your studio.' : 'Your first captioned video is free. No card needed.'}
          </p>

          {error && (
            <div className="mb-6 flex items-start gap-3 rounded-lg border-2 border-ink bg-blush p-4 shadow-hard-sm" role="alert">
              <AlertCircle className="mt-0.5 h-5 w-5 flex-shrink-0" />
              <p className="text-sm font-medium">{error}</p>
            </div>
          )}

          <form onSubmit={handleSubmit} className="space-y-5">
            {!isLogin && (
              <div>
                <label className="mb-1.5 block text-sm font-semibold" htmlFor="name">Your name</label>
                <div className="relative">
                  <User className="absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-gray-500" />
                  <input id="name" type="text" placeholder="Aarav Sharma" value={formData.name}
                    onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                    className="w-full rounded-lg border-2 border-ink bg-white py-3 pl-10 pr-4 focus:outline-none focus:ring-2 focus:ring-cherry" required />
                </div>
              </div>
            )}

            <div>
              <label className="mb-1.5 block text-sm font-semibold" htmlFor="email">Email</label>
              <div className="relative">
                <Mail className="absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-gray-500" />
                <input id="email" type="email" placeholder="you@example.com" value={formData.email}
                  onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                  className="w-full rounded-lg border-2 border-ink bg-white py-3 pl-10 pr-4 focus:outline-none focus:ring-2 focus:ring-cherry" required />
              </div>
            </div>

            <div>
              <div className="mb-1.5 flex items-center justify-between">
                <label className="block text-sm font-semibold" htmlFor="password">Password</label>
                {isLogin && (
                  <button type="button" onClick={() => navigate('/forgot-password')} className="text-sm font-semibold text-cherry underline underline-offset-4 hover:text-ink">
                    Forgot password?
                  </button>
                )}
              </div>
              <div className="relative">
                <Lock className="absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-gray-500" />
                <input id="password" type="password" placeholder="At least 6 characters" value={formData.password} minLength={6}
                  onChange={(e) => setFormData({ ...formData, password: e.target.value })}
                  className="w-full rounded-lg border-2 border-ink bg-white py-3 pl-10 pr-4 focus:outline-none focus:ring-2 focus:ring-cherry" required />
              </div>
            </div>

            {!isLogin && (
              <label className="flex cursor-pointer items-start gap-3 text-sm">
                <input type="checkbox" checked={acceptsTerms} onChange={(e) => setAcceptsTerms(e.target.checked)}
                  className="mt-0.5 h-5 w-5 rounded border-2 border-ink accent-cherry" />
                <span>
                  I agree to the{' '}
                  <a href="/terms" target="_blank" rel="noreferrer" className="font-semibold text-cherry underline underline-offset-4">Terms</a> and{' '}
                  <a href="/privacy" target="_blank" rel="noreferrer" className="font-semibold text-cherry underline underline-offset-4">Privacy Policy</a>.
                </span>
              </label>
            )}

            <button type="submit" disabled={loading}
              className="flex w-full items-center justify-center gap-2 rounded-lg border-2 border-ink bg-cherry py-3.5 font-bold text-white shadow-hard transition-all hover:-translate-y-0.5 hover:shadow-hard-lg active:translate-y-0 active:shadow-none disabled:opacity-60">
              {loading ? 'One moment...' : isLogin ? 'Sign in' : 'Create my account'}
              {!loading && <ArrowRight className="h-5 w-5" />}
            </button>
          </form>

          <div className="my-6 flex items-center gap-4 text-xs font-bold uppercase tracking-widest text-gray-600">
            <span className="h-0.5 flex-1 bg-ink" /> or <span className="h-0.5 flex-1 bg-ink" />
          </div>

          <div className="grid gap-3 sm:grid-cols-2">
            <button type="button" onClick={handleGoogleButtonClick} disabled={loading}
              className="rounded-lg border-2 border-ink bg-white py-3 font-semibold shadow-hard-sm transition-all hover:-translate-y-0.5 hover:shadow-hard disabled:opacity-60">
              Continue with Google
            </button>
            <button type="button" onClick={handleAppleSignIn} disabled={loading}
              className="rounded-lg border-2 border-ink bg-ink py-3 font-semibold text-white shadow-hard-sm transition-all hover:-translate-y-0.5 hover:shadow-hard disabled:opacity-60">
              Continue with Apple
            </button>
          </div>

          <p className="mt-8 text-center text-sm">
            {isLogin ? 'New to Captain Title?' : 'Already have an account?'}{' '}
            <button type="button" onClick={() => { setIsLogin(!isLogin); setError('') }} className="font-bold text-cherry underline underline-offset-4 hover:text-ink">
              {isLogin ? 'Create an account' : 'Sign in'}
            </button>
          </p>
        </div>
      </div>

      {/* Brand side: flat colour blocks, no gradients */}
      <aside className="relative hidden overflow-hidden border-l-2 border-ink bg-sun p-12 lg:flex lg:flex-col lg:justify-between">
        <p className="font-display text-sm font-extrabold uppercase tracking-[0.25em]">Words that hit on beat</p>
        <div>
          <h2 className="font-display text-6xl font-extrabold leading-[1.02] tracking-tight">
            Har <span className="inline-block -rotate-2 rounded-lg border-2 border-ink bg-cherry px-3 text-white shadow-hard">WORD</span><br />
            <span className="text-5xl">matters.</span>
          </h2>
          <p className="mt-6 max-w-md text-lg font-medium">
            Upload a clip. Get word-perfect Hinglish captions, mixed fonts and bold colours, ready to post.
          </p>
        </div>
        <ul className="grid gap-3 text-sm font-semibold">
          {['Transcribes Hindi and Hinglish, word by word', 'Edit text, timing, fonts and colours live', 'Export a finished MP4 or an SRT file'].map((t, i) => (
            <li key={t} className="flex items-center gap-3 rounded-lg border-2 border-ink bg-white px-4 py-3 shadow-hard-sm">
              <span className={`grid h-7 w-7 place-items-center rounded-md border-2 border-ink font-display font-extrabold ${['bg-mint', 'bg-tangerine', 'bg-blush'][i]}`}>{i + 1}</span>
              {t}
            </li>
          ))}
        </ul>
      </aside>
    </div>
  )
}
