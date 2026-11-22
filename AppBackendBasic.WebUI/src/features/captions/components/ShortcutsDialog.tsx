import { useEffect, useMemo, useState } from 'react'
import { X } from '@phosphor-icons/react'
import { CtDialog } from './ui'
import { comboKeys, SHORTCUT_ACTIONS, SHORTCUT_BINDINGS, SHORTCUT_GROUPS } from '../lib/shortcuts'

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
}

const Keys = ({ combo }: { combo: string }) => <>{comboKeys(combo).map((key, i) => <kbd key={i}>{key}</kbd>)}</>

/** Read-only list of every studio shortcut, filterable by name or key. The keys themselves are fixed (lib/shortcuts.ts). */
export const ShortcutsDialog = ({ open, onOpenChange }: Props) => {
  const [filter, setFilter] = useState('')
  useEffect(() => { if (!open) setFilter('') }, [open])

  const query = filter.trim().toLowerCase()
  const visible = useMemo(() => SHORTCUT_ACTIONS.filter((action) => !query
    || `${action.group} ${action.label} ${action.hint ?? ''}`.toLowerCase().includes(query)
    || SHORTCUT_BINDINGS[action.id].some((combo) => combo.toLowerCase().includes(query) || comboKeys(combo).join(' ').toLowerCase().includes(query))),
  [query])

  return (
    <CtDialog open={open} onOpenChange={onOpenChange} className="shortcuts-dialog" data-testid="shortcuts-dialog">
      <div className="export-dialog-head">
        <h2>Keyboard shortcuts</h2>
        <button className="export-dialog-close" onClick={() => onOpenChange(false)} aria-label="Close" data-testid="shortcuts-dialog-close"><X size={18} /></button>
      </div>
      <p className="shortcuts-intro">Shortcuts work while a project is open in the studio.</p>
      <input className="shortcuts-filter" value={filter} onChange={(e) => setFilter(e.target.value)} placeholder="Filter by name or key"
        aria-label="Filter shortcuts" data-testid="shortcuts-filter" />

      <div className="shortcuts-list" data-testid="shortcuts-list">
        {SHORTCUT_GROUPS.map((group) => {
          const rows = visible.filter((action) => action.group === group)
          if (!rows.length) return null
          return <section key={group} className="shortcuts-group">
            <h3>{group}</h3>
            {rows.map((action) => {
              const combos = SHORTCUT_BINDINGS[action.id]
              return <div key={action.id} className="shortcut-row" data-testid={`shortcut-row-${action.id}`}>
                <div className="shortcut-label">
                  <b>{action.label}</b>
                  {action.hint && <small>{action.hint}</small>}
                </div>
                <div className="shortcut-keys">
                  {combos.length === 0 && <span className="shortcut-none">Not set</span>}
                  {combos.map((combo) => <span key={combo} className="shortcut-chip"><Keys combo={combo} /></span>)}
                </div>
              </div>
            })}
          </section>
        })}
        {!visible.length && <p className="shortcuts-empty">No shortcut matches “{filter}”.</p>}
      </div>

      <div className="shortcuts-footer">
        <button className="shortcuts-done" onClick={() => onOpenChange(false)} data-testid="shortcuts-done">Done</button>
      </div>
    </CtDialog>
  )
}
