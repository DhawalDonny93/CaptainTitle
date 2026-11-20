import { useCallback, useEffect, useState } from 'react'

/** Studio colour themes - the ids match the [data-ct-theme] blocks in studio-themes.css. swatch = [background, accent] for the picker. */
export const STUDIO_THEMES = [
  { id: 'vscode-light', label: 'Studio Light 1', swatch: ['#f8f8f8', '#005fb8'] },
  { id: 'vs-light', label: 'Studio Light 2', swatch: ['#eeeef2', '#68217a'] },
  { id: 'snow', label: 'Snow', swatch: ['#ffffff', '#0969da'] },
  { id: 'paper', label: 'Paper', swatch: ['#faf7f2', '#e11d48'] },
  { id: 'solarized', label: 'Solarized Light', swatch: ['#fdf6e3', '#268bd2'] },
  { id: 'dark', label: 'Studio Dark', swatch: ['#0a0a0a', '#ff2d4d'] },
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
