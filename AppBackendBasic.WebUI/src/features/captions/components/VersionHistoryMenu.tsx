import { useEffect, useState } from 'react'
import { ClockCounterClockwise } from '@phosphor-icons/react'
import { captionsApi, errorMessage } from '../captionsApi'
import { timeAgo } from '../lib/timeAgo'
import { useDismissable } from '../hooks/useDismissable'
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

/** Header button listing the project's server-side restore points (autosaved versions); restoring one keeps the current edits as a new one. */
export const VersionHistoryMenu = ({ projectId, busy, onRestore }: Props) => {
  const { open, setOpen, ref } = useDismissable()
  const [versions, setVersions] = useState<CaptionProjectVersion[] | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [confirming, setConfirming] = useState<number | null>(null)

  useEffect(() => {
    if (!open) return undefined
    let cancelled = false
    setVersions(null)
    setLoadError(null)
    setConfirming(null)
    captionsApi.versions(projectId)
      .then((items) => { if (!cancelled) setVersions(items) })
      .catch((error) => { if (!cancelled) setLoadError(errorMessage(error)) })
    return () => { cancelled = true }
  }, [open, projectId])

  return (
    <div className="header-menu" ref={ref}>
      <button className="header-icon-button" onClick={() => setOpen((v) => !v)} aria-haspopup="menu" aria-expanded={open}
        title="Version history" aria-label="Version history" data-testid="version-history-button">
        <ClockCounterClockwise size={18} />
      </button>
      {open && <div className="header-menu-popover version-menu" role="menu" data-testid="version-history-menu">
        <p className="header-menu-caption">Version history</p>
        {loadError && <p className="version-menu-note">{loadError}</p>}
        {!loadError && !versions && <p className="version-menu-note">Loading…</p>}
        {versions?.length === 0 && <p className="version-menu-note">No earlier versions yet. They appear here as you edit.</p>}
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
