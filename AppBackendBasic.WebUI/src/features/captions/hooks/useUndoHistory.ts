import { useCallback, useEffect, useRef, useState } from 'react'

/** Edits closer together than this become ONE undo step, so a slider drag or a typing burst undoes in one go. */
const COALESCE_MS = 700
const MAX_STEPS = 100

/**
 * Undo / redo over an immutable document snapshot (the studio's autosave payload). Every change to `doc` that did not come from undo/redo
 * records the previous snapshot; `apply` must put a snapshot back into the editor's state. The keys (Ctrl/Cmd+Z, Ctrl/Cmd+Shift+Z, Ctrl/Cmd+Y
 * by default) are studio shortcuts (lib/shortcuts.ts), which skip text fields so the browser's own text undo stays in charge there.
 */
export function useUndoHistory<T>(doc: T, apply: (snapshot: T) => void, enabled = true) {
  const undoStack = useRef<T[]>([])
  const redoStack = useRef<T[]>([])
  const previous = useRef(doc)
  const lastPushAt = useRef(0)
  const applying = useRef(false)
  const [, setVersion] = useState(0)
  const bump = () => setVersion((v) => v + 1)

  useEffect(() => {
    if (previous.current === doc) return
    if (applying.current) {
      applying.current = false
    } else {
      const now = Date.now()
      // Within the coalesce window the step that is already on the stack keeps the state from before the burst started.
      if (now - lastPushAt.current > COALESCE_MS || undoStack.current.length === 0) {
        undoStack.current.push(previous.current)
        if (undoStack.current.length > MAX_STEPS) undoStack.current.shift()
      }
      lastPushAt.current = now
      if (redoStack.current.length) redoStack.current = []
      bump()
    }
    previous.current = doc
  }, [doc])

  const move = useCallback((from: React.MutableRefObject<T[]>, to: React.MutableRefObject<T[]>) => {
    const snapshot = from.current.pop()
    if (snapshot === undefined) return
    to.current.push(previous.current)
    applying.current = true
    lastPushAt.current = 0 // the next real edit always starts a new step
    apply(snapshot)
    bump()
  }, [apply])

  const undo = useCallback(() => { if (enabled) move(undoStack, redoStack) }, [enabled, move])
  const redo = useCallback(() => { if (enabled) move(redoStack, undoStack) }, [enabled, move])

  return { undo, redo, canUndo: enabled && undoStack.current.length > 0, canRedo: enabled && redoStack.current.length > 0 }
}
