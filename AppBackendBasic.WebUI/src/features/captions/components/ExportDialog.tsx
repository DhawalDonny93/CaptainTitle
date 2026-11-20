import { CheckCircle, DownloadSimple, Export, FileText, FilmStrip, Lightning, SpinnerGap, Sparkle, X } from '@phosphor-icons/react'
import { CtDialog } from './ui'
import type { RenderQuality } from '../types'

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  /** e.g. "1080×1920 · 9:16", when the source size is known */
  resolution: string | null
  duration: number
  quality: RenderQuality
  onQuality: (quality: RenderQuality) => void
  rendering: boolean
  renderProgress: number
  /** the latest export matches the current edits, so it can be downloaded as-is */
  exportIsCurrent: boolean
  /** an earlier export exists (it may be older than the current edits) */
  hasExport: boolean
  onExport: () => void
  onDownload: () => void
  onDownloadSrt: () => void
}

const QUALITIES: { id: RenderQuality; label: string; note: string; icon: typeof Sparkle }[] = [
  { id: 'high', label: 'High quality', note: 'Best for posting. Takes a little longer.', icon: Sparkle },
  { id: 'draft', label: 'Draft (fast)', note: 'Quick preview to check timing and styling.', icon: Lightning },
]

const formatDuration = (seconds: number) => {
  const s = Math.max(0, Math.round(seconds))
  return `${Math.floor(s / 60)}:${String(s % 60).padStart(2, '0')}`
}

/** Everything about getting the video out of the studio: quality, export, MP4 download and subtitles. */
export const ExportDialog = ({ open, onOpenChange, resolution, duration, quality, onQuality, rendering, renderProgress, exportIsCurrent, hasExport,
  onExport, onDownload, onDownloadSrt }: Props) => (
  <CtDialog open={open} onOpenChange={onOpenChange} className="export-dialog" data-testid="export-dialog">
    <div className="export-dialog-head">
      <div>
        <p className="eyebrow"><FilmStrip size={15} /> EXPORT</p>
        <h2>Export video</h2>
      </div>
      <button className="export-dialog-close" onClick={() => onOpenChange(false)} aria-label="Close" data-testid="export-dialog-close"><X size={18} /></button>
    </div>

    <div className="export-meta" data-testid="export-meta">
      {resolution && <span data-testid="resolution-badge">{resolution}</span>}
      <span>{formatDuration(duration)}</span>
      <span>MP4 · H.264</span>
    </div>

    <p className="export-section-label">Quality</p>
    <div className="export-quality" role="radiogroup" aria-label="Export quality" data-testid="render-quality-select">
      {QUALITIES.map(({ id, label, note, icon: Icon }) => (
        <button key={id} role="radio" aria-checked={quality === id} className={quality === id ? 'active' : ''} disabled={rendering}
          onClick={() => onQuality(id)} data-testid={`render-quality-${id}`}>
          <Icon size={18} />
          <span><b>{label}</b><small>{note}</small></span>
        </button>
      ))}
    </div>

    <div className="export-status" data-testid="export-status">
      {rendering
        ? <div className="export-progress">
            <div><span><SpinnerGap className="spin" size={14} /> Exporting your video…</span><b>{Math.round(renderProgress)}%</b></div>
            <i><em style={{ width: `${Math.max(4, renderProgress)}%` }} /></i>
            <p>It runs on our servers. You can close this box and keep editing.</p>
          </div>
        : exportIsCurrent
          ? <p className="export-ready"><CheckCircle size={16} weight="fill" /> Your latest edits are exported and ready to download.</p>
          : hasExport
            ? <p>You've edited since the last export. Export again to include your changes.</p>
            : <p>Your captions are burned into the video using the selected quality.</p>}
    </div>

    <div className="export-actions">
      <button className="panel-action-button outline" onClick={onDownloadSrt} data-testid="download-srt-button"><FileText size={16} /> Subtitles (.srt)</button>
      {hasExport && !exportIsCurrent && !rendering &&
        <button className="panel-action-button outline" onClick={onDownload} data-testid="download-previous-video-button"><DownloadSimple size={16} /> Previous export</button>}
      {exportIsCurrent && !rendering
        ? <button className="export-button" onClick={onDownload} data-testid="download-video-button"><DownloadSimple size={18} weight="bold" /> Download MP4</button>
        : <button className="export-button" onClick={onExport} disabled={rendering} data-testid="render-video-button">
            {rendering ? <SpinnerGap className="spin" size={18} /> : <Export size={18} weight="bold" />} {rendering ? 'Exporting…' : 'Export video'}
          </button>}
    </div>
  </CtDialog>
)
