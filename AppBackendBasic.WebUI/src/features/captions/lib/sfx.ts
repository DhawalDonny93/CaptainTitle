import type { SfxType } from '../types'

// No licensed SFX audio is bundled, so every marker plays a synthesized stand-in. Mirrors AssSubtitleWriter's/FfmpegRenderArgs.SfxSourceFilter
// technique for technique so the editor preview and the export sound alike: whoosh / pop / shutter are filtered white noise (a pure tone sweep
// reads as a generic beep), bass_drop is a pitch-dropping sine with soft saturation, cash is two short bell notes ("cha-ching").
// Any other (admin-defined) type keeps the previous generic behaviour: a tone sweep from its catalog freq / sweep_to / duration.

let sharedContext: AudioContext | null = null

function getContext(): AudioContext | null {
  const Ctor = window.AudioContext || (window as unknown as { webkitAudioContext?: typeof AudioContext }).webkitAudioContext
  if (!Ctor) return null
  if (!sharedContext || sharedContext.state === 'closed') sharedContext = new Ctor()
  if (sharedContext.state === 'suspended') sharedContext.resume().catch(() => undefined)
  return sharedContext
}

function noiseBurst(ctx: AudioContext, duration: number, amplitude: number) {
  const length = Math.max(1, Math.round(ctx.sampleRate * duration))
  const buffer = ctx.createBuffer(1, length, ctx.sampleRate)
  const data = buffer.getChannelData(0)
  for (let i = 0; i < length; i++) data[i] = (Math.random() * 2 - 1) * amplitude
  const source = ctx.createBufferSource()
  source.buffer = buffer
  return source
}

// Linear attack then exponential decay on a GainNode (same shape as the server's afade / aevalsrc chains).
function applyEnvelope(gain: GainNode, now: number, attack: number, duration: number, peak: number) {
  gain.gain.setValueAtTime(0.0001, now)
  gain.gain.linearRampToValueAtTime(peak, now + attack)
  gain.gain.exponentialRampToValueAtTime(0.0001, now + duration)
}

function playFilteredNoise(ctx: AudioContext, now: number, opts: { duration: number; amplitude: number; type: BiquadFilterType; freq: number; q?: number; attack: number; peak: number }) {
  const source = noiseBurst(ctx, opts.duration, opts.amplitude)
  const filter = ctx.createBiquadFilter()
  filter.type = opts.type
  filter.frequency.value = opts.freq
  if (opts.q) filter.Q.value = opts.q
  const gain = ctx.createGain()
  applyEnvelope(gain, now, opts.attack, opts.duration, opts.peak)
  source.connect(filter); filter.connect(gain); gain.connect(ctx.destination)
  source.start(now); source.stop(now + opts.duration + 0.01)
}

function playTone(ctx: AudioContext, now: number, freq: number, duration: number, peak: number, startAt = 0) {
  const osc = ctx.createOscillator()
  osc.type = 'sine'
  osc.frequency.value = freq
  const gain = ctx.createGain()
  applyEnvelope(gain, now + startAt, 0.01, duration, peak)
  osc.connect(gain); gain.connect(ctx.destination)
  osc.start(now + startAt); osc.stop(now + startAt + duration + 0.02)
}

function playBassDrop(ctx: AudioContext, now: number) {
  const osc = ctx.createOscillator()
  osc.type = 'sine'
  osc.frequency.setValueAtTime(90, now)
  osc.frequency.exponentialRampToValueAtTime(27, now + 0.4)
  // soft tanh-style saturation, the role asoftclip plays on the server
  const shaper = ctx.createWaveShaper()
  const curve = new Float32Array(1024)
  for (let i = 0; i < curve.length; i++) {
    const x = (i / (curve.length - 1)) * 2 - 1
    curve[i] = Math.tanh(x * 1.6) / Math.tanh(1.6)
  }
  shaper.curve = curve
  const gain = ctx.createGain()
  applyEnvelope(gain, now, 0.015, 0.4, 0.6)
  osc.connect(shaper); shaper.connect(gain); gain.connect(ctx.destination)
  osc.start(now); osc.stop(now + 0.42)
}

// Generic tone sweep for admin-defined SFX types.
function playGenericTone(ctx: AudioContext, now: number, meta: SfxType) {
  const osc = ctx.createOscillator()
  const gain = ctx.createGain()
  osc.type = meta.kind === 'drop' ? 'sine' : meta.kind === 'click' ? 'square' : 'triangle'
  osc.frequency.setValueAtTime(meta.freq, now)
  const rampSeconds = { sweep: 0.18, drop: 0.3, chime: 0.12 }[meta.kind as 'sweep' | 'drop' | 'chime']
  if (rampSeconds && meta.sweep_to > 0 && meta.sweep_to !== meta.freq) osc.frequency.exponentialRampToValueAtTime(meta.sweep_to, now + rampSeconds)
  gain.gain.setValueAtTime(0.0001, now)
  gain.gain.exponentialRampToValueAtTime(0.3, now + 0.015)
  gain.gain.exponentialRampToValueAtTime(0.0001, now + meta.duration)
  osc.connect(gain); gain.connect(ctx.destination)
  osc.start(now); osc.stop(now + meta.duration + 0.05)
}

export function playSfx(meta: SfxType | undefined) {
  const ctx = getContext()
  if (!ctx || !meta) return
  const now = ctx.currentTime
  switch (meta.id) {
    case 'whoosh': return playFilteredNoise(ctx, now, { duration: 0.3, amplitude: 0.6, type: 'bandpass', freq: 1100, q: 1.2, attack: 0.02, peak: 0.5 })
    case 'pop': return playFilteredNoise(ctx, now, { duration: 0.08, amplitude: 0.5, type: 'bandpass', freq: 2200, q: 1.2, attack: 0.002, peak: 0.6 })
    case 'shutter': return playFilteredNoise(ctx, now, { duration: 0.05, amplitude: 0.5, type: 'highpass', freq: 3500, attack: 0.001, peak: 0.5 })
    case 'cash':
      playTone(ctx, now, 880, 0.15, 0.5)
      return playTone(ctx, now, 1320, 0.15, 0.4, 0.07) // second bell note ~70ms later
    case 'bass_drop': return playBassDrop(ctx, now)
    default: return playGenericTone(ctx, now, meta)
  }
}
