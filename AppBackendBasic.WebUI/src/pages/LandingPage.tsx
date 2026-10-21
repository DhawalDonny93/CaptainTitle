import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { CheckCircle2, XCircle, Globe, ArrowRight, TrendingUp, AlertCircle, Search } from 'lucide-react'
import Header from '../components/Header'
import Footer from '../components/Footer'
import { useAuth } from '../contexts/AuthContext'
import { useWaitlist } from '../contexts/WaitlistContext'
import { HeroSection01 } from '../components/ui/hero-01'
import { LLMLogo, type LLMName } from '../components/ui/llm-logo'

/* ─── Mock UI: Visibility Score card ─────────────────────────────────────── */
function VisibilityMockup() {
  const platforms: { name: LLMName; pct: number; color: string }[] = [
    { name: 'ChatGPT',    pct: 78, color: '#000000' },
    { name: 'Claude',     pct: 63, color: '#d77655' },
    { name: 'Gemini',     pct: 51, color: '#5684D1' },
    { name: 'Perplexity', pct: 44, color: '#25A5B8' },
  ]
  return (
    <div className="rounded-2xl border border-gray-200 bg-white shadow-xl overflow-hidden select-none">
      {/* window chrome */}
      <div className="flex items-center gap-1.5 px-4 py-3 border-b border-gray-100 bg-gray-50">
        <span className="w-3 h-3 rounded-full bg-red-400" />
        <span className="w-3 h-3 rounded-full bg-yellow-400" />
        <span className="w-3 h-3 rounded-full bg-green-400" />
        <span className="ml-3 text-xs text-gray-400 font-mono">ProQuest AI — Visibility Dashboard</span>
      </div>
      <div className="p-6">
        {/* header row */}
        <div className="flex items-center justify-between mb-5">
          <div>
            <p className="text-xs font-medium text-gray-500 uppercase tracking-wider">Overall Score</p>
            <p className="text-4xl font-bold text-gray-900 leading-none mt-1">74<span className="text-lg text-gray-400">/100</span></p>
          </div>
          <span className="inline-flex items-center gap-1 text-xs font-semibold text-emerald-700 bg-emerald-50 border border-emerald-200 rounded-full px-3 py-1">
            <TrendingUp className="w-3 h-3" /> +23% this month
          </span>
        </div>
        {/* bars */}
        <div className="space-y-3">
          {platforms.map(p => (
            <div key={p.name}>
              <div className="flex justify-between text-sm mb-1">
                <span className="font-medium text-gray-700 flex items-center gap-2">
                  <LLMLogo name={p.name} size="sm" />
                  {p.name}
                </span>
                <span className="font-semibold text-gray-900">{p.pct}%</span>
              </div>
              <div className="h-2 rounded-full bg-gray-100 overflow-hidden">
                <div
                  className="h-full rounded-full transition-all"
                  style={{ width: `${p.pct}%`, backgroundColor: p.color }}
                />
              </div>
            </div>
          ))}
        </div>
        {/* footer badge */}
        <div className="mt-5 pt-4 border-t border-gray-100 flex items-center gap-2">
          <span className="w-2 h-2 rounded-full bg-emerald-400 animate-pulse" />
          <span className="text-xs text-gray-500">Live — updated 2 min ago</span>
        </div>
      </div>
    </div>
  )
}

/* ─── Mock UI: Prompt Tracking list ─────────────────────────────────────── */
function PromptMockup() {
  const prompts = [
    { text: '"best AI SEO monitoring tool"',   results: [{ p: 'ChatGPT', rank: 2, found: true }, { p: 'Claude', rank: 4, found: true }, { p: 'Gemini', rank: null, found: false }] },
    { text: '"AI visibility tracking software"', results: [{ p: 'ChatGPT', rank: 1, found: true }, { p: 'Claude', rank: 1, found: true }, { p: 'Perplexity', rank: 2, found: true }] },
    { text: '"how to rank in ChatGPT answers"',  results: [{ p: 'ChatGPT', rank: null, found: false }, { p: 'Gemini', rank: 3, found: true }, { p: 'Perplexity', rank: null, found: false }] },
  ]
  const colors: Record<string, string> = { ChatGPT: '#000000', Claude: '#d77655', Gemini: '#5684D1', Perplexity: '#25A5B8' }
  return (
    <div className="rounded-2xl border border-gray-200 bg-white shadow-xl overflow-hidden select-none">
      <div className="flex items-center gap-1.5 px-4 py-3 border-b border-gray-100 bg-gray-50">
        <span className="w-3 h-3 rounded-full bg-red-400" />
        <span className="w-3 h-3 rounded-full bg-yellow-400" />
        <span className="w-3 h-3 rounded-full bg-green-400" />
        <span className="ml-3 text-xs text-gray-400 font-mono">ProQuest AI — Prompt Tracker</span>
      </div>
      <div className="divide-y divide-gray-100">
        {prompts.map((prompt, i) => (
          <div key={i} className="px-5 py-4">
            <div className="flex items-start gap-2 mb-3">
              <Search className="w-4 h-4 text-gray-400 mt-0.5 flex-shrink-0" />
              <p className="text-sm font-medium text-gray-800">{prompt.text}</p>
            </div>
            <div className="flex flex-wrap gap-2 pl-6">
              {prompt.results.map(r => (
                <span
                  key={r.p}
                  className="inline-flex items-center gap-1.5 text-xs rounded-full px-2.5 py-1 font-medium border"
                  style={r.found
                    ? { backgroundColor: colors[r.p] + '15', color: colors[r.p], borderColor: colors[r.p] + '40' }
                    : { backgroundColor: '#f3f4f6', color: '#9ca3af', borderColor: '#e5e7eb' }
                  }
                >
                  {r.found ? <CheckCircle2 className="w-3 h-3" /> : <XCircle className="w-3 h-3" />}
                  <LLMLogo name={r.p as LLMName} size="sm" />
                  {r.p}{r.rank ? ` #${r.rank}` : ''}
                </span>
              ))}
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}

/* ─── Mock UI: Site Audit card ───────────────────────────────────────────── */
function AuditMockup() {
  const checks = [
    { label: 'llms.txt file detected',           pass: true },
    { label: 'Schema markup valid',              pass: true },
    { label: 'robots.txt allows AI crawlers',    pass: false },
    { label: 'FAQ structured data present',       pass: false },
    { label: 'Page speed ≥ 90 (mobile)',         pass: true },
    { label: 'Canonical tags correct',           pass: true },
  ]
  const score = Math.round((checks.filter(c => c.pass).length / checks.length) * 100)
  return (
    <div className="rounded-2xl border border-gray-200 bg-white shadow-xl overflow-hidden select-none">
      <div className="flex items-center gap-1.5 px-4 py-3 border-b border-gray-100 bg-gray-50">
        <span className="w-3 h-3 rounded-full bg-red-400" />
        <span className="w-3 h-3 rounded-full bg-yellow-400" />
        <span className="w-3 h-3 rounded-full bg-green-400" />
        <span className="ml-3 text-xs text-gray-400 font-mono">ProQuest AI — Site Audit</span>
      </div>
      <div className="p-5">
        {/* score ring placeholder */}
        <div className="flex items-center gap-4 mb-5 pb-5 border-b border-gray-100">
          <div className="w-16 h-16 rounded-full border-4 border-amber-400 flex items-center justify-center flex-shrink-0">
            <span className="text-xl font-bold text-gray-900">{score}</span>
          </div>
          <div>
            <p className="font-semibold text-gray-900">AI Readiness Score</p>
            <p className="text-sm text-amber-600 flex items-center gap-1 mt-0.5">
              <AlertCircle className="w-3.5 h-3.5" /> 2 critical issues found
            </p>
          </div>
        </div>
        {/* checklist */}
        <div className="space-y-2.5">
          {checks.map((c, i) => (
            <div key={i} className="flex items-center gap-2.5">
              {c.pass
                ? <CheckCircle2 className="w-4 h-4 text-emerald-500 flex-shrink-0" />
                : <XCircle className="w-4 h-4 text-red-500 flex-shrink-0" />
              }
              <span className={`text-sm ${c.pass ? 'text-gray-700' : 'text-red-700 font-medium'}`}>{c.label}</span>
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}

/* ─── Mock UI: Content Generator ────────────────────────────────────────── */
function ContentGeneratorMockup() {
  const suggestions = [
    { title: 'How to Optimize for AI-Generated Answers', platform: 'ChatGPT', relevance: 94, status: 'ready' },
    { title: 'Complete Guide: AI Visibility Best Practices', platform: 'Claude', relevance: 87, status: 'ready' },
    { title: 'Why AI Models Cite Your Competitors Over You', platform: 'Perplexity', relevance: 79, status: 'draft' },
  ]
  const platformColors: Record<string, string> = { ChatGPT: '#000000', Claude: '#d77655', Perplexity: '#25A5B8' }

  return (
    <div className="rounded-2xl border border-gray-200 bg-white shadow-xl overflow-hidden select-none">
      <div className="flex items-center gap-1.5 px-4 py-3 border-b border-gray-100 bg-gray-50">
        <span className="w-3 h-3 rounded-full bg-red-400" />
        <span className="w-3 h-3 rounded-full bg-yellow-400" />
        <span className="w-3 h-3 rounded-full bg-green-400" />
        <span className="ml-3 text-xs text-gray-400 font-mono">ProQuest AI — Content Recommendations</span>
      </div>
      <div className="divide-y divide-gray-100">
        {suggestions.map((s, i) => (
          <div key={i} className="px-5 py-4">
            <div className="flex items-start justify-between mb-3">
              <div className="flex-1">
                <p className="font-semibold text-gray-900 text-sm mb-1">{s.title}</p>
                <div className="flex items-center gap-2">
                  <span className="inline-flex items-center gap-1 text-xs rounded-full px-2.5 py-1 border"
                    style={{ backgroundColor: platformColors[s.platform] + '15', color: platformColors[s.platform], borderColor: platformColors[s.platform] + '40' }}>
                    <LLMLogo name={s.platform as LLMName} size="sm" />
                    {s.platform}
                  </span>
                </div>
              </div>
              <div className="text-right flex-shrink-0">
                <div className="text-2xl font-bold text-blue-600">{s.relevance}<span className="text-xs text-gray-500">%</span></div>
                <span className={`inline-block text-xs font-medium px-2 py-0.5 rounded ${s.status === 'ready' ? 'bg-emerald-100 text-emerald-700' : 'bg-amber-100 text-amber-700'}`}>
                  {s.status}
                </span>
              </div>
            </div>
            <div className="flex items-center gap-2">
              <div className="flex-1 h-1.5 bg-gray-100 rounded-full overflow-hidden">
                <div className="h-full bg-blue-500 rounded-full" style={{ width: `${s.relevance}%` }} />
              </div>
            </div>
          </div>
        ))}
      </div>
      <div className="px-5 py-3 bg-blue-50 border-t border-blue-100 flex items-center justify-between">
        <span className="text-xs text-blue-700 font-medium">Generated automatically based on visibility gaps →</span>
        <span className="text-xs font-semibold text-blue-600 cursor-pointer hover:text-blue-700">View All</span>
      </div>
    </div>
  )
}

/* ─── Mock UI: LLM Ranking Improvement ───────────────────────────────────── */
function RankingMockup() {
  const maxRank = 15
  const platforms: { name: LLMName; before: number; after: number; color: string }[] = [
    { name: 'ChatGPT',    before: 9,  after: 2, color: '#000000' },
    { name: 'Claude',     before: 11, after: 3, color: '#d77655' },
    { name: 'Gemini',     before: 7,  after: 1, color: '#5684D1' },
    { name: 'Perplexity', before: 14, after: 4, color: '#25A5B8' },
  ]
  return (
    <div className="rounded-2xl border border-gray-200 bg-white shadow-xl overflow-hidden select-none">
      <div className="flex items-center gap-1.5 px-4 py-3 border-b border-gray-100 bg-gray-50">
        <span className="w-3 h-3 rounded-full bg-red-400" />
        <span className="w-3 h-3 rounded-full bg-yellow-400" />
        <span className="w-3 h-3 rounded-full bg-green-400" />
        <span className="ml-3 text-xs text-gray-400 font-mono">ProQuest AI — LLM Rank Optimizer</span>
      </div>
      <div className="p-6">
        <div className="flex items-center justify-between mb-5">
          <div>
            <p className="text-xs font-medium text-gray-500 uppercase tracking-wider">Avg. Rank Improvement</p>
            <p className="text-4xl font-bold text-gray-900 leading-none mt-1">+8.5 <span className="text-lg text-gray-400">positions</span></p>
          </div>
          <span className="inline-flex items-center gap-1 text-xs font-semibold text-violet-700 bg-violet-50 border border-violet-200 rounded-full px-3 py-1">
            <TrendingUp className="w-3 h-3" /> 60-day result
          </span>
        </div>
        <div className="space-y-4">
          {platforms.map(p => (
            <div key={p.name}>
              <div className="flex items-center justify-between mb-1.5">
                <span className="font-medium text-gray-700 flex items-center gap-2 text-sm">
                  <LLMLogo name={p.name} size="sm" />
                  {p.name}
                </span>
                <div className="flex items-center gap-2 text-sm">
                  <span className="text-gray-400 text-xs">was #{p.before}</span>
                  <ArrowRight className="w-3 h-3 text-violet-400" />
                  <span className="font-bold text-violet-600">now #{p.after}</span>
                </div>
              </div>
              <div className="relative h-2 rounded-full bg-gray-100 overflow-hidden">
                <div
                  className="absolute top-0 left-0 h-full rounded-full opacity-25"
                  style={{ width: `${((maxRank - p.before) / maxRank) * 100}%`, backgroundColor: p.color }}
                />
                <div
                  className="absolute top-0 left-0 h-full rounded-full"
                  style={{ width: `${((maxRank - p.after) / maxRank) * 100}%`, backgroundColor: p.color }}
                />
              </div>
            </div>
          ))}
        </div>
        <div className="mt-5 pt-4 border-t border-gray-100 flex items-center gap-2">
          <span className="w-2 h-2 rounded-full bg-violet-400 animate-pulse" />
          <span className="text-xs text-gray-500">Rankings updated daily · 14 optimizations applied</span>
        </div>
      </div>
    </div>
  )
}

/* ═══════════════════════════════════════════════════════════════════════════ */
export default function LandingPage() {
  const [scanUrl, setScanUrl] = useState('')
  const navigate = useNavigate()
  const { isAuthenticated } = useAuth()
  const { openWaitlist } = useWaitlist()

  const handleScan = () => {
    const url = scanUrl.trim()
    if (isAuthenticated && url) navigate(`/projects?scanUrl=${encodeURIComponent(url)}`)
    else if (isAuthenticated)   navigate('/projects')
    else                        openWaitlist()
  }

  return (
    <div className="min-h-screen bg-white text-gray-900" style={{ fontFamily: "'Inter', system-ui, sans-serif" }}>
      <Header />

      {/* ── NEW HERO SECTION ──────────────────────────────────────────────── */}
      <HeroSection01 />

      {/* ── PLATFORMS STRIP with Marquee ───────────────────────────────── */}
      <section className="py-16 px-6 border-y border-gray-100 bg-white overflow-hidden">
        <div className="max-w-5xl mx-auto">
          <p className="text-center text-xs font-semibold uppercase tracking-widest text-gray-400 mb-10">
            Monitoring brand visibility across
          </p>
          {/* Marquee — single flex row with items repeated 4× so it always overfills the viewport.
              Animates translateX(0) → translateX(-50%): the second half is identical to the
              first, so the snap-back is pixel-perfect with no visible jump. */}
          <div className="overflow-hidden">
            <style>{`
              @keyframes marquee-scroll {
                0%   { transform: translateX(0); }
                100% { transform: translateX(-50%); }
              }
              .marquee-inner {
                display: flex;
                gap: 1rem;
                width: max-content;
                animation: marquee-scroll 22s linear infinite;
              }
              .marquee-inner:hover {
                animation-play-state: paused;
              }
            `}</style>
            <div className="marquee-inner">
              {Array.from({ length: 4 }).flatMap((_, copy) =>
                [
                  { id: 'openai',            src: '/images/platform-logos/openai.svg',           bg: '#f9fafb', border: '#e5e7eb', h: 20 },
                  { id: 'claude',            src: '/images/platform-logos/claude.svg',           bg: '#fff7f4', border: '#fde8df', h: 16 },
                  { id: 'gemini',            src: '/images/platform-logos/gemini.svg',           bg: '#f0f4ff', border: '#dbe4fe', h: 20 },
                  { id: 'deepseek',          src: '/images/platform-logos/deepseek.svg',         bg: '#f0f2ff', border: '#dde3fe', h: 20 },
                  { id: 'google-ai-overview',src: '/images/platform-logos/google-ai-overview.svg', bg: '#f8f8ff', border: '#e8eaff', h: 24 },
                  { id: 'grok',              src: '/images/platform-logos/grok.svg',             bg: '#f9fafb', border: '#e5e7eb', h: 20 },
                ].map(p => (
                  <div
                    key={`${p.id}-${copy}`}
                    className="flex items-center justify-center rounded-2xl py-4 px-7 flex-shrink-0"
                    style={{ background: p.bg, border: `1.5px solid ${p.border}` }}
                  >
                    <img
                      src={p.src}
                      alt={p.id}
                      style={{ height: p.h, width: 'auto', display: 'block' }}
                      loading="lazy"
                    />
                  </div>
                ))
              )}
            </div>
          </div>
        </div>
      </section>

      {/* ── FEATURE 0: LLM Rank Optimization ──────────────────────────────── */}
      <section className="py-28 px-6 bg-gray-50">
        <div className="max-w-6xl mx-auto grid lg:grid-cols-2 gap-20 items-center">
          <div className="order-2 lg:order-1">
            <RankingMockup />
          </div>
          <div className="order-1 lg:order-2">
            <p className="text-sm font-semibold text-violet-600 uppercase tracking-widest mb-4">LLM rank optimization</p>
            <h2 className="text-4xl font-extrabold leading-tight mb-5">
              Move your product from{' '}
              <span
                className="bg-clip-text text-transparent"
                style={{ backgroundImage: 'linear-gradient(135deg, #7c3aed 0%, #a855f7 100%)' }}
              >
                ignored to recommended
              </span>{' '}
              by AI
            </h2>
            <p className="text-lg text-gray-500 leading-relaxed mb-8">
              ProQuest AI reverse-engineers what makes a brand rank at the top of ChatGPT, Claude, Gemini, and Perplexity answers. Get a prioritized, step-by-step action plan so every optimization directly lifts your position in AI-generated responses.
            </p>
            <ul className="space-y-3 mb-10">
              {[
                'Pinpoint exactly why competitors outrank you',
                'Get a ranked fix list ordered by ranking impact',
                'Track position changes after every optimization',
                'Platform-specific strategies for each LLM',
              ].map(t => (
                <li key={t} className="flex items-center gap-3 text-gray-700">
                  <CheckCircle2 className="w-5 h-5 text-violet-500 flex-shrink-0" />{t}
                </li>
              ))}
            </ul>
            <button
              onClick={() => openWaitlist()}
              className="inline-flex items-center gap-2 px-6 py-3 rounded-xl bg-gray-900 hover:bg-gray-700 text-white font-semibold transition-colors"
            >
              Boost my LLM ranking <ArrowRight className="w-4 h-4" />
            </button>
          </div>
        </div>
      </section>

      {/* ── FEATURE 1 ─────────────────────────────────────────────────────── */}
      <section className="py-28 px-6">
        <div className="max-w-6xl mx-auto grid lg:grid-cols-2 gap-20 items-center">
          <div>
            <p className="text-sm font-semibold text-emerald-600 uppercase tracking-widest mb-4">Visibility tracking</p>
            <h2 className="text-4xl font-extrabold leading-tight mb-5">
              See exactly where you{' '}
              <span
                className="bg-clip-text text-transparent"
                style={{ backgroundImage: 'linear-gradient(135deg, #10b981 0%, #06b6d4 100%)' }}
              >
                stand
              </span>{' '}
              across every AI platform
            </h2>
            <p className="text-lg text-gray-500 leading-relaxed mb-8">
              Get a live score showing how often your brand appears in <span className="inline-flex items-center gap-1"><LLMLogo name="ChatGPT" size="md" /> ChatGPT</span>, <span className="inline-flex items-center gap-1"><LLMLogo name="Claude" size="md" /> Claude</span>, <span className="inline-flex items-center gap-1"><LLMLogo name="Gemini" size="md" /> Gemini</span>, <span className="inline-flex items-center gap-1"><LLMLogo name="Perplexity" size="md" /> Perplexity</span>, and more — and how you stack up against your top competitors.
            </p>
            <ul className="space-y-3 mb-10">
              {['Real-time scores updated every 24 hours', 'Side-by-side competitor comparison', 'Platform-by-platform breakdown', 'Week-over-week trend tracking'].map(t => (
                <li key={t} className="flex items-center gap-3 text-gray-700">
                  <CheckCircle2 className="w-5 h-5 text-emerald-500 flex-shrink-0" />{t}
                </li>
              ))}
            </ul>
            <button
              onClick={() => openWaitlist()}
              className="inline-flex items-center gap-2 px-6 py-3 rounded-xl bg-gray-900 hover:bg-gray-700 text-white font-semibold transition-colors"
            >
              Start free audit <ArrowRight className="w-4 h-4" />
            </button>
          </div>
          <VisibilityMockup />
        </div>
      </section>

      {/* ── FEATURE 2 ─────────────────────────────────────────────────────── */}
      <section className="py-28 px-6 bg-gray-50">
        <div className="max-w-6xl mx-auto grid lg:grid-cols-2 gap-20 items-center">
          <div className="order-2 lg:order-1">
            <PromptMockup />
          </div>
          <div className="order-1 lg:order-2">
            <p className="text-sm font-semibold text-orange-500 uppercase tracking-widest mb-4">Prompt intelligence</p>
            <h2 className="text-4xl font-extrabold leading-tight mb-5">
              Track which AI queries{' '}
              <span
                className="bg-clip-text text-transparent"
                style={{ backgroundImage: 'linear-gradient(135deg, #f59e0b 0%, #f43f5e 100%)' }}
              >
                mention you
              </span>{' '}
              — and which don't
            </h2>
            <p className="text-lg text-gray-500 leading-relaxed mb-8">
              Monitor 40+ prompts across all major LLMs. See your ranking for each query, identify where competitors beat you, and discover the prompts that could bring you the most visibility if you ranked higher.
            </p>
            <ul className="space-y-3 mb-10">
              {['40+ tracked prompts refreshed daily', 'Rank positions per platform', 'Instant gap identification', 'Content recommendations per query'].map(t => (
                <li key={t} className="flex items-center gap-3 text-gray-700">
                  <CheckCircle2 className="w-5 h-5 text-orange-400 flex-shrink-0" />{t}
                </li>
              ))}
            </ul>
            <button
              onClick={() => openWaitlist()}
              className="inline-flex items-center gap-2 px-6 py-3 rounded-xl bg-gray-900 hover:bg-gray-700 text-white font-semibold transition-colors"
            >
              Start tracking prompts <ArrowRight className="w-4 h-4" />
            </button>
          </div>
        </div>
      </section>

      {/* ── FEATURE 3 ─────────────────────────────────────────────────────── */}
      <section className="py-28 px-6">
        <div className="max-w-6xl mx-auto grid lg:grid-cols-2 gap-20 items-center">
          <div>
            <p className="text-sm font-semibold text-blue-500 uppercase tracking-widest mb-4">Technical audit</p>
            <h2 className="text-4xl font-extrabold leading-tight mb-5">
              Find out why AI crawlers might be{' '}
              <span
                className="bg-clip-text text-transparent"
                style={{ backgroundImage: 'linear-gradient(135deg, #3b82f6 0%, #8b5cf6 100%)' }}
              >
                ignoring
              </span>{' '}
              your site
            </h2>
            <p className="text-lg text-gray-500 leading-relaxed mb-8">
              Run a full technical audit to check if your website is configured to be discovered and cited by AI. We check llms.txt, robots.txt, structured data, page speed, and more — then tell you exactly what to fix.
            </p>
            <ul className="space-y-3 mb-10">
              {['llms.txt & robots.txt validation', 'Schema and structured data check', 'AI-crawlability scoring', 'Prioritised fix recommendations'].map(t => (
                <li key={t} className="flex items-center gap-3 text-gray-700">
                  <CheckCircle2 className="w-5 h-5 text-blue-400 flex-shrink-0" />{t}
                </li>
              ))}
            </ul>
            <button
              onClick={() => openWaitlist()}
              className="inline-flex items-center gap-2 px-6 py-3 rounded-xl bg-gray-900 hover:bg-gray-700 text-white font-semibold transition-colors"
            >
              Audit my site free <ArrowRight className="w-4 h-4" />
            </button>
          </div>
          <AuditMockup />
        </div>
      </section>

      {/* ── FEATURE 4 ─────────────────────────────────────────────────────── */}
      <section className="py-28 px-6 bg-gray-50">
        <div className="max-w-6xl mx-auto grid lg:grid-cols-2 gap-20 items-center">
          <div className="order-2 lg:order-1">
            <ContentGeneratorMockup />
          </div>
          <div className="order-1 lg:order-2">
            <p className="text-sm font-semibold text-indigo-600 uppercase tracking-widest mb-4">Content recommendations</p>
            <h2 className="text-4xl font-extrabold leading-tight mb-5">
              Get AI-powered{' '}
              <span
                className="bg-clip-text text-transparent"
                style={{ backgroundImage: 'linear-gradient(135deg, #6366f1 0%, #8b5cf6 100%)' }}
              >
                content ideas
              </span>{' '}
              instantly
            </h2>
            <p className="text-lg text-gray-500 leading-relaxed mb-8">
              Stop guessing what content will help you rank in AI. Our system automatically generates personalized content recommendations based on your visibility gaps across all major LLMs, with relevance scores and platform targeting.
            </p>
            <ul className="space-y-3 mb-10">
              {['AI-generated content suggestions', 'Relevance scores for each idea', 'Platform-specific recommendations', 'Visibility gap analysis'].map(t => (
                <li key={t} className="flex items-center gap-3 text-gray-700">
                  <CheckCircle2 className="w-5 h-5 text-indigo-500 flex-shrink-0" />{t}
                </li>
              ))}
            </ul>
            <button
              onClick={() => openWaitlist()}
              className="inline-flex items-center gap-2 px-6 py-3 rounded-xl bg-gray-900 hover:bg-gray-700 text-white font-semibold transition-colors"
            >
              See recommendations <ArrowRight className="w-4 h-4" />
            </button>
          </div>
        </div>
      </section>

      {/* ── FINAL CTA ─────────────────────────────────────────────────────── */}
      <section
        className="py-28 px-6 text-center"
        style={{ background: 'radial-gradient(ellipse 80% 70% at 50% 100%, #f0fdf4 0%, #ffffff 65%)' }}
      >
        <div
          className="absolute inset-0 pointer-events-none opacity-30"
          style={{
            backgroundImage: 'radial-gradient(circle, #d1d5db 1px, transparent 1px)',
            backgroundSize: '28px 28px',
          }}
        />
        <div className="relative max-w-2xl mx-auto">
          <h2 className="text-5xl font-extrabold tracking-tight mb-5">
            Find out where AI ranks{' '}
            <span
              className="bg-clip-text text-transparent"
              style={{ backgroundImage: 'linear-gradient(135deg, #f43f5e 0%, #a855f7 100%)' }}
            >
              your brand
            </span>{' '}
            right now.
          </h2>
          <p className="text-xl text-gray-500 mb-10">
            Enter your site, see which LLMs mention you, where your competitors outrank you, and exactly what to fix — in one report.
          </p>

          <div className="max-w-md mx-auto">
            <div className="flex items-center gap-2 rounded-2xl border-2 border-gray-200 bg-white px-4 py-2 shadow-md focus-within:border-emerald-400 transition-colors mb-3">
              <Globe className="w-5 h-5 text-gray-400 flex-shrink-0" />
              <input
                type="url"
                placeholder="yourcompany.com"
                value={scanUrl}
                onChange={e => setScanUrl(e.target.value)}
                onKeyDown={e => e.key === 'Enter' && handleScan()}
                className="flex-1 py-2 text-base outline-none placeholder-gray-400 bg-transparent"
              />
              <button
                onClick={handleScan}
                className="flex-shrink-0 px-5 py-2 rounded-xl bg-gray-900 hover:bg-gray-700 text-white font-semibold text-sm transition-colors"
              >
                Get early access →
              </button>
            </div>
            <div className="flex items-center justify-center gap-6 text-sm text-gray-500">
              <span className="flex items-center gap-1.5"><CheckCircle2 className="w-4 h-4 text-emerald-500" />Free plan available</span>
              <span className="flex items-center gap-1.5"><CheckCircle2 className="w-4 h-4 text-emerald-500" />No credit card needed</span>
            </div>
          </div>
        </div>
      </section>

      <Footer />
    </div>
  )
}
