import type { CSSProperties, ReactNode } from 'react'
import { useTicker, type Look, type Word } from './captionLooks'

/** Word-by-word caption line: the spoken word lights up, emphasised words use the look's accent font and colour. */
export function CaptionLine({ words, look, playing = true, scale = 1 }: { words: Word[]; look: Look; playing?: boolean; scale?: number }) {
  const active = useTicker(words.length + 2, 420, !playing)
  return (
    <p className="flex flex-wrap items-end justify-center gap-x-1.5 gap-y-0.5 text-center leading-none">
      {words.map((w, i) => {
        const on = !playing || i <= active
        const style: CSSProperties = {
          fontFamily: `'${w.em ? look.emFont : look.font}', sans-serif`,
          fontSize: (w.em ? look.emSize : look.size) * scale,
          fontWeight: 800,
          color: w.em ? look.emColor : look.color,
          textTransform: w.em && look.upper ? 'uppercase' : undefined,
          textShadow: look.glow && w.em ? '0 0 8px rgba(255,46,46,.9), 0 0 16px rgba(255,46,46,.5)' : '0 2px 0 rgba(0,0,0,.55)',
          opacity: on ? 1 : 0.25,
          transform: playing && i === active ? 'scale(1.12)' : 'scale(1)',
          transition: 'opacity .15s, transform .15s',
          ...(look.marker && w.em ? { background: '#FACC15', padding: '0 6px', borderRadius: '255px 15px 225px 15px/15px 225px 15px 255px', textShadow: 'none' } : {}),
        }
        return <span key={i} style={style}>{w.t}</span>
      })}
    </p>
  )
}

/** Flat illustrated "video frame" (shapes only) with a caption line near the bottom. */
export function Frame({ scene, words, look, ratio = 'aspect-[9/16]', playing = true, scale = 1, children }: {
  scene: [string, string, string]; words: Word[]; look: Look; ratio?: string; playing?: boolean; scale?: number; children?: ReactNode
}) {
  const [bg, a, b] = scene
  return (
    <div className={`relative ${ratio} w-full overflow-hidden rounded-2xl`} style={{ background: bg }}>
      <div className="absolute -bottom-10 -left-8 h-1/2 w-2/3 rounded-full" style={{ background: a }} />
      <div className="absolute -right-6 top-[12%] h-1/3 w-1/2 rounded-full" style={{ background: b }} />
      <div className="absolute inset-x-[30%] top-[22%] h-[30%] rounded-t-full bg-ink/80" />
      <div className="absolute inset-x-3 bottom-[16%]"><CaptionLine words={words} look={look} playing={playing} scale={scale} /></div>
      {children}
    </div>
  )
}

/** Infinite horizontal strip; the content is rendered twice so the -50% loop is seamless. Pauses on hover. */
export function Marquee({ children, reverse }: { children: ReactNode; reverse?: boolean }) {
  return (
    <div className="group relative overflow-hidden [mask-image:linear-gradient(to_right,transparent,black_8%,black_92%,transparent)]">
      <div className={`flex w-max animate-marquee gap-4 group-hover:[animation-play-state:paused] motion-reduce:animate-none ${reverse ? '[animation-direction:reverse]' : ''}`}>
        <div className="flex gap-4">{children}</div>
        <div className="flex gap-4" aria-hidden>{children}</div>
      </div>
    </div>
  )
}
