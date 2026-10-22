import type { CaptionTheme } from '../types'

/** CSS font-family stack for a font family name stored in the catalog (the server stores the ASS/libass family name). */
export const cssFamily = (family: string) => `'${family}', sans-serif`

/** Colour/size baked into a word for a template and emphasis tier (mirror of ThemeStyle.BakeFor on the server). */
export function bakeWordStyle(theme: CaptionTheme, emphasis: string | undefined, important: boolean): { color: string; font_size: number } {
  if (theme.tiers) {
    const tier = (emphasis && theme.tiers[emphasis]) || theme.tiers.support
    return { color: tier.color, font_size: tier.size }
  }
  return important ? { color: theme.color, font_size: 62 } : { color: theme.base_color ?? '#FFFFFF', font_size: 52 }
}
