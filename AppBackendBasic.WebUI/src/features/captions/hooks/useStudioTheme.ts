import { useCallback, useEffect, useState } from 'react'

/** Studio colour themes - the ids match the [data-ct-theme] blocks in studio-themes.css. */
export const STUDIO_THEMES = [
  { id: 'vscode-light', label: 'Studio Light 1' },
  { id: 'vs-light', label: 'Studio Light 2' },
  { id: 'snow', label: 'Snow' },
  { id: 'paper', label: 'Paper' },
  { id: 'solarized', label: 'Solarized Light' },
  { id: 'dark', label: 'Studio Dark' },
] as const

export type StudioThemeId = (typeof STUDIO_THEMES)[number]['id']

const STORAGE_KEY = 'ct-studio-theme'
const DEFAULT_THEME: StudioThemeId = 'vscode-light'

const readStored = (): StudioThemeId => {
  try {
    const saved = localStorage.getItem(STORAGE_KEY)
    return STUDIO_THEMES.some((t) => t.id === saved) ? (saved as StudioThemeId) : DEFAULT_THEME
  } catch {
    return DEFAULT_THEME
  }
}

/** Picks the studio theme, remembers it in localStorage and applies it on <html> so portalled dialogs/popovers follow it too. */
export function useStudioTheme() {
  const [theme, setThemeState] = useState<StudioThemeId>(readStored)

  useEffect(() => {
    const root = document.documentElement
    root.dataset.ctTheme = theme
    window.dispatchEvent(new CustomEvent('ct-theme-change', { detail: theme }))
    return () => { delete root.dataset.ctTheme }
  }, [theme])

  const setTheme = useCallback((next: StudioThemeId) => {
    setThemeState(next)
    try { localStorage.setItem(STORAGE_KEY, next) } catch { /* private mode: theme just won't persist */ }
  }, [])

  return { theme, setTheme }
}
