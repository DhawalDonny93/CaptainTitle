import { useEffect, useRef, useState } from 'react'
import { ClockCounterClockwise } from '@phosphor-icons/react'
import { captionsApi, errorMessage } from '../captionsApi'
import { timeAgo } from '../lib/timeAgo'
import type { CaptionProjectVersion } from '../types'

const REASONS: Record<string, string> = {
  autosave: 'Autosaved version',
  before_relisten: 'Before re-listen',
  before_ai_emphasis: 'Before AI emphasis',
  before_restore: 'Before a restore',
}

interface Props {
  projectId: number
  busy: boolean
  onRestore: (versionId: number) => void
}

export const VersionHistoryMenu = ({ projectId, busy, onRestore }: Props) => {
  const [open, setOpen] = useState(false)
  const [versions, setVersions] = useState<CaptionProjectVersion[] | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [confirming, setConfirming] = useState<number | null>(null)
  const root = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!open) return undefined
    let cancelled = false
    setVersions(null)
    setLoadError(null)
    setConfirming(null)
    captionsApi.versions(projectId)
      .then((items) => { if (!cancelled) setVersions(items) })
      .catch((error) => { if (!cancelled) setLoadError(errorMessage(error)) })
    const onMouseDown = (e: MouseEvent) => { if (!root.current?.contains(e.target as Node)) setOpen(false) }
    const onKeyDown = (e: KeyboardEvent) => { if (e.key === 'Escape') setOpen(false) }
    window.addEventListener('mousedown', onMouseDown)
    window.addEventListener('keydown', onKeyDown)
    return () => {
      cancelled = true
      window.removeEventListener('mousedown', onMouseDown)
      window.removeEventListener('keydown', onKeyDown)
    }
  }, [open, projectId])

  return (
    <div className="version-menu" ref={root}>
      <button className="utility-chip" onClick={() => setOpen((current) => !current)} aria-expanded={open} data-testid="version-history-button"><ClockCounterClockwise size={16} /> History</button>
      {open && <div className="version-menu-popover" data-testid="version-history-menu">
        {loadError && <p>{loadError}</p>}
        {!loadError && !versions && <p>Loading…</p>}
        {versions?.length === 0 && <p>No earlier versions yet. They appear here as you edit.</p>}
        {versions?.map((version) => <div className="version-row" key={version.version_id} data-testid={`version-row-${version.version_id}`}>
          <div><b>{REASONS[version.reason] ?? 'Saved version'}</b><span>{timeAgo(version.created_at)}</span></div>
          {confirming === version.version_id
            ? <button className="confirm" disabled={busy} onClick={() => onRestore(version.version_id)} data-testid={`version-confirm-${version.version_id}`}>{busy ? 'Restoring…' : 'Confirm'}</button>
            : <button disabled={busy} onClick={() => setConfirming(version.version_id)} data-testid={`version-restore-${version.version_id}`}>Restore</button>}
        </div>)}
      </div>}
    </div>
  )
}
