// Ratio / Color / Audio panel maths for the live preview. Mirrors AppBackendBasic.Services/Captions/FrameLayout.cs and FfmpegRenderArgs.cs:
// keep the sizes, the white-balance coefficients and the fade rule identical so the preview matches the exported MP4.

export type AspectRatio = 'original' | '9:16' | '1:1' | '4:5' | '16:9'
export type FrameFit = 'blur' | 'color' | 'crop'

export const ASPECT_RATIOS: { id: AspectRatio; label: string; hint: string }[] = [
  { id: 'original', label: 'Original', hint: 'Keep the uploaded size' },
  { id: '9:16', label: '9:16', hint: 'Reels · Shorts · TikTok' },
  { id: '1:1', label: '1:1', hint: 'Square feed post' },
  { id: '4:5', label: '4:5', hint: 'Portrait feed post' },
  { id: '16:9', label: '16:9', hint: 'YouTube · landscape' },
]

const SIZES: Record<Exclude<AspectRatio, 'original'>, [number, number]> = {
  '9:16': [1080, 1920],
  '1:1': [1080, 1080],
  '4:5': [1080, 1350],
  '16:9': [1920, 1080],
}

/** The exported frame size: the fixed size of a ratio, or the source size for "original" (1080x1920 when unknown). */
export function outputSize(ratio: AspectRatio, sourceWidth: number | null, sourceHeight: number | null): [number, number] {
  if (ratio !== 'original') return SIZES[ratio]
  return [sourceWidth || 1080, sourceHeight || 1920]
}

export type RenderResolution = 'original' | '1080' | '720' | '480'
export type RenderFormat = 'mp4' | 'mov'

/** The final encoded size: the frame scaled so its SHORT side is the resolution, both sides even. Same as FrameLayout.ScaledSize. */
export function scaledSize(resolution: RenderResolution, frameWidth: number, frameHeight: number): [number, number] {
  if (resolution === 'original' || frameWidth <= 0 || frameHeight <= 0) return [frameWidth, frameHeight]
  const shortSide = Number(resolution)
  // Math.round on a positive half rounds up, matching MidpointRounding.AwayFromZero on the server.
  const even = (v: number) => Math.max(2, Math.round(v / 2) * 2)
  return frameWidth <= frameHeight
    ? [even(shortSide), even((frameHeight * shortSide) / frameWidth)]
    : [even((frameWidth * shortSide) / frameHeight), even(shortSide)]
}

/** A short name for a frame size by its short side: 4K / QHD / Full HD / HD / SD. */
export function resolutionName(width: number, height: number): string {
  const short = Math.min(width, height)
  if (short >= 2160) return '4K'
  if (short >= 1440) return 'QHD'
  if (short >= 1080) return 'Full HD'
  if (short >= 720) return 'HD'
  return 'SD'
}

/** Warmth (-50 cool .. 50 warm) and Tint (-50 green .. 50 magenta) as per-channel gains [r, g, b]. Same as FrameLayout.WhiteBalance. */
export function whiteBalance(warmth: number, tint: number): [number, number, number] {
  const clamp = (v: number) => Math.max(-50, Math.min(50, v))
  const w = (clamp(warmth) / 50) * 0.12
  const t = (clamp(tint) / 50) * 0.1
  return [1 + w, 1 - t, 1 - w]
}

/** The original audio's fade gain (0..1) at time t: linear like ffmpeg's default afade curve. */
export function fadeGain(t: number, duration: number, fadeIn: number, fadeOut: number): number {
  let gain = 1
  if (fadeIn > 0 && t < fadeIn) gain = Math.min(gain, Math.max(0, t / fadeIn))
  if (fadeOut > 0 && duration > 0) {
    const fade = Math.min(fadeOut, duration)
    const start = duration - fade
    if (t > start) gain = Math.min(gain, Math.max(0, (duration - t) / fade))
  }
  return gain
}

export interface ColorValues { brightness: number; contrast: number; saturation: number; warmth: number; tint: number }

/** One-click looks: each preset just sets the five Color sliders (nothing else is stored), so a preset can be fine-tuned afterwards. */
export const COLOR_PRESETS: { id: string; label: string; values: ColorValues }[] = [
  { id: 'none', label: 'None', values: { brightness: 0, contrast: 100, saturation: 100, warmth: 0, tint: 0 } },
  { id: 'vivid', label: 'Vivid', values: { brightness: 4, contrast: 112, saturation: 140, warmth: 4, tint: 0 } },
  { id: 'warm', label: 'Warm', values: { brightness: 3, contrast: 104, saturation: 110, warmth: 30, tint: 5 } },
  { id: 'cool', label: 'Cool', values: { brightness: 0, contrast: 106, saturation: 95, warmth: -30, tint: -4 } },
  { id: 'cinematic', label: 'Cinematic', values: { brightness: -4, contrast: 120, saturation: 85, warmth: -12, tint: -6 } },
  { id: 'vintage', label: 'Vintage', values: { brightness: 6, contrast: 90, saturation: 70, warmth: 25, tint: 8 } },
  { id: 'fade', label: 'Faded', values: { brightness: 10, contrast: 80, saturation: 85, warmth: 5, tint: 0 } },
  { id: 'mono', label: 'Mono', values: { brightness: 0, contrast: 115, saturation: 0, warmth: 0, tint: 0 } },
]

export const sameColor = (a: ColorValues, b: ColorValues) =>
  a.brightness === b.brightness && a.contrast === b.contrast && a.saturation === b.saturation && a.warmth === b.warmth && a.tint === b.tint
