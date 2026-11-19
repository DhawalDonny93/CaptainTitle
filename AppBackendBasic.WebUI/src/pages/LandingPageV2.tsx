import { useEffect, useState, type ReactNode } from 'react'
import { useNavigate } from 'react-router-dom'
import { motion, AnimatePresence } from 'framer-motion'
import { ArrowRight, Check, ChevronDown, Film, Languages, Loader2, Menu, Mic, Sparkles, Upload, Wand2, X } from 'lucide-react'
import Footer from '../components/Footer'
import { Logo } from '../components/Header'
import { useAuth } from '../contexts/AuthContext'
import { LANDING_FAQ } from './landingContent'
import { Frame, Marquee } from '../components/landing/CaptionMock'
import { LOOKS, useTicker, type Word } from '../components/landing/captionLooks'

/**
 * Alternative landing page (/home-v2): a calmer, product-led layout with two-line headlines, numbered sample tabs, a mock studio,
 * chip-filtered feature blocks and marquees. Uses the Captain Title palette (flat colours, no gradients); every caption shown is
 * an illustration built from text and shapes, not a real customer video.
 */

// ---------- shared bits ----------

function Chip({ active, onClick, children }: { active: boolean; onClick: () => void; children: ReactNode }) {
  return (
    <button onClick={onClick} aria-pressed={active}
      className={`rounded-full border px-4 py-1.5 text-sm font-semibold transition-colors ${active ? 'border-ink bg-ink text-paper' : 'border-ink/15 bg-white text-ink hover:border-ink/40'}`}>
      {children}
    </button>
  )
}

/** Two-line section heading: plain first line, accented second line. */
function Heading({ a, b, sub, center }: { a: string; b: string; sub?: string; center?: boolean }) {
  return (
    <div className={center ? 'mx-auto max-w-3xl text-center' : 'max-w-3xl'}>
      <h2 className="font-display text-4xl font-extrabold leading-[1.05] tracking-tight sm:text-5xl">
        {a}<br /><span className="text-cherry">{b}</span>
      </h2>
      {sub && <p className="mt-4 text-lg text-ink/70">{sub}</p>}
    </div>
  )
}

function CtaLink({ onClick, children }: { onClick: () => void; children: ReactNode }) {
  return (
    <button onClick={onClick} className="group mt-8 inline-flex items-center gap-2 rounded-full border border-ink/15 bg-white px-5 py-2.5 font-semibold transition-colors hover:border-ink">
      {children} <ArrowRight className="h-4 w-4 transition-transform group-hover:translate-x-0.5" />
    </button>
  )
}

// ---------- content ----------

const SAMPLES = [
  { key: 'talking', label: 'Talking head', meta: '9:16 · 00:32', look: LOOKS.viral, scene: ['#FF8A00', '#16C47F', '#FFD6DE'] as [string, string, string],
    words: [{ t: 'yeh' }, { t: 'trick' }, { t: 'sach', em: true }, { t: 'mein' }, { t: 'kaam' }, { t: 'karta', em: true }, { t: 'hai' }] },
  { key: 'podcast', label: 'Podcast clip', meta: '1:1 · 00:58', look: LOOKS.cinematic, scene: ['#14110F', '#3A2E25', '#FF2D4D'] as [string, string, string],
    words: [{ t: 'sabse' }, { t: 'badi' }, { t: 'galti', em: true }, { t: 'thi' }, { t: 'wait', em: true }, { t: 'karna' }] },
  { key: 'product', label: 'Product reel', meta: '4:5 · 00:15', look: LOOKS.vogue, scene: ['#FFD6DE', '#FF2D4D', '#FFFFFF'] as [string, string, string],
    words: [{ t: 'only' }, { t: '₹499' }, { t: 'today', em: true }, { t: 'link' }, { t: 'in' }, { t: 'bio', em: true }] },
  { key: 'vlog', label: 'Daily vlog', meta: '9:16 · 01:40', look: LOOKS.doodle, scene: ['#16C47F', '#FFD60A', '#FFEFC9'] as [string, string, string],
    words: [{ t: 'aaj' }, { t: 'ka' }, { t: 'plan', em: true }, { t: 'ekdum' }, { t: 'simple' }] },
]

const PIPELINE = ['Upload', 'Transcribe', 'Hinglish', 'Word timing', 'Emphasis', 'Style', 'Export']

const AGENT_STEPS = [
  { t: 'Listening to your clip', d: '00:32 · Hindi + English' },
  { t: 'Writing it in Hinglish', d: '86 words, Roman script' },
  { t: 'Timing every word', d: '3 words flagged to check' },
  { t: 'Picking words that should pop', d: 'sach, karta, kaam' },
  { t: 'Styling with Viral', d: 'Anton + Poppins' },
]

const STYLES = [
  { name: 'Viral', tag: 'Bold', look: LOOKS.viral, scene: ['#14110F', '#2A2420', '#FF2D4D'] as [string, string, string], words: [{ t: 'yeh' }, { t: 'BAHUT', em: true }, { t: 'zabardast' }] },
  { name: 'Street Glow', tag: 'Bold', look: LOOKS.glow, scene: ['#14110F', '#24201C', '#3A2E25'] as [string, string, string], words: [{ t: 'stay', em: true }, { t: 'hungry' }] },
  { name: 'Cinematic', tag: 'Clean', look: LOOKS.cinematic, scene: ['#2A2420', '#14110F', '#FF8A00'] as [string, string, string], words: [{ t: 'kuch' }, { t: 'khaas', em: true }, { t: 'hai' }] },
  { name: 'Clean Vogue', tag: 'Clean', look: LOOKS.vogue, scene: ['#14110F', '#3A2E25', '#FFD6DE'] as [string, string, string], words: [{ t: 'simple' }, { t: 'HOOK', em: true }] },
  { name: 'Meme Mix', tag: 'Playful', look: LOOKS.meme, scene: ['#FF8A00', '#14110F', '#FFD60A'] as [string, string, string], words: [{ t: 'bro' }, { t: 'WAIT', em: true }, { t: 'what' }] },
  { name: 'Doodle Marker', tag: 'Playful', look: LOOKS.doodle, scene: ['#16C47F', '#14110F', '#FFEFC9'] as [string, string, string], words: [{ t: 'this' }, { t: 'changes', em: true }] },
]

const VARIANT_WORDS: Word[] = [{ t: 'ek' }, { t: 'baar' }, { t: 'try', em: true }, { t: 'karo' }]
const VARIANTS = [
  { name: 'Original', look: LOOKS.viral },
  { name: 'Softer', look: { ...LOOKS.viral, emFont: 'Poppins', emSize: 24, emColor: '#FFD60A', upper: false } },
  { name: 'Louder', look: { ...LOOKS.viral, emSize: 44, emColor: '#FFD60A' } },
  { name: 'Remix', look: LOOKS.meme },
]

const EXTRAS = ['Anton', 'Bebas Neue', 'Caveat', 'Permanent Marker', 'Poppins', 'Playfair Display', 'Whoosh SFX', 'Pop SFX', 'Shutter SFX', 'Cash SFX', 'Bass drop SFX', 'Stickers', 'Text layers', 'SRT export']

const AUDIENCES = [
  { icon: Mic, tag: 'Podcasters', title: 'Clip the best minute', body: 'Turn a long episode into captioned shorts that make sense with the sound off.' },
  { icon: Film, tag: 'Creators', title: 'Post every day', body: 'Captions done in minutes, not an evening of typing and nudging timestamps.' },
  { icon: Wand2, tag: 'Editors', title: 'Hand off clean files', body: 'Burned-in MP4 for the client and an SRT for your own timeline.' },
  { icon: Languages, tag: 'Brands', title: 'Talk like your audience', body: 'Hinglish written the way people actually type it, not stiff transliteration.' },
]

// ---------- sections ----------

function Nav({ start, isAuthenticated }: { start: () => void; isAuthenticated: boolean }) {
  const navigate = useNavigate()
  const [open, setOpen] = useState(false)
  const links = [['#demo', 'Product'], ['#styles', 'Styles'], ['#faq', 'FAQ']] as const
  return (
    <header className="sticky top-0 z-40 border-b border-ink/10 bg-paper/90 backdrop-blur">
      <div className="mx-auto flex h-16 max-w-7xl items-center justify-between px-4 sm:px-6">
        <Logo onClick={() => navigate('/')} />
        <nav className="hidden items-center gap-8 md:flex">
          {links.map(([href, label]) => <a key={href} href={href} className="text-sm font-semibold text-ink/70 hover:text-ink">{label}</a>)}
          <button onClick={() => navigate('/pricing')} className="text-sm font-semibold text-ink/70 hover:text-ink">Pricing</button>
        </nav>
        <div className="hidden items-center gap-2 md:flex">
          {!isAuthenticated && <button onClick={() => navigate('/login')} className="rounded-full px-4 py-2 text-sm font-semibold hover:bg-ink/5">Sign in</button>}
          <button onClick={start} className="rounded-full bg-ink px-5 py-2 text-sm font-bold text-paper hover:bg-cherry">{isAuthenticated ? 'Open studio' : 'Start for free'}</button>
        </div>
        <button className="md:hidden" onClick={() => setOpen(!open)} aria-label="Menu" aria-expanded={open}>{open ? <X className="h-6 w-6" /> : <Menu className="h-6 w-6" />}</button>
      </div>
      {open && (
        <div className="grid gap-3 border-t border-ink/10 px-4 py-4 md:hidden">
          {links.map(([href, label]) => <a key={href} href={href} onClick={() => setOpen(false)} className="font-semibold">{label}</a>)}
          <button onClick={() => navigate('/pricing')} className="text-left font-semibold">Pricing</button>
          <button onClick={start} className="rounded-full bg-ink py-2.5 font-bold text-paper">{isAuthenticated ? 'Open studio' : 'Start for free'}</button>
        </div>
      )}
    </header>
  )
}

function Hero({ start, isAuthenticated }: { start: () => void; isAuthenticated: boolean }) {
  const [tab, setTab] = useState(0)
  return (
    <section className="mx-auto max-w-7xl px-4 pb-20 pt-16 sm:px-6 lg:pt-24">
      <div className="mx-auto max-w-4xl text-center">
        <motion.h1 initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.5 }}
          className="font-display text-5xl font-extrabold leading-[1.02] tracking-tight sm:text-7xl">
          Captions that<br /><span className="text-cherry">carry the hook.</span>
        </motion.h1>
        <p className="mx-auto mt-6 max-w-2xl text-lg text-ink/70 sm:text-xl">
          Word-perfect Hinglish captions, timed word by word and styled to stop the scroll. Upload a clip, tweak anything, export an MP4.
        </p>
        <div className="mt-8 flex flex-wrap justify-center gap-3">
          <button onClick={start} className="inline-flex items-center gap-2 rounded-full bg-cherry px-7 py-3.5 font-bold text-white hover:bg-ink">
            {isAuthenticated ? 'Open studio' : 'Start for free'} <ArrowRight className="h-5 w-5" />
          </button>
          <a href="#demo" className="rounded-full border border-ink/20 bg-white px-7 py-3.5 font-bold hover:border-ink">See it work</a>
        </div>
        <p className="mt-4 text-sm text-ink/60">First video free, up to 45 seconds. No card needed.</p>
      </div>

      <div className="mt-14 flex flex-wrap justify-center gap-2" role="tablist" aria-label="Sample videos">
        {SAMPLES.map((s, i) => (
          <button key={s.key} role="tab" aria-selected={tab === i} onClick={() => setTab(i)}
            className={`rounded-full px-4 py-2 text-sm font-semibold transition-colors ${tab === i ? 'bg-ink text-paper' : 'text-ink/60 hover:text-ink'}`}>
            <span className="mr-1.5 font-display tabular-nums opacity-60">{String(i + 1).padStart(2, '0')}</span>{s.label}
          </button>
        ))}
      </div>
      <div className="-mx-4 mt-8 flex snap-x gap-4 overflow-x-auto px-4 pb-2 sm:mx-0 sm:grid sm:grid-cols-2 sm:overflow-visible sm:px-0 lg:grid-cols-4">
        {SAMPLES.map((s, i) => (
          <button key={s.key} onClick={() => setTab(i)} className={`w-56 flex-shrink-0 snap-center text-left transition-all sm:w-auto ${tab === i ? '' : 'opacity-50 hover:opacity-80'}`}>
            <div className={`rounded-3xl bg-white p-2 transition-shadow ${tab === i ? 'shadow-xl ring-2 ring-ink' : 'shadow-sm ring-1 ring-ink/10'}`}>
              <Frame scene={s.scene} words={s.words} look={s.look} playing={tab === i} />
            </div>
            <p className="mt-3 px-1 font-semibold">{s.label}</p>
            <p className="px-1 text-sm text-ink/50">{s.meta}</p>
          </button>
        ))}
      </div>
    </section>
  )
}

function DirectorSection({ start }: { start: () => void }) {
  const step = useTicker(AGENT_STEPS.length + 2, 1300)
  const done = Math.min(step, AGENT_STEPS.length)
  return (
    <section id="demo" className="scroll-mt-20 border-y border-ink/10 bg-white">
      <div className="mx-auto max-w-7xl px-4 py-20 sm:px-6 lg:py-28">
        <Heading center a="Drop a clip." b="Get a finished reel." sub="Captain Title listens, writes, times and styles. You only step in where you want to." />
        <button onClick={start} className="mx-auto mt-10 flex w-full max-w-2xl items-center gap-3 rounded-2xl border border-ink/15 bg-paper px-4 py-3 text-left transition-colors hover:border-ink">
          <Upload className="h-5 w-5 flex-shrink-0 text-ink/50" />
          <span className="flex-1 truncate text-ink/50">Drop an MP4 or MOV, up to two minutes…</span>
          <span className="flex-shrink-0 rounded-xl bg-cherry px-4 py-2 text-sm font-bold text-white">Start your first video</span>
        </button>

        {/* Mock studio */}
        <div className="mx-auto mt-14 grid max-w-6xl overflow-hidden rounded-3xl border border-ink/10 bg-paper shadow-xl lg:grid-cols-[1fr_1.1fr]">
          <div className="border-b border-ink/10 p-6 lg:border-b-0 lg:border-r">
            <p className="text-xs font-bold uppercase tracking-widest text-ink/50">Captain</p>
            <ul className="mt-5 grid gap-4">
              {AGENT_STEPS.map((s, i) => (
                <li key={s.t} className={`flex gap-3 transition-opacity ${i <= done ? 'opacity-100' : 'opacity-30'}`}>
                  <span className={`mt-0.5 grid h-6 w-6 flex-shrink-0 place-items-center rounded-full ${i < done ? 'bg-mint text-white' : 'bg-ink/10'}`}>
                    {i < done ? <Check className="h-3.5 w-3.5" /> : i === done ? <Loader2 className="h-3.5 w-3.5 animate-spin motion-reduce:animate-none" /> : null}
                  </span>
                  <div><p className="font-semibold">{s.t}</p><p className="text-sm text-ink/50">{s.d}</p></div>
                </li>
              ))}
            </ul>
          </div>
          <div className="p-6">
            <div className="mx-auto max-w-[220px]">
              <Frame scene={SAMPLES[0].scene} words={SAMPLES[0].words} look={done >= 4 ? LOOKS.viral : { ...LOOKS.viral, emFont: 'Poppins', emSize: 18, emColor: '#FFFFFF', upper: false }} />
            </div>
            <div className="mt-6 flex gap-1 overflow-hidden rounded-xl bg-white p-2 ring-1 ring-ink/10">
              {SAMPLES[0].words.map((w, i) => (
                <span key={i} className={`flex-1 truncate rounded-md px-1.5 py-1 text-center text-xs font-semibold ${done >= 3 && w.em ? 'bg-cherry text-white' : 'bg-butter'}`}>{w.t}</span>
              ))}
            </div>
          </div>
        </div>

        <ol className="mx-auto mt-10 flex max-w-5xl flex-wrap items-center justify-center gap-x-2 gap-y-3 text-sm font-semibold">
          {PIPELINE.map((p, i) => (
            <li key={p} className="flex items-center gap-2">
              <span className="rounded-full border border-ink/15 bg-paper px-3 py-1">{p}</span>
              {i < PIPELINE.length - 1 && <ArrowRight className="h-4 w-4 text-ink/30" />}
            </li>
          ))}
        </ol>
      </div>
    </section>
  )
}

function StylesSection({ start }: { start: () => void }) {
  const [filter, setFilter] = useState('All')
  const shown = filter === 'All' ? STYLES : STYLES.filter((s) => s.tag === filter)
  return (
    <section id="styles" className="scroll-mt-20 py-20 lg:py-28">
      <div className="mx-auto max-w-7xl px-4 sm:px-6">
        <div className="flex flex-col gap-6 lg:flex-row lg:items-end lg:justify-between">
          <Heading a="Out of ideas?" b="Start from a style that works." />
          <div className="flex flex-wrap gap-2">{['All', 'Bold', 'Clean', 'Playful'].map((f) => <Chip key={f} active={filter === f} onClick={() => setFilter(f)}>{f}</Chip>)}</div>
        </div>
      </div>
      <div className="mt-12">
        <Marquee key={filter}>
          {(shown.length < 4 ? [...shown, ...shown] : shown).map((s, i) => (
            <div key={`${s.name}-${i}`} className="w-56 flex-shrink-0 rounded-3xl bg-white p-2 shadow-sm ring-1 ring-ink/10">
              <Frame scene={s.scene} words={s.words} look={s.look} ratio="aspect-[4/5]" />
              <div className="flex items-center justify-between px-2 pb-1 pt-3">
                <div><p className="font-semibold">{s.name}</p><p className="text-xs text-ink/50">{s.tag}</p></div>
                <span className="rounded-full bg-butter px-2.5 py-1 text-xs font-semibold">One-tap apply</span>
              </div>
            </div>
          ))}
        </Marquee>
      </div>
      <div className="mx-auto max-w-7xl px-4 sm:px-6"><CtaLink onClick={start}>Try a style</CtaLink></div>
    </section>
  )
}

function EditSection({ start }: { start: () => void }) {
  const modes = ['Text', 'Timing', 'Emphasis'] as const
  const [mode, setMode] = useState<(typeof modes)[number]>('Text')
  const words = SAMPLES[0].words
  return (
    <section className="border-y border-ink/10 bg-white">
      <div className="mx-auto grid max-w-7xl items-center gap-12 px-4 py-20 sm:px-6 lg:grid-cols-2 lg:py-28">
        <div>
          <Heading a="Point, click, fix." b="Nothing else moves." sub="Change one word, nudge its timing or make it pop. The rest of your captions stay exactly as they were." />
          <div className="mt-8 flex flex-wrap gap-2">{modes.map((m) => <Chip key={m} active={mode === m} onClick={() => setMode(m)}>{m}</Chip>)}</div>
          <CtaLink onClick={start}>Start editing</CtaLink>
        </div>
        <div className="rounded-3xl bg-paper p-6 ring-1 ring-ink/10">
          <AnimatePresence mode="wait">
            <motion.div key={mode} initial={{ opacity: 0, y: 8 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0, y: -8 }} transition={{ duration: 0.2 }}>
              {mode === 'Text' && (
                <div className="grid gap-3">
                  {words.slice(0, 4).map((w, i) => (
                    <div key={i} className={`flex items-center justify-between rounded-xl bg-white px-4 py-3 ring-1 ${i === 2 ? 'ring-2 ring-cherry' : 'ring-ink/10'}`}>
                      <span className="font-semibold">{i === 2 ? <><s className="text-ink/40">sach</s> <span className="text-cherry">sachmuch</span></> : w.t}</span>
                      {i === 2 && <span className="rounded-full bg-sun px-2.5 py-0.5 text-xs font-bold">AI unsure · check</span>}
                    </div>
                  ))}
                </div>
              )}
              {mode === 'Timing' && (
                <div>
                  <div className="relative h-24 rounded-xl bg-white ring-1 ring-ink/10">
                    {words.map((w, i) => (
                      <span key={i} className={`absolute top-1/2 -translate-y-1/2 rounded-md px-2 py-1 text-xs font-semibold ${i === 3 ? 'bg-cherry text-white ring-2 ring-ink' : 'bg-butter'}`}
                        style={{ left: `${3 + i * 13.5}%` }}>{w.t}</span>
                    ))}
                    <span className="absolute inset-y-2 w-0.5 bg-cherry" style={{ left: '44%' }} />
                  </div>
                  <p className="mt-4 text-sm text-ink/60">Drag a word on the timeline. Its neighbours keep their timing.</p>
                </div>
              )}
              {mode === 'Emphasis' && (
                <div className="grid gap-4 sm:grid-cols-2">
                  {(['Before', 'After'] as const).map((label) => (
                    <div key={label}>
                      <p className="mb-2 text-xs font-bold uppercase tracking-widest text-ink/50">{label}</p>
                      <Frame scene={SAMPLES[0].scene} words={label === 'Before' ? words.map((w) => ({ t: w.t })) : words} look={LOOKS.viral} ratio="aspect-[4/5]" playing={false} scale={0.85} />
                    </div>
                  ))}
                </div>
              )}
            </motion.div>
          </AnimatePresence>
        </div>
      </div>
    </section>
  )
}

function VariationsSection({ start }: { start: () => void }) {
  return (
    <section className="mx-auto max-w-7xl px-4 py-20 sm:px-6 lg:py-28">
      <Heading a="Like one look?" b="Try it four ways." sub="Every style comes in variants. Switch in one click and the preview updates live." />
      <div className="mt-12 grid grid-cols-2 gap-4 lg:grid-cols-4">
        {VARIANTS.map((v) => (
          <div key={v.name} className="rounded-3xl bg-white p-2 shadow-sm ring-1 ring-ink/10">
            <Frame scene={['#14110F', '#2A2420', '#FF8A00']} words={VARIANT_WORDS} look={v.look} ratio="aspect-[4/5]" />
            <p className="px-2 pb-1 pt-3 font-semibold">{v.name}</p>
          </div>
        ))}
      </div>
      <CtaLink onClick={start}>Find your look</CtaLink>
    </section>
  )
}

function FactsSection() {
  const facts = [['45s', 'First video free'], ['2 min', 'Per upload'], ['Every word', 'Timed on its own'], ['MP4 + SRT', 'Ready to post']]
  return (
    <section className="border-y border-ink/10 bg-ink text-paper">
      <div className="mx-auto max-w-7xl px-4 py-16 sm:px-6">
        <p className="text-center text-sm font-semibold uppercase tracking-widest text-paper/60">Built for Hindi, English and everything in between</p>
        <div className="mt-10 grid grid-cols-2 gap-8 text-center lg:grid-cols-4">
          {facts.map(([n, l]) => <div key={l}><p className="font-display text-4xl font-extrabold text-sun sm:text-5xl">{n}</p><p className="mt-2 text-paper/70">{l}</p></div>)}
        </div>
      </div>
      <div className="pb-14">
        <Marquee reverse>
          {EXTRAS.map((e) => <span key={e} className="flex-shrink-0 rounded-full border border-paper/20 px-5 py-2 text-sm font-semibold text-paper/80">{e}</span>)}
        </Marquee>
      </div>
    </section>
  )
}

function AudienceSection() {
  return (
    <section className="mx-auto max-w-7xl px-4 py-20 sm:px-6 lg:py-28">
      <Heading a="Made for people" b="who post every day." sub="Podcasters, creators, editors and brands use the same studio for very different jobs." />
      <div className="mt-12 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {AUDIENCES.map(({ icon: Icon, tag, title, body }) => (
          <div key={tag} className="rounded-3xl bg-white p-6 ring-1 ring-ink/10 transition-shadow hover:shadow-lg">
            <span className="inline-flex items-center gap-1.5 rounded-full bg-butter px-3 py-1 text-xs font-bold"><Icon className="h-3.5 w-3.5" />{tag}</span>
            <h3 className="mt-5 font-display text-xl font-extrabold">{title}</h3>
            <p className="mt-2 text-ink/70">{body}</p>
          </div>
        ))}
      </div>
    </section>
  )
}

function FaqSection() {
  const [open, setOpen] = useState<number | null>(0)
  return (
    <section id="faq" className="scroll-mt-20 border-t border-ink/10 bg-white">
      <div className="mx-auto max-w-3xl px-4 py-20 sm:px-6 lg:py-28">
        <Heading center a="Frequently asked" b="questions." />
        <div className="mt-10 divide-y divide-ink/10 border-y border-ink/10">
          {LANDING_FAQ.map((f, i) => (
            <div key={f.q}>
              <button onClick={() => setOpen(open === i ? null : i)} aria-expanded={open === i} className="flex w-full items-center justify-between gap-4 py-5 text-left font-display text-lg font-bold">
                {f.q}<ChevronDown className={`h-5 w-5 flex-shrink-0 transition-transform ${open === i ? 'rotate-180' : ''}`} />
              </button>
              {open === i && <p className="pb-5 text-ink/70">{f.a}</p>}
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}

function ClosingCta({ start, isAuthenticated }: { start: () => void; isAuthenticated: boolean }) {
  const navigate = useNavigate()
  return (
    <section className="mx-auto max-w-7xl px-4 py-20 sm:px-6">
      <div className="grid items-center gap-10 overflow-hidden rounded-[2rem] bg-cherry px-6 py-14 text-white sm:px-12 lg:grid-cols-[1.3fr_1fr]">
        <div>
          <h2 className="font-display text-4xl font-extrabold leading-[1.05] tracking-tight sm:text-5xl">Captions that<br /><span className="text-sun">carry the hook.</span></h2>
          <div className="mt-8 flex flex-wrap gap-3">
            <button onClick={start} className="inline-flex items-center gap-2 rounded-full bg-white px-7 py-3.5 font-bold text-ink hover:bg-sun">
              {isAuthenticated ? 'Open studio' : 'Start for free'} <ArrowRight className="h-5 w-5" />
            </button>
            <button onClick={() => navigate('/pricing')} className="rounded-full border border-white/40 px-7 py-3.5 font-bold hover:border-white">See pricing</button>
          </div>
        </div>
        <div className="grid grid-cols-4 gap-2">
          {STYLES.slice(0, 4).map((s) => <Frame key={s.name} scene={s.scene} words={s.words.slice(0, 2)} look={s.look} playing={false} scale={0.55} />)}
        </div>
      </div>
    </section>
  )
}

export default function LandingPageV2() {
  const navigate = useNavigate()
  const { isAuthenticated } = useAuth()
  const start = () => navigate(isAuthenticated ? '/studio' : '/login?mode=signup')

  useEffect(() => { window.scrollTo(0, 0) }, [])

  return (
    <div className="min-h-screen bg-paper font-sans text-ink">
      <a href="#demo" className="flex items-center justify-center gap-2 bg-ink px-4 py-2 text-center text-sm text-paper hover:text-sun">
        <Sparkles className="h-4 w-4 text-sun" /> AI emphasis now picks the words that should pop. <span className="font-semibold underline underline-offset-2">See it →</span>
      </a>
      <Nav start={start} isAuthenticated={isAuthenticated} />
      <main>
        <Hero start={start} isAuthenticated={isAuthenticated} />
        <DirectorSection start={start} />
        <StylesSection start={start} />
        <EditSection start={start} />
        <VariationsSection start={start} />
        <FactsSection />
        <AudienceSection />
        <FaqSection />
        <ClosingCta start={start} isAuthenticated={isAuthenticated} />
      </main>
      <Footer />
    </div>
  )
}
