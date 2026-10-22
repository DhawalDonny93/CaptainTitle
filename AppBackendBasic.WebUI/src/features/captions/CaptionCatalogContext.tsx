import { useEffect, useMemo, useState, type ReactNode } from 'react'
import { captionsApi } from './captionsApi'
import { CatalogContext, type CatalogValue } from './catalogContext'
import type { CaptionCatalog } from './types'


/** Loads the admin-managed catalog once; replaces the constants that used to be hardcoded in the old bundle. */
export function CaptionCatalogProvider({ children }: { children: ReactNode }) {
  const [catalog, setCatalog] = useState<CaptionCatalog | null>(null)
  const [error, setError] = useState(false)

  useEffect(() => {
    captionsApi.catalog().then(setCatalog).catch(() => setError(true))
  }, [])

  const value = useMemo<CatalogValue | null>(() => catalog && ({
    catalog,
    themeById: Object.fromEntries(catalog.themes.map((t) => [t.id, t])),
    fontById: Object.fromEntries(catalog.fonts.map((f) => [f.id, f])),
    variantById: Object.fromEntries(catalog.variants.map((v) => [v.id, v])),
    sfxById: Object.fromEntries(catalog.sfx_types.map((s) => [s.id, s])),
  }), [catalog])

  if (error) return <div className="auth-loading ct-scope" data-testid="catalog-error">Captain Title could not load its configuration. Please refresh.</div>
  if (!value) return <div className="auth-loading ct-scope" data-testid="auth-checking-status">Loading Captain Title…</div>
  return <CatalogContext.Provider value={value}>{children}</CatalogContext.Provider>
}
