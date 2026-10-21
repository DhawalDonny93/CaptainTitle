import { useState, useEffect, useRef } from 'react'
import { CheckCircle2, X } from 'lucide-react'
import { api } from '../lib/api'

interface Props {
  open: boolean
  onClose: () => void
}

export default function WaitlistModal({ open, onClose }: Props) {
  const [email, setEmail] = useState('')
  const [error, setError] = useState('')
  const [submitted, setSubmitted] = useState(() => !!localStorage.getItem('waitlist_subscribed'))
  const [wasAlreadySubscribed, setWasAlreadySubscribed] = useState(() => !!localStorage.getItem('waitlist_subscribed'))
  const [loading, setLoading] = useState(false)
  const inputRef = useRef<HTMLInputElement>(null)

  useEffect(() => {
    if (!open) { setEmail(''); setError(''); setLoading(false) }
    else {
      const alreadyDone = !!localStorage.getItem('waitlist_subscribed')
      setSubmitted(alreadyDone)
      setWasAlreadySubscribed(alreadyDone)
      setTimeout(() => inputRef.current?.focus(), 50)
    }
  }, [open])

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => { if (e.key === 'Escape') onClose() }
    if (open) document.addEventListener('keydown', onKey)
    return () => document.removeEventListener('keydown', onKey)
  }, [open, onClose])

  const handleSubscribe = async () => {
    setError('')
    if (!email.trim()) { setError('Please enter your email address.'); return }
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim())) { setError('Please enter a valid email address.'); return }
    setLoading(true)
    try {
      await api.post('/waitlist', {
        email: email.trim(),
        userAgent: navigator.userAgent,
        language: navigator.language,
        timezone: Intl.DateTimeFormat().resolvedOptions().timeZone,
        screenResolution: `${window.screen.width}x${window.screen.height}`,
        referrer: document.referrer || 'direct',
        timestamp: new Date().toISOString(),
      })
      localStorage.setItem('waitlist_subscribed', 'true')
      setSubmitted(true)
    } catch (err: any) {
      const msg = err.response?.data?.message
      setError(msg || 'Something went wrong. Please try again.')
    } finally {
      setLoading(false)
    }
  }

  if (!open) return null

  return (
    <div
      className="fixed inset-0 z-[100] flex items-center justify-center p-4"
      style={{ background: 'rgba(0,0,0,0.5)', backdropFilter: 'blur(4px)' }}
      onClick={onClose}
    >
      <div
        className="relative w-full max-w-md rounded-2xl bg-white shadow-2xl p-8"
        onClick={e => e.stopPropagation()}
      >
        <button
          onClick={onClose}
          className="absolute top-4 right-4 text-gray-400 hover:text-gray-700 transition-colors"
        >
          <X className="w-5 h-5" />
        </button>

        {submitted ? (
          <div className="text-center py-6">
            <div className="w-16 h-16 rounded-full bg-emerald-50 flex items-center justify-center mx-auto mb-5">
              <CheckCircle2 className="w-8 h-8 text-emerald-500" />
            </div>
            {wasAlreadySubscribed ? (
              <>
                <h3 className="text-xl font-bold text-gray-900 mb-2">Already on the list!</h3>
                <p className="text-gray-500 text-sm leading-relaxed">
                  You've already subscribed from this device. We'll notify you the moment ProQuest AI goes live with your <strong>15% discount</strong> ready to go.
                </p>
              </>
            ) : (
              <>
                <h3 className="text-xl font-bold text-gray-900 mb-2">You're on the list!</h3>
                <p className="text-gray-500 text-sm leading-relaxed">
                  We'll notify you the moment ProQuest AI goes live. Early subscribers get <strong>15% off</strong> their first month automatically.
                </p>
              </>
            )}
            <button
              onClick={onClose}
              className="mt-6 px-6 py-2.5 rounded-xl bg-gray-900 hover:bg-gray-700 text-white font-semibold text-sm transition-colors"
            >
              Close
            </button>
          </div>
        ) : (
          <>
            <div className="mb-6">
              <span
                className="inline-block text-xs font-bold uppercase tracking-widest mb-3 bg-clip-text text-transparent"
                style={{ backgroundImage: 'linear-gradient(135deg, #f43f5e 0%, #a855f7 50%, #22d3ee 100%)' }}
              >
                Early Access
              </span>
              <h3 className="text-2xl font-extrabold text-gray-900 leading-tight mb-2">
                Join the waitlist
              </h3>
              <p className="text-gray-500 text-sm leading-relaxed">
                We'll notify you the moment we go live. Early subscribers get <strong className="text-gray-700">15% off their first month</strong> — no code needed.
              </p>
            </div>

            <ul className="space-y-2 mb-6">
              {[
                'First to know when ProQuest AI launches',
                '15% discount applied automatically',
                'Priority onboarding support',
              ].map(p => (
                <li key={p} className="flex items-center gap-2.5 text-sm text-gray-600">
                  <CheckCircle2 className="w-4 h-4 text-emerald-500 flex-shrink-0" />
                  {p}
                </li>
              ))}
            </ul>

            <div className="space-y-3">
              <input
                ref={inputRef}
                type="email"
                value={email}
                onChange={e => { setEmail(e.target.value); setError('') }}
                onKeyDown={e => e.key === 'Enter' && handleSubscribe()}
                placeholder="Enter your email"
                className="w-full h-12 rounded-xl px-4 border-2 border-gray-200 focus:border-gray-900 text-gray-900 placeholder-gray-400 text-sm font-medium outline-none transition-colors"
              />
              {error && <p className="text-red-500 text-xs">{error}</p>}
              <button
                onClick={handleSubscribe}
                disabled={loading}
                className="w-full h-12 rounded-xl font-semibold text-white text-sm transition-all disabled:opacity-60"
                style={{ background: 'linear-gradient(135deg, #f43f5e 0%, #a855f7 50%, #22d3ee 100%)' }}
              >
                {loading ? 'Saving…' : "Subscribe — it's free"}
              </button>
              <p className="text-center text-xs text-gray-400">No spam. Unsubscribe anytime.</p>
            </div>
          </>
        )}
      </div>
    </div>
  )
}
