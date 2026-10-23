import { ArrowsOutCardinal, Palette, Rows, Sparkle, SquaresFour, TextT, type Icon } from '@phosphor-icons/react'
import { CtSlider } from './ui'
import { useCaptionCatalog } from '../catalogContext'
import { cssFamily, DEFAULT_WORD_SIZE, DOODLE_MARKER, STREET_GLOW } from '../lib/styles'
import type { CaptionPosition, CaptionTheme, CaptionWord, TextStyle } from '../types'

const wordsPerCaptionOptions: (number | null)[] = [null, 1, 2, 3, 4, 5, 6, 8]
const positions: CaptionPosition[] = ['top', 'middle', 'bottom']

export type ThemeTab = 'templates' | 'customize' | 'text' | 'transform'
const tabs: { id: ThemeTab; label: string; icon: Icon }[] = [
  { id: 'templates', label: 'Templates', icon: SquaresFour },
  { id: 'customize', label: 'Customize', icon: Rows },
  { id: 'text', label: 'Text', icon: TextT },
  { id: 'transform', label: 'Transform', icon: ArrowsOutCardinal },
]
// Quick Position Y presets inside the Transform tab - the same percentages as POSITION_DEFAULTS (CaptionStudio) and the server's
// CaptionLayout.PositionDefaults, so a preset here lands exactly where the Top/Middle/Bottom layout button would.
const TRANSFORM_Y_PRESETS = [{ label: 'Top', y: 14 }, { label: 'Middle', y: 50 }, { label: 'Bottom', y: 80 }]

interface PreviewWord { text: string; font: string; color: string; size: number; weight: number }

// Fallback card for an admin-added (non-fixed) template: two or three sample words in the template's real font/colour choices.
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

// The two fixed motion-graphic templates have bespoke cards mirroring their real look (Street Glow: glowing headline + cursive subline;
// Doodle Marker: highlighter pill). Any other template falls back to the generic data-driven card.
const TemplateCard = ({ item }: { item: CaptionTheme }) => {
  if (item.id === STREET_GLOW) return <>
    <span className="template-card-word" style={{ fontFamily: "'Anton', sans-serif", color: '#FF2E2E', fontSize: '19px', fontWeight: 700, textShadow: '0 0 7px rgba(255,46,46,.95), 0 0 14px rgba(255,46,46,.6)' }}>STAY</span>
    <span className="template-card-word" style={{ fontFamily: "'Playfair Display', serif", fontStyle: 'italic', fontWeight: 600, color: '#FFFFFF', fontSize: '15px', marginTop: '-3px' }}>hungry</span>
  </>
  if (item.id === DOODLE_MARKER) return <>
    <span className="template-card-word" style={{ fontFamily: "'Caveat', cursive", fontWeight: 700, color: '#FFFFFF', fontSize: '18px' }}>this</span>
    <span className="template-card-word" style={{ fontFamily: "'Caveat', cursive", fontWeight: 700, color: '#111111', fontSize: '18px', background: '#FACC15', padding: '0 6px', borderRadius: '255px 15px 225px 15px/15px 225px 15px 255px' }}>changes</span>
  </>
  const words = previewWords(item)
  return <>{words.map((word, index) => {
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
  })}</>
}

const OnOff = ({ on, onChange, id }: { on: boolean; onChange: (on: boolean) => void; id: string }) => (
  <div className="layout-grid two" data-testid={`${id}-toggle-selector`}>
    <button className={!on ? 'active' : ''} onClick={() => onChange(false)} data-testid={`${id}-off-button`}>Off</button>
    <button className={on ? 'active' : ''} onClick={() => onChange(true)} data-testid={`${id}-on-button`}>On</button>
  </div>
)

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
  textStyle: TextStyle
  onTextStyle: (change: Partial<TextStyle>) => void
  bold: boolean
  onBold: (bold: boolean) => void
  italic: boolean
  onItalic: (italic: boolean) => void
  /** Transform tab: whether a caption is on screen at the playhead, and that caption's current box values. */
  hasActiveCaption: boolean
  boxX: number
  boxY: number
  boxFontSize: number
  boxRotation: number
  onCaptionPos: (pos: { x?: number; y?: number }) => void
  onCaptionScale: (size: number) => void
  onCaptionRotation: (deg: number) => void
  onApplyTransformToAll: () => void
  onResetTransform: () => void
}

export const ThemePanel = ({ theme, onTheme, fontId, onFont, wordsPerCaption, onWordsPerCaption, captionLines, onCaptionLines,
  captionPosition, onCaptionPosition, selectedWord, onWordStyle, tab, onTab, textStyle, onTextStyle, bold, onBold, italic, onItalic,
  hasActiveCaption, boxX, boxY, boxFontSize, boxRotation, onCaptionPos, onCaptionScale, onCaptionRotation, onApplyTransformToAll, onResetTransform }: Props) => {
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
          <TabIcon size={14} /> {label}
        </button>)}
      </div>

      {tab === 'templates' && <>
        <div className="panel-heading"><Sparkle size={18} /><div><span>STYLE ENGINE</span><h2 data-testid="theme-panel-heading">Caption template</h2></div></div>
        <div className="template-grid" data-testid="caption-theme-selector">
          {catalog.themes.map((item) => (
            <button key={item.id} className={theme === item.id ? 'active' : ''} onClick={() => onTheme(item.id)} data-testid={`theme-${item.id}-button`}>
              <span className="template-card-preview"><TemplateCard item={item} /></span>
              <span className="template-card-name">{item.name}</span>
            </button>
          ))}
        </div>
      </>}

      {tab === 'customize' && <>
        {theme === STREET_GLOW ? <>
          <div className="panel-heading"><TextT size={18} /><div><span>STYLE ENGINE</span><h2 data-testid="font-panel-heading">Fixed design</h2></div></div>
          <p className="panel-note" data-testid="street-glow-fixed-note">Street Glow is a complete, fixed design - its fonts, colours, and glow/shadow are built into the template, not configurable here.</p>
        </> : <>
          <div className="panel-heading"><TextT size={18} /><div><span>TYPEFACE</span><h2 data-testid="font-panel-heading">Caption font</h2></div></div>
          {isTiered && <p className="panel-note" data-testid="tiered-font-note">This template sets its own three fonts - the picker below doesn't apply.</p>}
          <div className="font-grid" data-testid="caption-font-selector">
            {catalog.fonts.map((item) => <button key={item.id} className={fontId === item.id ? 'active' : ''} disabled={isTiered} style={{ fontFamily: cssFamily(item.family) }} onClick={() => onFont(item.id)} data-testid={`font-${item.id}-button`}>
              {item.name}
            </button>)}
          </div>
          <label className="control-label" data-testid="font-weight-style-label">Weight &amp; style</label>
          <div className="layout-grid two" data-testid="font-weight-style-selector">
            <button className={bold ? 'active' : ''} onClick={() => onBold(!bold)} data-testid="bold-toggle-button" style={{ fontWeight: 800 }}>Bold</button>
            <button className={italic ? 'active' : ''} onClick={() => onItalic(!italic)} data-testid="italic-toggle-button" style={{ fontStyle: 'italic' }}>Italic</button>
          </div>
          <label className="control-label" data-testid="background-toggle-label">Background box</label>
          <OnOff id="background" on={textStyle.background_enabled} onChange={(on) => onTextStyle({ background_enabled: on })} />
          {textStyle.background_enabled ? <>
            <label className="control-label" data-testid="background-color-label"><span><Palette size={16} /> Box colour</span>
              <input type="color" value={textStyle.background_color} onChange={(e) => onTextStyle({ background_color: e.target.value })} data-testid="background-color-input" />
            </label>
            <label className="control-label" data-testid="background-opacity-label">Box opacity <b>{Math.round(textStyle.background_opacity * 100)}%</b></label>
            <CtSlider min={0.1} max={1} step={0.05} value={[textStyle.background_opacity]} onValueChange={([v]) => onTextStyle({ background_opacity: v })} data-testid="background-opacity-slider" />
            <p className="panel-note" data-testid="background-note">A solid box behind each caption line. Outline, Shadow and Glow turn off while a Background is on - an ASS export can't combine both.</p>
          </> : <>
            <label className="control-label" data-testid="outline-toggle-label">Outline</label>
            <OnOff id="outline" on={textStyle.outline_enabled} onChange={(on) => onTextStyle({ outline_enabled: on })} />
            {textStyle.outline_enabled && <>
              <label className="control-label" data-testid="outline-width-label">Outline width <b>{textStyle.outline_width}px</b></label>
              <CtSlider min={0.5} max={8} step={0.5} value={[textStyle.outline_width]} onValueChange={([v]) => onTextStyle({ outline_width: v })} data-testid="outline-width-slider" />
              <label className="control-label" data-testid="outline-color-label"><span><Palette size={16} /> Outline colour</span>
                <input type="color" value={textStyle.outline_color} onChange={(e) => onTextStyle({ outline_color: e.target.value })} data-testid="outline-color-input" />
              </label>
            </>}
            <label className="control-label" data-testid="shadow-toggle-label">Shadow</label>
            <OnOff id="shadow" on={textStyle.shadow_enabled} onChange={(on) => onTextStyle({ shadow_enabled: on })} />
            {textStyle.shadow_enabled && <>
              <label className="control-label" data-testid="shadow-angle-label">Direction <b>{textStyle.shadow_angle}°</b></label>
              <CtSlider min={0} max={360} step={15} value={[textStyle.shadow_angle]} onValueChange={([v]) => onTextStyle({ shadow_angle: v })} data-testid="shadow-angle-slider" />
              <label className="control-label" data-testid="shadow-distance-label">Distance <b>{textStyle.shadow_distance}px</b></label>
              <CtSlider min={0} max={15} step={1} value={[textStyle.shadow_distance]} onValueChange={([v]) => onTextStyle({ shadow_distance: v })} data-testid="shadow-distance-slider" />
              <label className="control-label" data-testid="shadow-blur-label">Smoothness <b>{textStyle.shadow_blur}</b></label>
              <CtSlider min={0} max={15} step={1} value={[textStyle.shadow_blur]} onValueChange={([v]) => onTextStyle({ shadow_blur: v })} data-testid="shadow-blur-slider" />
              <label className="control-label" data-testid="shadow-opacity-label">Opacity <b>{Math.round(textStyle.shadow_opacity * 100)}%</b></label>
              <CtSlider min={0.1} max={1} step={0.05} value={[textStyle.shadow_opacity]} onValueChange={([v]) => onTextStyle({ shadow_opacity: v })} data-testid="shadow-opacity-slider" />
              <label className="control-label" data-testid="shadow-color-label"><span><Palette size={16} /> Shadow colour</span>
                <input type="color" value={textStyle.shadow_color} onChange={(e) => onTextStyle({ shadow_color: e.target.value })} data-testid="shadow-color-input" />
              </label>
            </>}
            <label className="control-label" data-testid="glow-toggle-label">Glow</label>
            <OnOff id="glow" on={textStyle.glow_enabled} onChange={(on) => onTextStyle({ glow_enabled: on })} />
            {textStyle.glow_enabled && <>
              <label className="control-label" data-testid="glow-blur-label">Intensity <b>{textStyle.glow_blur}</b></label>
              <CtSlider min={0} max={24} step={1} value={[textStyle.glow_blur]} onValueChange={([v]) => onTextStyle({ glow_blur: v })} data-testid="glow-blur-slider" />
              <label className="control-label" data-testid="glow-opacity-label">Opacity <b>{Math.round(textStyle.glow_opacity * 100)}%</b></label>
              <CtSlider min={0.1} max={1} step={0.05} value={[textStyle.glow_opacity]} onValueChange={([v]) => onTextStyle({ glow_opacity: v })} data-testid="glow-opacity-slider" />
              <label className="control-label" data-testid="glow-color-mode-label">Glow colour</label>
              <div className="layout-grid two" data-testid="glow-color-mode-selector">
                <button className={textStyle.glow_color === null ? 'active' : ''} onClick={() => onTextStyle({ glow_color: null })} data-testid="glow-color-auto-button">Word's own</button>
                <button className={textStyle.glow_color !== null ? 'active' : ''} onClick={() => onTextStyle({ glow_color: textStyle.glow_color || '#FFD400' })} data-testid="glow-color-custom-button">Custom</button>
              </div>
              {textStyle.glow_color !== null && <label className="control-label" data-testid="glow-color-label"><span><Palette size={16} /> Glow colour</span>
                <input type="color" value={textStyle.glow_color} onChange={(e) => onTextStyle({ glow_color: e.target.value })} data-testid="glow-color-input" />
              </label>}
            </>}
          </>}
        </>}
        <div className="tool-divider" />
        <div className="panel-heading compact"><Rows size={18} /><div><span>LAYOUT</span><h2 data-testid="layout-panel-heading">Caption layout</h2></div></div>
        <label className="control-label" data-testid="words-per-caption-label">Words per caption</label>
        <div className="layout-grid" data-testid="words-per-caption-selector">
          {wordsPerCaptionOptions.map((n) => <button key={n ?? 'auto'} className={wordsPerCaption === n ? 'active' : ''} onClick={() => onWordsPerCaption(n)} data-testid={`words-per-caption-${n ?? 'auto'}-button`}>{n ?? 'Auto'}</button>)}
        </div>
        <label className="control-label" data-testid="caption-lines-label">Lines</label>
        {(isTiered || theme === STREET_GLOW) && <p className="panel-note" data-testid="tiered-lines-note">{theme === STREET_GLOW ? "Street Glow is always a fixed two-line headline/subline - the picker below doesn't apply." : "This template breaks lines at each tier change, not a fixed line count - the picker below doesn't apply."}</p>}
        <div className="layout-grid two" data-testid="caption-lines-selector">
          <button className={captionLines === 1 ? 'active' : ''} disabled={isTiered || theme === STREET_GLOW} onClick={() => onCaptionLines(1)} data-testid="caption-lines-1-button">One line</button>
          <button className={captionLines === 2 ? 'active' : ''} disabled={isTiered || theme === STREET_GLOW} onClick={() => onCaptionLines(2)} data-testid="caption-lines-2-button">Two lines</button>
        </div>
        <label className="control-label" data-testid="caption-position-label">Position</label>
        <div className="layout-grid three" data-testid="caption-position-selector">
          {positions.map((p) => <button key={p} className={captionPosition === p ? 'active' : ''} onClick={() => onCaptionPosition(p)} data-testid={`caption-position-${p}-button`}>{p[0].toUpperCase() + p.slice(1)}</button>)}
        </div>
      </>}

      {tab === 'text' && <>
        <div className="panel-heading"><TextT size={18} /><div><span>SELECTED WORD</span><h2 data-testid="selected-word-heading">{selectedWord?.text || 'Select a word'}</h2></div></div>
        {theme === STREET_GLOW && <p className="panel-note" data-testid="street-glow-word-note">Street Glow's size and colour come from which line a word is on (set below), not per word.</p>}
        <label className="control-label" data-testid="font-size-control-label">Font size <b data-testid="selected-word-font-size">{selectedWord?.font_size || DEFAULT_WORD_SIZE}px</b></label>
        <CtSlider min={16} max={120} step={1} value={[selectedWord?.font_size || DEFAULT_WORD_SIZE]} disabled={!selectedWord || theme === STREET_GLOW} onValueChange={([value]) => onWordStyle({ font_size: value })} data-testid="font-size-slider" />
        <label className="control-label" data-testid="word-color-control-label"><span><Palette size={16} /> Word colour</span>
          <input type="color" value={selectedWord?.color || '#FFFFFF'} disabled={!selectedWord || theme === STREET_GLOW} onChange={(e) => onWordStyle({ color: e.target.value })} data-testid="word-color-input" />
        </label>
        {isTiered && <>
          <label className="control-label" data-testid="word-emphasis-label">Role in caption</label>
          <div className="layout-grid three" data-testid="word-emphasis-selector">
            {['support', 'feature', 'key'].map((tier) => <button key={tier} className={selectedWord?.emphasis === tier ? 'active' : ''} disabled={!selectedWord} onClick={() => setEmphasis(tier)} data-testid={`word-emphasis-${tier}-button`}>{tier[0].toUpperCase() + tier.slice(1)}</button>)}
          </div>
        </>}
        <label className="control-label" data-testid="line-break-label">Line break before this word</label>
        <div className="layout-grid three" data-testid="line-break-selector">
          <button className={(selectedWord?.new_line ?? null) === null ? 'active' : ''} disabled={!selectedWord} onClick={() => onWordStyle({ new_line: null })} data-testid="line-break-auto-button">Auto</button>
          <button className={selectedWord?.new_line === false ? 'active' : ''} disabled={!selectedWord} onClick={() => onWordStyle({ new_line: false })} data-testid="line-break-merge-button">Merge up</button>
          <button className={selectedWord?.new_line === true ? 'active' : ''} disabled={!selectedWord} onClick={() => onWordStyle({ new_line: true })} data-testid="line-break-split-button">Split here</button>
        </div>
        <label className="control-label" data-testid="caption-break-label">Caption split before this word</label>
        <div className="layout-grid three" data-testid="caption-break-selector">
          <button className={(selectedWord?.new_caption ?? null) === null ? 'active' : ''} disabled={!selectedWord} onClick={() => onWordStyle({ new_caption: null })} data-testid="caption-break-auto-button">Auto</button>
          <button className={selectedWord?.new_caption === false ? 'active' : ''} disabled={!selectedWord} onClick={() => onWordStyle({ new_caption: false })} data-testid="caption-break-merge-button">Merge up</button>
          <button className={selectedWord?.new_caption === true ? 'active' : ''} disabled={!selectedWord} onClick={() => onWordStyle({ new_caption: true })} data-testid="caption-break-split-button">Split here</button>
        </div>
        <p className="panel-note" data-testid="caption-break-note">A caption split starts a whole new caption box (own position/size/timing) here - a line break above only wraps text within the same box.</p>
      </>}

      {tab === 'transform' && <>
        <div className="panel-heading"><ArrowsOutCardinal size={18} /><div><span>THIS CAPTION</span><h2 data-testid="transform-panel-heading">Transform &amp; style</h2></div></div>
        {!hasActiveCaption && <p className="panel-note" data-testid="transform-no-caption-note">Play or scrub to a caption to transform it.</p>}
        <label className="control-label" data-testid="transform-scale-label">Scale / size <b data-testid="transform-scale-value">{Math.round(boxFontSize)}px</b></label>
        <CtSlider min={16} max={120} step={1} value={[boxFontSize]} disabled={!hasActiveCaption} onValueChange={([value]) => onCaptionScale(value)} data-testid="transform-scale-slider" />

        <label className="control-label" data-testid="transform-pos-y-label">Position Y <b data-testid="transform-pos-y-value">{Math.round(boxY)}%</b></label>
        <CtSlider min={5} max={95} step={1} value={[boxY]} disabled={!hasActiveCaption} onValueChange={([value]) => onCaptionPos({ y: value })} data-testid="transform-pos-y-slider" />
        <div className="layout-grid three" data-testid="transform-pos-y-presets">
          {TRANSFORM_Y_PRESETS.map(({ label, y }) => <button key={label} disabled={!hasActiveCaption} onClick={() => onCaptionPos({ y })} data-testid={`transform-pos-y-preset-${label.toLowerCase()}-button`}>{label}</button>)}
        </div>

        <label className="control-label" data-testid="transform-pos-x-label">Position X <b data-testid="transform-pos-x-value">{Math.round(boxX)}%</b></label>
        <CtSlider min={0} max={100} step={1} value={[boxX]} disabled={!hasActiveCaption} onValueChange={([value]) => onCaptionPos({ x: value })} data-testid="transform-pos-x-slider" />

        <label className="control-label" data-testid="transform-rotation-label">Rotation <b data-testid="transform-rotation-value">{Math.round(boxRotation)}&deg;</b></label>
        <CtSlider min={-45} max={45} step={1} value={[boxRotation]} disabled={!hasActiveCaption} onValueChange={([value]) => onCaptionRotation(value)} data-testid="transform-rotation-slider" />

        <div className="tool-divider" />
        <div className="layout-grid two" data-testid="transform-actions">
          <button disabled={!hasActiveCaption} onClick={onApplyTransformToAll} data-testid="transform-apply-all-button">Apply to All Captions</button>
          <button disabled={!hasActiveCaption} onClick={onResetTransform} data-testid="transform-reset-button">Reset to Default</button>
        </div>
      </>}
    </aside>
  )
}
