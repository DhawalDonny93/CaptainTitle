import type { CaptionStyleTab, LeftTab } from '../components/LeftRail'
import type { ThemeTab } from '../components/ThemePanel'
import type { TextMode } from '../components/TextPanel'

// A saved editor state can outlive the tabs that existed when it was written, so it is checked before being trusted.
const LEFT_TABS: readonly string[] = ['captions', 'text', 'audio', 'ratio', 'adjustment', 'ai',
  'templates', 'customize', 'transform'] satisfies LeftTab[]
const THEME_TABS: readonly string[] = ['templates', 'customize', 'transform'] satisfies ThemeTab[]

export const isLeftTab = (value: string | null | undefined): value is LeftTab => LEFT_TABS.includes(value ?? '')
export const isThemeTab = (value: string | null | undefined): value is ThemeTab => THEME_TABS.includes(value ?? '')

// Tabs that were merged or removed: the old "Word" tool is now the Text tool's caption-word mode, and the old "Text" tool its
// overlay mode (a state saved on the old Text tool simply reopens the Text tool). "Media" and "Elements" are gone, so a state saved on one of them opens on Captions.
export const restoreLeftTab = (value: string | null | undefined): { tab: LeftTab; textMode?: TextMode } => {
  if (value === 'word') return { tab: 'text', textMode: 'word' }
  return { tab: isLeftTab(value) ? value : 'captions' }
}

// The caption style tools live in the left rail; ThemePanel (and the saved editor state's "theme_tab") use the same ids.
export const isCaptionStyleTab = (tab: LeftTab): tab is CaptionStyleTab => THEME_TABS.includes(tab)
export const themeTabOf = (tab: CaptionStyleTab): ThemeTab => tab
