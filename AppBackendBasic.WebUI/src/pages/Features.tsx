import {
  Eye, TrendingUp, Target, BarChart3, Bell, ShieldCheck,
  Zap, Globe, GitCompare, FileText, Search, RefreshCw
} from 'lucide-react'
import Header from '../components/Header'
import Footer from '../components/Footer'
import { useWaitlist } from '../contexts/WaitlistContext'

export default function Features() {
  const { openWaitlist } = useWaitlist()

  const features = [
    {
      icon: <Eye className="w-5 h-5" />,
      title: 'Real-Time AI Monitoring',
      description: 'Track how ChatGPT, Claude, Gemini, Perplexity, DeepSeek, and Grok mention your brand — updated live, 24/7.',
      gradient: 'from-rose-500 to-pink-500',
    },
    {
      icon: <BarChart3 className="w-5 h-5" />,
      title: 'Visibility Score Dashboard',
      description: 'A single score that tells you how often your brand appears across all AI platforms. Track trends over time, by platform, and by topic.',
      gradient: 'from-purple-500 to-violet-500',
    },
    {
      icon: <Target className="w-5 h-5" />,
      title: 'Prompt Tracking',
      description: 'Track specific queries that matter to your business. See which prompts surface your brand and which give it to competitors.',
      gradient: 'from-cyan-500 to-blue-500',
    },
    {
      icon: <GitCompare className="w-5 h-5" />,
      title: 'Competitor Intelligence',
      description: 'See exactly which competitors AI platforms recommend in your category. Identify gaps and craft content to close them.',
      gradient: 'from-amber-500 to-orange-500',
    },
    {
      icon: <Search className="w-5 h-5" />,
      title: 'Citation Source Analysis',
      description: 'Discover which pages, articles, and sources AI models are citing when they mention your category. Know what to create.',
      gradient: 'from-emerald-500 to-teal-500',
    },
    {
      icon: <FileText className="w-5 h-5" />,
      title: 'AI Content Recommendations',
      description: 'Get actionable, AI-generated suggestions for content that\'s likely to improve your mention rate across major platforms.',
      gradient: 'from-indigo-500 to-purple-500',
    },
    {
      icon: <TrendingUp className="w-5 h-5" />,
      title: 'Trend Analytics',
      description: 'Visualize your visibility trajectory over days, weeks, and months. Correlate spikes with content you published.',
      gradient: 'from-rose-500 to-orange-500',
    },
    {
      icon: <Bell className="w-5 h-5" />,
      title: 'Visibility Alerts',
      description: 'Get notified instantly when your visibility score drops, a competitor surges, or a new citation source appears.',
      gradient: 'from-pink-500 to-purple-500',
    },
    {
      icon: <Globe className="w-5 h-5" />,
      title: 'Multi-Platform Coverage',
      description: 'We cover ChatGPT, Claude, Gemini, Perplexity, DeepSeek, and Grok — with new platforms added as they emerge.',
      gradient: 'from-cyan-500 to-emerald-500',
    },
    {
      icon: <RefreshCw className="w-5 h-5" />,
      title: 'Automated Checks',
      description: 'Schedule recurring visibility checks so your data is always fresh without manual effort. Daily, weekly, or custom cadences.',
      gradient: 'from-violet-500 to-blue-500',
    },
    {
      icon: <ShieldCheck className="w-5 h-5" />,
      title: 'Site SEO Audit',
      description: 'Run a full technical SEO audit on your site to find issues that may be preventing AI models from citing your content.',
      gradient: 'from-amber-500 to-rose-500',
    },
    {
      icon: <Zap className="w-5 h-5" />,
      title: 'API Access',
      description: 'Embed ProQuest AI data directly into your own dashboards, reporting tools, or client portals via our REST API.',
      gradient: 'from-teal-500 to-cyan-500',
    },
  ]

  const highlights = [
    { value: '6',       label: 'LLMs tracked from day one' },
    { value: '< 5 min', label: 'Setup — no code required' },
    { value: 'Daily',   label: 'Automated visibility checks' },
    { value: '40+',     label: 'Prompt templates per project' },
  ]

  return (
    <div className="min-h-screen bg-white">
      <Header />

      {/* ── Hero ─────────────────────────────────────────────────────────── */}
      <section className="pt-36 pb-20 px-6 relative overflow-hidden">
        <div className="absolute inset-0 pointer-events-none"
          style={{ background: 'radial-gradient(ellipse 80% 50% at 50% -10%, rgba(168,85,247,0.08) 0%, transparent 70%)' }} />
        <div className="max-w-3xl mx-auto text-center relative z-10">
          <div className="inline-flex items-center gap-2 bg-gray-50 border border-gray-200 rounded-full px-4 py-1.5 mb-6">
            <span className="w-1.5 h-1.5 rounded-full bg-purple-500 animate-pulse" />
            <span className="text-xs font-semibold text-gray-600 uppercase tracking-widest">What's inside</span>
          </div>
          <h1 className="text-4xl md:text-5xl font-bold tracking-tight text-gray-900 mb-5 leading-tight">
            Everything you need to understand — and{' '}
            <span className="bg-clip-text text-transparent"
              style={{ backgroundImage: 'linear-gradient(135deg, #f43f5e 0%, #a855f7 50%, #22d3ee 100%)' }}>
              improve
            </span>{' '}
            — how AI talks about your brand
          </h1>
          <p className="text-lg text-gray-500 font-light leading-relaxed max-w-2xl mx-auto mb-8">
            Track where you appear, why competitors outrank you, what technical gaps are hurting you, and which content changes will move the needle — across ChatGPT, Claude, Gemini, Perplexity, DeepSeek, and Grok.
          </p>
          <button
            onClick={openWaitlist}
            className="px-7 py-3 rounded-full text-sm font-semibold text-white transition-all"
            style={{ backgroundImage: 'linear-gradient(135deg, #f43f5e 0%, #a855f7 50%, #22d3ee 100%)' }}
          >
            Join the Waitlist
          </button>
        </div>
      </section>

      {/* ── Quick stats strip ─────────────────────────────────────────────── */}
      <section className="py-10 px-6 bg-gray-50 border-y border-gray-200">
        <div className="max-w-3xl mx-auto">
          <div className="grid grid-cols-2 md:grid-cols-4 gap-6 text-center">
            {highlights.map(h => (
              <div key={h.label}>
                <p className="text-2xl font-bold text-gray-900">{h.value}</p>
                <p className="text-xs text-gray-500 mt-1">{h.label}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* ── Features grid ─────────────────────────────────────────────────── */}
      <section className="py-20 px-6">
        <div className="max-w-5xl mx-auto">
          <div className="text-center mb-12">
            <p className="text-xs font-semibold text-gray-400 uppercase tracking-widest mb-3">Full feature set</p>
            <h2 className="text-2xl font-bold text-gray-900">Everything, out of the box</h2>
          </div>
          <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-5">
            {features.map(f => (
              <div key={f.title}
                className="bg-white rounded-2xl border border-gray-200 p-6 hover:shadow-md hover:border-gray-300 transition-all"
              >
                <div className={`w-9 h-9 rounded-xl bg-gradient-to-br ${f.gradient} flex items-center justify-center text-white mb-4 flex-shrink-0`}>
                  {f.icon}
                </div>
                <h3 className="text-sm font-bold text-gray-900 mb-2">{f.title}</h3>
                <p className="text-sm text-gray-500 leading-relaxed">{f.description}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* ── Platform coverage callout ──────────────────────────────────────── */}
      <section className="py-16 px-6 bg-gray-50 border-y border-gray-200">
        <div className="max-w-4xl mx-auto text-center">
          <p className="text-xs font-semibold text-gray-400 uppercase tracking-widest mb-4">Platforms we track</p>
          <div className="flex flex-wrap justify-center gap-3">
            {['ChatGPT', 'Claude', 'Gemini', 'Perplexity', 'DeepSeek', 'Grok'].map(p => (
              <span key={p} className="px-4 py-2 bg-white border border-gray-200 rounded-full text-sm font-semibold text-gray-700 shadow-sm">
                {p}
              </span>
            ))}
          </div>
          <p className="text-sm text-gray-400 mt-4">New platforms added as they emerge</p>
        </div>
      </section>

      {/* ── CTA ───────────────────────────────────────────────────────────── */}
      <section className="py-20 px-6">
        <div className="max-w-2xl mx-auto text-center">
          <h2 className="text-2xl font-bold text-gray-900 mb-3">Start tracking your AI visibility today</h2>
          <p className="text-base text-gray-500 mb-8">Set up in under 5 minutes. Free plan available, no credit card required.</p>
          <div className="flex flex-wrap items-center justify-center gap-3">
            <button
              onClick={openWaitlist}
              className="px-7 py-3 rounded-full text-sm font-semibold text-white transition-all"
              style={{ backgroundImage: 'linear-gradient(135deg, #f43f5e 0%, #a855f7 50%, #22d3ee 100%)' }}
            >
              Join the Waitlist
            </button>
          </div>
        </div>
      </section>

      <Footer />
    </div>
  )
}
