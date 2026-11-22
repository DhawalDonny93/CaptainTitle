import { Crop, FrameCorners, PaintBucket, Drop } from '@phosphor-icons/react'
import { ASPECT_RATIOS, outputSize, type AspectRatio, type FrameFit } from '../lib/frame'

export interface RatioSettings { ratio: AspectRatio; fit: FrameFit; color: string }

interface Props {
  value: RatioSettings
  sourceWidth: number | null
  sourceHeight: number | null
  onChange: (changes: Partial<RatioSettings>) => void
}

const FITS: { id: FrameFit; label: string; hint: string; icon: typeof Crop }[] = [
  { id: 'blur', label: 'Blur fill', hint: 'Whole video, blurred copy behind', icon: Drop },
  { id: 'color', label: 'Color fill', hint: 'Whole video on a solid color', icon: PaintBucket },
  { id: 'crop', label: 'Crop to fill', hint: 'Fill the frame, trim the edges', icon: Crop },
]

const SWATCHES = ['#000000', '#FFFFFF', '#111827', '#F5F0E8', '#FF2D4D', '#2563EB', '#16A34A', '#FACC15']

/** A tiny rectangle with the ratio's shape, sized to fit a 26px box. */
const RatioShape = ({ w, h }: { w: number; h: number }) => {
  const scale = 26 / Math.max(w, h)
  return <i className="ratio-shape" style={{ width: Math.round(w * scale), height: Math.round(h * scale) }} />
}

export const RatioPanel = ({ value, sourceWidth, sourceHeight, onChange }: Props) => {
  const [outW, outH] = outputSize(value.ratio, sourceWidth, sourceHeight)
  const sourceAspect = sourceWidth && sourceHeight ? sourceWidth / sourceHeight : null
  // The fit only matters when the frame's shape differs from the video's own shape.
  const fitMatters = value.ratio !== 'original' && (sourceAspect == null || Math.abs(sourceAspect - outW / outH) > 0.01)
  return (
    <aside className="tool-panel" data-testid="ratio-panel">
      <div className="panel-heading"><FrameCorners size={18} /><div><span>CANVAS</span><h2 data-testid="ratio-panel-heading">Ratio</h2></div></div>

      <div className="ratio-grid" role="radiogroup" aria-label="Aspect ratio">
        {ASPECT_RATIOS.map(({ id, label, hint }) => {
          const [w, h] = outputSize(id, sourceWidth, sourceHeight)
          return (
            <button key={id} role="radio" aria-checked={value.ratio === id} className={value.ratio === id ? 'active' : ''} onClick={() => onChange({ ratio: id })}
              data-testid={`ratio-${id.replace(':', 'x')}`}>
              <RatioShape w={w} h={h} />
              <b>{label}</b>
              <small>{hint}</small>
            </button>
          )
        })}
      </div>

      <div className="tool-divider" />
      <p className="control-label" style={{ marginTop: 0 }}>Fill <b>{outW}×{outH}</b></p>
      <div className={`fit-list${fitMatters ? '' : ' muted'}`} role="radiogroup" aria-label="How the video fills the frame">
        {FITS.map(({ id, label, hint, icon: Icon }) => (
          <button key={id} role="radio" aria-checked={value.fit === id} className={value.fit === id ? 'active' : ''} disabled={!fitMatters}
            onClick={() => onChange({ fit: id })} data-testid={`frame-fit-${id}`}>
            <Icon size={18} />
            <span><b>{label}</b><small>{hint}</small></span>
          </button>
        ))}
      </div>

      {fitMatters && value.fit === 'color' && <>
        <label className="control-label">Background color <span><input type="color" value={value.color} onChange={(e) => onChange({ color: e.target.value.toUpperCase() })} data-testid="frame-color-input" /></span></label>
        <div className="swatch-row">
          {SWATCHES.map((c) => (
            <button key={c} className={value.color.toUpperCase() === c ? 'active' : ''} style={{ background: c }} onClick={() => onChange({ color: c })}
              title={c} aria-label={`Background ${c}`} data-testid={`frame-color-${c.slice(1)}`} />
          ))}
        </div>
      </>}

      <p className="panel-note">{value.ratio === 'original'
        ? 'The export keeps the uploaded video size.'
        : fitMatters
          ? 'Captions and stickers are placed on the new frame, so check their positions after changing the ratio.'
          : 'Your video already has this shape, so it fills the frame exactly.'}</p>
    </aside>
  )
}
