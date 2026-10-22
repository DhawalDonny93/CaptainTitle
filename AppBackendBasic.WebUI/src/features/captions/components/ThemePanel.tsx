import { Palette, Rows, Sparkle, SquaresFour, TextT, type Icon } from '@phosphor-icons/react'
import { CtSlider } from './ui'
import { useCaptionCatalog } from '../catalogContext'
import { cssFamily } from '../lib/styles'
import type { CaptionPosition, CaptionTheme, CaptionWord } from '../types'

const wordsPerCaptionOptions: (number | null)[] = [null, 1, 2, 3, 4, 5, 6, 8]
const positions: CaptionPosition[] = ['top', 'middle', 'bottom']

export type ThemeTab = 'templates' | 'customize' | 'text'
const tabs: { id: ThemeTab; label: string; icon: Icon }[] = [
  { id: 'templates', label: 'Templates', icon: SquaresFour },
  { id: 'customize', label: 'Customize', icon: Rows },
  { id: 'text', label: 'Text', icon: TextT },
]

interface PreviewWord { text: string; font: string; color: string; size: number; weight: number }

// Two or three sample words rendered at card scale, reusing each template's real font/colour choices - a flat template
// contrasts its base vs accent word exactly like `word.important` does; a tiered one walks its tiers.
function previewWords(template: CaptionTheme): PreviewWord[] {
  if (template.tiers) {
    const tiers = template.tiers
    return (['support', 'feature', 'key'] as const).filter((tier) => tiers[tier]).map((tier) => {
      const config = tiers[tier]
      return { text: tier === 'key' ? 'HOOK' : tier, font: config.css_font, color: config.color,
        size: Math.min(26, Math.max(12, Math.round(config.size * 0.32))), weight: tier === 'support' ? 600 : 800 }
    })
  }
  const accentFont = template.accent_font ? cssFamily(template.accent_font) : cssFamily('Poppins')
  return [
    { text: 'the', font: cssFamily('Poppins'), color: template.base_color ?? '#FFFFFF', size: 15, weight: 600 },
    { text: 'HOOK', font: accentFont, color: template.color, size: 25, weight: 700 },
  ]
}

interface Props {
  theme: string
  onTheme: (id: string) => void
  fontId: string
  onFont: (id: string) => void
  wordsPerCaption: number | null
  onWordsPerCaption: (n: number | null) => void
  captionLines: number
  onCaptionLines: (n: number) => void
  captionPosition: CaptionPosition
  onCaptionPosition: (p: CaptionPosition) => void
  selectedWord: CaptionWord | undefined
  onWordStyle: (change: Partial<CaptionWord>) => void
  tab: ThemeTab
  onTab: (tab: ThemeTab) => void
  variant: string | null
  onVariant: (id: string | null) => void
}

export const ThemePanel = ({ theme, onTheme, fontId, onFont, wordsPerCaption, onWordsPerCaption, captionLines, onCaptionLines,
  captionPosition, onCaptionPosition, selectedWord, onWordStyle, tab, onTab, variant, onVariant }: Props) => {
  const { catalog } = useCaptionCatalog()
  const activeTiers = catalog.themes.find((item) => item.id === theme)?.tiers ?? null
  const isTiered = !!activeTiers
  const setEmphasis = (tier: string) => {
    if (!activeTiers) return
    const config = activeTiers[tier]
    onWordStyle({ emphasis: tier, important: tier !== 'support', color: config.color, font_size: config.size })
  }
  return (
    <aside className="tool-panel" data-testid="caption-tools-panel">
      <div className="panel-tabs" data-testid="caption-tool-tabs">
        {tabs.map(({ id, label, icon: TabIcon }) => <button key={id} className={tab === id ? 'active' : ''} onClick={() => onTab(id)} data-testid={`caption-tab-${id}-button`}>
          <TabIcon size={17} /> {label}
        </button>)}
      </div>

      {tab === 'templates' && <>
        <div className="panel-heading"><Sparkle size={18} /><div><span>STYLE ENGINE</span><h2 data-testid="theme-panel-heading">Caption template</h2></div></div>
        <div className="template-grid" data-testid="caption-theme-selector">
          {catalog.themes.map((item) => {
            const words = previewWords(item)
            return (
              <button key={item.id} className={theme === item.id ? 'active' : ''} onClick={() => onTheme(item.id)} data-testid={`theme-${item.id}-button`}>
                <span className="template-card-preview">
                  {words.map((word, index) => {
                    const highlighted = item.card.highlight && index === words.length - 1
                    return <span key={index} className="template-card-word"
                      style={{
                        fontFamily: word.font, color: word.color, fontSize: `${word.size}px`, fontWeight: word.weight,
                        WebkitTextStroke: item.card.stroke || undefined,
                        textShadow: [item.card.shadow, item.card.glow].filter(Boolean).join(', ') || undefined,
                        background: highlighted ? 'rgba(255,255,255,.12)' : undefined,
                        padding: highlighted ? '1px 5px' : undefined,
                        borderRadius: highlighted ? '3px' : undefined,
                      }}>{word.text}</span>
                  })}
                </span>
                <span className="template-card-name">{item.name}</span>
              </button>
            )
          })}
        </div>
        <div className="tool-divider" />
        <label className="control-label" data-testid="variant-selector-label"><span>Variants</span></label>
        <div className="variant-pills" data-testid="variant-selector">
          {catalog.variants.map((item) => <button key={item.id} className={variant === item.id ? 'active' : ''} onClick={() => onVariant(variant === item.id ? null : item.id)}
            style={{ fontFamily: cssFamily(item.font_family), color: item.accent }} data-testid={`variant-${item.id}-button`}>{item.label}</button>)}
        </div>
      </>}

      {tab === 'customize' && <>
        <div className="panel-heading"><TextT size={18} /><div><span>TYPEFACE</span><h2 data-testid="font-panel-heading">Caption font</h2></div></div>
        {isTiered && <p className="panel-note" data-testid="tiered-font-note">This template sets its own three fonts - the picker below doesn't apply.</p>}
        <div className="font-grid" data-testid="caption-font-selector">
          {catalog.fonts.map((item) => <button key={item.id} className={fontId === item.id ? 'active' : ''} disabled={isTiered} style={{ fontFamily: cssFamily(item.family) }} onClick={() => onFont(item.id)} data-testid={`font-${item.id}-button`}>
            {item.name}
          </button>)}
        </div>
        <div className="tool-divider" />
        <div className="panel-heading compact"><Rows size={18} /><div><span>LAYOUT</span><h2 data-testid="layout-panel-heading">Caption layout</h2></div></div>
        <label className="control-label" data-testid="words-per-caption-label">Words per caption</label>
        <div className="layout-grid" data-testid="words-per-caption-selector">
          {wordsPerCaptionOptions.map((n) => <button key={n ?? 'auto'} className={wordsPerCaption === n ? 'active' : ''} onClick={() => onWordsPerCaption(n)} data-testid={`words-per-caption-${n ?? 'auto'}-button`}>{n ?? 'Auto'}</button>)}
        </div>
        <label className="control-label" data-testid="caption-lines-label">Lines</label>
        <div className="layout-grid two" data-testid="caption-lines-selector">
          <button className={captionLines === 1 ? 'active' : ''} onClick={() => onCaptionLines(1)} data-testid="caption-lines-1-button">One line</button>
          <button className={captionLines === 2 ? 'active' : ''} onClick={() => onCaptionLines(2)} data-testid="caption-lines-2-button">Two lines</button>
        </div>
        <label className="control-label" data-testid="caption-position-label">Position</label>
        <div className="layout-grid three" data-testid="caption-position-selector">
          {positions.map((p) => <button key={p} className={captionPosition === p ? 'active' : ''} onClick={() => onCaptionPosition(p)} data-testid={`caption-position-${p}-button`}>{p[0].toUpperCase() + p.slice(1)}</button>)}
        </div>
      </>}

      {tab === 'text' && <>
        <div className="panel-heading"><TextT size={18} /><div><span>SELECTED WORD</span><h2 data-testid="selected-word-heading">{selectedWord?.text || 'Select a word'}</h2></div></div>
        <label className="control-label" data-testid="font-size-control-label">Font size <b data-testid="selected-word-font-size">{selectedWord?.font_size || 52}px</b></label>
        <CtSlider min={24} max={92} step={1} value={[selectedWord?.font_size || 52]} disabled={!selectedWord} onValueChange={([value]) => onWordStyle({ font_size: value })} data-testid="font-size-slider" />
        <label className="control-label" data-testid="word-color-control-label"><span><Palette size={16} /> Word colour</span>
          <input type="color" value={selectedWord?.color || '#FFFFFF'} disabled={!selectedWord} onChange={(e) => onWordStyle({ color: e.target.value })} data-testid="word-color-input" />
        </label>
        {isTiered ? <>
          <label className="control-label" data-testid="word-emphasis-label">Role in caption</label>
          <div className="layout-grid three" data-testid="word-emphasis-selector">
            {['support', 'feature', 'key'].map((tier) => <button key={tier} className={selectedWord?.emphasis === tier ? 'active' : ''} disabled={!selectedWord} onClick={() => setEmphasis(tier)} data-testid={`word-emphasis-${tier}-button`}>{tier[0].toUpperCase() + tier.slice(1)}</button>)}
          </div>
        </> : <button className={`emphasis-toggle ${selectedWord?.important ? 'active' : ''}`} disabled={!selectedWord} onClick={() => onWordStyle({ important: !selectedWord?.important })} data-testid="important-word-toggle">
          <span>Impact emphasis</span><i />
        </button>}
        <label className="control-label" data-testid="line-break-label">Line break before this word</label>
        <div className="layout-grid three" data-testid="line-break-selector">
          <button className={(selectedWord?.new_line ?? null) === null ? 'active' : ''} disabled={!selectedWord} onClick={() => onWordStyle({ new_line: null })} data-testid="line-break-auto-button">Auto</button>
          <button className={selectedWord?.new_line === false ? 'active' : ''} disabled={!selectedWord} onClick={() => onWordStyle({ new_line: false })} data-testid="line-break-merge-button">Merge up</button>
          <button className={selectedWord?.new_line === true ? 'active' : ''} disabled={!selectedWord} onClick={() => onWordStyle({ new_line: true })} data-testid="line-break-split-button">Split here</button>
        </div>
      </>}
    </aside>
  )
}
