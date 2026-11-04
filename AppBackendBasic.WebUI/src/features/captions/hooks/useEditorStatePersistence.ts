import { useEffect, useRef } from 'react'
import { captionsApi } from '../captionsApi'
import type { EditorState } from '../types'

const SAVE_EVERY_MS = 8000

/** Remembers where the user left off (playhead, open tabs). Failures are ignored: losing a position must never get in the way of editing. */
export function useEditorStatePersistence(projectId: number, getState: () => EditorState) {
  const getStateRef = useRef(getState)
  getStateRef.current = getState

  useEffect(() => {
    let lastSent = JSON.stringify(getStateRef.current())
    const changedState = () => {
      const state = getStateRef.current()
      const json = JSON.stringify(state)
      if (json === lastSent) return null
      lastSent = json
      return state
    }
    const save = () => {
      const state = changedState()
      if (state) captionsApi.saveEditorState(projectId, state).catch(() => undefined)
    }
    const saveOnExit = () => {
      const state = changedState()
      if (state) captionsApi.saveEditorStateOnExit(projectId, state)
    }
    const onVisibility = () => { if (document.visibilityState === 'hidden') saveOnExit() }
    const timer = window.setInterval(save, SAVE_EVERY_MS)
    document.addEventListener('visibilitychange', onVisibility)
    window.addEventListener('pagehide', saveOnExit)
    return () => {
      window.clearInterval(timer)
      document.removeEventListener('visibilitychange', onVisibility)
      window.removeEventListener('pagehide', saveOnExit)
      save()
    }
  }, [projectId])
}
