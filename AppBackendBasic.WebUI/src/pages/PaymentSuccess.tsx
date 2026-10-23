import { useEffect, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { CheckCircle, ArrowRight } from 'lucide-react';
import Header from '../components/Header';
import Footer from '../components/Footer';

export default function PaymentSuccess() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const sessionId = searchParams.get('session_id');
  const [countdown, setCountdown] = useState(5);

  useEffect(() => {
    if (!sessionId) {
      navigate('/pricing');
      return;
    }

    const timer = setInterval(() => {
      setCountdown((prev) => {
        if (prev <= 1) {
          clearInterval(timer);
          navigate('/dashboard');
          return 0;
        }
        return prev - 1;
      });
    }, 1000);

    return () => clearInterval(timer);
  }, [sessionId, navigate]);

  return (
    <div className="min-h-screen bg-white flex flex-col">
      <Header />

      <div className="flex-1 flex items-center justify-center px-6 py-24">
        <div className="max-w-2xl w-full text-center">
          {/* Success Icon */}
          <div className="mb-8 flex justify-center">
            <div className="relative">
              <div className="absolute inset-0 bg-green-500/20 rounded-full blur-2xl"></div>
              <CheckCircle className="w-24 h-24 text-green-500 relative" />
            </div>
          </div>

          {/* Success Message */}
          <h1 className="text-5xl md:text-6xl font-light tracking-tight text-gray-900 mb-6">
            Payment Successful!
          </h1>

          <p className="text-xl text-gray-600 font-light mb-12 leading-relaxed">
            Thank you for subscribing to Captain Title. Your account has been upgraded and you now have access to all premium features.
          </p>

          {/* What's Next */}
          <div className="bg-gray-50 rounded-3xl p-8 mb-12">
            <h2 className="text-2xl font-normal text-gray-900 mb-6">What's next?</h2>
            <div className="space-y-4 text-left">
              <div className="flex items-start gap-4">
                <div className="w-8 h-8 bg-black rounded-full flex items-center justify-center flex-shrink-0 mt-1">
                  <span className="text-white text-sm font-medium">1</span>
                </div>
                <div>
                  <h3 className="font-medium text-gray-900 mb-1">Set up your first project</h3>
                  <p className="text-gray-600 font-light text-sm">
                    Create a project and add your website domain to start tracking AI visibility
                  </p>
                </div>
              </div>

              <div className="flex items-start gap-4">
                <div className="w-8 h-8 bg-black rounded-full flex items-center justify-center flex-shrink-0 mt-1">
                  <span className="text-white text-sm font-medium">2</span>
                </div>
                <div>
                  <h3 className="font-medium text-gray-900 mb-1">Configure LLM providers</h3>
                  <p className="text-gray-600 font-light text-sm">
                    Add your API keys for ChatGPT, Claude, and Gemini to enable tracking
                  </p>
                </div>
              </div>

              <div className="flex items-start gap-4">
                <div className="w-8 h-8 bg-black rounded-full flex items-center justify-center flex-shrink-0 mt-1">
                  <span className="text-white text-sm font-medium">3</span>
                </div>
                <div>
                  <h3 className="font-medium text-gray-900 mb-1">Start tracking prompts</h3>
                  <p className="text-gray-600 font-light text-sm">
                    Add prompts to monitor and see how AI engines mention your brand
                  </p>
                </div>
              </div>
            </div>
          </div>

          {/* CTA Buttons */}
          <div className="flex flex-col sm:flex-row gap-4 justify-center mb-8">
            <button
              onClick={() => navigate('/dashboard')}
              className="px-8 py-4 bg-black text-white font-medium rounded-full hover:bg-gray-800 transition-all flex items-center justify-center gap-2"
            >
              Go to Dashboard
              <ArrowRight className="w-5 h-5" />
            </button>
            <button
              onClick={() => navigate('/settings')}
              className="px-8 py-4 border-2 border-gray-200 text-gray-900 font-medium rounded-full hover:border-gray-400 transition-all"
            >
              Manage Subscription
            </button>
          </div>

          {/* Auto-redirect notice */}
          <p className="text-sm text-gray-500 font-light">
            Redirecting to dashboard in {countdown} seconds...
          </p>
        </div>
      </div>

      <Footer />
    </div>
  );
}
