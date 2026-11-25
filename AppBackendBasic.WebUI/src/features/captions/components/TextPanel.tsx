import { useState } from 'react'
import { ClosedCaptioning, CursorClick, Palette, Plus, TextAa, TextT, Trash } from '@phosphor-icons/react'
import { CtSlider } from './ui'
import { useCaptionCatalog } from '../catalogContext'
import { cssFamily, DEFAULT_WORD_SIZE, STREET_GLOW } from '../lib/styles'
import type { CaptionElement, CaptionWord } from '../types'

// The one "Text" tool in the left rail. It edits two different kinds of text, picked with the switch at the top:
//  - 'word':  a word of the captions (its size/colour, tier, line and caption breaks) - was the rail's old "Word" tool;
//  - 'layer': free text overlays placed on the video (titles, labels) - was the rail's old "Text" tool.
export type TextMode = 'word' | 'layer'

interface Props {
  mode: TextMode
  onMode: (mode: TextMode) => void
  // caption word
  theme: string
  selectedWord: CaptionWord | undefined
  onWordStyle: (change: Partial<CaptionWord>) => void
  // text overlays
  elements: CaptionElement[]
  selectedId: string | null
  onSelect: (id: string) => void
  onAdd: (text: string) => void
  onUpdate: (id: string, changes: Partial<CaptionElement>) => void
  onDelete: (id: string) => void
  currentTime: number
  duration: number
}

const MODES: { id: TextMode; label: string; icon: typeof TextAa; hint: string }[] = [
  { id: 'word', label: 'Caption word', icon: ClosedCaptioning, hint: 'Style one word of your captions.' },
  { id: 'layer', label: 'Text overlay', icon: TextT, hint: 'Add titles and labels on top of the video.' },
]

export const TextPanel = ({ mode, onMode, ...rest }: Props) => {
  const active = MODES.find((item) => item.id === mode) ?? MODES[0]
  return (
    <aside className="tool-panel" data-testid="text-panel">
      <div className="panel-heading"><TextAa size={18} /><div><span>TEXT</span><h2 data-testid="text-panel-heading">Text &amp; words</h2></div></div>
      <div className="text-mode-switch" role="tablist" aria-label="What to edit" data-testid="text-mode-switch">
        {MODES.map(({ id, label, icon: Icon }) => (
          <button key={id} role="tab" aria-selected={mode === id} className={mode === id ? 'active' : ''} onClick={() => onMode(id)} data-testid={`text-mode-${id}-button`}>
            <Icon size={15} weight={mode === id ? 'fill' : 'regular'} /><span>{label}</span>
          </button>
        ))}
      </div>
      <p className="text-mode-hint" data-testid="text-mode-hint">{active.hint}</p>
      {mode === 'word' ? <CaptionWordSection {...rest} /> : <TextOverlaySection {...rest} />}
    </aside>
  )
}

type SectionProps = Omit<Props, 'mode' | 'onMode'>

const CaptionWordSection = ({ theme, selectedWord, onWordStyle }: SectionProps) => {
  const { catalog } = useCaptionCatalog()
  const activeTiers = catalog.themes.find((item) => item.id === theme)?.tiers ?? null
  if (!selectedWord) return (
    <div className="text-empty-state" data-testid="selected-word-empty">
      <CursorClick size={22} />
      <b data-testid="selected-word-heading">No word selected</b>
      <p>Click a word in Captions, on the timeline or on the video to style it here.</p>
    </div>
  )
  const setEmphasis = (tier: string) => {
    if (!activeTiers) return
    const config = activeTiers[tier]
    onWordStyle({ emphasis: tier, important: tier !== 'support', color: config.color, font_size: config.size })
  }
  const glow = theme === STREET_GLOW
  return <>
    <div className="selected-word-card" data-testid="selected-word-card">
      <span>SELECTED WORD</span>
      <b data-testid="selected-word-heading" style={{ color: selectedWord.color || undefined }}>{selectedWord.text}</b>
    </div>
    {glow && <p className="panel-note" data-testid="street-glow-word-note">Street Glow's size and colour come from which line a word is on (set below), not per word.</p>}
    <label className="control-label" data-testid="font-size-control-label">Font size <b data-testid="selected-word-font-size">{selectedWord.font_size || DEFAULT_WORD_SIZE}px</b></label>
    <CtSlider min={16} max={120} step={1} value={[selectedWord.font_size || DEFAULT_WORD_SIZE]} disabled={glow} onValueChange={([value]) => onWordStyle({ font_size: value })} data-testid="font-size-slider" />
    <label className="control-label" data-testid="word-color-control-label"><span><Palette size={16} /> Word colour</span>
      <input type="color" value={selectedWord.color || '#FFFFFF'} disabled={glow} onChange={(e) => onWordStyle({ color: e.target.value })} data-testid="word-color-input" />
    </label>
    {activeTiers && <>
      <label className="control-label" data-testid="word-emphasis-label">Role in caption</label>
      <div className="layout-grid three" data-testid="word-emphasis-selector">
        {['support', 'feature', 'key'].map((tier) => <button key={tier} className={selectedWord.emphasis === tier ? 'active' : ''} onClick={() => setEmphasis(tier)} data-testid={`word-emphasis-${tier}-button`}>{tier[0].toUpperCase() + tier.slice(1)}</button>)}
      </div>
    </>}
    <div className="tool-divider" />
    <label className="control-label" data-testid="line-break-label">Line break before this word</label>
    <div className="layout-grid three" data-testid="line-break-selector">
      <button className={(selectedWord.new_line ?? null) === null ? 'active' : ''} onClick={() => onWordStyle({ new_line: null })} data-testid="line-break-auto-button">Auto</button>
      <button className={selectedWord.new_line === false ? 'active' : ''} onClick={() => onWordStyle({ new_line: false })} data-testid="line-break-merge-button">Merge up</button>
      <button className={selectedWord.new_line === true ? 'active' : ''} onClick={() => onWordStyle({ new_line: true })} data-testid="line-break-split-button">Split here</button>
    </div>
    <label className="control-label" data-testid="caption-break-label">Caption split before this word</label>
    <div className="layout-grid three" data-testid="caption-break-selector">
      <button className={(selectedWord.new_caption ?? null) === null ? 'active' : ''} onClick={() => onWordStyle({ new_caption: null })} data-testid="caption-break-auto-button">Auto</button>
      <button className={selectedWord.new_caption === false ? 'active' : ''} onClick={() => onWordStyle({ new_caption: false })} data-testid="caption-break-merge-button">Merge up</button>
      <button className={selectedWord.new_caption === true ? 'active' : ''} onClick={() => onWordStyle({ new_caption: true })} data-testid="caption-break-split-button">Split here</button>
    </div>
    <p className="panel-note" data-testid="caption-break-note">A caption split starts a whole new caption box (own position/size/timing) here - a line break only wraps text within the same box.</p>
  </>
}

const TextOverlaySection = ({ elements, selectedId, onSelect, onAdd, onUpdate, onDelete, currentTime, duration }: SectionProps) => {
  const { catalog } = useCaptionCatalog()
  const [draft, setDraft] = useState('')
  // Stickers can no longer be added, but projects saved before that may still hold some: they are listed so they can be removed.
  const selected = elements.find((item) => item.id === selectedId)
  const isText = selected?.kind === 'text'
  const submit = () => {
    if (!draft.trim()) return
    onAdd(draft.trim())
    setDraft('')
  }
  return <>
    <div className="text-add-row" data-testid="text-add-row">
      <input value={draft} onChange={(e) => setDraft(e.target.value)} onKeyDown={(e) => e.key === 'Enter' && submit()}
        placeholder="Type a title or label…" maxLength={60} data-testid="text-draft-input" />
      <button onClick={submit} disabled={!draft.trim()} title="Add at the playhead" data-testid="add-text-button"><Plus size={16} weight="bold" /></button>
    </div>
    <div className="tool-divider" />
    <div className="panel-heading compact"><div><span>PLACED</span><h2 data-testid="text-layer-list-heading">On this video</h2></div></div>
    {!elements.length && <p className="panel-note" data-testid="no-text-layers-note">No overlays yet - type something above to add it at the playhead.</p>}
    <div className="element-list" data-testid="text-layer-list">
      {elements.map((item) => <div key={item.id} className={`element-row ${selectedId === item.id ? 'selected' : ''}`} onClick={() => onSelect(item.id)} data-testid={`text-layer-row-${item.id}`}>
        <span className="element-glyph" style={{ color: item.color, fontSize: item.kind === 'text' ? 11 : undefined }}>{item.char}</span>
        <span className="element-time" data-testid={`text-layer-time-${item.id}`}>{item.start.toFixed(1)}s – {item.end.toFixed(1)}s</span>
        <button onClick={(e) => { e.stopPropagation(); onDelete(item.id) }} title="Remove" data-testid={`text-layer-delete-${item.id}`}><Trash size={14} /></button>
      </div>)}
    </div>
    {selected && <>
      <div className="tool-divider" />
      {isText && <>
        <label className="control-label" data-testid="text-font-label">Font</label>
        <div className="font-grid" data-testid="text-font-selector">
          {catalog.fonts.map((item) => <button key={item.id} className={selected.font_id === item.id ? 'active' : ''} style={{ fontFamily: cssFamily(item.family) }} onClick={() => onUpdate(selected.id, { font_id: item.id })} data-testid={`text-font-${item.id}-button`}>{item.name}</button>)}
        </div>
      </>}
      <label className="control-label" data-testid="text-size-label">Size <b data-testid="text-size-value">{selected.font_size}px</b></label>
      <CtSlider min={isText ? 20 : 40} max={isText ? 160 : 280} step={isText ? 2 : 4} value={[selected.font_size]} onValueChange={([value]) => onUpdate(selected.id, { font_size: value })} data-testid="text-size-slider" />
      <label className="control-label" data-testid="text-color-label"><span><Palette size={16} /> Colour</span>
        <input type="color" value={selected.color} onChange={(e) => onUpdate(selected.id, { color: e.target.value })} data-testid="text-color-input" />
      </label>
      <label className="control-label" data-testid="text-layer-depth-label">Layer</label>
      <div className="layout-grid two layer-depth-toggle" data-testid="text-layer-depth-toggle">
        <button className={(selected.layer ?? 'front') === 'front' ? 'active' : ''} onClick={() => onUpdate(selected.id, { layer: 'front' })} data-testid="text-layer-front-button">In front</button>
        <button className={selected.layer === 'behind' ? 'active' : ''} onClick={() => onUpdate(selected.id, { layer: 'behind' })} data-testid="text-layer-behind-button">Behind captions</button>
      </div>
      <label className="control-label" data-testid="text-timing-label">Timing</label>
      <div className="layout-grid two">
        <button onClick={() => onUpdate(selected.id, { start: Math.min(currentTime, selected.end - 0.1) })} data-testid="text-set-start-button">Start here</button>
        <button onClick={() => onUpdate(selected.id, { end: Math.max(Math.min(duration, currentTime), selected.start + 0.1) })} data-testid="text-set-end-button">End here</button>
      </div>
    </>}
  </>
}
