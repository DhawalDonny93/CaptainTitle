import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Mail, ArrowRight, AlertCircle, CheckCircle, Smartphone, Lock } from 'lucide-react'
import { authService } from '../services/authService'

export default function ForgotPassword() {
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    setLoading(true)

    try {
      await authService.requestPasswordReset(email)
      setSuccess(true)
    } catch (err: any) {
      setError(err.message || 'Failed to send reset email')
    } finally {
      setLoading(false)
    }
  }

  if (success) {
    return (
      <div className="min-h-screen bg-white flex items-center justify-center p-8">
        <div className="w-full max-w-md text-center">
          <div className="w-20 h-20 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-6">
            <CheckCircle className="w-10 h-10 text-green-600" />
          </div>
          <h1 className="text-3xl font-light text-gray-900 mb-2">
            Check Your Email
          </h1>
          <p className="text-gray-600 font-light mb-8">
            We've sent password reset instructions to <span className="font-medium text-gray-900">{email}</span>
          </p>
          <div className="space-y-4">
            <button
              onClick={() => navigate('/login')}
              className="w-full py-4 bg-black text-white rounded-full hover:bg-gray-800 transition-all flex items-center justify-center gap-2 font-medium"
            >
              Back to Login
              <ArrowRight className="w-5 h-5" />
            </button>
            <button
              onClick={() => {
                setSuccess(false)
                setEmail('')
              }}
              className="w-full py-4 border-2 border-gray-200 rounded-full hover:border-gray-400 transition-all font-medium"
            >
              Try Another Email
            </button>
          </div>
        </div>
      </div>
    )
  }

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
              <div className="w-10 h-10 bg-black rounded-2xl flex items-center justify-center group-hover:scale-110 transition-transform">
                <Smartphone className="w-5 h-5 text-white" />
              </div>
              <span className="text-xl font-semibold text-gray-900">
                PuffCount
              </span>
            </button>
            <h1 className="text-4xl font-light tracking-tight text-gray-900 mb-2">
              Reset your password
            </h1>
            <p className="text-gray-600 font-light">
              Enter your email address and we'll send you instructions to reset your password
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
          <form onSubmit={handleSubmit} className="space-y-6">
            <div>
              <label className="block text-sm font-medium text-gray-900 mb-2">
                Email Address
              </label>
              <div className="relative">
                <Mail className="absolute left-4 top-1/2 -translate-y-1/2 w-5 h-5 text-gray-400" />
                <input
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  className="w-full pl-12 pr-4 py-4 border-2 border-gray-200 rounded-2xl focus:border-black focus:outline-none transition-colors font-light"
                  placeholder="you@example.com"
                  required
                  disabled={loading}
                />
              </div>
            </div>

            <button
              type="submit"
              disabled={loading}
              className="w-full py-4 bg-black text-white rounded-full hover:bg-gray-800 transition-all flex items-center justify-center gap-2 font-medium disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {loading ? (
                <>
                  <svg className="w-5 h-5 animate-spin" fill="none" viewBox="0 0 24 24">
                    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                    <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                  </svg>
                  Sending...
                </>
              ) : (
                <>
                  Send Reset Instructions
                  <ArrowRight className="w-5 h-5" />
                </>
              )}
            </button>
          </form>

          {/* Back to Login */}
          <div className="mt-8 text-center">
            <button
              onClick={() => navigate('/login')}
              className="text-sm text-gray-600 hover:text-gray-900 font-light"
            >
              ← Back to login
            </button>
          </div>
        </div>
      </div>

      {/* Right Side - Visual */}
      <div className="hidden lg:flex flex-1 bg-cherry to-black p-16 items-center justify-center relative overflow-hidden">
        {/* Animated background elements */}
        <div className="absolute inset-0 opacity-10">
          <div className="absolute top-1/4 left-1/4 w-96 h-96 bg-white rounded-full blur-3xl"></div>
          <div className="absolute bottom-1/4 right-1/4 w-96 h-96 bg-white rounded-full blur-3xl"></div>
        </div>

        {/* Content */}
        <div className="relative z-10 text-white max-w-lg">
          <Lock className="w-16 h-16 mb-8" />
          <h2 className="text-5xl font-light mb-6 leading-tight">
            Forgot Your Password?
          </h2>
          <p className="text-xl text-gray-300 font-light mb-8">
            Don't worry, it happens to everyone. We'll help you reset it quickly and securely.
          </p>
          <div className="space-y-4">
            <div className="flex items-start gap-4">
              <CheckCircle className="w-6 h-6 flex-shrink-0 mt-1" />
              <div>
                <h3 className="font-medium mb-1">Secure Process</h3>
                <p className="text-gray-400 font-light">
                  Your password reset link is encrypted and expires after 1 hour
                </p>
              </div>
            </div>
            <div className="flex items-start gap-4">
              <CheckCircle className="w-6 h-6 flex-shrink-0 mt-1" />
              <div>
                <h3 className="font-medium mb-1">Quick Recovery</h3>
                <p className="text-gray-400 font-light">
                  Get back to your account in just a few minutes
                </p>
              </div>
            </div>
            <div className="flex items-start gap-4">
              <CheckCircle className="w-6 h-6 flex-shrink-0 mt-1" />
              <div>
                <h3 className="font-medium mb-1">Email Verification</h3>
                <p className="text-gray-400 font-light">
                  Only you can access the reset link sent to your email
                </p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
