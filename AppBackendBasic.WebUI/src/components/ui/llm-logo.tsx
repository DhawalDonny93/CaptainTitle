/**
 * LLMLogo — displays the official square/icon logo for any supported LLM platform.
 *
 * Icons are stored in: /public/images/llm-icons/
 *
 * Usage:
 *   <LLMLogo name="ChatGPT" size="sm" />
 *   <LLMLogo name="Claude"  size="md" />
 *   <LLMLogo name="Gemini"  size="lg" className="rounded-full" />
 */

export type LLMName =
  | 'ChatGPT'
  | 'Claude'
  | 'Gemini'
  | 'Perplexity'
  | 'Google AI Overview'
  | 'Google AI'
  | 'DeepSeek'
  | 'Grok'

interface LLMLogoProps {
  name: LLMName
  /** sm=16px  md=20px  lg=24px  xl=32px */
  size?: 'sm' | 'md' | 'lg' | 'xl'
  className?: string
  alt?: string
}

const logoMap: Record<LLMName, string> = {
  ChatGPT:            '/images/llm-icons/openai.svg',
  Claude:             '/images/llm-icons/claude.svg',
  Gemini:             '/images/llm-icons/gemini.svg',
  Perplexity:         '/images/llm-icons/perplexity.svg',
  'Google AI Overview': '/images/llm-icons/google.svg',
  'Google AI':        '/images/llm-icons/google.svg',
  DeepSeek:           '/images/llm-icons/deepseek.svg',
  Grok:               '/images/llm-icons/grok.svg',
}

const sizeMap = {
  sm: 'w-4 h-4',   // 16px
  md: 'w-5 h-5',   // 20px
  lg: 'w-6 h-6',   // 24px
  xl: 'w-8 h-8',   // 32px
}

export function LLMLogo({ name, size = 'md', className = '', alt }: LLMLogoProps) {
  const src = logoMap[name]
  if (!src) return null

  return (
    <img
      src={src}
      alt={alt ?? `${name} logo`}
      className={`inline-block ${sizeMap[size]} object-contain ${className}`}
      loading="lazy"
    />
  )
}

export default LLMLogo
