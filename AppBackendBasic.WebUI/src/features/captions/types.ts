// Wire types for the Captain Title caption API (snake_case is preserved from the original API - see
// docs/captain-title/DECISIONS.md D-04).

export type Emphasis = 'support' | 'feature' | 'key'
export type KineticStyle = 'pulse' | 'pop' | 'scale' | 'box' | 'wobble'
export type CaptionPosition = 'top' | 'middle' | 'bottom'
export type ScriptMode = 'roman' | 'native'
export type RenderQuality = 'high' | 'draft'
export type ProjectStatus = 'queued' | 'processing' | 'ready' | 'failed' | 'rendering' | 'rendered'

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
  /** Degrees (-45..45) the whole caption box is tilted around its centre; null = none. */
  rotation: number | null
  /** Tri-state caption boundary: true = new caption starts here (manual split), false = joins the previous one (manual merge), null = auto. */
  new_caption: boolean | null
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
  /** Draw order vs the caption text (plain z-staging, no subject matting). */
  layer?: 'front' | 'behind'
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
  script_mode: ScriptMode
  outline_enabled: boolean
  outline_width: number
  outline_color: string
  shadow_enabled: boolean
  shadow_angle: number
  shadow_distance: number
  shadow_blur: number
  shadow_opacity: number
  shadow_color: string
  glow_enabled: boolean
  /** null = each word's own colour. */
  glow_color: string | null
  glow_blur: number
  glow_opacity: number
  background_enabled: boolean
  background_color: string
  background_opacity: number
  bold: boolean
  italic: boolean
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

export interface RazorpayCheckoutInfo {
  key_id: string
  order_id?: string
  subscription_id?: string
  amount: number
  currency: string
  name: string
  description: string
  prefill_email?: string
  prefill_name?: string
}

export interface CheckoutResponse {
  provider: 'stripe' | 'razorpay'
  checkout_url?: string
  session_id: string
  razorpay?: RazorpayCheckoutInfo
}

export interface PaymentProviders { stripe: boolean; razorpay: boolean; razorpay_key_id?: string }

/** Project-wide typography controls (Outline / Shadow / Glow / Background box) - one object so the editor can spread it into the payload. */
export type TextStyle = Pick<CaptionProject,
  'outline_enabled' | 'outline_width' | 'outline_color' | 'shadow_enabled' | 'shadow_angle' | 'shadow_distance' | 'shadow_blur' | 'shadow_opacity' |
  'shadow_color' | 'glow_enabled' | 'glow_color' | 'glow_blur' | 'glow_opacity' | 'background_enabled' | 'background_color' | 'background_opacity'>

export type ProjectEditPayload = Pick<CaptionProject,
  'theme' | 'font_id' | 'words_per_caption' | 'caption_lines' | 'caption_position' | 'words' | 'audio_volume' | 'audio_muted' |
  'brightness' | 'contrast' | 'saturation' | 'render_quality' | 'elements' | 'sfx_markers' | 'script_mode' | 'bold' | 'italic'> & TextStyle
