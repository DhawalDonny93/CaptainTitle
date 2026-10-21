import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { ArrowRight } from 'lucide-react'
import { GradientWave } from './gradient-wave'
import AnimatedTextCycle from './animated-text-cycle'
import { api } from '../../lib/api'

export function HeroSection01() {
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [error, setError] = useState('')
  const [submitted, setSubmitted] = useState(() => !!localStorage.getItem('waitlist_subscribed'))
  const [wasAlreadySubscribed] = useState(() => !!localStorage.getItem('waitlist_subscribed'))
  const [loading, setLoading] = useState(false)

  const validateEmail = (value: string) => {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value)
  }

  const handleNotify = async () => {
    setError('')

    if (!email.trim()) {
      setError('Please enter your email address.')
      return
    }

    if (!validateEmail(email.trim())) {
      setError('Please enter a valid email address.')
      return
    }

    setLoading(true)

    try {
      const meta = {
        email: email.trim(),
        userAgent: navigator.userAgent,
        language: navigator.language,
        timezone: Intl.DateTimeFormat().resolvedOptions().timeZone,
        screenResolution: `${window.screen.width}x${window.screen.height}`,
        referrer: document.referrer || 'direct',
        timestamp: new Date().toISOString(),
      }

      await api.post('/waitlist', meta)
      localStorage.setItem('waitlist_subscribed', 'true')
      setSubmitted(true)
    } catch (err: any) {
      const message = err.response?.data?.message
      setError(message || 'Something went wrong. Please try again.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="h-screen w-full flex items-center justify-center relative overflow-hidden">
      {/* GradientWave behind the text */}
      <GradientWave
        colors={["#ffffff", "#fb7185", '#e879f9', "#a3e635", "#ffffff"]}
        shadowPower={4}
        darkenTop={false}
        noiseFrequency={[0.0001, 0.0002]}
        deform={{ incline: 0.2, noiseAmp: 100, noiseFlow: 2 }}
      />

      <div className="flex flex-col text-center relative z-10">
        {/* ProQuest AI Logo/Icon */}

        <h2 className="font-extrabold pt-4 tracking-tighter text-6xl md:text-7xl lg:text-8xl leading-tight">
          <span className="text-black mix-blend-overlay">Get </span>

            <span
            className="bg-clip-text text-transparent"
            style={{ backgroundImage: 'linear-gradient(135deg, #db3ef0 0%, #6d55f7 50%, #22ee8f 100%)' }}
          >
            Mentioned 
          </span>
           <span className="ml-1 text-black mix-blend-overlay">
            by AI & Monitor </span>
          <br />
          <span
            className="bg-clip-text text-transparent"
            style={{ backgroundImage: 'linear-gradient(135deg, #f43f5e 0%, #a855f7 50%, #22d3ee 100%)' }}
          >
           AI Visibility
          </span>
        </h2>

        <div className="space-y-6 z-10 pt-16 flex justify-center items-center flex-col text-center px-6">
          <p className="text-gray-700 w-full max-w-xl font-light text-base md:text-lg leading-relaxed">
            Track how <AnimatedTextCycle
              items={[
                { word: 'ChatGPT',    color: '#000000', logo: '/images/llm-icons/openai.svg',     isImageLogo: true },
                { word: 'Claude',     color: '#d77655', logo: '/images/llm-icons/claude.svg',     isImageLogo: true },
                { word: 'Gemini',     color: '#5684D1', logo: '/images/llm-icons/gemini.svg',     isImageLogo: true },
                { word: 'Perplexity', color: '#25A5B8', logo: '/images/llm-icons/perplexity.svg', isImageLogo: true },
                { word: 'Google AI',  color: '#4285F4', logo: '/images/llm-icons/google.svg',     isImageLogo: true },
                { word: 'DeepSeek',   color: '#4D6BFE', logo: '/images/llm-icons/deepseek.svg',   isImageLogo: true },
                { word: 'Grok',       color: '#111111', logo: '/images/llm-icons/grok.svg',       isImageLogo: true },
              ]}
              interval={2500}
              className=""
            /> mentions your brand. Get real-time visibility scores, identify gaps, and outrank competitors across all major AI platforms.
          </p>

          <p><b>Join our waitlist. Our subscribers will be notified first! <br />They will get a 15% discount on their first month</b></p>

          {/* Original buttons — hidden for now, will be shown later */}
          <div className="hidden flex gap-4 mt-8 flex-wrap justify-center">
            <button
              onClick={() => navigate('/login')}
              className="h-12 md:h-14 rounded-full cursor-pointer px-8 md:px-10 bg-gray-900 hover:bg-gray-800 text-white font-semibold transition-all flex items-center gap-2"
            >
              Go to Dashboard
              <ArrowRight className="w-4 h-4" />
            </button>

            <button
              onClick={() => navigate('/pricing')}
              className="h-12 md:h-14 cursor-pointer rounded-full px-8 md:px-10 border-2 border-gray-900 bg-white hover:bg-gray-50 text-gray-900 font-semibold transition-all"
            >
              View Pricing
            </button>
          </div>

          {/* Notify form */}
          {submitted ? (
            <div className="h-12 md:h-14 rounded-full px-8 md:px-10 bg-gray-900 text-white font-semibold flex items-center gap-2 text-sm md:text-base">
              {wasAlreadySubscribed ? '✓ Already subscribed from this device!' : '✓ You\'re on the list! We\'ll notify you at launch.'}
            </div>
          ) : (
            <div className="flex flex-col items-center gap-2 w-full max-w-md">
              <div className="flex gap-2 w-full">
                <input
                  type="email"
                  value={email}
                  onChange={e => { setEmail(e.target.value); setError('') }}
                  onKeyDown={e => e.key === 'Enter' && handleNotify()}
                  placeholder="Enter your email"
                  className="h-12 md:h-14 flex-1 rounded-full px-6 border-2 border-gray-900 bg-white text-gray-900 placeholder-gray-400 font-medium text-sm md:text-base focus:outline-none focus:ring-2 focus:ring-gray-900 transition-all"
                />
                <button
                  onClick={handleNotify}
                  disabled={loading}
                  className="h-12 md:h-14 rounded-full cursor-pointer px-8 md:px-10 bg-gray-900 hover:bg-gray-800 disabled:opacity-60 text-white font-semibold transition-all whitespace-nowrap text-sm md:text-base"
                >
                  {loading ? 'Saving…' : 'Subscribe'}
                </button>
              </div>
              {error && (
                <p className="text-red-500 text-sm mt-1">{error}</p>
              )}
            </div>
          )}

        </div>
      </div>

      {/* Scroll indicator */}
      <div className="absolute bottom-8 left-1/2 -translate-x-1/2 z-20 flex flex-col items-center gap-2 opacity-60 hover:opacity-90 transition-opacity duration-300 cursor-default select-none">
        <style>{`
          @keyframes scroll-dot {
            0%   { opacity: 1; transform: translateY(0); }
            60%  { opacity: 0; transform: translateY(8px); }
            100% { opacity: 0; transform: translateY(0); }
          }
          .scroll-dot-anim { animation: scroll-dot 1.8s ease-in-out infinite; }
        `}</style>
        <div
          className="flex justify-center items-start pt-[6px] rounded-full border border-gray-500"
          style={{ width: 20, height: 32, borderWidth: '1.5px' }}
        >
          <div className="scroll-dot-anim w-[3px] h-[6px] rounded-full bg-gray-600" />
        </div>
        <span className="text-[9px] font-semibold uppercase tracking-[0.22em] text-gray-500">scroll</span>
      </div>
    </div>
  )
}
