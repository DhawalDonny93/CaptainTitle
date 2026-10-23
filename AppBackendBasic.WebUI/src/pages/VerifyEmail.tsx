import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { Mail, ArrowRight, AlertCircle, CheckCircle, Smartphone } from 'lucide-react'
import { authService } from '../services/authService'

export default function VerifyEmail() {
  const navigate = useNavigate()
  const [otp, setOtp] = useState(['', '', '', '', '', ''])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState(false)
  const [resendLoading, setResendLoading] = useState(false)
  const [resendCooldown, setResendCooldown] = useState(0)
  
  const email = localStorage.getItem('verificationEmail') || ''
  const sessionId = localStorage.getItem('emailVerificationSessionId') || ''

  useEffect(() => {
    if (!email || !sessionId) {
      navigate('/login')
    }
  }, [email, sessionId, navigate])

  useEffect(() => {
    if (resendCooldown > 0) {
      const timer = setTimeout(() => setResendCooldown(resendCooldown - 1), 1000)
      return () => clearTimeout(timer)
    }
  }, [resendCooldown])

  const handleOtpChange = (index: number, value: string) => {
    if (value.length > 1) {
      value = value[0]
    }

    if (!/^\d*$/.test(value)) {
      return
    }

    const newOtp = [...otp]
    newOtp[index] = value
    setOtp(newOtp)

    // Auto-focus next input
    if (value && index < 5) {
      const nextInput = document.getElementById(`otp-${index + 1}`)
      nextInput?.focus()
    }

    // Auto-submit when all fields are filled
    if (index === 5 && value && newOtp.every(digit => digit !== '')) {
      handleVerify(newOtp.join(''))
    }
  }

  const handleKeyDown = (index: number, e: React.KeyboardEvent) => {
    if (e.key === 'Backspace' && !otp[index] && index > 0) {
      const prevInput = document.getElementById(`otp-${index - 1}`)
      prevInput?.focus()
    }
  }

  const handlePaste = (e: React.ClipboardEvent) => {
    e.preventDefault()
    const pastedData = e.clipboardData.getData('text').slice(0, 6)
    if (!/^\d+$/.test(pastedData)) {
      return
    }

    const newOtp = [...otp]
    for (let i = 0; i < pastedData.length && i < 6; i++) {
      newOtp[i] = pastedData[i]
    }
    setOtp(newOtp)

    // Auto-submit if complete
    if (pastedData.length === 6) {
      handleVerify(pastedData)
    }
  }

  const handleVerify = async (otpValue?: string) => {
    const otpCode = otpValue || otp.join('')
    
    if (otpCode.length !== 6) {
      setError('Please enter all 6 digits')
      return
    }

    setError('')
    setLoading(true)

    try {
      await authService.verifyEmailOtp({
        email,
        otp: otpCode,
        sessionId,
      })
      
      setSuccess(true)
      
      // Clean up
      localStorage.removeItem('emailVerificationSessionId')
      localStorage.removeItem('verificationEmail')
      
      // Redirect after short delay
      setTimeout(() => {
        navigate('/onboarding')
      }, 1500)
    } catch (err: any) {
      setError(err.message || 'Invalid verification code')
      setOtp(['', '', '', '', '', ''])
      document.getElementById('otp-0')?.focus()
    } finally {
      setLoading(false)
    }
  }

  const handleResend = async () => {
    if (resendCooldown > 0) return

    setResendLoading(true)
    setError('')

    try {
      await authService.resendEmailOtp({
        email,
        sessionId,
      })
      
      setResendCooldown(60)
      setError('')
    } catch (err: any) {
      setError(err.message || 'Failed to resend code')
    } finally {
      setResendLoading(false)
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
            Email Verified!
          </h1>
          <p className="text-gray-600 font-light">
            Redirecting you to complete your profile...
          </p>
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
              Verify your email
            </h1>
            <p className="text-gray-600 font-light">
              We sent a 6-digit code to <span className="font-medium text-gray-900">{email}</span>
            </p>
          </div>

          {/* Error Message */}
          {error && (
            <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-2xl flex items-start gap-3">
              <AlertCircle className="w-5 h-5 text-red-600 flex-shrink-0 mt-0.5" />
              <p className="text-sm text-red-800 font-light">{error}</p>
            </div>
          )}

          {/* OTP Input */}
          <div className="mb-8">
            <div className="flex gap-3 justify-center mb-6">
              {otp.map((digit, index) => (
                <input
                  key={index}
                  id={`otp-${index}`}
                  type="text"
                  inputMode="numeric"
                  maxLength={1}
                  value={digit}
                  onChange={(e) => handleOtpChange(index, e.target.value)}
                  onKeyDown={(e) => handleKeyDown(index, e)}
                  onPaste={index === 0 ? handlePaste : undefined}
                  className="w-14 h-14 text-center text-2xl font-medium border-2 border-gray-200 rounded-2xl focus:border-black focus:outline-none transition-colors"
                  disabled={loading}
                />
              ))}
            </div>

            <button
              onClick={() => handleVerify()}
              disabled={loading || otp.some(digit => !digit)}
              className="w-full py-4 bg-black text-white rounded-full hover:bg-gray-800 transition-all flex items-center justify-center gap-2 font-medium disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {loading ? (
                <>
                  <svg className="w-5 h-5 animate-spin" fill="none" viewBox="0 0 24 24">
                    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                    <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                  </svg>
                  Verifying...
                </>
              ) : (
                <>
                  Verify Email
                  <ArrowRight className="w-5 h-5" />
                </>
              )}
            </button>
          </div>

          {/* Resend Code */}
          <div className="text-center">
            <p className="text-sm text-gray-600 font-light mb-3">
              Didn't receive the code?
            </p>
            <button
              onClick={handleResend}
              disabled={resendLoading || resendCooldown > 0}
              className="text-sm font-medium text-black hover:underline disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {resendLoading ? (
                'Sending...'
              ) : resendCooldown > 0 ? (
                `Resend in ${resendCooldown}s`
              ) : (
                'Resend Code'
              )}
            </button>
          </div>

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
          <Mail className="w-16 h-16 mb-8" />
          <h2 className="text-5xl font-light mb-6 leading-tight">
            Secure Your Account
          </h2>
          <p className="text-xl text-gray-300 font-light mb-8">
            Email verification helps us ensure that you're the rightful owner of this account.
          </p>
          <div className="space-y-4">
            <div className="flex items-start gap-4">
              <CheckCircle className="w-6 h-6 flex-shrink-0 mt-1" />
              <div>
                <h3 className="font-medium mb-1">Enhanced Security</h3>
                <p className="text-gray-400 font-light">
                  Protect your account from unauthorized access
                </p>
              </div>
            </div>
            <div className="flex items-start gap-4">
              <CheckCircle className="w-6 h-6 flex-shrink-0 mt-1" />
              <div>
                <h3 className="font-medium mb-1">Account Recovery</h3>
                <p className="text-gray-400 font-light">
                  Easily recover your account if you forget your password
                </p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
