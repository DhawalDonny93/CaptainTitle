import { useNavigate } from 'react-router-dom'
import { useEffect, useState } from 'react'
import {
  Check, Loader2, CreditCard, ShieldCheck, Star,
  TrendingUp, AlertTriangle, Eye, Zap
} from 'lucide-react'
import { api } from '../lib/api'
import { authService } from '../services/authService'
import { toast } from '../utils/toast'

interface SubscriptionPlan {
  id: number
  name: string
  displayName: string
  description: string
  monthlyPrice: number
  yearlyPrice: number
  currency: string
  maxProjects: number
  maxPromptsPerProject: number
  maxVisibilityChecksPerMonth: number
  maxSiteAuditsPerMonth: number
  maxContentArticles: number
  maxLlmRequestsPerMonth: number
  allowCompetitorTracking: boolean
  allowAdvancedAnalytics: boolean
  allowApiAccess: boolean
  allowWhiteLabel: boolean
  allowPrioritySupport: boolean
}

function fmt(val: number) {
  return val === -1 ? 'Unlimited' : val?.toLocaleString() ?? '—'
}

function planFeatures(plan: SubscriptionPlan): string[] {
  const feats: string[] = [
    `${fmt(plan.maxProjects)} Project${plan.maxProjects !== 1 ? 's' : ''}`,
    `${fmt(plan.maxPromptsPerProject)} Prompts / project`,
    `${fmt(plan.maxVisibilityChecksPerMonth)} AI checks / month`,
    `${fmt(plan.maxSiteAuditsPerMonth)} Site audit${plan.maxSiteAuditsPerMonth !== 1 ? 's' : ''} / month`,
  ]
  if (plan.allowCompetitorTracking) feats.push('Competitor tracking')
  if (plan.allowAdvancedAnalytics)  feats.push('Advanced analytics')
  if (plan.allowApiAccess)          feats.push('Full API access')
  if (plan.allowWhiteLabel)         feats.push('White-label options')
  if (plan.allowPrioritySupport)    feats.push('Priority support')
  else                              feats.push('Email support')
  return feats
}

// ── Testimonial ──────────────────────────────────────────────────────────────
function Testimonial() {
  return (
    <div className="flex flex-col justify-between h-full pl-6 border-l border-gray-200">
      <div>
        <div className="flex gap-0.5 mb-3">
          {[1,2,3,4,5].map(i => <Star key={i} className="w-4 h-4 fill-amber-400 text-amber-400" />)}
        </div>
        <p className="text-sm text-gray-700 leading-relaxed mb-5">
          "Thanks to ProQuest AI, we quickly saw improvements in AI visibility for our clients.
          We've been using it for over 2 months — an absolute game changer for our SEO agency."
        </p>
      </div>
      <div>
        <div className="flex items-center gap-3 mb-3">
          <div className="w-10 h-10 rounded-full bg-gradient-to-br from-rose-400 to-purple-500 flex items-center justify-center text-white text-sm font-bold flex-shrink-0">
            SL
          </div>
          <div>
            <p className="text-sm font-semibold text-gray-900">Steve Lee</p>
            <p className="text-xs text-gray-500">Founder, SEO Aesthetic (Ex-Google, Microsoft)</p>
          </div>
        </div>
        <p className="text-base font-bold text-gray-900 tracking-tight">
          <span className="font-black">SEO</span>Aesthetic
          <span className="text-amber-500 ml-0.5">✳</span>
        </p>
      </div>
    </div>
  )
}

// ── Main ─────────────────────────────────────────────────────────────────────
export default function OnboardingPlans() {
  const navigate = useNavigate()
  const [plans, setPlans] = useState<SubscriptionPlan[]>([])
  const [loading, setLoading] = useState(true)
  const [subscribing, setSubscribing] = useState<number | null>(null)
  const [billingCycle, setBillingCycle] = useState<'monthly' | 'yearly'>('yearly')

  useEffect(() => { loadPricingPlans() }, [])

  const loadPricingPlans = async () => {
    try {
      setLoading(true)
      const response = await api.get('/subscription/plans')
      // Sort: Starter → Professional → Scale, exclude Enterprise
      const order = ['Starter', 'Professional', 'Scale']
      const sorted = (response.data as SubscriptionPlan[])
        .filter(p => p.name !== 'Enterprise')
        .sort((a, b) => {
          const ai = order.indexOf(a.name)
          const bi = order.indexOf(b.name)
          return (ai === -1 ? 99 : ai) - (bi === -1 ? 99 : bi)
        })
      setPlans(sorted)
    } catch (err) {
      console.error('Error loading pricing plans:', err)
      toast.error('Failed to load pricing plans')
    } finally {
      setLoading(false)
    }
  }

  const handleSubscribe = async (plan: SubscriptionPlan) => {
    if (!authService.isAuthenticated()) {
      localStorage.setItem('selectedPlanId', plan.id.toString())
      localStorage.setItem('selectedBillingCycle', billingCycle)
      toast.info('Please log in to continue')
      navigate('/login?returnUrl=/onboarding/plans')
      return
    }
    setSubscribing(plan.id)
    try {
      if (plan.monthlyPrice === 0) {
        const response = await api.post('/subscription/assign-free')
        if (response.data.success) {
          toast.success('Free plan activated!')
          await authService.getCurrentUser()
          navigate('/dashboard')
        }
      } else {
        const response = await api.post('/subscription/create-checkout', {
          planId: plan.id,
          billingCycle: billingCycle === 'monthly' ? 'Monthly' : 'Yearly',
        })
        if (response.data.url) window.location.href = response.data.url
      }
    } catch (err: any) {
      toast.error(err.response?.data?.message || 'Failed to process subscription')
    } finally {
      setSubscribing(null)
    }
  }

  if (loading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <Loader2 className="w-7 h-7 animate-spin text-gray-400" />
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-gray-50">

      {/* ── Header ───────────────────────────────────────────────────────── */}
      <div className="bg-white border-b border-gray-200 px-6 py-3.5 flex items-center justify-center">
        <div className="flex items-center gap-2">
          <div className="w-7 h-7 rounded-lg bg-gradient-to-br from-rose-500 via-purple-500 to-cyan-400 flex items-center justify-center">
            <Eye className="w-4 h-4 text-white" />
          </div>
          <span className="font-bold text-gray-900 text-sm tracking-tight">ProQuest AI</span>
        </div>
      </div>

      {/* ── Visibility hero ───────────────────────────────────────────────── */}
      <div className="bg-white border-b border-gray-200">
        <div className="max-w-3xl mx-auto px-6 pt-10 pb-8 text-center">
          <h1 className="text-xl font-semibold text-gray-900 mb-1">
            Your brand shows up{' '}
            <span className="font-bold bg-clip-text text-transparent"
              style={{ backgroundImage: 'linear-gradient(135deg, #f43f5e 0%, #a855f7 50%, #22d3ee 100%)' }}>
              0%
            </span>{' '}
            of the time in AI answers
          </h1>
          <p className="text-sm text-amber-600 font-medium flex items-center justify-center gap-1.5 mt-2">
            <AlertTriangle className="w-4 h-4" />
            It's not performing well in any LLM
          </p>

          {/* Stats */}
          <div className="flex items-center justify-center gap-12 mt-7 pt-7 border-t border-gray-100">
            {[
              { label: 'Prompts tracked', value: '0', color: 'text-gray-800' },
              { label: 'With visibility',  value: '0', color: 'text-gray-800' },
              { label: 'Zero visibility', value: '0', color: 'text-rose-500' },
            ].map((s) => (
              <div key={s.label} className="text-center">
                <p className={`text-2xl font-bold ${s.color}`}>{s.value}</p>
                <p className="text-xs text-gray-500 mt-0.5">{s.label}</p>
              </div>
            ))}
          </div>
        </div>

        {/* ── Unlock + Testimonial ─────────────────────────────────────────── */}
        <div className="max-w-4xl mx-auto px-6 pb-10">
          <div className="grid md:grid-cols-2 gap-0 border border-gray-200 rounded-2xl overflow-hidden bg-white">
            {/* Left */}
            <div className="p-6 bg-white">
              <h2 className="text-base font-bold text-gray-900 mb-1">Unlock Your Full AI Visibility Report</h2>
              <p className="text-xs text-gray-500 mb-5 leading-relaxed">
                Get access to sources, competitor analysis, and actionable steps to improve your AI rankings.
              </p>
              <div className="space-y-2.5">
                {[
                  { icon: CreditCard,  color: 'text-gray-500',   title: 'You are not charged today',           body: 'Cancel before day 7 if you decide not to use it & you won\'t be charged at all.' },
                  { icon: Check,       color: 'text-emerald-500', title: 'Cancel anytime, no questions asked',  body: 'You can cancel anytime from our website, no interaction with us required.' },
                  { icon: ShieldCheck, color: 'text-blue-500',    title: 'Secure payment, Powered by Stripe',   body: 'Your card is safe, we do not store your cc details, its all powered by Stripe.' },
                ].map(({ icon: Icon, color, title, body }) => (
                  <div key={title} className="flex items-start gap-3 bg-gray-50 border border-gray-200 rounded-xl p-3">
                    <Icon className={`w-4 h-4 mt-0.5 flex-shrink-0 ${color}`} />
                    <div>
                      <p className="text-xs font-semibold text-gray-900">{title}</p>
                      <p className="text-xs text-gray-500 mt-0.5 leading-relaxed">{body}</p>
                    </div>
                  </div>
                ))}
              </div>
            </div>
            {/* Right */}
            <div className="p-6 bg-white">
              <Testimonial />
            </div>
          </div>
        </div>
      </div>

      {/* ── Billing toggle ────────────────────────────────────────────────── */}
      <div className="pt-8 pb-5 text-center">
        <p className="text-[11px] font-semibold text-gray-400 uppercase tracking-widest mb-3">Choose your plan</p>
        <div className="inline-flex items-center bg-white border border-gray-200 rounded-lg p-1 shadow-sm">
          <button
            onClick={() => setBillingCycle('monthly')}
            className={`px-5 py-1.5 rounded-md text-sm font-semibold transition-all ${
              billingCycle === 'monthly' ? 'bg-gray-900 text-white' : 'text-gray-500 hover:text-gray-900'
            }`}
          >
            Monthly
          </button>
          <button
            onClick={() => setBillingCycle('yearly')}
            className={`px-5 py-1.5 rounded-md text-sm font-semibold transition-all ${
              billingCycle === 'yearly' ? 'bg-gray-900 text-white' : 'text-gray-500 hover:text-gray-900'
            }`}
          >
            Annual <span className="text-[10px] font-bold text-emerald-500 ml-1">Save 20%</span>
          </button>
        </div>
      </div>

      {/* ── Pricing cards — dynamic, all plans ───────────────────────────── */}
      <div className="max-w-5xl mx-auto px-6 pb-10">
        {plans.length === 0 ? (
          <p className="text-center text-sm text-gray-400 py-10">No plans available. Please try again later.</p>
        ) : (
          <div className={`grid gap-4 ${plans.length === 3 ? 'md:grid-cols-3' : plans.length === 2 ? 'md:grid-cols-2' : 'md:grid-cols-1 max-w-sm mx-auto'}`}>
            {plans.map((plan) => {
              const isPro   = plan.name === 'Professional'
              const isFree  = plan.monthlyPrice === 0
              const price   = billingCycle === 'monthly' ? plan.monthlyPrice : Math.floor(plan.yearlyPrice / 12)
              const savings = !isFree && billingCycle === 'yearly'
                ? ((plan.monthlyPrice * 12) - plan.yearlyPrice).toFixed(0)
                : null
              const isSubscribing = subscribing === plan.id
              const features = planFeatures(plan)

              const checkColor = isPro ? 'text-purple-500' : plan.name === 'Scale' ? 'text-cyan-500' : 'text-emerald-500'

              return (
                <div
                  key={plan.id}
                  className={`rounded-2xl bg-white flex flex-col transition-all ${
                    isPro
                      ? 'border-2 shadow-lg'
                      : 'border border-gray-200 hover:border-gray-300 hover:shadow-md'
                  }`}
                  style={isPro ? { borderColor: '#a855f7' } : {}}
                >
                  {/* Top gradient strip for Pro */}
                  {isPro && (
                    <div className="h-1 rounded-t-2xl w-full"
                      style={{ backgroundImage: 'linear-gradient(135deg, #f43f5e 0%, #a855f7 50%, #22d3ee 100%)' }} />
                  )}

                  <div className="p-6 flex flex-col flex-1">
                    {/* Badge */}
                    {isPro && (
                      <div className="mb-3">
                        <span className="inline-flex items-center gap-1 text-[10px] font-bold uppercase tracking-wide text-white rounded-full px-3 py-1"
                          style={{ backgroundImage: 'linear-gradient(135deg, #f43f5e 0%, #a855f7 100%)' }}>
                          ⭐ Most Popular
                        </span>
                      </div>
                    )}

                    {/* Name + description */}
                    <h3 className="text-sm font-bold text-gray-900">{plan.displayName}</h3>
                    <p className="text-xs text-gray-500 mt-1 mb-4 leading-relaxed">{plan.description}</p>

                    {/* Price */}
                    <div className="mb-4">
                      <div className="flex items-baseline gap-1">
                        <span className="text-3xl font-bold text-gray-900">
                          {isFree ? '$0' : `$${price}`}
                        </span>
                        <span className="text-sm text-gray-400">/mo</span>
                      </div>
                      {isFree && <p className="text-xs text-emerald-600 font-semibold mt-1">Free forever</p>}
                      {savings && <p className="text-xs text-emerald-600 font-semibold mt-1">Save ${savings}/yr with annual</p>}
                      {isPro && <p className="text-xs text-purple-600 font-semibold mt-1">14-day free trial · $0 due today</p>}
                    </div>

                    {/* CTA */}
                    {isPro ? (
                      <button
                        onClick={() => handleSubscribe(plan)}
                        disabled={isSubscribing}
                        className="w-full py-2.5 rounded-xl text-sm font-semibold text-white transition-all mb-5 disabled:opacity-60 hover:opacity-90"
                        style={{ backgroundImage: 'linear-gradient(135deg, #f43f5e 0%, #a855f7 50%, #22d3ee 100%)' }}
                      >
                        {isSubscribing ? 'Processing…' : 'Start 14-Day Free Trial'}
                      </button>
                    ) : (
                      <button
                        onClick={() => handleSubscribe(plan)}
                        disabled={isSubscribing}
                        className="w-full py-2.5 rounded-xl text-sm font-semibold bg-gray-900 text-white hover:bg-gray-800 transition-all mb-5 disabled:opacity-60"
                      >
                        {isSubscribing ? 'Processing…' : isFree ? 'Start for Free' : `Get ${plan.displayName}`}
                      </button>
                    )}

                    {/* Features */}
                    <ul className="space-y-2.5 flex-1">
                      {features.map(f => (
                        <li key={f} className="flex items-start gap-2 text-xs text-gray-700">
                          <Check className={`w-3.5 h-3.5 flex-shrink-0 mt-0.5 ${checkColor}`} />
                          {f}
                        </li>
                      ))}
                    </ul>
                  </div>
                </div>
              )
            })}
          </div>
        )}
      </div>

      {/* ── Stats bar ─────────────────────────────────────────────────────── */}
      <div className="bg-white border-y border-gray-200 py-7">
        <div className="max-w-3xl mx-auto px-6">
          <div className="grid grid-cols-2 md:grid-cols-4 gap-6 text-center">
            {[
              { value: '3,000+', label: 'Active Users',            sub: '↑ 340% YoY',    color: 'text-emerald-500' },
              { value: '50M+',   label: 'AI Checks Monthly',       sub: '↑ 520% Growth', color: 'text-emerald-500' },
              { value: '285%',   label: 'Avg Visibility Increase',  sub: 'In 90 days',    color: 'text-blue-500'    },
              { value: '6',      label: 'AI Platforms Tracked',     sub: 'Always updated', color: 'text-purple-500' },
            ].map(s => (
              <div key={s.label}>
                <p className="text-xl font-bold text-gray-900">{s.value}</p>
                <p className="text-xs text-gray-500 mt-0.5">{s.label}</p>
                <p className={`text-[10px] font-semibold mt-1 ${s.color}`}>{s.sub}</p>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* ── Trust signals ─────────────────────────────────────────────────── */}
      <div className="max-w-3xl mx-auto px-6 py-8">
        <div className="grid md:grid-cols-3 gap-4">
          {[
            { icon: Zap,        title: 'Setup in 2 minutes',  body: 'Start with the free plan, no hidden charges' },
            { icon: Check,      title: 'Cancel Anytime',      body: 'No lock-in contracts, cancel in one click' },
            { icon: TrendingUp, title: '30-Day Money Back',   body: "Full refund if you're not satisfied" },
          ].map(({ icon: Icon, title, body }) => (
            <div key={title} className="flex gap-3 bg-white rounded-xl border border-gray-200 p-4">
              <div className="w-7 h-7 rounded-lg bg-gray-50 border border-gray-200 flex items-center justify-center flex-shrink-0">
                <Icon className="w-3.5 h-3.5 text-gray-600" />
              </div>
              <div>
                <p className="text-xs font-semibold text-gray-900">{title}</p>
                <p className="text-[11px] text-gray-500 mt-0.5">{body}</p>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* ── FAQ ───────────────────────────────────────────────────────────── */}
      <div className="max-w-2xl mx-auto px-6 pb-14">
        <h2 className="text-base font-bold text-gray-900 mb-5 text-center">Frequently Asked Questions</h2>
        <div className="space-y-2.5">
          {[
            { q: 'Can I upgrade or downgrade anytime?',  a: 'Yes — change your plan at any time from account settings. We\'ll prorate charges so you only pay for what you use.' },
            { q: 'What happens after my free trial?',    a: 'After 14 days you\'ll be charged for the plan you selected. Cancel anytime before that and you won\'t be charged.' },
            { q: 'Which AI platforms do you track?',     a: 'We track ChatGPT, Claude, Gemini, Perplexity, DeepSeek, and Grok — with new platforms added continuously.' },
            { q: 'How does the free plan compare?',      a: 'The free plan gives you 1 project with limited checks — perfect for trying ProQuest AI. Upgrade when ready for more.' },
          ].map(({ q, a }) => (
            <div key={q} className="bg-white border border-gray-200 rounded-xl p-4">
              <h3 className="text-xs font-semibold text-gray-900 mb-1">{q}</h3>
              <p className="text-xs text-gray-500 leading-relaxed">{a}</p>
            </div>
          ))}
        </div>
      </div>

    </div>
  )
}
