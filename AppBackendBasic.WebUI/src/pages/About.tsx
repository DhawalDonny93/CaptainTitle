import Header from '../components/Header'
import Footer from '../components/Footer'
import { useWaitlist } from '../contexts/WaitlistContext'

export default function About() {
  const { openWaitlist } = useWaitlist()

  return (
    <div className="min-h-screen bg-white">
      <Header />

      {/* ── Hero ─────────────────────────────────────────────────────────── */}
      <section className="pt-36 pb-20 px-6 relative overflow-hidden">
        <div className="absolute inset-0 pointer-events-none"
          style={{ background: 'radial-gradient(ellipse 80% 50% at 50% -10%, rgba(168,85,247,0.08) 0%, transparent 70%)' }} />
        <div className="max-w-3xl mx-auto text-center relative z-10">
          <div className="inline-flex items-center gap-2 bg-gray-50 border border-gray-200 rounded-full px-4 py-1.5 mb-6">
            <span className="w-1.5 h-1.5 rounded-full bg-emerald-500 animate-pulse" />
            <span className="text-xs font-semibold text-gray-600 uppercase tracking-widest">Our Mission</span>
          </div>
          <h1 className="text-4xl md:text-5xl font-bold tracking-tight text-gray-900 mb-5 leading-tight">
            AI is deciding who gets{' '}
            <span className="bg-clip-text text-transparent"
              style={{ backgroundImage: 'linear-gradient(135deg, #f43f5e 0%, #a855f7 50%, #22d3ee 100%)' }}>
              recommended.
            </span>
          </h1>
          <p className="text-lg text-gray-500 font-light leading-relaxed max-w-2xl mx-auto">
            When someone asks ChatGPT which tool to use, they take that answer seriously. ProQuest AI exists to make sure your brand is the one that gets named.
          </p>
        </div>
      </section>

      {/* ── Story ─────────────────────────────────────────────────────────── */}
      <section className="py-20 px-6 border-t border-gray-100">
        <div className="max-w-3xl mx-auto">
          <p className="text-xs font-semibold text-gray-400 uppercase tracking-widest mb-4">How it started</p>
          <h2 className="text-2xl font-bold text-gray-900 mb-6">We noticed before anyone was measuring it</h2>
          <div className="space-y-5 text-base text-gray-600 leading-relaxed">
            <p>
              It started with a specific frustration: we were running SEO for a client, traffic was healthy, rankings were solid — but they kept losing deals to a competitor. When we asked prospects why, they kept saying "ChatGPT recommended them."
            </p>
            <p>
              We went and checked. Sure enough, for every query in their category, the competitor came up first. Our client wasn't mentioned at all. No one was tracking this channel — not the client, not the agencies they'd hired, not anyone.
            </p>
            <p>
              We built the first version of ProQuest AI just to answer one question: where does your brand actually appear when AI responds to the queries that matter in your market? From there, the product grew: why you rank where you do, what competitors are doing differently, what to fix, what content to create.
            </p>
            <p>
              We track ChatGPT, Claude, Gemini, Perplexity, DeepSeek, and Grok — and we add new platforms as they matter, not just as they launch.
            </p>
          </div>
        </div>
      </section>

      {/* ── How we're different ───────────────────────────────────────────── */}
      <section className="py-20 px-6 bg-gray-50 border-y border-gray-200">
        <div className="max-w-3xl mx-auto">
          <p className="text-xs font-semibold text-gray-400 uppercase tracking-widest mb-4">How we're different</p>
          <h2 className="text-2xl font-bold text-gray-900 mb-10">Three things we care about getting right</h2>
          <div className="space-y-8">
            <div className="flex gap-6">
              <span className="text-3xl font-black text-gray-200 flex-shrink-0 leading-none">01</span>
              <div>
                <h3 className="text-base font-bold text-gray-900 mb-2">Real queries, not synthetic ones</h3>
                <p className="text-sm text-gray-500 leading-relaxed">
                  We run actual prompts through each LLM's live API every day. Not simulations, not cached results. Your visibility score reflects what a real user asking right now would see.
                </p>
              </div>
            </div>
            <div className="flex gap-6">
              <span className="text-3xl font-black text-gray-200 flex-shrink-0 leading-none">02</span>
              <div>
                <h3 className="text-base font-bold text-gray-900 mb-2">Rank position, not just mention rate</h3>
                <p className="text-sm text-gray-500 leading-relaxed">
                  Being mentioned fifth in a ChatGPT response is not the same as being mentioned first. We track your exact position in the response, so you know whether you're the recommendation or the footnote.
                </p>
              </div>
            </div>
            <div className="flex gap-6">
              <span className="text-3xl font-black text-gray-200 flex-shrink-0 leading-none">03</span>
              <div>
                <h3 className="text-base font-bold text-gray-900 mb-2">Fixes, not just reports</h3>
                <p className="text-sm text-gray-500 leading-relaxed">
                  Visibility data is only useful if it tells you what to do. Every metric in ProQuest AI connects to a specific action: a technical fix, a content gap, a citation source to target.
                </p>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* ── CTA ───────────────────────────────────────────────────────────── */}
      <section className="py-20 px-6">
        <div className="max-w-2xl mx-auto text-center">
          <h2 className="text-2xl font-bold text-gray-900 mb-3">See where your brand stands right now</h2>
          <p className="text-base text-gray-500 mb-8">Free plan available. No credit card required to get started.</p>
          <div className="flex flex-wrap items-center justify-center gap-3">
            <button
              onClick={openWaitlist}
              className="px-7 py-3 rounded-full text-sm font-semibold text-white transition-all"
              style={{ backgroundImage: 'linear-gradient(135deg, #f43f5e 0%, #a855f7 50%, #22d3ee 100%)' }}
            >
              Get early access
            </button>
          </div>
        </div>
      </section>

      <Footer />
    </div>
  )
}
