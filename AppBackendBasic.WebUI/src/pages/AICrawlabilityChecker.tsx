import { useState } from 'react'
import { Search, CheckCircle2, XCircle, AlertCircle, Shield, ExternalLink, Globe } from 'lucide-react'
import Header from '../components/Header'
import Footer from '../components/Footer'
import { proxiedFetch } from '../utils/proxiedFetch'

interface BotResult {
  name: string
  company: string
  userAgent: string
  allowed: boolean | null
  rules: string[]
  color: string
  logo: string
}

const AI_BOTS = [
  { name: 'GPTBot',             company: 'OpenAI / ChatGPT',     userAgent: 'GPTBot',             color: '#000000', logo: '/images/llm-icons/openai.svg' },
  { name: 'ChatGPT-User',       company: 'OpenAI / ChatGPT',     userAgent: 'ChatGPT-User',        color: '#000000', logo: '/images/llm-icons/openai.svg' },
  { name: 'ClaudeBot',          company: 'Anthropic / Claude',   userAgent: 'ClaudeBot',           color: '#d77655', logo: '/images/llm-icons/claude.svg' },
  { name: 'anthropic-ai',       company: 'Anthropic / Claude',   userAgent: 'anthropic-ai',        color: '#d77655', logo: '/images/llm-icons/claude.svg' },
  { name: 'PerplexityBot',      company: 'Perplexity AI',        userAgent: 'PerplexityBot',       color: '#25A5B8', logo: '/images/llm-icons/perplexity.svg' },
  { name: 'Googlebot-Extended', company: 'Google AI Overview',   userAgent: 'Googlebot-Extended',  color: '#4285F4', logo: '/images/llm-icons/google.svg' },
  { name: 'CCBot',              company: 'Common Crawl (AI training)', userAgent: 'CCBot',          color: '#6366f1', logo: '' },
  { name: 'Bytespider',         company: 'ByteDance / TikTok',   userAgent: 'Bytespider',          color: '#111827', logo: '' },
]

interface ParsedRobots {
  globalDisallows: string[]
  botSections: Record<string, { allows: string[]; disallows: string[] }>
  raw: string
}

function parseRobotsTxt(text: string): ParsedRobots {
  const lines = text.split('\n').map(l => l.trim())
  const botSections: Record<string, { allows: string[]; disallows: string[] }> = {}
  const globalDisallows: string[] = []
  let currentAgents: string[] = []

  for (const line of lines) {
    if (line.startsWith('#') || line === '') {
      if (line === '' && currentAgents.length > 0) {
        currentAgents = []
      }
      continue
    }

    const [key, ...rest] = line.split(':')
    const value = rest.join(':').trim()
    const directive = key.trim().toLowerCase()

    if (directive === 'user-agent') {
      currentAgents.push(value.toLowerCase())
    } else if (directive === 'disallow') {
      for (const agent of currentAgents) {
        if (agent === '*') globalDisallows.push(value)
        if (!botSections[agent]) botSections[agent] = { allows: [], disallows: [] }
        botSections[agent].disallows.push(value)
      }
    } else if (directive === 'allow') {
      for (const agent of currentAgents) {
        if (!botSections[agent]) botSections[agent] = { allows: [], disallows: [] }
        botSections[agent].allows.push(value)
      }
    }
  }

  return { globalDisallows, botSections, raw: text }
}

function isBotBlocked(bot: typeof AI_BOTS[0], parsed: ParsedRobots): { allowed: boolean; rules: string[] } {
  const agentKey = bot.userAgent.toLowerCase()
  const section = parsed.botSections[agentKey]
  const globalSection = parsed.botSections['*']

  const rules: string[] = []
  let blocked = false

  // Check bot-specific rules first (override global)
  if (section) {
    const fullBlock = section.disallows.some(d => d === '/' || d === '')
    if (fullBlock) {
      rules.push(`User-agent: ${bot.userAgent}`)
      rules.push(`Disallow: ${section.disallows.find(d => d === '/' || d === '')}`)
      blocked = true
    } else if (section.disallows.length > 0 && section.allows.length === 0) {
      rules.push(`User-agent: ${bot.userAgent}`)
      section.disallows.forEach(d => rules.push(`Disallow: ${d}`))
      blocked = true
    } else {
      // Has explicit allow — allowed
      return { allowed: true, rules: [`User-agent: ${bot.userAgent}`, ...section.allows.map(a => `Allow: ${a}`)] }
    }
  } else if (globalSection) {
    // Fall through to global rules
    const fullBlock = globalSection.disallows.some(d => d === '/' || d === '')
    if (fullBlock) {
      rules.push(`User-agent: *`)
      rules.push(`Disallow: /`)
      blocked = true
    }
  }

  return { allowed: !blocked, rules }
}

function normalizeUrl(input: string): string {
  let url = input.trim()
  if (!url.startsWith('http://') && !url.startsWith('https://')) {
    url = 'https://' + url
  }
  try {
    const u = new URL(url)
    return `${u.protocol}//${u.hostname}`
  } catch {
    return url
  }
}

export default function AICrawlabilityChecker() {
  const [domain, setDomain] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [results, setResults] = useState<BotResult[] | null>(null)
  const [rawRobots, setRawRobots] = useState('')
  const [showRaw, setShowRaw] = useState(false)
  const [checkedDomain, setCheckedDomain] = useState('')
  const [noRobots, setNoRobots] = useState(false)

  async function handleCheck() {
    if (!domain.trim()) return
    setError('')
    setResults(null)
    setRawRobots('')
    setNoRobots(false)
    setLoading(true)

    try {
      const base = normalizeUrl(domain)
      const robotsUrl = `${base}/robots.txt`
      setCheckedDomain(base)

      let text = ''
      try {
        text = await proxiedFetch(robotsUrl)
      } catch {
        // No robots.txt — treat as "allow all"
        setNoRobots(true)
      }
      const parsed = parseRobotsTxt(text)
      setRawRobots(text)

      const botResults: BotResult[] = AI_BOTS.map(bot => {
        const { allowed, rules } = isBotBlocked(bot, parsed)
        return {
          name: bot.name,
          company: bot.company,
          userAgent: bot.userAgent,
          allowed,
          rules,
          color: bot.color,
          logo: bot.logo,
        }
      })

      setResults(botResults)
    } catch (e: any) {
      setError(e.message || 'Failed to fetch robots.txt')
    } finally {
      setLoading(false)
    }
  }

  const blockedCount = results?.filter(r => !r.allowed).length ?? 0
  const allowedCount = results?.filter(r => r.allowed).length ?? 0

  return (
    <div className="min-h-screen bg-white text-gray-900" style={{ fontFamily: "'Inter', system-ui, sans-serif" }}>
      <Header />

      {/* Page header */}
      <div className="pt-32 pb-12 px-6 text-center bg-gradient-to-b from-gray-50 to-white border-b border-gray-100">
        <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-blue-50 border border-blue-200 text-blue-700 text-xs font-semibold uppercase tracking-wider mb-5">
          <Shield className="w-3.5 h-3.5" /> Free Tool
        </div>
        <h1 className="text-4xl md:text-5xl font-extrabold tracking-tight mb-4">
          AI Crawlability Checker
        </h1>
        <p className="text-lg text-gray-500 max-w-2xl mx-auto">
          Enter your domain and instantly see which AI crawlers your <code className="text-sm bg-gray-100 px-1.5 py-0.5 rounded font-mono">robots.txt</code> is blocking — and which ones can index your content.
        </p>
      </div>

      {/* Input */}
      <div className="max-w-2xl mx-auto px-6 py-10">
        <div className="flex gap-3">
          <div className="flex-1 flex items-center gap-2 rounded-2xl border-2 border-gray-200 bg-white px-4 py-2.5 shadow-sm focus-within:border-blue-400 transition-colors">
            <Globe className="w-5 h-5 text-gray-400 flex-shrink-0" />
            <input
              type="text"
              value={domain}
              onChange={e => setDomain(e.target.value)}
              onKeyDown={e => e.key === 'Enter' && handleCheck()}
              placeholder="yourcompany.com"
              className="flex-1 text-base outline-none placeholder-gray-400 bg-transparent"
            />
          </div>
          <button
            onClick={handleCheck}
            disabled={loading || !domain.trim()}
            className="flex items-center gap-2 px-6 py-2.5 rounded-2xl bg-gray-900 hover:bg-gray-700 disabled:opacity-50 text-white font-semibold text-sm transition-colors whitespace-nowrap"
          >
            {loading ? (
              <>
                <svg className="animate-spin w-4 h-4" viewBox="0 0 24 24" fill="none">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z" />
                </svg>
                Checking…
              </>
            ) : (
              <>
                <Search className="w-4 h-4" /> Check
              </>
            )}
          </button>
        </div>

        {error && (
          <div className="mt-4 flex items-start gap-2 p-4 rounded-xl bg-red-50 border border-red-200 text-red-700 text-sm">
            <AlertCircle className="w-4 h-4 flex-shrink-0 mt-0.5" />
            {error}
          </div>
        )}
      </div>

      {/* Results */}
      {results && (
        <div className="max-w-4xl mx-auto px-6 pb-16 space-y-8">

          {/* No robots.txt notice */}
          {noRobots && (
            <div className="flex items-start gap-3 p-4 rounded-xl bg-emerald-50 border border-emerald-200">
              <CheckCircle2 className="w-5 h-5 text-emerald-600 flex-shrink-0 mt-0.5" />
              <div>
                <p className="text-sm font-semibold text-emerald-800">No robots.txt found</p>
                <p className="text-sm text-emerald-700 mt-1">
                  No <code className="font-mono bg-emerald-100 px-1 rounded text-xs">robots.txt</code> was found at this domain — all crawlers are permitted by default.
                </p>
              </div>
            </div>
          )}

          {/* Summary bar */}
          <div className="grid grid-cols-3 gap-4">
            <div className="rounded-2xl border border-gray-200 p-5 text-center">
              <p className="text-3xl font-extrabold text-gray-900">{results.length}</p>
              <p className="text-sm text-gray-500 mt-1">Bots checked</p>
            </div>
            <div className="rounded-2xl border border-emerald-200 bg-emerald-50 p-5 text-center">
              <p className="text-3xl font-extrabold text-emerald-700">{allowedCount}</p>
              <p className="text-sm text-emerald-600 mt-1">Can crawl your site</p>
            </div>
            <div className="rounded-2xl border border-red-200 bg-red-50 p-5 text-center">
              <p className="text-3xl font-extrabold text-red-700">{blockedCount}</p>
              <p className="text-sm text-red-600 mt-1">Blocked by robots.txt</p>
            </div>
          </div>

          {/* Alert if all or many blocked */}
          {blockedCount >= 4 && (
            <div className="flex items-start gap-3 p-4 rounded-xl bg-amber-50 border border-amber-200">
              <AlertCircle className="w-5 h-5 text-amber-600 flex-shrink-0 mt-0.5" />
              <div>
                <p className="text-sm font-semibold text-amber-800">Multiple AI crawlers are blocked</p>
                <p className="text-sm text-amber-700 mt-1">
                  Your robots.txt is preventing {blockedCount} AI crawlers from indexing your site. This likely reduces your chances of appearing in AI-generated answers. Consider adding explicit <code className="font-mono bg-amber-100 px-1 rounded text-xs">Allow: /</code> rules for bots you want to permit.
                </p>
              </div>
            </div>
          )}

          {/* Bot grid */}
          <div>
            <h2 className="text-lg font-bold text-gray-900 mb-4">AI Crawler Status — <span className="font-normal text-gray-500 text-base">{checkedDomain}/robots.txt</span></h2>
            <div className="grid sm:grid-cols-2 gap-4">
              {results.map(bot => (
                <div
                  key={bot.name}
                  className={`rounded-2xl border p-5 transition-all ${
                    bot.allowed
                      ? 'border-emerald-200 bg-emerald-50'
                      : 'border-red-200 bg-red-50'
                  }`}
                >
                  <div className="flex items-start justify-between gap-3 mb-3">
                    <div className="flex items-center gap-2.5">
                      {bot.logo ? (
                        <img src={bot.logo} alt={bot.name} className="w-5 h-5 object-contain flex-shrink-0" />
                      ) : (
                        <div
                          className="w-5 h-5 rounded-full flex-shrink-0"
                          style={{ backgroundColor: bot.color }}
                        />
                      )}
                      <div>
                        <p className="text-sm font-bold text-gray-900">{bot.name}</p>
                        <p className="text-xs text-gray-500">{bot.company}</p>
                      </div>
                    </div>
                    <div className={`flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-semibold flex-shrink-0 ${
                      bot.allowed
                        ? 'bg-emerald-100 text-emerald-700'
                        : 'bg-red-100 text-red-700'
                    }`}>
                      {bot.allowed
                        ? <><CheckCircle2 className="w-3.5 h-3.5" /> Allowed</>
                        : <><XCircle className="w-3.5 h-3.5" /> Blocked</>
                      }
                    </div>
                  </div>

                  {bot.rules.length > 0 && (
                    <div className={`rounded-lg px-3 py-2 font-mono text-xs leading-relaxed ${
                      bot.allowed ? 'bg-emerald-100 text-emerald-900' : 'bg-red-100 text-red-900'
                    }`}>
                      {bot.rules.map((r, i) => <div key={i}>{r}</div>)}
                    </div>
                  )}

                  {bot.rules.length === 0 && (
                    <p className="text-xs text-gray-500 italic">No specific rule found — inherits from <code className="font-mono">User-agent: *</code></p>
                  )}
                </div>
              ))}
            </div>
          </div>

          {/* Recommended fix if blocked bots exist */}
          {blockedCount > 0 && (
            <div className="rounded-2xl border border-gray-200 p-6">
              <h3 className="font-bold text-gray-900 mb-2 flex items-center gap-2">
                <CheckCircle2 className="w-5 h-5 text-emerald-500" /> Recommended Fix
              </h3>
              <p className="text-sm text-gray-600 mb-4">
                Add these lines to your <code className="font-mono bg-gray-100 px-1 py-0.5 rounded text-xs">robots.txt</code> to explicitly allow AI crawlers:
              </p>
              <pre className="bg-gray-900 text-gray-100 rounded-xl p-4 text-xs font-mono overflow-auto whitespace-pre">
{`# Allow AI crawlers
User-agent: GPTBot
Allow: /

User-agent: ChatGPT-User
Allow: /

User-agent: ClaudeBot
Allow: /

User-agent: anthropic-ai
Allow: /

User-agent: PerplexityBot
Allow: /

User-agent: Googlebot-Extended
Allow: /`}
              </pre>
            </div>
          )}

          {/* Raw robots.txt */}
          <div className="rounded-2xl border border-gray-200 overflow-hidden">
            <button
              onClick={() => setShowRaw(!showRaw)}
              className="w-full flex items-center justify-between px-5 py-4 text-sm font-semibold text-gray-700 hover:bg-gray-50 transition-colors"
            >
              <span className="flex items-center gap-2">
                <ExternalLink className="w-4 h-4 text-gray-400" />
                View raw robots.txt
              </span>
              <span className="text-gray-400 text-xs">{showRaw ? '▲ Hide' : '▼ Show'}</span>
            </button>
            {showRaw && (
              <pre className="p-5 bg-gray-50 border-t border-gray-100 text-xs font-mono text-gray-700 overflow-auto max-h-80 whitespace-pre-wrap">
                {rawRobots}
              </pre>
            )}
          </div>
        </div>
      )}

      <Footer />
    </div>
  )
}
