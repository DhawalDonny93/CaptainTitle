import { useEffect, useId, useState, type ReactNode } from 'react'
import { CaretDown, CaretRight, CheckCircle, Info, SpinnerGap } from '@phosphor-icons/react'
import { CtDialog } from './ui'
import type { RenderQuality } from '../types'
import { resolutionName, scaledSize, type RenderFormat, type RenderResolution } from '../lib/frame'

export type CaptionFileFormat = 'srt' | 'vtt' | 'txt'

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  /** The exported frame [width, height] (source size or the Ratio panel's frame), when known. */
  frameSize: [number, number] | null
  resolution: RenderResolution
  onResolution: (resolution: RenderResolution) => void
  format: RenderFormat
  onFormat: (format: RenderFormat) => void
  quality: RenderQuality
  onQuality: (quality: RenderQuality) => void
  rendering: boolean
  renderProgress: number
  /** the latest export matches the current edits (resolution / format / quality included), so it can be downloaded as-is */
  exportIsCurrent: boolean
  /** an earlier export exists (it may be older than the current edits) */
  hasExport: boolean
  onExport: () => void
  onDownload: () => void
  onDownloadCaptions: (format: CaptionFileFormat) => void
}

const FORMATS: { id: RenderFormat; label: string; note: string }[] = [
  { id: 'mp4', label: 'MP4', note: 'Universal compatibility. Recommended for most uses.' },
  { id: 'mov', label: 'MOV', note: 'QuickTime file for Apple devices and editing apps such as Final Cut Pro and Premiere.' },
]

const QUALITIES: { id: RenderQuality; label: string; note: string }[] = [
  { id: 'high', label: 'High', note: 'Recommended for most uploads.' },
  { id: 'draft', label: 'Draft', note: 'Faster export with a smaller file. Good for checking timing and styling.' },
]

const CAPTION_FORMATS: { id: CaptionFileFormat; label: string; note: string }[] = [
  { id: 'srt', label: 'SRT', note: 'Subtitle file for YouTube, Facebook, LinkedIn and most video players.' },
  { id: 'vtt', label: 'VTT', note: 'WebVTT subtitles for web players and HTML5 video.' },
  { id: 'txt', label: 'TXT', note: 'Plain transcript text, without timings.' },
]

const SCALED: Exclude<RenderResolution, 'original'>[] = ['1080', '720', '480']

const sizeText = ([w, h]: [number, number]) => `${w} × ${h}`

function Field({ label, hint, info, children }: { label: string; hint: string; info?: string; children: (id: string) => ReactNode }) {
  const id = useId()
  return (
    <div className="export-field">
      <label htmlFor={id}>
        {label}
        {info && <span className="export-info" title={info} aria-label={info} tabIndex={0}><Info size={15} /></span>}
      </label>
      {children(id)}
      <p className="export-hint">{hint}</p>
    </div>
  )
}

/**
 * Getting the video out of the studio: resolution, render target, format and quality (all saved with the project, so changing one makes the
 * previous export out of date), the export itself with progress, the download, and "Export captions instead" for SRT / VTT / TXT files.
 */
export function ExportDialog({ open, onOpenChange, frameSize, resolution, onResolution, format, onFormat, quality, onQuality, rendering,
  renderProgress, exportIsCurrent, hasExport, onExport, onDownload, onDownloadCaptions }: Props) {
  const [mode, setMode] = useState<'video' | 'captions'>('video')
  const [advanced, setAdvanced] = useState(true)
  const [captionFormat, setCaptionFormat] = useState<CaptionFileFormat>('srt')
  // Every time the dialog opens it starts on the video export.
  useEffect(() => { if (open) setMode('video') }, [open])

  const originalLabel = frameSize ? `Keep original (${resolutionName(...frameSize)} · ${sizeText(frameSize)})` : 'Keep original'
  // Sizes equal to the original frame would be a no-op duplicate of "Keep original", so they are not offered (unless already chosen).
  const scaledOptions = SCALED.filter((r) => {
    if (r === resolution || !frameSize) return true
    const [w, h] = scaledSize(r, ...frameSize)
    return w !== frameSize[0] || h !== frameSize[1]
  })
  const formatNote = FORMATS.find((f) => f.id === format)?.note ?? ''
  const qualityNote = QUALITIES.find((q) => q.id === quality)?.note ?? ''

  if (mode === 'captions') {
    const note = CAPTION_FORMATS.find((f) => f.id === captionFormat)?.note ?? ''
    return (
      <CtDialog open={open} onOpenChange={onOpenChange} className="export-dialog" data-testid="export-dialog">
        <h2 className="export-title">Export captions</h2>
        <Field label="Format" hint={note}>
          {(id) => (
            <select id={id} className="export-select" value={captionFormat} onChange={(e) => setCaptionFormat(e.target.value as CaptionFileFormat)}
              data-testid="caption-format-select">
              {CAPTION_FORMATS.map((f) => <option key={f.id} value={f.id}>{f.label}</option>)}
            </select>
          )}
        </Field>
        <div className="export-footer">
          <button type="button" className="export-link" onClick={() => setMode('video')} data-testid="export-video-instead">Export video instead</button>
          <div className="export-footer-buttons">
            <button type="button" className="export-cancel" onClick={() => onOpenChange(false)}>Cancel</button>
            <button type="button" className="export-primary" onClick={() => onDownloadCaptions(captionFormat)} data-testid="download-captions-button">
              Export
            </button>
          </div>
        </div>
      </CtDialog>
    )
  }

  return (
    <CtDialog open={open} onOpenChange={onOpenChange} className="export-dialog" data-testid="export-dialog">
      <h2 className="export-title">Export video</h2>

      <Field label="Resolution" hint="Output keeps your video's frame rate.">
        {(id) => (
          <select id={id} className="export-select" value={resolution} disabled={rendering}
            onChange={(e) => onResolution(e.target.value as RenderResolution)} data-testid="render-resolution-select">
            <option value="original">{originalLabel}</option>
            {scaledOptions.map((r) => {
              const size = frameSize ? scaledSize(r, ...frameSize) : null
              const name = r === '1080' ? 'Full HD' : r === '720' ? 'HD' : 'SD'
              return <option key={r} value={r}>{size ? `${name} · ${sizeText(size)}` : `${name} · ${r}p`}</option>
            })}
          </select>
        )}
      </Field>

      <button type="button" className="export-advanced-toggle" aria-expanded={advanced} onClick={() => setAdvanced((v) => !v)}
        data-testid="export-advanced-toggle">
        {advanced ? <CaretDown size={14} weight="bold" /> : <CaretRight size={14} weight="bold" />} Advanced options
      </button>

      {advanced && <>
        <Field label="Render on" hint="Runs on our servers. You can close this window and keep editing while it exports.">
          {(id) => (
            <select id={id} className="export-select" value="cloud" disabled={rendering} onChange={() => undefined} data-testid="render-target-select">
              <option value="cloud">Our servers</option>
              <option value="device" disabled>This device (coming soon)</option>
            </select>
          )}
        </Field>

        <Field label="Format" hint={formatNote}
          info="MP4 and MOV both use H.264 video with AAC audio; only the file container differs. Pick MP4 unless an app asks for MOV.">
          {(id) => (
            <select id={id} className="export-select" value={format} disabled={rendering} onChange={(e) => onFormat(e.target.value as RenderFormat)}
              data-testid="render-format-select">
              {FORMATS.map((f) => <option key={f.id} value={f.id}>{f.label}</option>)}
            </select>
          )}
        </Field>

        <Field label="Quality" hint={qualityNote}>
          {(id) => (
            <select id={id} className="export-select" value={quality} disabled={rendering} onChange={(e) => onQuality(e.target.value as RenderQuality)}
              data-testid="render-quality-select">
              {QUALITIES.map((q) => <option key={q.id} value={q.id}>{q.label}</option>)}
            </select>
          )}
        </Field>
      </>}

      {(rendering || exportIsCurrent || hasExport) &&
        <div className="export-status" data-testid="export-status">
          {rendering
            ? <div className="export-progress">
                <div><span><SpinnerGap className="spin" size={14} /> Exporting your video…</span><b>{Math.round(renderProgress)}%</b></div>
                <i><em style={{ width: `${Math.max(4, renderProgress)}%` }} /></i>
              </div>
            : exportIsCurrent
              ? <p className="export-ready"><CheckCircle size={16} weight="fill" /> Your latest edits are exported and ready to download.</p>
              : <p>
                  You've changed the video or these options since the last export. Export again to include them, or{' '}
                  <button type="button" className="export-inline-link" onClick={onDownload} data-testid="download-previous-video-button">
                    download the previous export
                  </button>.
                </p>}
        </div>}

      <div className="export-footer">
        <button type="button" className="export-link" onClick={() => setMode('captions')} data-testid="export-captions-instead">
          Export captions instead
        </button>
        <div className="export-footer-buttons">
          <button type="button" className="export-cancel" onClick={() => onOpenChange(false)} data-testid="export-dialog-close">
            {rendering ? 'Close' : 'Cancel'}
          </button>
          {exportIsCurrent && !rendering
            ? <button type="button" className="export-primary" onClick={onDownload} data-testid="download-video-button">Download</button>
            : <button type="button" className="export-primary" onClick={onExport} disabled={rendering} data-testid="render-video-button">
                {rendering ? <><SpinnerGap className="spin" size={16} /> Exporting…</> : 'Export'}
              </button>}
        </div>
      </div>
    </CtDialog>
  )
}
