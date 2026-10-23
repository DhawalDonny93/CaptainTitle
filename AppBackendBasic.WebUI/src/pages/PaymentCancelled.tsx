import { useNavigate } from 'react-router-dom';
import { XCircle, ArrowLeft, HelpCircle } from 'lucide-react';
import Header from '../components/Header';
import Footer from '../components/Footer';

export default function PaymentCancelled() {
  const navigate = useNavigate();

  return (
    <div className="min-h-screen bg-white flex flex-col">
      <Header />

      <div className="flex-1 flex items-center justify-center px-6 py-24">
        <div className="max-w-2xl w-full text-center">
          {/* Cancelled Icon */}
          <div className="mb-8 flex justify-center">
            <div className="relative">
              <div className="absolute inset-0 bg-gray-500/20 rounded-full blur-2xl"></div>
              <XCircle className="w-24 h-24 text-gray-500 relative" />
            </div>
          </div>

          {/* Cancelled Message */}
          <h1 className="text-5xl md:text-6xl font-light tracking-tight text-gray-900 mb-6">
            Payment Cancelled
          </h1>

          <p className="text-xl text-gray-600 font-light mb-12 leading-relaxed">
            Your payment was cancelled. No charges were made to your account.
          </p>

          {/* Help Section */}
          <div className="bg-gray-50 rounded-3xl p-8 mb-12">
            <div className="flex items-center justify-center gap-3 mb-4">
              <HelpCircle className="w-6 h-6 text-gray-600" />
              <h2 className="text-xl font-normal text-gray-900">Need help?</h2>
            </div>
            <p className="text-gray-600 font-light mb-6">
              If you encountered any issues during checkout or have questions about our pricing, we're here to help.
            </p>
            <div className="flex flex-col sm:flex-row gap-4 justify-center">
              <a
                href="mailto:support@yourdomain.com"
                className="px-6 py-3 border-2 border-gray-200 text-gray-900 font-medium rounded-full hover:border-gray-400 transition-all"
              >
                Contact Support
              </a>
              <button
                onClick={() => navigate('/features')}
                className="px-6 py-3 border-2 border-gray-200 text-gray-900 font-medium rounded-full hover:border-gray-400 transition-all"
              >
                Learn More
              </button>
            </div>
          </div>

          {/* CTA Buttons */}
          <div className="flex flex-col sm:flex-row gap-4 justify-center">
            <button
              onClick={() => navigate('/pricing')}
              className="px-8 py-4 bg-black text-white font-medium rounded-full hover:bg-gray-800 transition-all flex items-center justify-center gap-2"
            >
              <ArrowLeft className="w-5 h-5" />
              Back to Pricing
            </button>
            <button
              onClick={() => navigate('/dashboard')}
              className="px-8 py-4 border-2 border-gray-200 text-gray-900 font-medium rounded-full hover:border-gray-400 transition-all"
            >
              Go to Dashboard
            </button>
          </div>

          {/* Free Plan Notice */}
          <div className="mt-12 p-6 bg-butter rounded-2xl">
            <p className="text-gray-700 font-light">
              <span className="font-medium">Good news:</span> You can still use Captain Title with our free Starter plan. 
              Upgrade anytime when you're ready.
            </p>
          </div>
        </div>
      </div>

      <Footer />
    </div>
  );
}
