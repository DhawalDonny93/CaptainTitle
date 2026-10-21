import { useState } from 'react'
import { Download, Copy, Check, FileText, Globe, ArrowRight, Loader2, CheckCircle2, Mail } from 'lucide-react'
import Header from '../components/Header'
import Footer from '../components/Footer'
import { analyzeSite, buildLLMsTxt } from '../utils/siteAnalyzer'

interface LogLine { msg: string; done: boolean }

export default function LLMsTxtGenerator() {
  const [domain, setDomain] = useState('')
  const [loading, setLoading] = useState(false)
  const [log, setLog] = useState<LogLine[]>([])
  const [error, setError] = useState('')
  const [output, setOutput] = useState('')
  const [copied, setCopied] = useState(false)

  const [email, setEmail] = useState('')
  const [emailSent, setEmailSent] = useState(false)
  const [emailError, setEmailError] = useState('')

  function pushLog(msg: string) {
    setLog(prev => {
      const updated = prev.map((l, i) => i === prev.length - 1 ? { ...l, done: true } : l)
      return [...updated, { msg, done: false }]
    })
  }

  async function handleGenerate() {
    if (!domain.trim() || loading) return
    setError('')
    setOutput('')
    setLog([])
    setEmail('')
    setEmailSent(false)
    setEmailError('')
    setLoading(true)

    try {
      const analysis = await analyzeSite(domain, pushLog)
      setLog(prev => prev.map(l => ({ ...l, done: true })))
      const txt = buildLLMsTxt(analysis)
      setOutput(txt)
    } catch (e: any) {
      setError(e.message || 'Could not analyze the site. Check the domain and try again.')
      setLog(prev => prev.map(l => ({ ...l, done: true })))
    } finally {
      setLoading(false)
    }
  }

  function handleCopy() {
    navigator.clipboard.writeText(output).then(() => {
      setCopied(true)
      setTimeout(() => setCopied(false), 2000)
    })
  }

  function handleDownload() {
    const blob = new Blob([output], { type: 'text/plain' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = 'llms.txt'
    a.click()
    URL.revokeObjectURL(url)
  }

  function handleEmailSubmit(e: React.FormEvent) {
    e.preventDefault()
    const trimmed = email.trim()
    if (!trimmed) return
    const valid = /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(trimmed)
    if (!valid) {
      setEmailError('Please enter a valid email address.')
      return
    }
    setEmailError('')
    setEmailSent(true)
  }

  return (
    <div className="min-h-screen bg-white text-gray-900" style={{ fontFamily: "'Inter', system-ui, sans-serif" }}>
      <Header />

      {/* Hero */}
      <div className="pt-32 pb-12 px-6 text-center bg-gradient-to-b from-gray-50 to-white border-b border-gray-100">
        <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-violet-50 border border-violet-200 text-violet-700 text-xs font-semibold uppercase tracking-wider mb-5">
          <FileText className="w-3.5 h-3.5" /> Free Tool
        </div>
        <h1 className="text-4xl md:text-5xl font-extrabold tracking-tight mb-4">
          llms.txt Generator
        </h1>
        <p className="text-lg text-gray-500 max-w-2xl mx-auto">
          Enter your domain — we scan your homepage, sitemap, key pages, and blog posts, then generate a
          comprehensive <code className="text-sm bg-gray-100 px-1.5 py-0.5 rounded font-mono">llms.txt</code> that tells AI models exactly what your site is about.
        </p>
      </div>

      {/* Info strip */}
      <div className="bg-violet-50 border-b border-violet-100 px-6 py-4">
        <div className="max-w-5xl mx-auto flex flex-col sm:flex-row items-start sm:items-center gap-3">
          <p className="flex-1 text-sm text-violet-900">
            <strong>What is llms.txt?</strong> An emerging standard that lets you tell AI language models what your site covers and which pages matter most — increasing how often AI answers cite you.
          </p>
          <a
            href="https://llmstxt.org"
            target="_blank"
            rel="noopener noreferrer"
            className="flex-shrink-0 flex items-center gap-1 text-xs font-semibold text-violet-700 hover:text-violet-900 transition-colors"
          >
            Learn the spec <ArrowRight className="w-3 h-3" />
          </a>
        </div>
      </div>

      {/* Input */}
      <div className="max-w-2xl mx-auto px-6 py-10">
        <div className="flex gap-3">
          <div className="flex-1 flex items-center gap-2 rounded-2xl border-2 border-gray-200 bg-white px-4 py-2.5 shadow-sm focus-within:border-violet-400 transition-colors">
            <Globe className="w-5 h-5 text-gray-400 flex-shrink-0" />
            <input
              type="text"
              value={domain}
              onChange={e => setDomain(e.target.value)}
              onKeyDown={e => e.key === 'Enter' && !loading && handleGenerate()}
              placeholder="yourcompany.com"
              className="flex-1 text-base outline-none placeholder-gray-400 bg-transparent"
              disabled={loading}
            />
          </div>
          <button
            onClick={handleGenerate}
            disabled={loading || !domain.trim()}
            className="flex items-center gap-2 px-6 py-2.5 rounded-2xl bg-gray-900 hover:bg-gray-700 disabled:opacity-50 text-white font-semibold text-sm transition-colors whitespace-nowrap"
          >
            {loading
              ? <><Loader2 className="w-4 h-4 animate-spin" /> Scanning…</>
              : 'Generate'
            }
          </button>
        </div>

        {/* Live progress log */}
        {log.length > 0 && (
          <div className="mt-5 rounded-2xl border border-gray-200 bg-gray-50 divide-y divide-gray-100 overflow-hidden">
            {log.map((line, i) => (
              <div key={i} className="flex items-center gap-3 px-4 py-2.5">
                {line.done
                  ? <CheckCircle2 className="w-4 h-4 text-emerald-500 flex-shrink-0" />
                  : <Loader2 className="w-4 h-4 text-violet-500 animate-spin flex-shrink-0" />
                }
                <span className={`text-sm ${line.done ? 'text-gray-500' : 'text-gray-900 font-medium'}`}>
                  {line.msg}
                </span>
              </div>
            ))}
          </div>
        )}

        {/* Error */}
        {error && (
          <div className="mt-4 p-4 rounded-xl bg-red-50 border border-red-200 text-red-700 text-sm">
            {error}
          </div>
        )}
      </div>

      {/* Output */}
      {output && (
        <div className="max-w-5xl mx-auto px-6 pb-20 space-y-5">

          {/* Stats strip */}
          <div className="flex flex-wrap gap-3 text-xs font-medium text-gray-600">
            {(() => {
              const sections = output.match(/^##\s.+/gm) ?? []
              const pageCount = (output.match(/^- \[/gm) ?? []).length
              return (
                <>
                  <span className="px-3 py-1.5 rounded-full bg-violet-50 border border-violet-200 text-violet-700">
                    {sections.length} sections
                  </span>
                  <span className="px-3 py-1.5 rounded-full bg-emerald-50 border border-emerald-200 text-emerald-700">
                    {pageCount} pages & articles linked
                  </span>
                  <span className="px-3 py-1.5 rounded-full bg-gray-100 border border-gray-200">
                    {output.split('\n').length} lines · {(new Blob([output]).size / 1024).toFixed(1)} KB
                  </span>
                </>
              )
            })()}
          </div>

          {/* File preview + actions */}
          <div className="rounded-2xl border border-gray-200 overflow-hidden shadow-md">
            <div className="flex items-center justify-between px-5 py-3.5 border-b border-gray-100 bg-gray-50">
              <div className="flex items-center gap-3">
                <div className="flex gap-1.5">
                  <span className="w-3 h-3 rounded-full bg-red-400" />
                  <span className="w-3 h-3 rounded-full bg-yellow-400" />
                  <span className="w-3 h-3 rounded-full bg-green-400" />
                </div>
                <span className="text-xs text-gray-500 font-mono">llms.txt</span>
              </div>
              <div className="flex items-center gap-2">
                <button
                  onClick={handleCopy}
                  className="flex items-center gap-1.5 text-xs font-medium px-3 py-1.5 rounded-lg border border-gray-200 hover:bg-white transition-colors text-gray-600"
                >
                  {copied ? <Check className="w-3.5 h-3.5 text-green-500" /> : <Copy className="w-3.5 h-3.5" />}
                  {copied ? 'Copied!' : 'Copy'}
                </button>
                <button
                  onClick={handleDownload}
                  className="flex items-center gap-1.5 text-xs font-semibold px-3 py-1.5 rounded-lg bg-gray-900 hover:bg-gray-700 text-white transition-colors"
                >
                  <Download className="w-3.5 h-3.5" /> Download llms.txt
                </button>
              </div>
            </div>

            {/* Editable — user can tweak before downloading */}
            <textarea
              value={output}
              onChange={e => setOutput(e.target.value)}
              className="w-full p-5 text-sm font-mono text-gray-800 bg-white focus:outline-none resize-none leading-relaxed"
              rows={Math.min(Math.max(output.split('\n').length + 3, 20), 80)}
              spellCheck={false}
            />
          </div>

          {/* Placement tip */}
          <div className="rounded-xl bg-amber-50 border border-amber-200 px-4 py-3 text-xs text-amber-800">
            <strong>Where to place it:</strong> Upload to your domain root so AI crawlers find it at{' '}
            <code className="font-mono bg-amber-100 px-1 rounded">https://yourdomain.com/llms.txt</code>.
            You can also link to it from your <code className="font-mono bg-amber-100 px-1 rounded">robots.txt</code> with a{' '}
            <code className="font-mono bg-amber-100 px-1 rounded">Sitemap:</code> directive.
          </div>

          {/* Email capture */}
          <div className="rounded-2xl border border-gray-200 bg-gray-50 px-6 py-6">
            {emailSent ? (
              <div className="flex items-center gap-3 text-emerald-700">
                <CheckCircle2 className="w-5 h-5 flex-shrink-0" />
                <div>
                  <p className="font-semibold text-sm">You're on the list.</p>
                  <p className="text-sm text-gray-500 mt-0.5">We'll send the report to <strong>{email}</strong> once email delivery is live.</p>
                </div>
              </div>
            ) : (
              <>
                <div className="flex items-start gap-3 mb-4">
                  <div className="w-8 h-8 rounded-lg bg-violet-100 flex items-center justify-center flex-shrink-0 mt-0.5">
                    <Mail className="w-4 h-4 text-violet-600" />
                  </div>
                  <div>
                    <p className="font-semibold text-gray-900 text-sm">Get this report in your inbox</p>
                    <p className="text-sm text-gray-500 mt-0.5">Enter your email and we'll send you the generated llms.txt along with tips on where and how to deploy it.</p>
                  </div>
                </div>
                <form onSubmit={handleEmailSubmit} className="flex flex-col sm:flex-row gap-2">
                  <input
                    type="email"
                    value={email}
                    onChange={e => { setEmail(e.target.value); setEmailError('') }}
                    placeholder="you@company.com"
                    className="flex-1 px-4 py-2.5 rounded-xl border border-gray-200 bg-white text-sm outline-none focus:border-violet-400 transition-colors placeholder-gray-400"
                  />
                  <button
                    type="submit"
                    className="flex-shrink-0 px-5 py-2.5 rounded-xl bg-gray-900 hover:bg-gray-700 text-white text-sm font-semibold transition-colors"
                  >
                    Send to my email
                  </button>
                </form>
                {emailError && (
                  <p className="text-xs text-red-600 mt-2">{emailError}</p>
                )}
              </>
            )}
          </div>

        </div>
      )}

      <Footer />
    </div>
  )
}
