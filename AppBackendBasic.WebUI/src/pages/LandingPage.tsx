import { useEffect, useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { ArrowRight, Subtitles, Download, Film, Pencil, Sparkles, Type } from 'lucide-react'
import Header from '../components/Header'
import Footer from '../components/Footer'
import { useAuth } from '../contexts/AuthContext'
import { api } from '../lib/api'

interface Plan { id: string; name: string; price: number; currency: string; kind: string; videos: number; max_seconds: number; description: string | null }

/** Sample caption styles shown in the "Styles" section (purely illustrative - the studio ships the full catalogue). */
const STYLE_SAMPLES = [
  { name: 'Viral', bg: 'bg-ink', words: [{ t: 'yeh', c: '#FFFFFF', f: 'Poppins' }, { t: 'BAHUT', c: '#FF3B30', f: 'Anton' }, { t: 'zabardast', c: '#FFFFFF', f: 'Poppins' }] },
  { name: 'Cinematic', bg: 'bg-ink', words: [{ t: 'kuch', c: '#FFF7DB', f: 'Poppins' }, { t: 'khaas', c: '#FFB800', f: 'Caveat' }, { t: 'hai', c: '#FFF7DB', f: 'Poppins' }] },
  { name: 'Meme Mix', bg: 'bg-ink', words: [{ t: 'bro', c: '#FFFFFF', f: 'Poppins' }, { t: 'WAIT', c: '#FFEB3B', f: 'Permanent Marker' }, { t: 'what', c: '#FFFFFF', f: 'Poppins' }] },
  { name: 'Clean Vogue', bg: 'bg-ink', words: [{ t: 'simple', c: '#E4E4E7', f: 'Poppins' }, { t: 'elegant', c: '#FF8FC7', f: 'Playfair Display' }, { t: 'HOOK', c: '#FFFFFF', f: 'Bebas Neue' }] },
]

const STEPS = [
  { icon: Film, title: 'Upload a clip', body: 'Drop an MP4 or MOV, up to two minutes. Hindi, English or a mix of both.', tone: 'bg-sun' },
  { icon: Pencil, title: 'Fix and style', body: 'Every word comes with exact timing. Change text, colours, fonts, add stickers and sound effects.', tone: 'bg-mint' },
  { icon: Download, title: 'Export and post', body: 'Download a finished MP4 with captions burned in, or an SRT file for your editor.', tone: 'bg-blush' },
]

const FEATURES = [
  { icon: Subtitles, title: 'Word-by-word Hinglish', body: 'Speech is recognised, converted to Roman Hinglish, and timed word by word. Words the AI is unsure about are flagged so you can check them.' },
  { icon: Type, title: 'Mixed fonts and colours', body: 'Nine ready styles, four variants and per-word emphasis: key words pop in a different font, size and colour.' },
  { icon: Sparkles, title: 'Smart clean-up', body: 'Remove filler words in one click and re-run the AI highlighting after you edit the text.' },
  { icon: Pencil, title: 'Edit like a video editor', body: 'Drag captions on the frame, retime words on the timeline, add stickers, text layers and sound effects.' },
]

const FAQ = [
  { q: 'Which languages work?', a: 'Hindi, English and Hinglish (a mix of both). Captions are written in the Roman alphabet, the way creators type Hinglish.' },
  { q: 'How long can my video be?', a: 'Up to two minutes per upload. Your plan decides how long a video can be before it needs a pass or a subscription.' },
  { q: 'Is the first video really free?', a: 'Yes. Your first captioned video is free, up to 45 seconds. No card is needed to try it.' },
  { q: 'Can I edit the captions?', a: 'Everything is editable: text, timing, fonts, colours, position, stickers and sound effects. The preview is live.' },
  { q: 'What do I get at the end?', a: 'A finished MP4 with captions burned in and an SRT subtitle file.' },
]

function PricingTeaser() {
  const navigate = useNavigate()
  const [plans, setPlans] = useState<Plan[]>([])
  useEffect(() => { api.get('/captions/payments/catalog').then((r) => setPlans(r.data.items ?? [])).catch(() => setPlans([])) }, [])
  const money = (p: Plan) => `${p.currency === 'INR' ? '₹' : p.currency + ' '}${p.price}`
  return (
    <div className="mt-10 grid gap-6 md:grid-cols-3">
      <div className="rounded-xl border-2 border-ink bg-white p-6 shadow-hard">
        <p className="font-display text-sm font-extrabold uppercase tracking-widest">Free</p>
        <p className="mt-2 font-display text-4xl font-extrabold">₹0</p>
        <p className="mt-2 text-sm">Your first video, up to 45 seconds. No card needed.</p>
      </div>
      {plans.map((p, i) => (
        <div key={p.id} className={`rounded-xl border-2 border-ink p-6 shadow-hard ${i === plans.length - 1 ? 'bg-sun' : 'bg-white'}`}>
          <p className="font-display text-sm font-extrabold uppercase tracking-widest">{p.name}</p>
          <p className="mt-2 font-display text-4xl font-extrabold">{money(p)}{p.kind === 'subscription' && <span className="text-base font-semibold"> /month</span>}</p>
          <p className="mt-2 text-sm">{p.description ?? `${p.videos} video(s), up to ${p.max_seconds} seconds`}</p>
        </div>
      ))}
      <div className="md:col-span-3">
        <button onClick={() => navigate('/pricing')} className="font-bold text-cherry underline underline-offset-4 hover:text-ink">See full pricing</button>
      </div>
    </div>
  )
}

export default function LandingPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const { isAuthenticated } = useAuth()

  // Scroll to #section links (react-router does not do this by itself)
  useEffect(() => {
    if (!location.hash) { window.scrollTo(0, 0); return }
    const el = document.getElementById(location.hash.slice(1))
    if (el) setTimeout(() => el.scrollIntoView({ behavior: 'smooth', block: 'start' }), 50)
  }, [location.hash])

  const start = () => navigate(isAuthenticated ? '/studio' : '/login?mode=signup')

  return (
    <div className="min-h-screen bg-paper text-ink" style={{ fontFamily: "'IBM Plex Sans', system-ui, sans-serif" }}>
      <Header />

      {/* Hero */}
      <section className="border-b-2 border-ink bg-sun">
        <div className="mx-auto grid max-w-6xl items-center gap-12 px-4 py-16 sm:px-6 lg:grid-cols-2 lg:py-24">
          <div>
            <p className="mb-5 inline-block rounded-md border-2 border-ink bg-white px-3 py-1 font-display text-xs font-extrabold uppercase tracking-[0.2em] shadow-hard-sm">Hinglish caption generator</p>
            <h1 className="font-display text-5xl font-extrabold leading-[1.02] tracking-tight sm:text-6xl lg:text-7xl">
              Turn every spoken word into a <span className="inline-block -rotate-1 rounded-lg border-2 border-ink bg-cherry px-3 text-white shadow-hard">visual hook</span>.
            </h1>
            <p className="mt-6 max-w-xl text-lg font-medium">
              Upload a short video and get word-perfect Hinglish captions with bold fonts and colours. Edit anything, then export a finished MP4.
            </p>
            <div className="mt-8 flex flex-wrap items-center gap-4">
              <button onClick={start} className="flex items-center gap-2 rounded-lg border-2 border-ink bg-cherry px-7 py-3.5 text-lg font-bold text-white shadow-hard transition-all hover:-translate-y-0.5 hover:shadow-hard-lg active:translate-y-0 active:shadow-none">
                {isAuthenticated ? 'Open studio' : 'Caption my first video free'} <ArrowRight className="h-5 w-5" />
              </button>
              <a href="#how-it-works" className="font-bold underline underline-offset-4 hover:text-cherry">See how it works</a>
            </div>
          </div>

          {/* Phone-style caption mock-up (illustration built from flat shapes) */}
          <div className="mx-auto w-full max-w-xs">
            <div className="relative aspect-[9/16] overflow-hidden rounded-3xl border-[3px] border-ink bg-ink shadow-hard-lg">
              <div className="absolute inset-0 bg-tangerine/90" />
              <div className="absolute -bottom-10 -left-10 h-48 w-48 rounded-full bg-mint" />
              <div className="absolute -right-8 top-10 h-32 w-32 rounded-full bg-blush" />
              <div className="absolute inset-x-0 bottom-[22%] px-4 text-center">
                <p className="font-display text-xl font-extrabold text-white drop-shadow-[0_3px_0_#14110F]">yeh video</p>
                <p className="font-['Anton'] text-5xl uppercase leading-none text-sun drop-shadow-[0_4px_0_#14110F]">viral</p>
                <p className="font-display text-xl font-extrabold text-white drop-shadow-[0_3px_0_#14110F]">hone wala hai</p>
              </div>
              <div className="absolute left-4 right-4 top-4 flex gap-1.5">{[0, 1, 2, 3].map((i) => <span key={i} className={`h-1.5 flex-1 rounded-full ${i < 2 ? 'bg-white' : 'bg-white/40'}`} />)}</div>
            </div>
          </div>
        </div>
      </section>

      {/* How it works */}
      <section id="how-it-works" className="mx-auto max-w-6xl scroll-mt-20 px-4 py-20 sm:px-6">
        <h2 className="font-display text-4xl font-extrabold tracking-tight sm:text-5xl">Three steps. No editing suite.</h2>
        <div className="mt-10 grid gap-6 md:grid-cols-3">
          {STEPS.map(({ icon: Icon, title, body, tone }, i) => (
            <div key={title} className="rounded-xl border-2 border-ink bg-white p-6 shadow-hard">
              <div className={`mb-5 grid h-12 w-12 place-items-center rounded-lg border-2 border-ink ${tone}`}><Icon className="h-6 w-6" /></div>
              <p className="font-display text-sm font-extrabold uppercase tracking-widest text-cherry">Step {i + 1}</p>
              <h3 className="mt-1 font-display text-2xl font-extrabold">{title}</h3>
              <p className="mt-3">{body}</p>
            </div>
          ))}
        </div>
      </section>

      {/* Styles */}
      <section id="styles" className="scroll-mt-20 border-y-2 border-ink bg-mint">
        <div className="mx-auto max-w-6xl px-4 py-20 sm:px-6">
          <h2 className="font-display text-4xl font-extrabold tracking-tight sm:text-5xl">Styles that stop the scroll</h2>
          <p className="mt-3 max-w-2xl text-lg font-medium">Key words get their own font, size and colour. Pick a template, then tweak every word.</p>
          <div className="mt-10 grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
            {STYLE_SAMPLES.map((s) => (
              <div key={s.name} className="overflow-hidden rounded-xl border-2 border-ink bg-white shadow-hard">
                <div className={`flex min-h-[9rem] flex-wrap items-center justify-center gap-x-2 px-3 py-6 text-center ${s.bg}`}>
                  {s.words.map((w) => (
                    <span key={w.t} style={{ color: w.c, fontFamily: `'${w.f}', sans-serif`, fontSize: w.f === 'Poppins' ? 20 : 30, fontWeight: 700, textShadow: '0 2px 0 rgba(0,0,0,.6)' }}>{w.t}</span>
                  ))}
                </div>
                <p className="border-t-2 border-ink px-4 py-3 font-display font-extrabold">{s.name}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Features */}
      <section className="mx-auto max-w-6xl px-4 py-20 sm:px-6">
        <h2 className="font-display text-4xl font-extrabold tracking-tight sm:text-5xl">Built for creators</h2>
        <div className="mt-10 grid gap-6 md:grid-cols-2">
          {FEATURES.map(({ icon: Icon, title, body }, i) => (
            <div key={title} className="flex gap-5 rounded-xl border-2 border-ink bg-white p-6 shadow-hard">
              <div className={`grid h-12 w-12 flex-shrink-0 place-items-center rounded-lg border-2 border-ink ${['bg-cherry text-white', 'bg-sun', 'bg-tangerine', 'bg-mint'][i]}`}><Icon className="h-6 w-6" /></div>
              <div><h3 className="font-display text-xl font-extrabold">{title}</h3><p className="mt-2">{body}</p></div>
            </div>
          ))}
        </div>
      </section>

      {/* Pricing */}
      <section className="border-y-2 border-ink bg-butter">
        <div className="mx-auto max-w-6xl px-4 py-20 sm:px-6">
          <h2 className="font-display text-4xl font-extrabold tracking-tight sm:text-5xl">Simple pricing</h2>
          <p className="mt-3 text-lg font-medium">Pay per video or subscribe. Start free.</p>
          <PricingTeaser />
        </div>
      </section>

      {/* FAQ */}
      <section className="mx-auto max-w-3xl px-4 py-20 sm:px-6">
        <h2 className="font-display text-4xl font-extrabold tracking-tight sm:text-5xl">Questions</h2>
        <div className="mt-8 grid gap-4">
          {FAQ.map((f) => (
            <details key={f.q} className="group rounded-xl border-2 border-ink bg-white p-5 shadow-hard-sm">
              <summary className="cursor-pointer list-none font-display text-lg font-extrabold">{f.q}</summary>
              <p className="mt-3">{f.a}</p>
            </details>
          ))}
        </div>
      </section>

      {/* Final CTA */}
      <section className="border-t-2 border-ink bg-cherry">
        <div className="mx-auto max-w-4xl px-4 py-20 text-center sm:px-6">
          <h2 className="font-display text-4xl font-extrabold tracking-tight text-white sm:text-5xl">Your next reel deserves better captions.</h2>
          <button onClick={start} className="mt-8 inline-flex items-center gap-2 rounded-lg border-2 border-ink bg-sun px-8 py-4 text-lg font-bold text-ink shadow-hard-lg transition-all hover:-translate-y-0.5 active:translate-y-0 active:shadow-none">
            {isAuthenticated ? 'Open studio' : 'Start free'} <ArrowRight className="h-5 w-5" />
          </button>
        </div>
      </section>

      <Footer />
    </div>
  )
}
