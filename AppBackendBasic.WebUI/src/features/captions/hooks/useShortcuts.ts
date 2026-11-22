import { useEffect, useRef } from 'react'
import { comboFromEvent, SHORTCUT_ACTIONS, SHORTCUT_BINDINGS, type ShortcutId } from '../lib/shortcuts'

/** Shortcuts only ever act on the studio's own pages. */
const STUDIO_PATH = /^\/studio(\/|$)/

/** A handler returns false when it had nothing to do, so the key falls through to the next action bound to it (or to the browser). */
export type ShortcutHandlers = Partial<Record<ShortcutId, () => boolean | void>>

function focusKind(target: EventTarget | null): 'text' | 'widget' | null {
  const el = target as HTMLElement | null
  if (!el || !el.tagName) return null
  if (el.isContentEditable || el.tagName === 'TEXTAREA') return 'text'
  if (el.tagName === 'SELECT') return 'widget'
  if (el.tagName === 'INPUT') {
    const type = (el as HTMLInputElement).type
    return ['range', 'checkbox', 'radio', 'color', 'button', 'submit', 'reset', 'file'].includes(type) ? 'widget' : 'text'
  }
  return null
}

/**
 * One window keydown listener for every studio shortcut. It only acts on /studio URLs and steps aside while a modal dialog is open (each dialog
 * handles its own keys), while typing in a text field (except Mod combos marked inInput), and for plain keys on a focused slider or select.
 */
export function useStudioShortcuts(handlers: ShortcutHandlers, enabled = true) {
  const handlersRef = useRef(handlers)
  handlersRef.current = handlers

  useEffect(() => {
    if (!enabled) return undefined
    const onKeyDown = (e: KeyboardEvent) => {
      if (e.defaultPrevented || e.isComposing || !STUDIO_PATH.test(window.location.pathname)) return
      if (document.querySelector('[aria-modal="true"]')) return
      const combo = comboFromEvent(e)
      if (!combo) return
      const focus = focusKind(e.target)
      const usesMod = combo.startsWith('Mod+')
      for (const action of SHORTCUT_ACTIONS) {
        if (!SHORTCUT_BINDINGS[action.id].includes(combo)) continue
        if (focus === 'text' && !(action.inInput && usesMod)) continue
        if (focus === 'widget' && !usesMod) continue
        if (e.repeat && !action.repeat) { e.preventDefault(); return }
        const handler = handlersRef.current[action.id]
        if (handler && handler() !== false) { e.preventDefault(); return }
      }
    }
    window.addEventListener('keydown', onKeyDown)
    return () => window.removeEventListener('keydown', onKeyDown)
  }, [enabled])
}
