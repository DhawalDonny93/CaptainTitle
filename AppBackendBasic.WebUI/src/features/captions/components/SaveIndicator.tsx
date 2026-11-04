import { CheckCircle, SpinnerGap, WarningCircle } from '@phosphor-icons/react'
import type { SaveStatus } from '../hooks/useProjectAutosave'

/** Conflicts get their own banner in the studio, because they need an action. */
export const SaveIndicator = ({ status, error }: { status: SaveStatus; error: string | null }) => {
  if (status === 'conflict') return null
  if (status === 'error') return <span className="studio-save-state error" title={error ?? undefined} role="status" data-testid="save-status"><WarningCircle size={14} /> Not saved: {error}</span>
  if (status === 'saved') return <span className="studio-save-state" role="status" data-testid="save-status"><CheckCircle size={14} /> Saved</span>
  return <span className="studio-save-state" role="status" data-testid="save-status"><SpinnerGap className="spin" size={14} /> Saving…</span>
}
