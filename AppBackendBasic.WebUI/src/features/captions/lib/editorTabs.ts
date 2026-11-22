import type { CaptionStyleTab, LeftTab } from '../components/LeftRail'
import type { ThemeTab } from '../components/ThemePanel'

// A saved editor state can outlive the tabs that existed when it was written, so it is checked before being trusted.
const LEFT_TABS: readonly string[] = ['media', 'audio', 'text', 'elements', 'captions', 'ratio', 'adjustment', 'ai',
  'templates', 'customize', 'word', 'transform'] satisfies LeftTab[]
const THEME_TABS: readonly string[] = ['templates', 'customize', 'text', 'transform'] satisfies ThemeTab[]

export const isLeftTab = (value: string | null | undefined): value is LeftTab => LEFT_TABS.includes(value ?? '')
export const isThemeTab = (value: string | null | undefined): value is ThemeTab => THEME_TABS.includes(value ?? '')

// The caption style tools now live in the left rail. ThemePanel (and the saved editor state's "theme_tab") still use the
// panel's own ids, where the rail's 'word' tab is called 'text'.
const STYLE_TO_THEME: Record<CaptionStyleTab, ThemeTab> = { templates: 'templates', customize: 'customize', word: 'text', transform: 'transform' }
export const isCaptionStyleTab = (tab: LeftTab): tab is CaptionStyleTab => tab in STYLE_TO_THEME
export const themeTabOf = (tab: CaptionStyleTab): ThemeTab => STYLE_TO_THEME[tab]
