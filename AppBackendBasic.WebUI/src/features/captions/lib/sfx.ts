import type { SfxType } from '../types'

// No licensed SFX audio is bundled: each marker plays a short synthesized tone. The tone parameters now come from
// the admin-managed catalog (freq / sweep_to / duration) - the same values the server uses for the export render,
// so editor preview and export can no longer drift apart (they were hand-synced in the old app).

let sharedContext: AudioContext | null = null

function getContext(): AudioContext | null {
  const Ctor = window.AudioContext || (window as unknown as { webkitAudioContext?: typeof AudioContext }).webkitAudioContext
  if (!Ctor) return null
  if (!sharedContext || sharedContext.state === 'closed') sharedContext = new Ctor()
  if (sharedContext.state === 'suspended') sharedContext.resume().catch(() => undefined)
  return sharedContext
}

export function playSfx(meta: SfxType | undefined) {
  const ctx = getContext()
  if (!ctx || !meta) return
  const now = ctx.currentTime
  const osc = ctx.createOscillator()
  const gain = ctx.createGain()
  osc.type = meta.kind === 'drop' ? 'sine' : meta.kind === 'click' ? 'square' : 'triangle'
  osc.frequency.setValueAtTime(meta.freq, now)
  // Ramp timing per preview shape (matches the original sfx.js: sweep 0.18s, drop 0.3s, chime 0.12s).
  const rampSeconds = { sweep: 0.18, drop: 0.3, chime: 0.12 }[meta.kind as 'sweep' | 'drop' | 'chime']
  if (rampSeconds && meta.sweep_to > 0 && meta.sweep_to !== meta.freq) osc.frequency.exponentialRampToValueAtTime(meta.sweep_to, now + rampSeconds)
  const duration = meta.duration
  gain.gain.setValueAtTime(0.0001, now)
  gain.gain.exponentialRampToValueAtTime(0.3, now + 0.015)
  gain.gain.exponentialRampToValueAtTime(0.0001, now + duration)
  osc.connect(gain)
  gain.connect(ctx.destination)
  osc.start(now)
  osc.stop(now + duration + 0.05)
}
