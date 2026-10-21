import { useState, useEffect, useCallback, useRef } from 'react'
import { useNavigate } from 'react-router-dom'
import { Mail, Lock, ArrowRight, Atom, TrendingUp, Shield, Zap, AlertCircle } from 'lucide-react'
import { authService } from '../services/authService'
import { useAuth } from '../contexts/AuthContext'
import { useUserProfile } from '../contexts/UserProfileContext'
import { toast } from '../utils/toast'
import { OnboardingPromptsModal } from '../components/OnboardingPromptsModal'
import { GradientWave } from '../components/ui/gradient-wave'

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
  const [showOnboardingPrompts, setShowOnboardingPrompts] = useState(false)
  const [userProjectId, setUserProjectId] = useState<number | null>(null)
  
  // Check for session expiry message and scan URL
  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    const reason = params.get('reason');
    const scanUrl = params.get('scan');

    if (reason === 'session_expired') {
      toast.error('Your session has expired. Please log in again.');
    }

    // Store scan URL for post-login redirect
    if (scanUrl) {
      localStorage.setItem('pendingScanUrl', scanUrl);
    }

    // Clear the query parameters but keep the page clean
    if (reason || scanUrl) {
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

          // Check if this is first login
          if (response.isFirstLogin) {
            // Check if there's a pending scan URL
            const scanUrl = localStorage.getItem('pendingScanUrl');

            if (scanUrl) {
              // User came from landing page with a scan URL - go to projects page
              localStorage.removeItem('pendingScanUrl');
              navigate(`/projects?scanUrl=${encodeURIComponent(scanUrl)}`, { replace: true });
            } else {
              // Get user's first project (or create one if needed)
              try {
                const projectsResponse = await fetch('/api/projects', {
                  headers: { Authorization: `Bearer ${response.accessToken}` }
                });
                const projectsData = await projectsResponse.json();

                if (projectsData.length > 0) {
                  setUserProjectId(projectsData[0].id);
                  setShowOnboardingPrompts(true);
                } else {
                  // No projects yet, skip onboarding prompts
                  navigate('/dashboard', { replace: true });
                }
              } catch (err) {
                console.error('Error fetching projects:', err);
                navigate('/dashboard', { replace: true });
              }
            }
          } else {
            // Not first login - normal flow
            // Check if there's a pending scan URL
            const scanUrl = localStorage.getItem('pendingScanUrl');

            if (scanUrl) {
              // User came from landing page with a scan URL
              localStorage.removeItem('pendingScanUrl');
              navigate(`/projects?scanUrl=${encodeURIComponent(scanUrl)}`, { replace: true });
            } else if (response.requiresSubscription) {
              // No subscription - go to pricing
              toast.info('Please select a subscription plan to continue');
              navigate('/pricing', { replace: true });
            } else {
              // Has subscription - go to dashboard
              navigate('/dashboard', { replace: true });
            }
          }
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
          navigate('/onboarding');
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

        // Check if user needs subscription first
        if (response.requiresSubscription) {
          console.log('➡️ Redirecting to pricing (no subscription)');
          toast.info('Please select a subscription plan to continue');
          navigate('/pricing', { replace: true });
        } else if (!response.user.onboardingCompleted) {
          console.log('➡️ Redirecting to onboarding (not completed)');
          navigate('/onboarding', { replace: true });
        } else {
          console.log('➡️ Redirecting to dashboard (onboarding completed)');
          navigate('/dashboard', { replace: true });
        }
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

    // Google OAuth URL
    const clientId = import.meta.env.VITE_GOOGLE_CLIENT_ID || '864894922252-4dgqu8d3lq1lpscrjsjklnottesihfjk.apps.googleusercontent.com';
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
    <div className="min-h-screen bg-white flex">
      {/* Left Side - Form */}
      <div className="flex-1 flex items-center justify-center p-8 lg:p-16">
        <div className="w-full max-w-md">
          {/* Logo */}
          <div className="mb-12">
            <button
              onClick={() => navigate('/')}
              className="flex items-center gap-2 mb-8 group"
            >
              <div className="w-10 h-10 bg-gradient-to-br from-blue-600 to-purple-600 rounded-2xl flex items-center justify-center group-hover:scale-110 transition-transform">
                <Atom className="w-5 h-5 text-white" />
              </div>
              <span className="text-xl font-semibold text-gray-900">
                ProQuest AI
              </span>
            </button>
            <h1 className="text-4xl font-light tracking-tight text-gray-900 mb-2">
              {isLogin ? 'Welcome back' : 'Get started'}
            </h1>
            <p className="text-gray-600 font-light">
              {isLogin ? 'Sign in to continue your journey' : 'Create your account to begin'}
            </p>
          </div>

          {/* Error Message */}
          {error && (
            <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-2xl flex items-start gap-3">
              <AlertCircle className="w-5 h-5 text-red-600 flex-shrink-0 mt-0.5" />
              <p className="text-sm text-red-800 font-light">{error}</p>
            </div>
          )}

          {/* Form */}
          <form onSubmit={handleSubmit} className="space-y-5">
            {!isLogin && (
              <div className="animate-fadeIn">
                <label className="block text-sm font-medium text-gray-900 mb-2">
                  Full Name
                </label>
                <input
                  type="text"
                  placeholder="John Doe"
                  value={formData.name}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                  className="w-full px-4 py-3 border-2 border-gray-200 rounded-2xl focus:outline-none focus:border-black transition-all font-light"
                  required
                />
              </div>
            )}

            <div>
              <label className="block text-sm font-medium text-gray-900 mb-2">
                Email Address
              </label>
              <div className="relative">
                <Mail className="absolute left-4 top-1/2 transform -translate-y-1/2 w-5 h-5 text-gray-400" />
                <input
                  type="email"
                  placeholder="john@example.com"
                  className="w-full pl-12 pr-4 py-3 border-2 border-gray-200 rounded-2xl focus:outline-none focus:border-black transition-all font-light"
                  value={formData.email}
                  onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                  required
                />
              </div>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-900 mb-2">
                Password
              </label>
              <div className="relative">
                <Lock className="absolute left-4 top-1/2 transform -translate-y-1/2 w-5 h-5 text-gray-400" />
                <input
                  type="password"
                  placeholder="••••••••"
                  className="w-full pl-12 pr-4 py-3 border-2 border-gray-200 rounded-2xl focus:outline-none focus:border-black transition-all font-light"
                  value={formData.password}
                  onChange={(e) => setFormData({ ...formData, password: e.target.value })}
                  required
                />
              </div>
            </div>

            {isLogin && (
              <div className="flex items-center justify-between text-sm">
                <label className="flex items-center gap-2 cursor-pointer">
                  <input type="checkbox" className="rounded border-gray-300" />
                  <span className="text-gray-600 font-light">Remember me</span>
                </label>
                <button
                  type="button"
                  onClick={() => navigate('/forgot-password')}
                  className="text-black hover:underline font-medium"
                >
                  Forgot password?
                </button>
              </div>
            )}

            {/* Terms Acceptance (Signup only) */}
            {!isLogin && (
              <div className="flex items-start gap-3 animate-fadeIn">
                <input
                  type="checkbox"
                  id="terms"
                  checked={acceptsTerms}
                  onChange={(e) => setAcceptsTerms(e.target.checked)}
                  className="mt-1 w-4 h-4 rounded border-gray-300 text-black focus:ring-black"
                />
                <label htmlFor="terms" className="text-sm text-gray-600 font-light">
                  By clicking you accept our{' '}
                  <button
                    type="button"
                    onClick={() => window.open('/terms', '_blank')}
                    className="text-black hover:underline font-medium"
                  >
                    Terms of Service
                  </button>
                  {' '}and{' '}
                  <button
                    type="button"
                    onClick={() => window.open('/privacy', '_blank')}
                    className="text-black hover:underline font-medium"
                  >
                    Privacy Policy
                  </button>
                </label>
              </div>
            )}

            <button
              type="submit"
              disabled={loading}
              className="w-full py-4 bg-black text-white font-medium rounded-full hover:bg-gray-800 transition-all flex items-center justify-center gap-2 text-base mt-6 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {loading ? 'Please wait...' : (isLogin ? 'Sign In' : 'Create Account')}
              {!loading && <ArrowRight className="w-5 h-5" />}
            </button>
          </form>

          {/* Divider */}
          <div className="relative my-8">
            <div className="absolute inset-0 flex items-center">
              <div className="w-full border-t border-gray-200"></div>
            </div>
            <div className="relative flex justify-center text-sm">
              <span className="px-4 bg-white text-gray-500 font-light">Or continue with</span>
            </div>
          </div>

          {/* Social Login */}
          <div className="grid grid-cols-2 gap-4">
            {/* Google Sign-In Button */}
            <button
              type="button"
              onClick={handleGoogleButtonClick}
              disabled={loading || (!isLogin && !acceptsTerms)}
              className="py-3 border-2 border-gray-200 rounded-full hover:border-gray-400 transition-all flex items-center justify-center gap-2 font-medium disabled:opacity-50 disabled:cursor-not-allowed"
            >
              <svg className="w-5 h-5" viewBox="0 0 24 24">
                <path
                  fill="currentColor"
                  d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"
                />
                <path
                  fill="currentColor"
                  d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"
                />
                <path
                  fill="currentColor"
                  d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"
                />
                <path
                  fill="currentColor"
                  d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"
                />
              </svg>
              Google
            </button>
            <button
              type="button"
              onClick={handleAppleSignIn}
              disabled={loading || (!isLogin && !acceptsTerms)}
              className="py-3 border-2 border-gray-200 rounded-full hover:border-gray-400 transition-all flex items-center justify-center gap-2 font-medium disabled:opacity-50 disabled:cursor-not-allowed"
            >
              <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 24 24">
                <path d="M17.05 20.28c-.98.95-2.05.8-3.08.35-1.09-.46-2.09-.48-3.24 0-1.44.62-2.2.44-3.06-.35C2.79 15.25 3.51 7.59 9.05 7.31c1.35.07 2.29.74 3.08.8 1.18-.24 2.31-.93 3.57-.84 1.51.12 2.65.72 3.4 1.8-3.12 1.87-2.38 5.98.48 7.13-.57 1.5-1.31 2.99-2.54 4.09l.01-.01zM12.03 7.25c-.15-2.23 1.66-4.07 3.74-4.25.29 2.58-2.34 4.5-3.74 4.25z" />
              </svg>
              Apple
            </button>
          </div>

          {/* Toggle Login/Register */}
          <div className="mt-8 text-center text-sm">
            <span className="text-gray-600 font-light">
              {isLogin ? "Don't have an account?" : 'Already have an account?'}
            </span>
            <button
              type="button"
              onClick={() => setIsLogin(!isLogin)}
              className="ml-2 text-black hover:underline font-medium"
            >
              {isLogin ? 'Sign up' : 'Sign in'}
            </button>
          </div>
        </div>
      </div>

      {/* Right Side - Visual */}
      <div className="hidden lg:flex flex-1 p-16 items-center justify-center relative overflow-hidden">
        {/* GradientWave background replacing the old dark gradient */}
        <GradientWave
          colors={["#ffffff", "#fb7185", '#e879f9', "#a3e635", "#ffffff"]}
          shadowPower={4}
          darkenTop={false}
          noiseFrequency={[0.0001, 0.0002]}
          deform={{ incline: 0.2, noiseAmp: 100, noiseFlow: 2 }}
        />

        {/* Content */}
        <div className="relative z-10 max-w-lg">
          <div className="mb-12">
            <div className="inline-flex items-center gap-2 bg-black/10 backdrop-blur-sm px-4 py-2 rounded-full mb-6">
              <div className="w-2 h-2 bg-green-500 rounded-full animate-pulse"></div>
              <span className="text-gray-800 text-sm font-light">500+ brands optimized</span>
            </div>
            <h2 className="text-5xl font-light text-gray-900 mb-6 leading-tight">
              Dominate AI search
              <br />
              <span className="font-normal">Get recommended</span>
            </h2>
            <p className="text-gray-700 text-lg font-light leading-relaxed">
              Join hundreds of brands getting recommended by ChatGPT, Perplexity, and other AI engines with our GEO platform.
            </p>
          </div>

          {/* Features */}
          <div className="space-y-6">
            {[
              { icon: <TrendingUp className="w-5 h-5" />, text: 'Track AI visibility in real-time' },
              { icon: <Shield className="w-5 h-5" />, text: 'Private and secure data' },
              { icon: <Zap className="w-5 h-5" />, text: 'Automated GEO optimization' },
            ].map((feature, index) => (
              <div
                key={index}
                className="flex items-center gap-4 text-gray-800"
                style={{
                  animation: 'fadeInUp 0.6s ease-out',
                  animationDelay: `${index * 0.1}s`,
                  animationFillMode: 'both'
                }}
              >
                <div className="w-10 h-10 bg-black/10 backdrop-blur-sm rounded-xl flex items-center justify-center">
                  {feature.icon}
                </div>
                <span className="font-light">{feature.text}</span>
              </div>
            ))}
          </div>

          {/* Stats */}
          <div className="grid grid-cols-3 gap-6 mt-12 pt-12 border-t border-black/10">
            {[
              { value: '500+', label: 'Brands' },
              { value: '4.9', label: 'Rating' },
              { value: '92%', label: 'Visibility' },
            ].map((stat, index) => (
              <div key={index}>
                <div className="text-3xl font-light text-gray-900 mb-1">{stat.value}</div>
                <div className="text-sm text-gray-600 font-light">{stat.label}</div>
              </div>
            ))}
          </div>
        </div>
      </div>

      <style>{`
        @keyframes fadeInUp {
          from {
            opacity: 0;
            transform: translateY(20px);
          }
          to {
            opacity: 1;
            transform: translateY(0);
          }
        }
        @keyframes fadeIn {
          from { opacity: 0; }
          to { opacity: 1; }
        }
        .animate-fadeIn {
          animation: fadeIn 0.3s ease-out;
        }
      `}</style>

      {/* Onboarding Prompts Modal for First-Time Users */}
      {showOnboardingPrompts && userProjectId && (
        <OnboardingPromptsModal
          isOpen={showOnboardingPrompts}
          onClose={() => {
            setShowOnboardingPrompts(false);
            navigate('/dashboard', { replace: true });
          }}
          projectId={userProjectId}
          companyName={formData.name}
        />
      )}
    </div>
  )
}
