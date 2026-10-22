// Wire types for the Captain Title caption API (snake_case is preserved from the original API - see
// docs/captain-title/DECISIONS.md D-04).

export type Emphasis = 'support' | 'feature' | 'key'
export type KineticStyle = 'pulse' | 'pop' | 'scale' | 'box' | 'wobble'
export type CaptionPosition = 'top' | 'middle' | 'bottom'
export type RenderQuality = 'high' | 'draft'
export type ProjectStatus = 'queued' | 'processing' | 'ready' | 'failed' | 'rendered'

export interface CaptionWord {
  index: number
  text: string
  original: string
  start: number
  end: number
  important: boolean
  emphasis: Emphasis | string
  uncertain: boolean
  color: string
  font_size: number
  pos_x: number | null
  pos_y: number | null
  new_line: boolean | null
  box_width: number | null
  box_height: number | null
  kinetic: KineticStyle | null
  emphasize: boolean
}

export interface CaptionElement {
  id: string
  kind: 'sticker' | 'text'
  char: string
  font_id: string
  color: string
  font_size: number
  start: number
  end: number
  pos_x: number
  pos_y: number
}

export interface SfxMarker {
  id: string
  type: string
  time: number
  word_index: number | null
}

export interface CaptionProject {
  project_id: number
  name: string
  duration: number
  width: number | null
  height: number | null
  size_bytes: number
  theme: string
  font_id: string
  words_per_caption: number | null
  caption_lines: number
  caption_position: CaptionPosition
  audio_volume: number
  audio_muted: boolean
  brightness: number
  contrast: number
  saturation: number
  render_quality: RenderQuality
  elements: CaptionElement[]
  sfx_markers: SfxMarker[]
  variant: string | null
  words: CaptionWord[]
  source_transcript: string
  speech_language: string
  transcription_mode: string | null
  transcription_quality: unknown
  status: ProjectStatus
  progress: number
  processing_label: string
  error_message: string | null
  created_at: string
  updated_at: string | null
  /** Client-side only: set after a successful render. */
  download_url?: string
}

export interface CaptionTier { font: string; css_font: string; color: string; size: number }

export interface CaptionTheme {
  id: string
  name: string
  base_color: string | null
  color: string
  accent_font: string | null
  tiers: Record<string, CaptionTier> | null
  card: { stroke: string | null; shadow: string | null; glow: string | null; highlight: boolean }
}

export interface CaptionFont { id: string; name: string; family: string }

export interface CaptionVariant {
  id: string
  label: string
  font_family: string
  text_shadow: string | null
  stroke: string | null
  accent: string
}

export interface SfxType {
  id: string
  label: string
  icon: string
  color: string
  kind: 'sweep' | 'drop' | 'blip' | 'chime' | 'click'
  freq: number
  sweep_to: number
  duration: number
}

export interface CaptionLimits {
  absolute_max_seconds: number
  max_upload_mb: number
  free_videos: number
  free_max_seconds: number
  unlimited_testing: boolean
  allowed_extensions: string[]
}

export interface CaptionCatalog {
  themes: CaptionTheme[]
  fonts: CaptionFont[]
  variants: CaptionVariant[]
  sfx_types: SfxType[]
  stickers: string[]
  filler_words: string[]
  limits: CaptionLimits
  default_theme: string
}

export interface CaptionPlan {
  id: string
  name: string
  price: number
  currency: string
  kind: 'payment' | 'subscription'
  videos: number
  max_seconds: number
  max_purchases: number | null
  description: string | null
  featured: boolean
}

export interface CaptionUsage {
  free_remaining: number
  oneoff_credits: number
  oneoff_purchase_count: number
  basic_credits: number
  subscription_active: boolean
  videos_created: number
  unlimited_testing: boolean
  plans: CaptionPlan[]
}

export type ProjectEditPayload = Pick<CaptionProject,
  'theme' | 'font_id' | 'words_per_caption' | 'caption_lines' | 'caption_position' | 'words' | 'audio_volume' | 'audio_muted' |
  'brightness' | 'contrast' | 'saturation' | 'render_quality' | 'elements' | 'sfx_markers' | 'variant'>
