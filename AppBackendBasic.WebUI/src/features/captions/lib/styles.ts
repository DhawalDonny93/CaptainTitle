import type { CaptionTheme } from '../types'

/** Names the server stores for libass that differ from the Google-Fonts family the browser preview loads. */
const CSS_FAMILY_ALIAS: Record<string, string> = { 'Baloo 2 ExtraBold': 'Baloo 2', 'Mukta ExtraBold': 'Mukta' }

/** CSS font-family stack for a font family name stored in the catalog (the server stores the ASS/libass family name). */
export const cssFamily = (family: string) => `'${CSS_FAMILY_ALIAS[family] ?? family}', sans-serif`

/** The two fixed motion-graphic templates (look + motion live in CaptionStudio / the server's AssSubtitleWriter, not in the catalog row). */
export const STREET_GLOW = 'street_glow'
export const DOODLE_MARKER = 'doodle_marker'
export const isFixedTemplate = (theme: string) => theme === STREET_GLOW || theme === DOODLE_MARKER
export const DEFAULT_WORD_SIZE = 66

/**
 * Colour/size baked into a word for a template and emphasis tier (mirror of ThemeStyle.BakeFor on the server).
 * Street Glow is line-based and ignores word colour/size, so it just gets a harmless white/66.
 */
export function bakeWordStyle(theme: CaptionTheme | undefined, emphasis: string | undefined): { color: string; font_size: number } {
  if (theme?.tiers) {
    const tier = (emphasis && theme.tiers[emphasis]) || theme.tiers.support
    return { color: tier.color, font_size: tier.size }
  }
  return { color: '#FFFFFF', font_size: DEFAULT_WORD_SIZE }
}
