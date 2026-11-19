import { useEffect, useRef, useState, type FormEvent, type ReactNode } from 'react'
import { useNavigate } from 'react-router-dom'
import { AnimatePresence, animate, motion, useInView } from 'framer-motion'
import {
  ArrowRight, AudioLines, Check, ChevronDown, Clapperboard, Download, Eraser, FileText, Film, History, Languages, Menu, Mic,
  MousePointer2, Music2, Pencil, Sparkles, Sticker, Type, Upload, Wand2, X,
} from 'lucide-react'
import Footer from '../components/Footer'
import { Logo } from '../components/Header'
import { useAuth } from '../contexts/AuthContext'
import { LANDING_FAQ } from './landingContent'
import { CaptionLine, Frame, Marquee } from '../components/landing/CaptionMock'
import { LOOKS, useReducedMotion, useTicker, type Word } from '../components/landing/captionLooks'

/**
 * Alternative landing page (/home-v3): light, product-led layout with eyebrow-labelled sections, an auto-rotating hero product
 * tour, bento feature cards with a running border light, count-up numbers, a tools marquee and a final CTA ring that lights up.
 * All motion is code (framer-motion + CSS), not video files; every caption shown is an illustration built from text and shapes.
 */

const EASE: [number, number, number, number] = [0.22, 1, 0.36, 1]
const WORDS: Word[] = [{ t: 'yeh' }, { t: 'trick' }, { t: 'sach', em: true }, { t: 'mein' }, { t: 'kaam' }, { t: 'karta', em: true }, { t: 'hai' }]
const SCENE: [string, string, string] = ['#FF8A00', '#16C47F', '#FFD6DE']
const PLAIN = { ...LOOKS.viral, emFont: 'Poppins', emSize: 18, emColor: '#FFFFFF', upper: false }

// ---------- small building blocks ----------

/** Fades and lifts its children in the first time they scroll into view. */
function Reveal({ children, delay = 0, className = '' }: { children: ReactNode; delay?: number; className?: string }) {
  return (
    <motion.div className={className} initial={{ opacity: 0, y: 24 }} whileInView={{ opacity: 1, y: 0 }} viewport={{ once: true, margin: '-80px' }}
      transition={{ duration: 0.6, delay, ease: EASE }}>
      {children}
    </motion.div>
  )
}

function SectionHead({ eyebrow, title, sub, center }: { eyebrow: string; title: ReactNode; sub?: string; center?: boolean }) {
  return (
    <Reveal className={center ? 'mx-auto max-w-3xl text-center' : 'max-w-3xl'}>
      <p className="text-sm font-semibold text-cherry">{eyebrow}</p>
      <h2 className="mt-3 font-display text-4xl font-bold leading-[1.08] tracking-tight sm:text-5xl">{title}</h2>
      {sub && <p className="mt-5 text-lg text-ink/60">{sub}</p>}
    </Reveal>
  )
}

/** Card with a light that travels round its border (a spinning conic strip behind a 1px inset). */
function BeamCard({ children, className = '' }: { children: ReactNode; className?: string }) {
  return (
    <div className={`relative overflow-hidden rounded-2xl p-px ${className}`}>
      <div className="absolute inset-[-150%] animate-[spin_7s_linear_infinite] bg-[conic-gradient(from_0deg,transparent_0deg,transparent_290deg,#FF2D4D_340deg,#FFD60A_360deg)] motion-reduce:animate-none" />
      <div className="relative h-full rounded-[15px] bg-white">{children}</div>
    </div>
  )
}

function Card({ children, className = '' }: { children: ReactNode; className?: string }) {
  return <div className={`rounded-2xl border border-ink/10 bg-white ${className}`}>{children}</div>
}

function LinkArrow({ onClick, children }: { onClick: () => void; children: ReactNode }) {
  return (
    <button onClick={onClick} className="group inline-flex items-center gap-1.5 font-semibold text-ink hover:text-cherry">
      {children}<ArrowRight className="h-4 w-4 transition-transform group-hover:translate-x-1" />
    </button>
  )
}

function PrimaryButtons({ start, label, dark }: { start: () => void; label: string; dark?: boolean }) {
  const navigate = useNavigate()
  return (
    <div className="flex flex-wrap gap-3">
      <button onClick={start} className={`rounded-xl px-6 py-3 font-semibold transition-colors ${dark ? 'bg-white text-ink hover:bg-sun' : 'bg-ink text-white hover:bg-cherry'}`}>{label}</button>
      <button onClick={() => navigate('/pricing')} className={`rounded-xl border px-6 py-3 font-semibold transition-colors ${dark ? 'border-white/30 text-white hover:border-white' : 'border-ink/15 bg-white hover:border-ink'}`}>See pricing</button>
    </div>
  )
}

/** Counts from 0 to `to` once it scrolls into view. */
function CountUp({ to, suffix = '' }: { to: number; suffix?: string }) {
  const ref = useRef<HTMLSpanElement>(null)
  const inView = useInView(ref, { once: true })
  const reduced = useReducedMotion()
  const [v, setV] = useState(0)
  useEffect(() => {
    if (!inView) return
    if (reduced) { setV(to); return }
    const c = animate(0, to, { duration: 1.6, ease: EASE, onUpdate: (n) => setV(Math.round(n)) })
    return () => c.stop()
  }, [inView, reduced, to])
  return <span ref={ref} className="tabular-nums">{v}{suffix}</span>
}

// ---------- nav with a product mega-menu ----------

const PRODUCT_MENU = [
  { group: 'Create', items: [{ icon: Mic, t: 'Hinglish transcription', d: 'Speech to Roman Hinglish, word by word' }, { icon: Type, t: 'Caption styles', d: 'Nine looks, four variants each' }, { icon: Sparkles, t: 'AI emphasis', d: 'Key words pop automatically' }] },
  { group: 'Edit', items: [{ icon: Pencil, t: 'Word editor', d: 'Fix text without breaking timing' }, { icon: Sticker, t: 'Stickers & text layers', d: 'Drag anything onto the frame' }, { icon: Music2, t: 'Sound effects', d: 'Whoosh, pop, cash and more' }] },
  { group: 'Ship', items: [{ icon: Download, t: 'MP4 export', d: 'Captions burned in' }, { icon: FileText, t: 'SRT export', d: 'For your own editor' }, { icon: History, t: 'Restore points', d: 'Go back to any earlier version' }] },
]

function Nav({ start, isAuthenticated }: { start: () => void; isAuthenticated: boolean }) {
  const navigate = useNavigate()
  const [menu, setMenu] = useState(false)
  const [mobile, setMobile] = useState(false)
  return (
    <header className="sticky top-0 z-50 border-b border-ink/5 bg-white/80 backdrop-blur-md" onMouseLeave={() => setMenu(false)}>
      <div className="mx-auto flex h-16 max-w-7xl items-center justify-between px-4 sm:px-6">
        <div className="flex items-center gap-10">
          <Logo onClick={() => navigate('/')} />
          <nav className="hidden items-center gap-1 lg:flex">
            <button onMouseEnter={() => setMenu(true)} onClick={() => setMenu(!menu)} aria-expanded={menu}
              className="flex items-center gap-1 rounded-lg px-3 py-2 text-sm font-medium text-ink/70 hover:bg-ink/5 hover:text-ink">
              Product <ChevronDown className={`h-4 w-4 transition-transform ${menu ? 'rotate-180' : ''}`} />
            </button>
            {[['#styles', 'Styles'], ['#how', 'How it works'], ['#faq', 'FAQ']].map(([h, l]) => (
              <a key={h} href={h} onMouseEnter={() => setMenu(false)} className="rounded-lg px-3 py-2 text-sm font-medium text-ink/70 hover:bg-ink/5 hover:text-ink">{l}</a>
            ))}
            <button onClick={() => navigate('/pricing')} onMouseEnter={() => setMenu(false)} className="rounded-lg px-3 py-2 text-sm font-medium text-ink/70 hover:bg-ink/5 hover:text-ink">Pricing</button>
          </nav>
        </div>
        <div className="hidden items-center gap-2 lg:flex">
          {!isAuthenticated && <button onClick={() => navigate('/login')} className="rounded-lg px-4 py-2 text-sm font-medium hover:bg-ink/5">Log in</button>}
          <button onClick={start} className="rounded-lg bg-ink px-4 py-2 text-sm font-semibold text-white hover:bg-cherry">{isAuthenticated ? 'Open studio' : 'Start for free'}</button>
        </div>
        <button className="lg:hidden" onClick={() => setMobile(!mobile)} aria-label="Menu" aria-expanded={mobile}>{mobile ? <X className="h-6 w-6" /> : <Menu className="h-6 w-6" />}</button>
      </div>

      <AnimatePresence>
        {menu && (
          <motion.div initial={{ opacity: 0, y: -8 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0, y: -8 }} transition={{ duration: 0.18 }}
            className="absolute inset-x-0 top-16 hidden border-b border-ink/10 bg-white shadow-xl lg:block">
            <div className="mx-auto grid max-w-7xl grid-cols-3 gap-8 px-6 py-8">
              {PRODUCT_MENU.map((g) => (
                <div key={g.group}>
                  <p className="mb-3 text-xs font-semibold uppercase tracking-widest text-ink/40">{g.group}</p>
                  {g.items.map(({ icon: Icon, t, d }) => (
                    <button key={t} onClick={start} className="flex w-full gap-3 rounded-xl p-3 text-left hover:bg-paper">
                      <span className="grid h-9 w-9 flex-shrink-0 place-items-center rounded-lg bg-cherry/10 text-cherry"><Icon className="h-4 w-4" /></span>
                      <span><span className="block text-sm font-semibold">{t}</span><span className="block text-sm text-ink/50">{d}</span></span>
                    </button>
                  ))}
                </div>
              ))}
            </div>
          </motion.div>
        )}
      </AnimatePresence>

      {mobile && (
        <div className="grid gap-3 border-t border-ink/10 bg-white px-4 py-4 lg:hidden">
          {[['#styles', 'Styles'], ['#how', 'How it works'], ['#faq', 'FAQ']].map(([h, l]) => <a key={h} href={h} onClick={() => setMobile(false)} className="font-medium">{l}</a>)}
          <button onClick={() => navigate('/pricing')} className="text-left font-medium">Pricing</button>
          <button onClick={start} className="rounded-lg bg-ink py-2.5 font-semibold text-white">{isAuthenticated ? 'Open studio' : 'Start for free'}</button>
        </div>
      )}
    </header>
  )
}

// ---------- hero with an auto-rotating product tour ----------

const TOUR = [
  { key: 'transcribe', label: 'Transcribe', you: 'Upload podcast-clip.mp4', studio: 'Captions ready. 86 words, 3 flagged to check.' },
  { key: 'style', label: 'Style', you: 'Make it look like a reel', studio: 'Applied Viral. Key words in Anton, red.' },
  { key: 'edit', label: 'Edit', you: 'Change “sach” to “sachmuch”', studio: 'Updated. Every other word kept its timing.' },
  { key: 'emphasis', label: 'Emphasis', you: 'Which words should pop?', studio: 'Highlighted “sach” and “karta”.' },
  { key: 'export', label: 'Export', you: 'Export for Reels', studio: 'MP4 with burned-in captions and SRT ready.' },
]
const TOUR_MS = 5200

/** Types `text` out character by character (shows it whole when the visitor prefers reduced motion). */
function Typed({ text, delay = 0 }: { text: string; delay?: number }) {
  const reduced = useReducedMotion()
  const [n, setN] = useState(0)
  useEffect(() => {
    if (reduced) { setN(text.length); return }
    setN(0)
    let id = 0
    const t = window.setTimeout(() => { id = window.setInterval(() => setN((v) => (v >= text.length ? v : v + 1)), 28) }, delay)
    return () => { window.clearTimeout(t); window.clearInterval(id) }
  }, [text, delay, reduced])
  return <>{text.slice(0, n)}{n < text.length && <span className="ml-px inline-block h-4 w-0.5 translate-y-0.5 animate-pulse bg-cherry" />}</>
}

function TourVisual({ step }: { step: number }) {
  const key = TOUR[step].key
  const look = key === 'transcribe' || key === 'edit' ? PLAIN : LOOKS.viral
  const words: Word[] = key === 'edit' ? WORDS.map((w) => (w.t === 'sach' ? { t: 'sachmuch' } : w)) : key === 'emphasis' || key === 'style' || key === 'export' ? WORDS : WORDS.map((w) => ({ t: w.t }))
  return (
    <div className="grid h-full gap-4 sm:grid-cols-[180px_1fr]">
      <div className="mx-auto w-40 sm:w-full"><Frame scene={SCENE} words={words} look={look} scale={0.8} /></div>
      <div className="flex flex-col gap-3">
        <div className="rounded-xl border border-ink/10 bg-paper/60 p-3">
          <p className="mb-2 text-xs font-semibold uppercase tracking-widest text-ink/40">Timeline</p>
          <div className="flex gap-1">
            {words.map((w, i) => (
              <motion.span key={`${key}-${i}`} initial={{ scaleX: 0 }} animate={{ scaleX: 1 }} transition={{ delay: i * 0.06, duration: 0.35, ease: EASE }}
                className={`origin-left flex-1 truncate rounded-md px-1 py-1 text-center text-[11px] font-semibold ${w.em && key !== 'transcribe' ? 'bg-cherry text-white' : 'bg-butter'} ${key === 'edit' && w.t === 'sachmuch' ? 'ring-2 ring-ink' : ''}`}>
                {w.t}
              </motion.span>
            ))}
          </div>
        </div>
        <AnimatePresence mode="wait">
          <motion.div key={key} initial={{ opacity: 0, y: 8 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0, y: -8 }} transition={{ duration: 0.25 }}
            className="flex-1 rounded-xl border border-ink/10 bg-white p-3 text-sm">
            {key === 'transcribe' && <ul className="grid gap-1.5">{['yeh trick', 'sach mein', 'kaam karta hai'].map((l, i) => <li key={l} className="flex justify-between"><span>{l}</span><span className="text-ink/40">00:0{i * 2}</span></li>)}<li className="mt-1 inline-flex w-fit rounded-full bg-sun px-2 py-0.5 text-xs font-bold">3 words to check</li></ul>}
            {key === 'style' && <div className="flex flex-wrap gap-1.5">{['Viral', 'Cinematic', 'Meme Mix', 'Clean Vogue', 'Street Glow', 'Doodle Marker'].map((s, i) => <span key={s} className={`rounded-lg px-2.5 py-1 text-xs font-semibold ${i === 0 ? 'bg-ink text-white' : 'bg-paper'}`}>{s}</span>)}</div>}
            {key === 'edit' && <p>Word 3 · <s className="text-ink/40">sach</s> → <b className="text-cherry">sachmuch</b><br /><span className="text-ink/50">00:02.14 – 00:02.61 (unchanged)</span></p>}
            {key === 'emphasis' && <div className="flex flex-wrap gap-1.5">{WORDS.map((w) => <span key={w.t} className={`rounded-lg px-2 py-1 text-xs font-semibold ${w.em ? 'bg-cherry text-white' : 'bg-paper text-ink/50'}`}>{w.t}</span>)}</div>}
            {key === 'export' && <div className="grid gap-2">{['MP4 · 1080×1920', 'SRT subtitles'].map((f, i) => (
              <div key={f}><div className="mb-1 flex justify-between text-xs font-semibold"><span>{f}</span><span className="text-mint">Ready</span></div>
                <div className="h-1.5 overflow-hidden rounded-full bg-ink/10"><motion.div className="h-full rounded-full bg-mint" initial={{ width: 0 }} animate={{ width: '100%' }} transition={{ duration: 1.6, delay: i * 0.3, ease: EASE }} /></div></div>))}</div>}
          </motion.div>
        </AnimatePresence>
      </div>
    </div>
  )
}

function Hero({ isAuthenticated }: { isAuthenticated: boolean }) {
  const navigate = useNavigate()
  const reduced = useReducedMotion()
  const [step, setStep] = useState(0)
  const [paused, setPaused] = useState(false)
  const [email, setEmail] = useState('')
  useEffect(() => {
    if (paused || reduced) return
    const id = window.setTimeout(() => setStep((s) => (s + 1) % TOUR.length), TOUR_MS)
    return () => window.clearTimeout(id)
  }, [step, paused, reduced])

  const submit = (e: FormEvent) => {
    e.preventDefault()
    if (isAuthenticated) { navigate('/studio'); return }
    navigate(`/login?mode=signup${email.trim() ? `&email=${encodeURIComponent(email.trim())}` : ''}`)
  }

  return (
    <section className="relative overflow-hidden">
      {/* soft background glow */}
      <div className="pointer-events-none absolute left-1/2 top-[-20%] h-[600px] w-[900px] -translate-x-1/2 rounded-full bg-cherry/10 blur-3xl" />
      <div className="pointer-events-none absolute right-[-10%] top-[30%] h-[400px] w-[500px] rounded-full bg-sun/20 blur-3xl" />

      <div className="relative mx-auto max-w-7xl px-4 pb-16 pt-16 sm:px-6 lg:pt-24">
        <div className="mx-auto max-w-3xl text-center">
          <motion.a href="#how" initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.5, ease: EASE }}
            className="inline-flex items-center gap-2 rounded-full border border-ink/10 bg-white py-1 pl-1 pr-3 text-sm shadow-sm hover:border-ink/30">
            <span className="rounded-full bg-cherry px-2 py-0.5 text-xs font-semibold text-white">New</span> AI emphasis picks the words that pop <ArrowRight className="h-3.5 w-3.5" />
          </motion.a>
          <h1 className="mt-6 font-display text-5xl font-bold leading-[1.02] tracking-tight sm:text-7xl">
            {['The caption studio', 'for Hinglish creators'].map((line, i) => (
              <span key={line} className="block overflow-hidden pb-1">
                <motion.span className={`block ${i === 1 ? 'text-cherry' : ''}`} initial={{ y: '110%' }} animate={{ y: 0 }} transition={{ duration: 0.8, delay: 0.1 + i * 0.12, ease: EASE }}>{line}</motion.span>
              </span>
            ))}
          </h1>
          <motion.p initial={{ opacity: 0 }} animate={{ opacity: 1 }} transition={{ delay: 0.45, duration: 0.6 }} className="mx-auto mt-6 max-w-2xl text-lg text-ink/60 sm:text-xl">
            Transcribe, style, fix and export short videos in one place. Word-perfect Hinglish captions, timed word by word and styled to stop the scroll.
          </motion.p>
          <motion.form onSubmit={submit} initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.55, duration: 0.6, ease: EASE }}
            className="mx-auto mt-8 flex max-w-md flex-col gap-2 sm:flex-row">
            {!isAuthenticated && (
              <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} placeholder="Your email" aria-label="Your email" autoComplete="email"
                className="min-w-0 flex-1 rounded-xl border border-ink/15 bg-white px-4 py-3 outline-none focus:border-ink" />
            )}
            <button type="submit" className="rounded-xl bg-ink px-6 py-3 font-semibold text-white hover:bg-cherry">{isAuthenticated ? 'Open studio' : 'Start for free'}</button>
          </motion.form>
          <p className="mt-3 text-sm text-ink/50">First video free, up to 45 seconds. No card needed.</p>
        </div>

        {/* Product tour */}
        <motion.div initial={{ opacity: 0, y: 40 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.7, duration: 0.9, ease: EASE }}
          className="mx-auto mt-16 max-w-6xl" onMouseEnter={() => setPaused(true)} onMouseLeave={() => setPaused(false)}>
          <p className="mb-4 text-center text-sm font-medium text-ink/50">Everything between “recorded” and “posted”</p>
          <div className="mb-4 grid grid-cols-5 gap-2" role="tablist" aria-label="Product tour">
            {TOUR.map((t, i) => (
              <button key={t.key} role="tab" aria-selected={step === i} onClick={() => setStep(i)}
                className={`relative overflow-hidden rounded-xl px-2 py-3 text-xs font-semibold transition-colors sm:text-sm ${step === i ? 'bg-white text-ink shadow-sm ring-1 ring-ink/10' : 'text-ink/50 hover:text-ink'}`}>
                {t.label}
                {step === i && !reduced && (
                  <motion.span key={`${step}-${paused}`} className="absolute bottom-0 left-0 h-0.5 bg-cherry" initial={{ width: 0 }} animate={{ width: paused ? undefined : '100%' }}
                    transition={{ duration: TOUR_MS / 1000, ease: 'linear' }} />
                )}
              </button>
            ))}
          </div>
          <div className="grid overflow-hidden rounded-3xl border border-ink/10 bg-white shadow-2xl shadow-ink/10 lg:grid-cols-[340px_1fr]">
            <div className="flex flex-col border-b border-ink/10 bg-paper/50 p-5 lg:border-b-0 lg:border-r">
              <div className="flex items-center gap-2 text-sm font-semibold"><span className="h-2 w-2 animate-pulse rounded-full bg-mint" />Studio session</div>
              <div className="mt-5 flex-1 space-y-3 text-sm" aria-live="polite">
                <div className="ml-auto w-fit max-w-[90%] rounded-2xl rounded-br-sm bg-ink px-3.5 py-2 text-white"><Typed key={`you-${step}`} text={TOUR[step].you} /></div>
                <div className="w-fit max-w-[90%] rounded-2xl rounded-bl-sm border border-ink/10 bg-white px-3.5 py-2"><Typed key={`st-${step}`} text={TOUR[step].studio} delay={900} /></div>
              </div>
              <div className="mt-5 flex items-center gap-2 rounded-xl border border-ink/10 bg-white px-3 py-2 text-sm text-ink/40"><MousePointer2 className="h-4 w-4" />Click any word to edit it</div>
            </div>
            <div className="min-h-[340px] p-5"><TourVisual step={step} /></div>
          </div>
        </motion.div>
      </div>
    </section>
  )
}

// ---------- sections ----------

function Platforms() {
  const items = ['Instagram Reels', 'YouTube Shorts', 'Moj', 'Josh', 'Facebook Reels', 'LinkedIn video', 'Snapchat Spotlight', 'X video']
  return (
    <section className="border-y border-ink/5 bg-white py-10">
      <p className="mb-6 text-center text-sm font-medium text-ink/50">Vertical captions ready for every short-video feed</p>
      <Marquee>{items.map((p) => <span key={p} className="flex-shrink-0 px-6 font-display text-xl font-bold text-ink/30">{p}</span>)}</Marquee>
    </section>
  )
}

const ROLES = [
  { icon: Film, t: 'Creators', d: 'Post daily without spending the evening typing captions and nudging timestamps.' },
  { icon: Mic, t: 'Podcasters', d: 'Turn the best minute of an episode into a short that works with the sound off.' },
  { icon: Clapperboard, t: 'Editors', d: 'Hand the client a burned-in MP4 and keep the SRT for your own timeline.' },
  { icon: Languages, t: 'Brands', d: 'Write Hinglish the way your audience types it, not stiff transliteration.' },
]

function Roles() {
  return (
    <section className="mx-auto max-w-7xl px-4 py-24 sm:px-6">
      <SectionHead eyebrow="Built for every desk" title={<>For everyone behind<br />the video</>} />
      <div className="mt-12 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {ROLES.map(({ icon: Icon, t, d }, i) => (
          <Reveal key={t} delay={i * 0.08}>
            <Card className="group h-full p-6 transition-shadow hover:shadow-xl hover:shadow-ink/5">
              <span className="grid h-11 w-11 place-items-center rounded-xl bg-paper text-cherry transition-transform group-hover:-translate-y-0.5"><Icon className="h-5 w-5" /></span>
              <h3 className="mt-6 font-display text-xl font-bold">{t}</h3>
              <p className="mt-2 text-ink/60">{d}</p>
            </Card>
          </Reveal>
        ))}
      </div>
    </section>
  )
}

/** Waveform with word blocks snapping onto it: "word-level timing". */
function TimingArt() {
  const bars = [6, 14, 22, 12, 28, 18, 9, 24, 30, 16, 8, 20, 26, 12, 6, 18, 24, 10, 22, 14, 7, 16, 28, 12]
  return (
    <div className="mt-6 rounded-xl bg-paper/70 p-4">
      <div className="flex h-16 items-center gap-1">
        {bars.map((h, i) => (
          <motion.span key={i} className="flex-1 rounded-full bg-ink/20" style={{ height: h * 2 }}
            animate={{ scaleY: [1, 0.55, 1] }} transition={{ duration: 1.4, repeat: Infinity, delay: i * 0.05, ease: 'easeInOut' }} />
        ))}
      </div>
      <div className="mt-3 flex gap-1">
        {WORDS.map((w, i) => (
          <motion.span key={w.t} initial={{ opacity: 0, y: 8 }} whileInView={{ opacity: 1, y: 0 }} viewport={{ once: true }} transition={{ delay: 0.3 + i * 0.12 }}
            className={`flex-1 truncate rounded-md py-1 text-center text-xs font-semibold ${w.em ? 'bg-cherry text-white' : 'bg-white ring-1 ring-ink/10'}`}>{w.t}</motion.span>
        ))}
      </div>
    </div>
  )
}

/** Devanagari line flipping into Roman Hinglish. */
function ScriptFlip() {
  const i = useTicker(2, 2200)
  return (
    <div className="mt-6 grid h-24 place-items-center overflow-hidden rounded-xl bg-paper/70">
      <AnimatePresence mode="wait">
        <motion.p key={i} initial={{ y: 30, opacity: 0 }} animate={{ y: 0, opacity: 1 }} exit={{ y: -30, opacity: 0 }} transition={{ duration: 0.4, ease: EASE }}
          className={`font-display text-2xl font-bold ${i === 1 ? 'text-cherry' : 'text-ink/40'}`}>
          {i === 0 ? 'यह ट्रिक सच में काम करती है' : 'yeh trick sach mein kaam karti hai'}
        </motion.p>
      </AnimatePresence>
    </div>
  )
}

function Engine() {
  return (
    <section className="bg-paper/60 py-24">
      <div className="mx-auto max-w-7xl px-4 sm:px-6">
        <SectionHead eyebrow="Caption engine" title={<>The engine that listens<br />to every single word</>} sub="Speech in, Hinglish out, every word timed on its own so captions land exactly when they are spoken." />
        <div className="mt-12 grid gap-4 lg:grid-cols-3">
          <Reveal className="lg:col-span-2">
            <BeamCard className="h-full">
              <div className="p-7">
                <h3 className="font-display text-2xl font-bold">Word-level timing</h3>
                <p className="mt-2 max-w-lg text-ink/60">Every word gets its own start and end, so the highlight follows the voice instead of jumping a whole line at a time.</p>
                <TimingArt />
              </div>
            </BeamCard>
          </Reveal>
          <Reveal delay={0.08}>
            <Card className="h-full p-7">
              <h3 className="font-display text-2xl font-bold">Hinglish, the way people type it</h3>
              <p className="mt-2 text-ink/60">Hindi, English or both, written in the Roman alphabet.</p>
              <ScriptFlip />
            </Card>
          </Reveal>
          <Reveal>
            <Card className="h-full p-7">
              <h3 className="font-display text-2xl font-bold">Flags what to check</h3>
              <p className="mt-2 text-ink/60">Words the AI is unsure about are marked, so you review three words, not three hundred.</p>
              <div className="mt-6 flex flex-wrap gap-2">
                {['yeh', 'trick', 'sach', 'mein', 'kaam', 'karta', 'hai'].map((w) => (
                  <span key={w} className={`rounded-lg px-2.5 py-1 text-sm font-semibold ${w === 'sach' || w === 'kaam' ? 'bg-sun ring-2 ring-ink/80' : 'bg-paper'}`}>{w}</span>
                ))}
              </div>
            </Card>
          </Reveal>
          <Reveal delay={0.08}>
            <Card className="h-full p-7">
              <h3 className="font-display text-2xl font-bold">One-click clean-up</h3>
              <p className="mt-2 text-ink/60">Remove filler words, then re-run AI highlighting after you edit.</p>
              <p className="mt-6 text-lg font-semibold">
                {['matlab', 'yeh', 'umm', 'sach', 'mein', 'like', 'kaam', 'karta', 'hai'].map((w, i) => {
                  const filler = ['matlab', 'umm', 'like'].includes(w)
                  return (
                    <motion.span key={i} className="mr-1.5 inline-block" initial={false}
                      whileInView={filler ? { opacity: 0.25, textDecorationLine: 'line-through' } : undefined} viewport={{ once: true }} transition={{ delay: 0.6 + i * 0.05 }}>{w}</motion.span>
                  )
                })}
              </p>
              <span className="mt-4 inline-flex items-center gap-1.5 rounded-full bg-mint/15 px-3 py-1 text-sm font-semibold text-mint"><Eraser className="h-4 w-4" />3 fillers removed</span>
            </Card>
          </Reveal>
          <Reveal delay={0.16}>
            <Card className="h-full p-7">
              <h3 className="font-display text-2xl font-bold">MP4 or MOV in, MP4 and SRT out</h3>
              <p className="mt-2 text-ink/60">Up to two minutes per upload.</p>
              <div className="mt-6 flex items-center gap-3 text-sm font-semibold">
                <span className="rounded-lg bg-paper px-3 py-2">MP4 · MOV</span>
                <motion.span animate={{ x: [0, 6, 0] }} transition={{ duration: 1.4, repeat: Infinity }}><ArrowRight className="h-5 w-5 text-cherry" /></motion.span>
                <span className="rounded-lg bg-ink px-3 py-2 text-white">MP4 + SRT</span>
              </div>
            </Card>
          </Reveal>
        </div>
      </div>
    </section>
  )
}

function Numbers() {
  const stats = [{ n: 45, s: 's', l: 'Free on your first video' }, { n: 120, s: 's', l: 'Longest upload' }, { n: 9, s: '', l: 'Ready-made caption styles' }, { n: 4, s: '', l: 'Variants of every style' }]
  return (
    <section className="mx-auto max-w-7xl px-4 py-24 sm:px-6">
      <SectionHead center eyebrow="By the numbers" title={<>Small numbers.<br />Big hooks.</>} />
      <div className="mt-14 grid grid-cols-2 gap-px overflow-hidden rounded-3xl border border-ink/10 bg-ink/10 lg:grid-cols-4">
        {stats.map((x) => (
          <div key={x.l} className="bg-white p-8 text-center">
            <p className="font-display text-5xl font-bold tracking-tight sm:text-6xl"><CountUp to={x.n} suffix={x.s} /></p>
            <p className="mt-3 text-ink/60">{x.l}</p>
          </div>
        ))}
      </div>
    </section>
  )
}

const STYLE_ROW = [
  { name: 'Viral', look: LOOKS.viral, words: [{ t: 'yeh' }, { t: 'BAHUT', em: true }] },
  { name: 'Cinematic', look: LOOKS.cinematic, words: [{ t: 'kuch' }, { t: 'khaas', em: true }] },
  { name: 'Meme Mix', look: LOOKS.meme, words: [{ t: 'bro' }, { t: 'WAIT', em: true }] },
  { name: 'Clean Vogue', look: LOOKS.vogue, words: [{ t: 'simple' }, { t: 'HOOK', em: true }] },
]

function Styles({ start }: { start: () => void }) {
  const [pick, setPick] = useState(0)
  const [size, setSize] = useState(1)
  const [pos, setPos] = useState<'top' | 'middle' | 'bottom'>('bottom')
  const s = STYLE_ROW[pick]
  return (
    <section id="styles" className="scroll-mt-20 bg-paper/60 py-24">
      <div className="mx-auto max-w-7xl px-4 sm:px-6">
        <div className="flex flex-col gap-6 lg:flex-row lg:items-end lg:justify-between">
          <SectionHead eyebrow="Styles & layout" title={<>Design the look<br />without an editing suite</>} sub="Pick a style, make key words bigger, move captions anywhere on the frame. The preview updates as you click." />
          <Reveal><LinkArrow onClick={start}>Explore the studio</LinkArrow></Reveal>
        </div>
        <Reveal className="mt-12">
          <div className="grid overflow-hidden rounded-3xl border border-ink/10 bg-white lg:grid-cols-[1fr_380px]">
            <div className="grid place-items-center bg-ink/[0.03] p-8">
              <div className="relative aspect-[9/16] w-56 overflow-hidden rounded-2xl" style={{ background: '#14110F' }}>
                <div className="absolute -bottom-10 -left-8 h-1/2 w-2/3 rounded-full bg-[#2A2420]" />
                <div className="absolute inset-x-[30%] top-[22%] h-[30%] rounded-t-full bg-[#3A2E25]" />
                <motion.div layout transition={{ type: 'spring', stiffness: 300, damping: 30 }}
                  className={`absolute inset-x-3 ${pos === 'top' ? 'top-[10%]' : pos === 'middle' ? 'top-[45%]' : 'bottom-[12%]'}`}>
                  <CaptionLine words={s.words} look={{ ...s.look, emSize: s.look.emSize * size }} />
                </motion.div>
              </div>
            </div>
            <div className="grid content-start gap-6 border-t border-ink/10 p-6 lg:border-l lg:border-t-0">
              <div>
                <p className="mb-2 text-sm font-semibold">Style</p>
                <div className="grid grid-cols-2 gap-2">
                  {STYLE_ROW.map((x, i) => <button key={x.name} onClick={() => setPick(i)} className={`rounded-xl px-3 py-2 text-sm font-semibold ${pick === i ? 'bg-ink text-white' : 'bg-paper hover:bg-butter'}`}>{x.name}</button>)}
                </div>
              </div>
              <div>
                <p className="mb-2 flex justify-between text-sm font-semibold"><span>Key-word size</span><span className="text-ink/50">{Math.round(size * 100)}%</span></p>
                <input type="range" min={0.6} max={1.3} step={0.05} value={size} onChange={(e) => setSize(Number(e.target.value))} className="w-full accent-cherry" aria-label="Key-word size" />
              </div>
              <div>
                <p className="mb-2 text-sm font-semibold">Position</p>
                <div className="grid grid-cols-3 gap-2">
                  {(['top', 'middle', 'bottom'] as const).map((p) => <button key={p} onClick={() => setPos(p)} className={`rounded-xl py-2 text-sm font-semibold capitalize ${pos === p ? 'bg-ink text-white' : 'bg-paper hover:bg-butter'}`}>{p}</button>)}
                </div>
              </div>
              <p className="text-sm text-ink/50">Try it: this panel works like the real studio, on illustrated sample captions.</p>
            </div>
          </div>
        </Reveal>
      </div>
    </section>
  )
}

function PricingBand({ start, isAuthenticated }: { start: () => void; isAuthenticated: boolean }) {
  return (
    <section className="mx-auto max-w-7xl px-4 py-24 sm:px-6">
      <Reveal>
        <div className="relative overflow-hidden rounded-3xl bg-ink px-8 py-14 text-white sm:px-14">
          <div className="pointer-events-none absolute -right-20 -top-20 h-80 w-80 animate-pulse rounded-full bg-cherry/30 blur-3xl motion-reduce:animate-none" />
          <div className="relative grid items-center gap-8 lg:grid-cols-[1.4fr_1fr]">
            <div>
              <h2 className="font-display text-4xl font-bold leading-tight tracking-tight sm:text-5xl">First video free.<br /><span className="text-sun">Then pay per video or subscribe.</span></h2>
              <p className="mt-4 text-white/60">No card needed to try it.</p>
            </div>
            <div className="lg:justify-self-end"><PrimaryButtons start={start} label={isAuthenticated ? 'Open studio' : 'Start for free'} dark /></div>
          </div>
        </div>
      </Reveal>
    </section>
  )
}

/** Mini editor timeline with a playhead sweeping across word, sticker and SFX tracks. */
function LiveTimeline() {
  const reduced = useReducedMotion()
  return (
    <div className="relative overflow-hidden rounded-2xl border border-ink/10 bg-white p-5">
      <div className="mb-3 flex items-center justify-between text-xs font-semibold text-ink/50"><span>00:00</span><span className="flex items-center gap-1.5 text-mint"><span className="h-1.5 w-1.5 animate-pulse rounded-full bg-mint" />Live preview</span><span>00:08</span></div>
      {[
        { label: 'Captions', items: [[0, 22, 'yeh trick'], [24, 22, 'sach mein'], [48, 30, 'kaam karta hai']] },
        { label: 'Stickers', items: [[20, 18, '🔥'], [70, 16, '✅']] },
        { label: 'SFX', items: [[22, 8, 'pop'], [46, 10, 'whoosh'], [80, 8, 'cash']] },
      ].map((track) => (
        <div key={track.label} className="mb-2 flex items-center gap-3">
          <span className="w-16 text-xs font-semibold text-ink/50">{track.label}</span>
          <div className="relative h-8 flex-1 rounded-lg bg-paper/70">
            {track.items.map(([l, w, t]) => (
              <span key={String(t)} className={`absolute inset-y-1 truncate rounded-md px-1.5 text-[11px] font-semibold leading-6 ${track.label === 'Captions' ? 'bg-cherry text-white' : track.label === 'SFX' ? 'bg-sun' : 'bg-butter'}`}
                style={{ left: `${l}%`, width: `${w}%` }}>{t}</span>
            ))}
          </div>
        </div>
      ))}
      {!reduced && (
        <div className="pointer-events-none absolute bottom-3 left-[96px] right-5 top-10">
          <motion.span className="absolute inset-y-0 w-0.5 bg-ink" animate={{ left: ['0%', '100%'] }} transition={{ duration: 6, repeat: Infinity, ease: 'linear' }} />
        </div>
      )}
    </div>
  )
}

function Editor() {
  const points = [
    { t: 'Fix any word', d: 'Click it, type, done. Timing stays put.' },
    { t: 'Retime on the timeline', d: 'Drag a word when the voice and caption drift.' },
    { t: 'Drag on the frame', d: 'Move captions, stickers and text layers anywhere.' },
    { t: 'Restore points', d: 'Autosave keeps earlier versions you can go back to.' },
  ]
  return (
    <section id="how" className="scroll-mt-20 bg-paper/60 py-24">
      <div className="mx-auto max-w-7xl px-4 sm:px-6">
        <SectionHead eyebrow="Editor" title={<>See every change live.<br />Fix anything in seconds</>} />
        <div className="mt-12 grid items-start gap-10 lg:grid-cols-[1.4fr_1fr]">
          <Reveal><LiveTimeline /></Reveal>
          <div className="grid gap-6">
            {points.map((p, i) => (
              <Reveal key={p.t} delay={i * 0.06}>
                <div className="flex gap-4">
                  <span className="mt-0.5 grid h-7 w-7 flex-shrink-0 place-items-center rounded-full bg-cherry/10 text-cherry"><Check className="h-4 w-4" /></span>
                  <div><h3 className="font-semibold">{p.t}</h3><p className="text-ink/60">{p.d}</p></div>
                </div>
              </Reveal>
            ))}
          </div>
        </div>
      </div>
    </section>
  )
}

function MoreWays({ start }: { start: () => void }) {
  const cards = [
    { icon: Sticker, t: 'Stickers & text layers', d: 'Add emoji, labels and titles, then drag them where they belong.', tone: 'bg-butter' },
    { icon: AudioLines, t: 'Sound effects', d: 'Whoosh, pop, shutter, cash and bass drop, placed on the timeline.', tone: 'bg-blush' },
    { icon: Wand2, t: 'AI emphasis', d: 'Let the AI pick the words that should pop, and re-run it after you edit.', tone: 'bg-mint/20' },
  ]
  return (
    <section className="mx-auto max-w-7xl px-4 py-24 sm:px-6">
      <SectionHead eyebrow="More ways to stand out" title={<>Add what makes<br />people stop scrolling</>} />
      <div className="mt-12 grid gap-4 md:grid-cols-3">
        {cards.map(({ icon: Icon, t, d, tone }, i) => (
          <Reveal key={t} delay={i * 0.08}>
            <Card className="group flex h-full flex-col p-7 transition-shadow hover:shadow-xl hover:shadow-ink/5">
              <span className={`grid h-28 place-items-center rounded-xl ${tone}`}><Icon className="h-10 w-10 transition-transform duration-500 group-hover:scale-110 group-hover:-rotate-6" /></span>
              <h3 className="mt-6 font-display text-xl font-bold">{t}</h3>
              <p className="mt-2 flex-1 text-ink/60">{d}</p>
              <div className="mt-5"><LinkArrow onClick={start}>Try it</LinkArrow></div>
            </Card>
          </Reveal>
        ))}
      </div>
    </section>
  )
}

function GetStarted({ start }: { start: () => void }) {
  const [mode, setMode] = useState<'quick' | 'custom'>('quick')
  const steps = mode === 'quick'
    ? [{ icon: Upload, t: 'Upload your clip', d: 'MP4 or MOV, up to two minutes.' }, { icon: Type, t: 'Pick a style', d: 'Captions appear already timed and highlighted.' }, { icon: Download, t: 'Export', d: 'Download the MP4, or the SRT for your editor.' }]
    : [{ icon: Upload, t: 'Upload your clip', d: 'Hindi, English or a mix of both.' }, { icon: Pencil, t: 'Fix words and timing', d: 'Check flagged words, drag anything that drifts.' }, { icon: Sparkles, t: 'Tune emphasis and style', d: 'Fonts, colours, size, position, stickers and SFX.' }, { icon: Download, t: 'Export', d: 'MP4 with burned-in captions plus SRT.' }]
  return (
    <section className="bg-paper/60 py-24">
      <div className="mx-auto max-w-5xl px-4 sm:px-6">
        <SectionHead center eyebrow="Get started" title={<>From upload to posted<br />in a few minutes</>} />
        <Reveal className="mt-10 flex justify-center">
          <div className="inline-flex rounded-xl border border-ink/10 bg-white p-1">
            {(['quick', 'custom'] as const).map((m) => (
              <button key={m} onClick={() => setMode(m)} className={`relative rounded-lg px-5 py-2 text-sm font-semibold ${mode === m ? 'text-white' : 'text-ink/60'}`}>
                {mode === m && <motion.span layoutId="gs-pill" className="absolute inset-0 rounded-lg bg-ink" transition={{ type: 'spring', stiffness: 400, damping: 35 }} />}
                <span className="relative">{m === 'quick' ? 'In a hurry' : 'Make it perfect'}</span>
              </button>
            ))}
          </div>
        </Reveal>
        <AnimatePresence mode="wait">
          <motion.ol key={mode} initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0, y: -12 }} transition={{ duration: 0.25 }}
            className={`mt-10 grid gap-4 ${mode === 'quick' ? 'md:grid-cols-3' : 'md:grid-cols-4'}`}>
            {steps.map(({ icon: Icon, t, d }, i) => (
              <li key={t}><Card className="h-full p-6">
                <div className="flex items-center justify-between"><span className="grid h-10 w-10 place-items-center rounded-xl bg-paper text-cherry"><Icon className="h-5 w-5" /></span><span className="font-display text-sm font-bold text-ink/30">0{i + 1}</span></div>
                <h3 className="mt-5 font-semibold">{t}</h3><p className="mt-1 text-sm text-ink/60">{d}</p>
              </Card></li>
            ))}
          </motion.ol>
        </AnimatePresence>
        <div className="mt-10 flex justify-center"><PrimaryButtons start={start} label="Start for free" /></div>
      </div>
    </section>
  )
}

function Tools() {
  const tools = ['Premiere Pro', 'Final Cut Pro', 'DaVinci Resolve', 'CapCut', 'VN Editor', 'Instagram', 'YouTube', 'LinkedIn', 'VLC']
  return (
    <section className="py-24">
      <div className="mx-auto max-w-7xl px-4 sm:px-6"><SectionHead center eyebrow="Fits your workflow" title={<>Post the MP4 anywhere.<br />Open the SRT in your editor</>} /></div>
      <div className="mt-12 grid gap-3">
        <Marquee>{tools.map((t) => <span key={t} className="flex-shrink-0 rounded-2xl border border-ink/10 bg-white px-6 py-4 font-semibold shadow-sm">{t}</span>)}</Marquee>
        <Marquee reverse>{[...tools].reverse().map((t) => <span key={t} className="flex-shrink-0 rounded-2xl border border-ink/10 bg-white px-6 py-4 font-semibold shadow-sm">{t}</span>)}</Marquee>
      </div>
      <p className="mt-6 text-center text-sm text-ink/40">SRT is a standard subtitle format; these tools can import it. Not affiliated with any of them.</p>
    </section>
  )
}

function Faq() {
  const [open, setOpen] = useState<number | null>(null)
  return (
    <section id="faq" className="scroll-mt-20 bg-paper/60 py-24">
      <div className="mx-auto max-w-3xl px-4 sm:px-6">
        <SectionHead center eyebrow="FAQ" title="Questions, answered" />
        <div className="mt-10 grid gap-3">
          {LANDING_FAQ.map((f, i) => (
            <Card key={f.q} className="overflow-hidden">
              <button onClick={() => setOpen(open === i ? null : i)} aria-expanded={open === i} className="flex w-full items-center justify-between gap-4 px-6 py-5 text-left font-semibold">
                {f.q}<ChevronDown className={`h-5 w-5 flex-shrink-0 transition-transform ${open === i ? 'rotate-180' : ''}`} />
              </button>
              <AnimatePresence initial={false}>
                {open === i && (
                  <motion.div initial={{ height: 0 }} animate={{ height: 'auto' }} exit={{ height: 0 }} transition={{ duration: 0.25, ease: EASE }} className="overflow-hidden">
                    <p className="px-6 pb-5 text-ink/60">{f.a}</p>
                  </motion.div>
                )}
              </AnimatePresence>
            </Card>
          ))}
        </div>
      </div>
    </section>
  )
}

/** Ring of ticks that light up in sequence once the final CTA scrolls into view. */
function TickRing() {
  const ref = useRef<HTMLDivElement>(null)
  const inView = useInView(ref, { once: true })
  const ticks = 60
  return (
    <div ref={ref} className="pointer-events-none absolute left-1/2 top-1/2 h-[560px] w-[560px] -translate-x-1/2 -translate-y-1/2 opacity-70">
      <svg viewBox="-100 -100 200 200" className="h-full w-full animate-[spin_60s_linear_infinite] motion-reduce:animate-none">
        {Array.from({ length: ticks }, (_, i) => {
          const a = (i / ticks) * Math.PI * 2
          return (
            <motion.line key={i} x1={Math.cos(a) * 86} y1={Math.sin(a) * 86} x2={Math.cos(a) * 96} y2={Math.sin(a) * 96} strokeWidth={1.4} strokeLinecap="round"
              initial={{ stroke: 'rgba(255,255,255,0.15)' }} animate={inView ? { stroke: i % 5 === 0 ? '#FFD60A' : 'rgba(255,255,255,0.6)' } : undefined}
              transition={{ delay: i * 0.025, duration: 0.3 }} />
          )
        })}
      </svg>
    </div>
  )
}

function FinalCta({ start, isAuthenticated }: { start: () => void; isAuthenticated: boolean }) {
  return (
    <section className="mx-auto max-w-7xl px-4 py-24 sm:px-6">
      <div className="relative overflow-hidden rounded-[2rem] bg-cherry px-6 py-24 text-center text-white">
        <TickRing />
        <Reveal className="relative">
          <h2 className="font-display text-4xl font-bold leading-[1.05] tracking-tight sm:text-6xl">Your next reel,<br />captioned in minutes</h2>
          <p className="mx-auto mt-5 max-w-xl text-lg text-white/80">Upload a clip and get word-perfect Hinglish captions. Your first video is free.</p>
          <div className="mt-8 flex justify-center"><PrimaryButtons start={start} label={isAuthenticated ? 'Open studio' : 'Start for free'} dark /></div>
        </Reveal>
      </div>
    </section>
  )
}

export default function LandingPageV3() {
  const navigate = useNavigate()
  const { isAuthenticated } = useAuth()
  const start = () => navigate(isAuthenticated ? '/studio' : '/login?mode=signup')
  useEffect(() => { window.scrollTo(0, 0) }, [])

  return (
    <div className="min-h-screen bg-white font-sans text-ink">
      <Nav start={start} isAuthenticated={isAuthenticated} />
      <main>
        <Hero isAuthenticated={isAuthenticated} />
        <Platforms />
        <Roles />
        <Engine />
        <Numbers />
        <Styles start={start} />
        <PricingBand start={start} isAuthenticated={isAuthenticated} />
        <Editor />
        <MoreWays start={start} />
        <GetStarted start={start} />
        <Tools />
        <Faq />
        <FinalCta start={start} isAuthenticated={isAuthenticated} />
      </main>
      <Footer />
    </div>
  )
}
