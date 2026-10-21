import { useEffect } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { authService } from '../services/authService';

export default function AppleCallback() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();

  useEffect(() => {
    const handleAppleCallback = async () => {
      const code = searchParams.get('code');
      const error = searchParams.get('error');
      const state = searchParams.get('state');

      console.log('🍎 Apple OAuth callback received', { code: !!code, error, state });

      if (error) {
        console.error('❌ Apple OAuth error:', error);
        navigate('/login?error=apple_auth_failed');
        return;
      }

      if (!code) {
        console.error('❌ No authorization code received from Apple');
        navigate('/login?error=missing_code');
        return;
      }

      try {
        console.log('🔄 Exchanging Apple code for tokens...');
        
        // Call backend to exchange code for tokens
        const response = await authService.handleAppleCallback(code);
        
        console.log('✅ Apple authentication successful');
        
        // Check onboarding status and redirect accordingly
        if (!response.user.onboardingCompleted) {
          navigate('/onboarding');
        } else {
          navigate('/dashboard');
        }
      } catch (error: any) {
        console.error('❌ Apple authentication failed:', error);
        navigate('/login?error=apple_auth_failed');
      }
    };

    handleAppleCallback();
  }, [searchParams, navigate]);

  return (
    <div className="min-h-screen bg-white flex items-center justify-center">
      <div className="text-center">
        <div className="inline-block animate-spin rounded-full h-12 w-12 border-b-2 border-gray-900 mb-4"></div>
        <p className="text-gray-600 text-lg mb-2">Completing Apple Sign-In...</p>
        <p className="text-gray-400 text-sm">Please wait while we authenticate you</p>
      </div>
    </div>
  );
}
