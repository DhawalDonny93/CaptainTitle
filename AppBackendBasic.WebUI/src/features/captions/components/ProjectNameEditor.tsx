import { useEffect, useRef, useState } from 'react'
import { Check, SpinnerGap } from '@phosphor-icons/react'
import { toast } from '@/utils/toast'
import { captionsApi, errorMessage } from '../captionsApi'

const MAX_NAME = 120

interface Props {
  projectId: number
  name: string
  onRenamed: (name: string) => void
}

/**
 * Inline-editable project name for the header. Edits are kept as a draft until the green tick (or Enter) saves them; Escape reverts.
 * No save-status text here: the header's Save button shows the project's save state, and a failed rename is reported with a toast.
 */
export const ProjectNameEditor = ({ projectId, name, onRenamed }: Props) => {
  const [draft, setDraft] = useState(name)
  const [saving, setSaving] = useState(false)
  const input = useRef<HTMLInputElement>(null)

  useEffect(() => { setDraft(name) }, [name])

  const clean = draft.trim()
  const dirty = clean !== name && clean.length > 0

  const save = async () => {
    if (!dirty || saving) return
    setSaving(true)
    try {
      const saved = await captionsApi.rename(projectId, clean)
      onRenamed(saved)
      setDraft(saved)
    } catch (e) {
      toast.error(`Project name not saved: ${errorMessage(e)}`)
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className={`project-name-editor${dirty ? ' dirty' : ''}`}>
      <input ref={input} value={draft} maxLength={MAX_NAME} size={Math.min(40, Math.max(8, draft.length + 1))} spellCheck={false}
        aria-label="Project name" title="Click to rename the project" data-testid="current-project-name"
        onChange={(e) => setDraft(e.target.value)}
        onKeyDown={(e) => {
          if (e.key === 'Enter') { e.preventDefault(); void save() }
          if (e.key === 'Escape') { setDraft(name); input.current?.blur() }
        }}
        onBlur={() => { if (!clean) setDraft(name) }} />
      {dirty && <button className="name-save-tick" onClick={() => void save()} disabled={saving} title="Save project name" aria-label="Save project name" data-testid="project-name-save-button">
        {saving ? <SpinnerGap className="spin" size={13} weight="bold" /> : <Check size={13} weight="bold" />}
      </button>}
    </div>
  )
}
