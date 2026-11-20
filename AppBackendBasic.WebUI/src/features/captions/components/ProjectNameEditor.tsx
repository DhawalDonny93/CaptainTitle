import { useEffect, useRef, useState, type ReactNode } from 'react'
import { Check, CheckCircle, SpinnerGap, WarningCircle } from '@phosphor-icons/react'
import { captionsApi, errorMessage } from '../captionsApi'

const MAX_NAME = 120

interface Props {
  projectId: number
  name: string
  onRenamed: (name: string) => void
  /** The status shown while the name has nothing pending (the project's autosave indicator). */
  children?: ReactNode
}

type NameState = 'idle' | 'saving' | 'saved' | 'error'

/** Inline-editable project name for the header. Edits are kept as a draft until the green tick (or Enter) saves them; Escape reverts. */
export const ProjectNameEditor = ({ projectId, name, onRenamed, children }: Props) => {
  const [draft, setDraft] = useState(name)
  const [state, setState] = useState<NameState>('idle')
  const [error, setError] = useState<string | null>(null)
  const savedTimer = useRef<number>()
  const input = useRef<HTMLInputElement>(null)

  useEffect(() => { setDraft(name) }, [name])
  useEffect(() => () => window.clearTimeout(savedTimer.current), [])

  const clean = draft.trim()
  const dirty = clean !== name && clean.length > 0

  const save = async () => {
    if (!dirty || state === 'saving') return
    setState('saving')
    setError(null)
    try {
      const saved = await captionsApi.rename(projectId, clean)
      onRenamed(saved)
      setDraft(saved)
      setState('saved')
      window.clearTimeout(savedTimer.current)
      savedTimer.current = window.setTimeout(() => setState('idle'), 2200)
    } catch (e) {
      setError(errorMessage(e))
      setState('error')
    }
  }

  const status = state === 'saving'
    ? <span className="studio-save-state" role="status" data-testid="project-name-status"><SpinnerGap className="spin" size={14} /> Saving name…</span>
    : state === 'error'
      ? <span className="studio-save-state error" role="status" title={error ?? undefined} data-testid="project-name-status"><WarningCircle size={14} /> Name not saved</span>
      : dirty
        ? <span className="studio-save-state pending" role="status" data-testid="project-name-status">Unsaved name</span>
        : state === 'saved'
          ? <span className="studio-save-state ok" role="status" data-testid="project-name-status"><CheckCircle size={14} weight="fill" /> Name saved</span>
          : children

  return (
    <>
      <div className={`project-name-editor${dirty ? ' dirty' : ''}`}>
        <input ref={input} value={draft} maxLength={MAX_NAME} size={Math.min(40, Math.max(8, draft.length + 1))} spellCheck={false}
          aria-label="Project name" title="Click to rename the project" data-testid="current-project-name"
          onChange={(e) => { setDraft(e.target.value); if (state === 'error' || state === 'saved') setState('idle') }}
          onKeyDown={(e) => {
            if (e.key === 'Enter') { e.preventDefault(); void save() }
            if (e.key === 'Escape') { setDraft(name); setState('idle'); input.current?.blur() }
          }}
          onBlur={() => { if (!clean) setDraft(name) }} />
        {dirty && <button className="name-save-tick" onClick={() => void save()} disabled={state === 'saving'} title="Save project name" aria-label="Save project name" data-testid="project-name-save-button">
          {state === 'saving' ? <SpinnerGap className="spin" size={13} weight="bold" /> : <Check size={13} weight="bold" />}
        </button>}
      </div>
      {status}
    </>
  )
}
