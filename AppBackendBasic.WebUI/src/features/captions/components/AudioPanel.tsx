import { SpeakerHigh, SpeakerSlash, Waveform } from '@phosphor-icons/react'
import { CtSlider, CtSwitchRow } from './ui'

export interface AudioSettings {
  volume: number
  muted: boolean
  fadeIn: number
  fadeOut: number
  normalize: boolean
}

interface Props {
  value: AudioSettings
  duration: number
  onChange: (changes: Partial<AudioSettings>) => void
}

const VOLUME_PRESETS = [0, 50, 100, 150]
const seconds = (v: number) => (v === 0 ? 'Off' : `${v.toFixed(v % 1 ? 2 : 0).replace(/0$/, '')}s`)

export const AudioPanel = ({ value, duration, onChange }: Props) => {
  const { volume, muted, fadeIn, fadeOut, normalize } = value
  // Fades longer than half the clip would overlap; the export clamps too, this just keeps the slider honest.
  const maxFade = Math.max(0.5, Math.min(5, Math.floor((duration / 2) * 4) / 4 || 5))
  const isDefault = volume === 100 && !muted && fadeIn === 0 && fadeOut === 0 && !normalize
  return (
    <aside className="tool-panel" data-testid="audio-panel">
      <div className="panel-heading"><SpeakerHigh size={18} /><div><span>ORIGINAL AUDIO</span><h2 data-testid="audio-panel-heading">Audio</h2></div></div>

      <label className="control-label" data-testid="audio-volume-label">Volume <b data-testid="audio-volume-value">{muted ? 'Muted' : `${volume}%`}</b></label>
      <CtSlider min={0} max={200} step={5} value={[volume]} disabled={muted} onValueChange={([v]) => onChange({ volume: v })} data-testid="audio-volume-slider" />
      <div className="layout-grid" style={{ marginTop: 10 }}>
        {VOLUME_PRESETS.map((preset) => (
          <button key={preset} className={!muted && volume === preset ? 'active' : ''} disabled={muted} onClick={() => onChange({ volume: preset })}
            data-testid={`audio-volume-preset-${preset}`}>{preset}%</button>
        ))}
      </div>
      <CtSwitchRow icon={muted ? <SpeakerSlash size={16} /> : <SpeakerHigh size={16} />} label="Mute original audio"
        checked={muted} onCheckedChange={(on) => onChange({ muted: on })} data-testid="audio-mute-toggle" />

      <div className="tool-divider" />
      <div className="panel-heading compact"><Waveform size={18} /><div><span>SHAPE</span><h2>Fades</h2></div></div>
      <label className="control-label" data-testid="audio-fade-in-label">Fade in <b data-testid="audio-fade-in-value">{seconds(fadeIn)}</b></label>
      <CtSlider min={0} max={maxFade} step={0.25} value={[Math.min(fadeIn, maxFade)]} disabled={muted} onValueChange={([v]) => onChange({ fadeIn: v })} data-testid="audio-fade-in-slider" />
      <label className="control-label" data-testid="audio-fade-out-label">Fade out <b data-testid="audio-fade-out-value">{seconds(fadeOut)}</b></label>
      <CtSlider min={0} max={maxFade} step={0.25} value={[Math.min(fadeOut, maxFade)]} disabled={muted} onValueChange={([v]) => onChange({ fadeOut: v })} data-testid="audio-fade-out-slider" />

      <CtSwitchRow icon={<Waveform size={16} />} label="Normalize loudness" disabled={muted}
        checked={normalize} onCheckedChange={(on) => onChange({ normalize: on })} data-testid="audio-normalize-toggle" />
      <p className="panel-note">Normalize evens out quiet and loud speech to the level social apps expect (-14 LUFS). It is applied to the exported MP4 only; the preview plays the original level. Volume above 100% is also export-only.</p>

      <button className="panel-action-button outline" disabled={isDefault}
        onClick={() => onChange({ volume: 100, muted: false, fadeIn: 0, fadeOut: 0, normalize: false })} data-testid="reset-audio-button">Reset audio</button>
    </aside>
  )
}
