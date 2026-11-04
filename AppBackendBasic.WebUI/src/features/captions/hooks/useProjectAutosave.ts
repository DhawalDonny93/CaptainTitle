import { useCallback, useEffect, useRef, useState } from 'react'
import { captionsApi, errorCode, errorMessage, httpStatus } from '../captionsApi'
import type { ProjectEditPayload } from '../types'

export type SaveStatus = 'saved' | 'dirty' | 'saving' | 'error' | 'conflict'

const DEBOUNCE_MS = 2000
const MAX_RETRY_MS = 30000

interface Options {
  projectId: number
  initialRevision: number
  /** Must be memoized: its identity is what says "something changed", so typing and dragging never pay for a JSON.stringify. */
  payload: ProjectEditPayload
  paused: boolean
}

/**
 * Keeps the server's copy of the project in step with the editor. One save at a time, always with the revision it was based on, so a
 * second tab's newer edits are reported as a conflict and never overwritten. flush() saves right now and rejects if that fails.
 */
export function useProjectAutosave({ projectId, initialRevision, payload, paused }: Options) {
  const [status, setStatus] = useState<SaveStatus>('saved')
  const [error, setError] = useState<string | null>(null)
  const [revision, setRevision] = useState(initialRevision)
  const [rebaseTick, setRebaseTick] = useState(0)

  const latestRef = useRef(payload)
  latestRef.current = payload
  const pausedRef = useRef(paused)
  pausedRef.current = paused
  const statusRef = useRef(status)
  statusRef.current = status
  const revisionRef = useRef(initialRevision)
  const savedJsonRef = useRef<string | null>(null)
  if (savedJsonRef.current === null) savedJsonRef.current = JSON.stringify(payload)
  const inFlightRef = useRef<Promise<void> | null>(null)
  const timerRef = useRef<number | undefined>()
  const retryDelayRef = useRef(0)
  const skipEffectRef = useRef(true)
  const rebasedTickRef = useRef(0)

  const flushRef = useRef<() => Promise<void>>(async () => undefined)

  const schedule = useCallback((delay: number) => {
    window.clearTimeout(timerRef.current)
    timerRef.current = window.setTimeout(() => {
      if (!pausedRef.current) flushRef.current().catch(() => undefined) // the failure is already on screen via status/error
    }, delay)
  }, [])

  const flush = useCallback(async (): Promise<void> => {
    window.clearTimeout(timerRef.current)
    while (inFlightRef.current) await inFlightRef.current.catch(() => undefined)
    if (statusRef.current === 'conflict') throw new Error('This project was changed elsewhere. Reload to get the latest version.')

    const sending = latestRef.current
    const json = JSON.stringify(sending)
    if (json === savedJsonRef.current) { setStatus('saved'); setError(null); return }

    setStatus('saving')
    const attempt = captionsApi.save(projectId, sending, revisionRef.current).then((result) => {
      revisionRef.current = result.revision
      setRevision(result.revision)
      savedJsonRef.current = json
      retryDelayRef.current = 0
      setError(null)
      // Edits made while this request was in the air still need their own save.
      if (latestRef.current === sending) setStatus('saved')
      else { setStatus('dirty'); schedule(DEBOUNCE_MS) }
    }, (failure: unknown) => {
      setError(errorMessage(failure))
      if (errorCode(failure) === 'REVISION_CONFLICT') {
        statusRef.current = 'conflict'
        setStatus('conflict')
      } else {
        setStatus('error')
        // A rejected edit (e.g. a word left blank) can't succeed unchanged and waits for the next edit. Only outages and rate limits are retried.
        const code = httpStatus(failure)
        if (code === undefined || code === 429 || code >= 500) {
          retryDelayRef.current = Math.min(MAX_RETRY_MS, (retryDelayRef.current || 1000) * 2)
          schedule(retryDelayRef.current)
        }
      }
      throw failure
    }).finally(() => { inFlightRef.current = null })
    inFlightRef.current = attempt
    await attempt
  }, [projectId, schedule])
  flushRef.current = flush

  /** The server just replaced the content (AI emphasis, re-listen): adopt its revision, and treat the state set alongside this call as already saved. */
  const rebase = useCallback((nextRevision: number) => {
    revisionRef.current = nextRevision
    setRevision(nextRevision)
    setRebaseTick((tick) => tick + 1)
  }, [])

  useEffect(() => {
    if (rebaseTick !== rebasedTickRef.current) {
      rebasedTickRef.current = rebaseTick
      savedJsonRef.current = JSON.stringify(latestRef.current)
      setStatus('saved')
      setError(null)
      return
    }
    if (skipEffectRef.current) { skipEffectRef.current = false; return }
    if (paused || statusRef.current === 'conflict') return
    setStatus('dirty')
    retryDelayRef.current = 0
    schedule(DEBOUNCE_MS)
  }, [payload, paused, rebaseTick, schedule])

  useEffect(() => {
    const onVisibility = () => { if (document.visibilityState === 'hidden') flushRef.current().catch(() => undefined) }
    const onBeforeUnload = (e: BeforeUnloadEvent) => {
      if (statusRef.current === 'saved' || statusRef.current === 'conflict') return
      e.preventDefault()
      e.returnValue = ''
    }
    document.addEventListener('visibilitychange', onVisibility)
    window.addEventListener('beforeunload', onBeforeUnload)
    return () => {
      document.removeEventListener('visibilitychange', onVisibility)
      window.removeEventListener('beforeunload', onBeforeUnload)
      // Leaving the editor inside the app: send whatever the debounce was still holding.
      flushRef.current().catch(() => undefined)
    }
  }, [])

  return { status, error, revision, flush, rebase }
}
