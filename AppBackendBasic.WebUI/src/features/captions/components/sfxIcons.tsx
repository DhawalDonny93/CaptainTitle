import { Balloon, Camera, Money, SpeakerHigh, Wind, Waveform } from '@phosphor-icons/react'

const ICONS = { wind: Wind, speaker: SpeakerHigh, balloon: Balloon, money: Money, camera: Camera } as const

/** SFX icons are chosen by name in the admin catalog; unknown names fall back to a waveform glyph. */
export function SfxIcon({ name, size = 14 }: { name: string; size?: number }) {
  const Cmp = (ICONS as Record<string, typeof Wind>)[name] ?? Waveform
  return <Cmp size={size} weight="fill" />
}
