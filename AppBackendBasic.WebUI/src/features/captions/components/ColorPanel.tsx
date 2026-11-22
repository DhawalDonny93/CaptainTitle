import { Drop, Palette } from '@phosphor-icons/react'
import { CtSlider } from './ui'
import { COLOR_PRESETS, sameColor, whiteBalance, type ColorValues } from '../lib/frame'

interface Props {
  value: ColorValues
  onChange: (changes: Partial<ColorValues>) => void
}

/** The CSS filter one preset produces, so its swatch previews the look on a neutral gradient. */
const swatchFilter = ({ brightness, contrast, saturation, warmth, tint }: ColorValues) => {
  const [r, , b] = whiteBalance(warmth, tint)
  // A rough stand-in for the white-balance matrix in a tiny swatch: sepia + hue for warmth, the real filter is applied to the video.
  const warm = Math.max(0, (r - b) * 2.2)
  const cool = Math.max(0, (b - r) * 2.2)
  return `brightness(${100 + brightness}%) contrast(${contrast}%) saturate(${saturation}%) sepia(${warm}) hue-rotate(${cool * 40}deg)`
}

const signed = (v: number) => (v > 0 ? `+${v}` : `${v}`)

export const ColorPanel = ({ value, onChange }: Props) => {
  const active = COLOR_PRESETS.find((p) => sameColor(p.values, value))
  return (
    <aside className="tool-panel" data-testid="adjustment-panel">
      <div className="panel-heading"><Palette size={18} /><div><span>VIDEO</span><h2 data-testid="adjustment-panel-heading">Color</h2></div></div>

      <div className="color-presets" data-testid="color-presets">
        {COLOR_PRESETS.map((preset) => (
          <button key={preset.id} className={active?.id === preset.id ? 'active' : ''} onClick={() => onChange(preset.values)} data-testid={`color-preset-${preset.id}`}>
            <i style={{ filter: swatchFilter(preset.values) }} />
            <span>{preset.label}</span>
          </button>
        ))}
      </div>

      <div className="tool-divider" />
      <div className="panel-heading compact"><Drop size={18} /><div><span>FINE TUNE</span><h2>{active ? active.label : 'Custom'}</h2></div></div>
      <label className="control-label" data-testid="brightness-label">Brightness <b data-testid="brightness-value">{signed(value.brightness)}</b></label>
      <CtSlider min={-50} max={50} step={1} value={[value.brightness]} onValueChange={([v]) => onChange({ brightness: v })} data-testid="brightness-slider" />
      <label className="control-label" data-testid="contrast-label">Contrast <b data-testid="contrast-value">{value.contrast}%</b></label>
      <CtSlider min={50} max={200} step={1} value={[value.contrast]} onValueChange={([v]) => onChange({ contrast: v })} data-testid="contrast-slider" />
      <label className="control-label" data-testid="saturation-label">Saturation <b data-testid="saturation-value">{value.saturation}%</b></label>
      <CtSlider min={0} max={200} step={1} value={[value.saturation]} onValueChange={([v]) => onChange({ saturation: v })} data-testid="saturation-slider" />
      <label className="control-label" data-testid="warmth-label">Warmth <b data-testid="warmth-value">{signed(value.warmth)}</b></label>
      <div className="gradient-slider warmth"><CtSlider min={-50} max={50} step={1} value={[value.warmth]} onValueChange={([v]) => onChange({ warmth: v })} data-testid="warmth-slider" /></div>
      <label className="control-label" data-testid="tint-label">Tint <b data-testid="tint-value">{signed(value.tint)}</b></label>
      <div className="gradient-slider tint"><CtSlider min={-50} max={50} step={1} value={[value.tint]} onValueChange={([v]) => onChange({ tint: v })} data-testid="tint-slider" /></div>

      <button className="panel-action-button outline" disabled={active?.id === 'none'} onClick={() => onChange(COLOR_PRESETS[0].values)} data-testid="reset-adjustment-button">Reset to default</button>
      <p className="panel-note">Color applies to the video only and shows in this live preview and the exported MP4. Caption text and stickers stay full-strength.</p>
    </aside>
  )
}
