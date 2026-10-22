import { Shapes, Trash } from '@phosphor-icons/react'
import { CtSlider } from './ui'
import { useCaptionCatalog } from '../catalogContext'
import type { CaptionElement } from '../types'

interface Props {
  elements: CaptionElement[]
  selectedId: string | null
  onSelect: (id: string) => void
  onAdd: (char: string) => void
  onUpdate: (id: string, changes: Partial<CaptionElement>) => void
  onDelete: (id: string) => void
  currentTime: number
  duration: number
}

export const ElementsPanel = ({ elements, selectedId, onSelect, onAdd, onUpdate, onDelete, currentTime, duration }: Props) => {
  const { catalog } = useCaptionCatalog() // sticker glyphs are admin-managed
  const selected = elements.find((item) => item.id === selectedId)
  return (
    <aside className="tool-panel" data-testid="elements-panel">
      <div className="panel-heading"><Shapes size={18} /><div><span>STICKERS</span><h2 data-testid="elements-panel-heading">Add an element</h2></div></div>
      <div className="sticker-grid" data-testid="sticker-grid">
        {catalog.stickers.map((char) => <button key={char} onClick={() => onAdd(char)} data-testid={`sticker-${char}-button`}>{char}</button>)}
      </div>
      <div className="tool-divider" />
      <div className="panel-heading compact"><div><span>PLACED</span><h2 data-testid="element-list-heading">On this video</h2></div></div>
      {!elements.length && <p className="panel-note" data-testid="no-elements-note">No elements yet - tap a sticker above to drop one onto the frame at the current playhead time.</p>}
      <div className="element-list" data-testid="element-list">
        {elements.map((item) => <div key={item.id} className={`element-row ${selectedId === item.id ? 'selected' : ''}`} onClick={() => onSelect(item.id)} data-testid={`element-row-${item.id}`}>
          <span className="element-glyph" style={{ color: item.color }}>{item.char}</span>
          <span className="element-time" data-testid={`element-row-time-${item.id}`}>{item.start.toFixed(1)}s – {item.end.toFixed(1)}s</span>
          <button onClick={(e) => { e.stopPropagation(); onDelete(item.id) }} title="Remove element" data-testid={`element-delete-${item.id}`}><Trash size={14} /></button>
        </div>)}
      </div>
      {selected && <>
        <div className="tool-divider" />
        <label className="control-label" data-testid="element-size-label">Size <b data-testid="element-size-value">{selected.font_size}px</b></label>
        <CtSlider min={40} max={280} step={4} value={[selected.font_size]} onValueChange={([value]) => onUpdate(selected.id, { font_size: value })} data-testid="element-size-slider" />
        <label className="control-label" data-testid="element-color-label"><span>Colour</span>
          <input type="color" value={selected.color} onChange={(e) => onUpdate(selected.id, { color: e.target.value })} data-testid="element-color-input" />
        </label>
        <label className="control-label" data-testid="element-timing-label">Timing</label>
        <div className="layout-grid two">
          <button onClick={() => onUpdate(selected.id, { start: Math.min(currentTime, selected.end - 0.1) })} data-testid="element-set-start-button">Start here</button>
          <button onClick={() => onUpdate(selected.id, { end: Math.max(Math.min(duration, currentTime), selected.start + 0.1) })} data-testid="element-set-end-button">End here</button>
        </div>
      </>}
    </aside>
  )
}
