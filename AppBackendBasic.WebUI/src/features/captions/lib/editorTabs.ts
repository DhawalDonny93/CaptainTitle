import type { LeftTab } from '../components/LeftRail'
import type { ThemeTab } from '../components/ThemePanel'

// A saved editor state can outlive the tabs that existed when it was written, so it is checked before being trusted.
const LEFT_TABS: readonly string[] = ['media', 'audio', 'text', 'elements', 'captions', 'adjustment', 'ai'] satisfies LeftTab[]
const THEME_TABS: readonly string[] = ['templates', 'customize', 'text', 'transform'] satisfies ThemeTab[]

export const isLeftTab = (value: string | null | undefined): value is LeftTab => LEFT_TABS.includes(value ?? '')
export const isThemeTab = (value: string | null | undefined): value is ThemeTab => THEME_TABS.includes(value ?? '')
