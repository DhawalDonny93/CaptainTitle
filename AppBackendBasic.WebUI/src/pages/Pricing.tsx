import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Check, Zap } from 'lucide-react'
import Header from '../components/Header'
import Footer from '../components/Footer'

// ── Static plan definitions ──────────────────────────────────────────────────
const PLANS = [
  {
    name: 'Starter',
    displayName: 'Starter',
    description: 'Perfect for individuals and small brands getting started with AI visibility.',
    monthlyPrice: 0,
    yearlyPrice: 0,
    badge: null,
    gradient: null,
    features: [
      '1 Project',
      '10 Prompts tracked',
      '1,000 AI checks / month',
      '1 Site audit / month',
      '6 AI platforms monitored',
      'Email support',
    ],
    notIncluded: [
      'Competitor tracking',
      'Advanced analytics',
      'API access',
      'Priority support',
    ],
    cta: 'Start for Free',
    ctaStyle: 'outline' as const,
  },
  {
    name: 'Professional',
    displayName: 'Professional',
    description: 'For growing brands that need serious AI visibility and competitor intelligence.',
    monthlyPrice: 49,
    yearlyPrice: 470, // ~$39/mo
    badge: '⭐ Most Popular',
    gradient: 'linear-gradient(135deg, #f43f5e 0%, #a855f7 50%, #22d3ee 100%)',
    features: [
      'Unlimited Projects',
      'Unlimited Prompts',
      '50,000 AI checks / month',
      '10 Site audits / month',
      '6 AI platforms monitored',
      'Competitor tracking',
      'Advanced analytics & reports',
      'Citation source analysis',
      'AI content recommendations',
      'Priority support',
    ],
    notIncluded: [
      'API access',
      'White-label options',
    ],
    cta: 'Start 14-Day Free Trial',
    ctaStyle: 'gradient' as const,
  },
  {
    name: 'Scale',
    displayName: 'Scale',
    description: 'For agencies and teams managing multiple brands across every AI platform.',
    monthlyPrice: 149,
    yearlyPrice: 1430, // ~$119/mo
    badge: null,
    gradient: null,
    features: [
      'Unlimited everything',
      'Unlimited AI checks',
      'Unlimited Site audits',
      '6 AI platforms monitored',
      'Competitor tracking',
      'Advanced analytics & reports',
      'Citation source analysis',
      'AI content recommendations',
      'Full API access',
      'White-label options',
      'Custom integrations',
      'Dedicated account manager',
    ],
    notIncluded: [],
    cta: 'Upgrade to Scale',
    ctaStyle: 'outline' as const,
  },
]

const FAQS = [
  {
    q: 'Can I upgrade or downgrade anytime?',
    a: 'Yes — change your plan at any time from account settings. We prorate charges so you only pay for what you use.',
  },
  {
    q: 'What happens after the 14-day free trial?',
    a: 'After 14 days you\'ll be charged for the plan you selected. Cancel anytime before that and you won\'t be charged a cent.',
  },
  {
    q: 'Which AI platforms do you track?',
    a: 'We track ChatGPT, Claude, Gemini, Perplexity, DeepSeek, and Grok. New platforms are added as they gain traction.',
  },
  {
    q: 'Is there a free plan?',
    a: 'Yes. The Starter plan is free forever — 1 project, 10 prompts, 1,000 checks per month. No credit card required.',
  },
  {
    q: 'Do you offer agency or team pricing?',
    a: 'The Scale plan is built for agencies. For larger teams or custom contracts, contact us for enterprise pricing.',
  },
  {
    q: 'Is my data secure?',
    a: 'All data is encrypted in transit and at rest. We never sell your data or share it with third parties.',
  },
]

// ── Component ────────────────────────────────────────────────────────────────
export default function Pricing() {
  const navigate = useNavigate()
  const [billingCycle, setBillingCycle] = useState<'monthly' | 'yearly'>('yearly')

  const getPrice = (plan: typeof PLANS[0]) => {
    if (plan.monthlyPrice === 0) return { display: '$0', sub: 'free forever' }
    const price = billingCycle === 'monthly' ? plan.monthlyPrice : Math.floor(plan.yearlyPrice / 12)
    return { display: `$${price}`, sub: '/month' }
  }

  const getSavings = (plan: typeof PLANS[0]) => {
    if (plan.monthlyPrice === 0) return null
    const saved = (plan.monthlyPrice * 12) - plan.yearlyPrice
    return saved > 0 ? `Save $${saved}/yr` : null
  }

  return (
    <div className="min-h-screen bg-white">
      <Header />

      {/* ── Hero ───────────────────────────────────────────────────────────── */}
      <section className="pt-36 pb-16 px-6 relative overflow-hidden">
        <div className="absolute inset-0 pointer-events-none"
          style={{ background: 'radial-gradient(ellipse 80% 50% at 50% -10%, rgba(168,85,247,0.08) 0%, transparent 70%)' }} />
        <div className="max-w-3xl mx-auto text-center relative z-10">
          <div className="inline-flex items-center gap-2 bg-gray-50 border border-gray-200 rounded-full px-4 py-1.5 mb-6">
            <Zap className="w-3 h-3 text-purple-500" />
            <span className="text-xs font-semibold text-gray-600 uppercase tracking-widest">Simple pricing</span>
          </div>
          <h1 className="text-4xl md:text-5xl font-bold tracking-tight text-gray-900 mb-5 leading-tight">
            Start free.{' '}
            <span className="bg-clip-text text-transparent"
              style={{ backgroundImage: 'linear-gradient(135deg, #f43f5e 0%, #a855f7 50%, #22d3ee 100%)' }}>
              Scale when ready.
            </span>
          </h1>
          <p className="text-lg text-gray-500 font-light leading-relaxed max-w-xl mx-auto mb-8">
            No hidden fees, no lock-in. Pick the plan that fits your brand and upgrade any time.
          </p>

          {/* Billing toggle */}
          <div className="inline-flex items-center bg-gray-100 border border-gray-200 rounded-lg p-1">
            <button
              onClick={() => setBillingCycle('monthly')}
              className={`px-5 py-2 rounded-md text-sm font-semibold transition-all ${
                billingCycle === 'monthly' ? 'bg-white text-gray-900 shadow-sm' : 'text-gray-500 hover:text-gray-700'
              }`}
            >
              Monthly
            </button>
            <button
              onClick={() => setBillingCycle('yearly')}
              className={`px-5 py-2 rounded-md text-sm font-semibold transition-all relative ${
                billingCycle === 'yearly' ? 'bg-white text-gray-900 shadow-sm' : 'text-gray-500 hover:text-gray-700'
              }`}
            >
              Annual
              <span className="ml-2 text-[10px] font-bold text-emerald-500">Save 20%</span>
            </button>
          </div>
        </div>
      </section>

      {/* ── Pricing cards ──────────────────────────────────────────────────── */}
      <section className="pb-20 px-6">
        <div className="max-w-5xl mx-auto">
          <div className="grid md:grid-cols-3 gap-5 items-start">
            {PLANS.map(plan => {
              const { display, sub } = getPrice(plan)
              const savings = getSavings(plan)
              const isFeatured = plan.ctaStyle === 'gradient'

              return (
                <div
                  key={plan.name}
                  className={`rounded-2xl bg-white p-7 flex flex-col transition-all relative overflow-hidden ${
                    isFeatured
                      ? 'border-2 shadow-xl shadow-purple-100'
                      : 'border border-gray-200 hover:shadow-md hover:border-gray-300'
                  }`}
                  style={isFeatured ? { borderColor: '#a855f7' } : {}}
                >
                  {/* Gradient top strip for featured */}
                  {isFeatured && (
                    <div className="absolute top-0 left-0 right-0 h-1 rounded-t-2xl"
                      style={{ backgroundImage: plan.gradient! }} />
                  )}

                  {/* Badge */}
                  {plan.badge && (
                    <div className="mb-4 mt-1">
                      <span className="inline-flex items-center gap-1 text-[10px] font-bold uppercase tracking-wide text-white rounded-full px-3 py-1"
                        style={{ backgroundImage: plan.gradient! }}>
                        {plan.badge}
                      </span>
                    </div>
                  )}

                  <div className={plan.badge ? '' : 'mt-1'}>
                    <h3 className="text-base font-bold text-gray-900">{plan.displayName}</h3>
                    <p className="text-xs text-gray-500 mt-1 mb-5 leading-relaxed">{plan.description}</p>
                  </div>

                  {/* Price */}
                  <div className="mb-6">
                    <div className="flex items-baseline gap-1">
                      <span className="text-4xl font-bold text-gray-900">{display}</span>
                      <span className="text-sm text-gray-500">{sub}</span>
                    </div>
                    {billingCycle === 'yearly' && savings && (
                      <p className="text-xs text-emerald-600 font-semibold mt-1">{savings} with annual billing</p>
                    )}
                    {plan.monthlyPrice === 0 && (
                      <p className="text-xs text-gray-400 mt-1">No credit card required</p>
                    )}
                    {plan.monthlyPrice > 0 && plan.ctaStyle === 'gradient' && (
                      <p className="text-xs text-purple-600 font-semibold mt-1">14-day free trial · $0 due today</p>
                    )}
                  </div>

                  {/* CTA */}
                  {plan.ctaStyle === 'gradient' ? (
                    <button
                      onClick={() => navigate('/login')}
                      className="w-full py-2.5 rounded-xl text-sm font-semibold text-white transition-all mb-6 hover:opacity-90"
                      style={{ backgroundImage: plan.gradient! }}
                    >
                      {plan.cta}
                    </button>
                  ) : (
                    <button
                      onClick={() => navigate('/login')}
                      className="w-full py-2.5 rounded-xl text-sm font-semibold bg-gray-900 text-white hover:bg-gray-800 transition-all mb-6"
                    >
                      {plan.cta}
                    </button>
                  )}

                  {/* Features */}
                  <ul className="space-y-2.5 flex-1">
                    {plan.features.map(f => (
                      <li key={f} className="flex items-start gap-2.5 text-sm text-gray-700">
                        <Check className={`w-3.5 h-3.5 flex-shrink-0 mt-0.5 ${isFeatured ? 'text-purple-500' : 'text-emerald-500'}`} />
                        {f}
                      </li>
                    ))}
                    {plan.notIncluded.map(f => (
                      <li key={f} className="flex items-start gap-2.5 text-sm text-gray-400 line-through decoration-gray-300">
                        <Check className="w-3.5 h-3.5 flex-shrink-0 mt-0.5 text-gray-300" />
                        {f}
                      </li>
                    ))}
                  </ul>
                </div>
              )
            })}
          </div>

          {/* Enterprise callout */}
          <div className="mt-5 rounded-2xl border border-gray-200 bg-gray-50 p-6 flex flex-col md:flex-row items-center justify-between gap-4">
            <div>
              <p className="text-sm font-bold text-gray-900">Need an Enterprise plan?</p>
              <p className="text-sm text-gray-500 mt-0.5">Custom limits, SSO, dedicated SLAs, and white-glove onboarding for large teams.</p>
            </div>
            <button
              onClick={() => navigate('/about')}
              className="flex-shrink-0 px-6 py-2.5 rounded-xl text-sm font-semibold bg-gray-900 text-white hover:bg-gray-800 transition-all"
            >
              Contact Sales
            </button>
          </div>
        </div>
      </section>

      {/* ── Trust strip ───────────────────────────────────────────────────── */}
      <section className="py-10 px-6 bg-gray-50 border-y border-gray-200">
        <div className="max-w-4xl mx-auto">
          <div className="grid grid-cols-2 md:grid-cols-4 gap-6 text-center">
            {[
              { value: '6 LLMs', label: 'ChatGPT, Claude, Gemini, Perplexity, DeepSeek & Grok' },
              { value: '14 days', label: 'Free trial on Pro — $0 due today' },
              { value: 'Daily', label: 'Visibility checks — not weekly averages' },
              { value: 'Cancel anytime', label: 'No contracts, no lock-in' },
            ].map(s => (
              <div key={s.label}>
                <p className="text-2xl font-bold text-gray-900">{s.value}</p>
                <p className="text-xs text-gray-500 mt-1">{s.label}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* ── Why we built this ─────────────────────────────────────────────── */}
      <section className="py-16 px-6">
        <div className="max-w-2xl mx-auto">
          <div className="rounded-2xl border border-gray-200 bg-gray-50 p-8">
            <p className="text-xs font-semibold text-gray-400 uppercase tracking-widest mb-4">Why we built this</p>
            <p className="text-base text-gray-700 leading-relaxed mb-4">
              We kept watching clients lose deals to competitors that ChatGPT and Perplexity recommended instead of them. Nobody was measuring it, let alone fixing it.
            </p>
            <p className="text-base text-gray-700 leading-relaxed">
              ProQuest AI tracks your exact position across every major LLM daily — and gives you a prioritized fix list so you move up, not just watch the numbers.
            </p>
            <p className="text-sm text-gray-500 mt-5 font-medium">— The ProQuest AI team</p>
          </div>
        </div>
      </section>

      {/* ── FAQ ───────────────────────────────────────────────────────────── */}
      <section className="py-16 px-6 bg-gray-50 border-t border-gray-200">
        <div className="max-w-3xl mx-auto">
          <div className="text-center mb-10">
            <p className="text-xs font-semibold text-gray-400 uppercase tracking-widest mb-3">Got questions?</p>
            <h2 className="text-2xl font-bold text-gray-900">Frequently asked questions</h2>
          </div>
          <div className="space-y-3">
            {FAQS.map(({ q, a }) => (
              <div key={q} className="bg-white rounded-xl border border-gray-200 p-5">
                <h3 className="text-sm font-semibold text-gray-900 mb-1.5">{q}</h3>
                <p className="text-sm text-gray-500 leading-relaxed">{a}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* ── Final CTA ──────────────────────────────────────────────────────── */}
      <section className="py-20 px-6">
        <div className="max-w-2xl mx-auto text-center">
          <h2 className="text-2xl font-bold text-gray-900 mb-3">Ready to take control of your AI presence?</h2>
          <p className="text-base text-gray-500 mb-8">Start free today — no credit card required.</p>
          <div className="flex flex-wrap items-center justify-center gap-3">
            <button
              onClick={() => navigate('/login')}
              className="px-7 py-3 rounded-full text-sm font-semibold text-white transition-all hover:opacity-90"
              style={{ backgroundImage: 'linear-gradient(135deg, #f43f5e 0%, #a855f7 50%, #22d3ee 100%)' }}
            >
              Get Started Free
            </button>
            <button
              onClick={() => navigate('/features')}
              className="px-7 py-3 rounded-full text-sm font-semibold bg-white border border-gray-200 text-gray-700 hover:border-gray-300 hover:shadow-sm transition-all"
            >
              See All Features
            </button>
          </div>
        </div>
      </section>

      <Footer />
    </div>
  )
}
