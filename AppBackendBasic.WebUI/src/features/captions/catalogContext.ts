import { createContext, useContext } from 'react'
import type { CaptionCatalog, CaptionFont, CaptionTheme, CaptionVariant, SfxType } from './types'

export interface CatalogValue {
  catalog: CaptionCatalog
  themeById: Record<string, CaptionTheme>
  fontById: Record<string, CaptionFont>
  variantById: Record<string, CaptionVariant>
  sfxById: Record<string, SfxType>
}

export const CatalogContext = createContext<CatalogValue | null>(null)

export function useCaptionCatalog(): CatalogValue {
  const v = useContext(CatalogContext)
  if (!v) throw new Error('useCaptionCatalog must be used inside CaptionCatalogProvider')
  return v
}
