import { proxiedFetch } from './proxiedFetch'

// ─────────────────────────────────────────────────────────────────────────────
// Types
// ─────────────────────────────────────────────────────────────────────────────

export type PageType =
  | 'home' | 'about' | 'blog-index' | 'blog-post'
  | 'product' | 'service' | 'pricing' | 'contact'
  | 'docs' | 'features' | 'case-study' | 'other'

export interface Page {
  url: string
  title: string
  description: string
  type: PageType
  h1?: string
  h2s: string[]
  author?: string
  datePublished?: string
  lastmod?: string
  keywords?: string[]
}

export interface Organization {
  name: string
  orgType: string       // Organization, Person, SoftwareApplication, etc.
  description?: string
  url?: string
  foundingDate?: string
  location?: string
  sameAs: string[]      // social profile URLs
  applicationCategory?: string
  offers?: string       // pricing hint
}

export interface SiteAnalysis {
  url: string           // canonical base URL
  name: string
  tagline: string
  description: string
  language: string
  platform: string      // WordPress, Next.js, etc.
  keywords: string[]
  homepageH1: string
  homepageH2s: string[]
  homepageH3s: string[]
  organization?: Organization
  socials: Record<string, string>
  corePages: Page[]     // nav-linked key pages
  blogPosts: Page[]     // most recent articles
  sitemapFound: boolean
  sitemapUrl?: string
}

export type ProgressCallback = (msg: string) => void

// ─────────────────────────────────────────────────────────────────────────────
// HTML utilities
// ─────────────────────────────────────────────────────────────────────────────

function parseHtml(html: string): Document {
  return new DOMParser().parseFromString(html, 'text/html')
}

function getMeta(doc: Document, ...names: string[]): string {
  for (const name of names) {
    const val =
      doc.querySelector(`meta[property="${name}"]`)?.getAttribute('content') ??
      doc.querySelector(`meta[name="${name}"]`)?.getAttribute('content')
    if (val?.trim()) return val.trim()
  }
  return ''
}

function getTitle(doc: Document): string {
  return getMeta(doc, 'og:title', 'twitter:title') || doc.querySelector('title')?.textContent?.trim() || ''
}

function getDescription(doc: Document): string {
  return getMeta(doc, 'og:description', 'description', 'twitter:description')
}

function getKeywords(doc: Document): string[] {
  const raw = getMeta(doc, 'keywords', 'news_keywords')
  if (!raw) return []
  return raw.split(/[,;]/).map(k => k.trim()).filter(Boolean)
}

function getLanguage(doc: Document): string {
  return doc.documentElement.getAttribute('lang') || getMeta(doc, 'language') || ''
}

function stripTags(html: string): string {
  return html.replace(/<[^>]+>/g, '').replace(/\s+/g, ' ').trim()
}

function getHeadings(doc: Document, tag: 'h1' | 'h2' | 'h3', max = 10): string[] {
  return Array.from(doc.querySelectorAll(tag))
    .map(el => stripTags(el.innerHTML))
    .filter(t => t.length > 2 && t.length < 200)
    .slice(0, max)
}

// ─────────────────────────────────────────────────────────────────────────────
// JSON-LD extraction
// ─────────────────────────────────────────────────────────────────────────────

function extractJsonLd(doc: Document): any[] {
  const results: any[] = []
  doc.querySelectorAll('script[type="application/ld+json"]').forEach(s => {
    try {
      const data = JSON.parse(s.textContent || '')
      if (Array.isArray(data)) results.push(...data)
      // @graph pattern
      else if (data['@graph']) results.push(...data['@graph'])
      else results.push(data)
    } catch { /* malformed JSON-LD */ }
  })
  return results
}

function findInLd<T>(items: any[], ...types: string[]): T | undefined {
  return items.find(item =>
    types.some(t =>
      item['@type'] === t ||
      (Array.isArray(item['@type']) && item['@type'].includes(t))
    )
  ) as T | undefined
}

function extractOrganization(ldItems: any[]): Organization | undefined {
  const orgTypes = ['Organization', 'Corporation', 'LocalBusiness', 'SoftwareApplication', 'WebSite', 'Person', 'Brand']
  const item: any = findInLd(ldItems, ...orgTypes)
  if (!item) return undefined

  const sameAs: string[] = Array.isArray(item.sameAs) ? item.sameAs : item.sameAs ? [item.sameAs] : []

  return {
    name: item.name || '',
    orgType: Array.isArray(item['@type']) ? item['@type'][0] : item['@type'] || '',
    description: item.description || '',
    url: item.url || '',
    foundingDate: item.foundingDate || item.foundingYear || '',
    location: item.address?.addressLocality || item.address?.addressCountry || '',
    sameAs,
    applicationCategory: item.applicationCategory || '',
    offers: item.offers?.price !== undefined
      ? (item.offers.price === '0' ? 'Free plan available' : `Starting at ${item.offers.priceCurrency || ''}${item.offers.price}`)
      : '',
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Platform detection
// ─────────────────────────────────────────────────────────────────────────────

function detectPlatform(doc: Document): string {
  const generator = getMeta(doc, 'generator').toLowerCase()
  if (generator.includes('wordpress')) return 'WordPress'
  if (generator.includes('wix')) return 'Wix'
  if (generator.includes('squarespace')) return 'Squarespace'
  if (generator.includes('shopify')) return 'Shopify'
  if (generator.includes('webflow')) return 'Webflow'
  if (generator.includes('ghost')) return 'Ghost'
  if (generator.includes('joomla')) return 'Joomla'
  if (generator.includes('drupal')) return 'Drupal'

  // Framework fingerprints in DOM
  if (doc.querySelector('#__NEXT_DATA__')) return 'Next.js'
  if (doc.querySelector('#__nuxt') || doc.querySelector('#__nuxt-progress')) return 'Nuxt.js'
  if (doc.querySelector('[data-gatsby-root]') || doc.getElementById('gatsby-focus-wrapper')) return 'Gatsby'
  if (doc.querySelector('meta[name="generator"][content*="Remix"]')) return 'Remix'
  if (doc.querySelector('#svelte') || doc.querySelector('[data-sveltekit-scroll-offset]')) return 'SvelteKit'
  if (doc.querySelector('[data-reactroot]')) return 'React'
  if (doc.querySelector('[ng-version]') || doc.querySelector('[_nghost]')) return 'Angular'

  return ''
}

// ─────────────────────────────────────────────────────────────────────────────
// Social link extraction
// ─────────────────────────────────────────────────────────────────────────────

const SOCIAL_PATTERNS: [string, RegExp][] = [
  ['Twitter / X',  /(?:twitter\.com|x\.com)\/(?!intent|share|home)([A-Za-z0-9_]+)/],
  ['LinkedIn',     /linkedin\.com\/(?:company|in)\/[A-Za-z0-9_-]+/],
  ['GitHub',       /github\.com\/[A-Za-z0-9_-]+/],
  ['YouTube',      /youtube\.com\/(?:channel|user|@)[A-Za-z0-9_-]+/],
  ['Instagram',    /instagram\.com\/[A-Za-z0-9_.]+/],
  ['Facebook',     /facebook\.com\/[A-Za-z0-9_.]+/],
  ['TikTok',       /tiktok\.com\/@[A-Za-z0-9_.]+/],
  ['Discord',      /discord\.(?:gg|com\/invite)\/[A-Za-z0-9_-]+/],
]

function extractSocials(doc: Document): Record<string, string> {
  const socials: Record<string, string> = {}
  const anchors = Array.from(doc.querySelectorAll('a[href]'))

  for (const [label, pattern] of SOCIAL_PATTERNS) {
    for (const a of anchors) {
      const href = a.getAttribute('href') || ''
      if (pattern.test(href)) {
        socials[label] = href
        break
      }
    }
  }

  return socials
}

// ─────────────────────────────────────────────────────────────────────────────
// Navigation link extraction
// ─────────────────────────────────────────────────────────────────────────────

const SKIP_URL_PATTERNS = [
  /\/(login|signin|signup|register|logout|auth|admin|wp-admin|dashboard|account|cart|checkout)/i,
  /#/,
  /\.(pdf|png|jpg|gif|svg|zip|exe)$/i,
]

function extractNavLinks(doc: Document, baseHostname: string): string[] {
  const candidates = new Map<string, number>() // url → score (more central nav = higher)

  const navSelectors = [
    { sel: 'header nav a[href]', score: 10 },
    { sel: '[role="navigation"] a[href]', score: 9 },
    { sel: 'nav a[href]', score: 8 },
    { sel: 'header a[href]', score: 6 },
  ]

  for (const { sel, score } of navSelectors) {
    doc.querySelectorAll(sel).forEach(el => {
      // Always use getAttribute — .href resolves against the app's own origin in DOMParser docs,
      // which breaks the hostname check below for any site using relative links.
      const rawHref = el.getAttribute('href') || ''
      if (!rawHref || rawHref.startsWith('mailto:') || rawHref.startsWith('tel:') || rawHref.startsWith('javascript:')) return
      let fullUrl: string
      try {
        fullUrl = new URL(rawHref, `https://${baseHostname}`).href
      } catch { return }

      const urlObj = new URL(fullUrl)
      if (urlObj.hostname !== baseHostname) return
      if (SKIP_URL_PATTERNS.some(p => p.test(fullUrl))) return

      // Dedupe by pathname
      const key = urlObj.pathname.replace(/\/$/, '') || '/'
      if (key === '/') return // skip home — always included
      const canonical = `https://${baseHostname}${key}`
      candidates.set(canonical, Math.max(candidates.get(canonical) ?? 0, score))
    })
  }

  return [...candidates.entries()]
    .sort((a, b) => b[1] - a[1])
    .map(([url]) => url)
    .slice(0, 8)
}

// ─────────────────────────────────────────────────────────────────────────────
// Page classification
// ─────────────────────────────────────────────────────────────────────────────

function classifyPage(url: string): PageType {
  try {
    const path = new URL(url).pathname.toLowerCase().replace(/\/$/, '')
    if (!path || path === '') return 'home'
    if (/\/(about|company|team|who-we-are|our-story|mission|vision|us$)/.test(path)) return 'about'
    if (/\/(pricing|plans|packages|subscription|tiers)/.test(path)) return 'pricing'
    if (/\/(contact|reach|get-in-touch|hire-us|talk-to-us)/.test(path)) return 'contact'
    if (/\/(docs|documentation|help|support|guides?|manual|api|reference|kb|knowledge)/.test(path)) return 'docs'
    if (/\/(product|shop|store|item|solutions?)/.test(path)) return 'product'
    if (/\/(services?|offerings?|what-we-do|our-work|work$)/.test(path)) return 'service'
    if (/\/(features?|capabilities|how-it-works)/.test(path)) return 'features'
    if (/\/(case-stud|customer|success-stor|testimonial|portfolio)/.test(path)) return 'case-study'

    // Blog: determine if index or individual post
    const blogIdx = path.search(/\/(blog|news|articles?|posts?|journal|insights?|resources?|updates?)/)
    if (blogIdx !== -1) {
      const afterBlog = path.slice(blogIdx + 1).split('/').filter(Boolean)
      return afterBlog.length <= 1 ? 'blog-index' : 'blog-post'
    }
    return 'other'
  } catch { return 'other' }
}

// ─────────────────────────────────────────────────────────────────────────────
// Sitemap parsing
// ─────────────────────────────────────────────────────────────────────────────

interface SitemapEntry { url: string; lastmod?: string; priority?: number }

function parseSitemapXml(xml: string): { entries: SitemapEntry[]; isIndex: boolean; childSitemaps: string[] } {
  const doc = new DOMParser().parseFromString(xml, 'application/xml')
  const isIndex = doc.querySelector('sitemapindex') !== null

  if (isIndex) {
    const childSitemaps = Array.from(doc.querySelectorAll('sitemap > loc'))
      .map(el => el.textContent?.trim() ?? '')
      .filter(Boolean)
    return { entries: [], isIndex: true, childSitemaps }
  }

  const entries: SitemapEntry[] = Array.from(doc.querySelectorAll('url')).map(el => ({
    url: el.querySelector('loc')?.textContent?.trim() ?? '',
    lastmod: el.querySelector('lastmod')?.textContent?.trim(),
    priority: parseFloat(el.querySelector('priority')?.textContent ?? '0.5'),
  })).filter(e => e.url)

  return { entries, isIndex: false, childSitemaps: [] }
}

async function fetchSitemap(base: string, progress: ProgressCallback): Promise<SitemapEntry[]> {
  const candidates = ['/sitemap.xml', '/sitemap_index.xml', '/sitemap/sitemap.xml', '/wp-sitemap.xml']
  let xml = ''
  let foundPath = ''

  for (const path of candidates) {
    try {
      xml = await proxiedFetch(base + path, 7000)
      if (xml.includes('<urlset') || xml.includes('<sitemapindex')) { foundPath = path; break }
    } catch { /* try next */ }
  }

  if (!foundPath) return []

  const { entries, isIndex, childSitemaps } = parseSitemapXml(xml)
  if (!isIndex) return entries

  // Sitemap index — fetch child sitemaps (prefer posts/articles/pages sitemaps)
  progress('Reading sub-sitemaps…')
  const preferred = childSitemaps.filter(u =>
    /post|article|page|content|blog|news/.test(u.toLowerCase())
  )
  const toFetch = [...preferred, ...childSitemaps.filter(u => !preferred.includes(u))].slice(0, 3)

  const all: SitemapEntry[] = []
  for (const childUrl of toFetch) {
    try {
      const childXml = await proxiedFetch(childUrl, 7000)
      const { entries: childEntries } = parseSitemapXml(childXml)
      all.push(...childEntries)
    } catch { /* skip */ }
  }
  return all
}

// ─────────────────────────────────────────────────────────────────────────────
// Page fetcher — extracts everything useful from a single page
// ─────────────────────────────────────────────────────────────────────────────

async function fetchPage(url: string, knownType?: PageType): Promise<Page | null> {
  try {
    const html = await proxiedFetch(url, 8000)
    const doc = parseHtml(html)
    const type = knownType ?? classifyPage(url)
    const ldItems = extractJsonLd(doc)

    // Try to get article data from JSON-LD
    const article: any = findInLd(ldItems, 'Article', 'BlogPosting', 'NewsArticle', 'TechArticle')

    return {
      url,
      title: getTitle(doc),
      description: getDescription(doc),
      type,
      h1: getHeadings(doc, 'h1', 3).join(' · ') || undefined,
      h2s: getHeadings(doc, 'h2', 8),
      author: article?.author?.name || article?.author?.[0]?.name || undefined,
      datePublished: article?.datePublished?.slice(0, 10) || undefined,
      keywords: getKeywords(doc),
    }
  } catch {
    return null
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Main entry point
// ─────────────────────────────────────────────────────────────────────────────

export async function analyzeSite(rawInput: string, progress: ProgressCallback): Promise<SiteAnalysis> {
  // Normalise
  let input = rawInput.trim()
  if (!input.startsWith('http://') && !input.startsWith('https://')) input = 'https://' + input
  const baseUrl = new URL(input)
  const base = `${baseUrl.protocol}//${baseUrl.hostname}`
  const hostname = baseUrl.hostname

  // ── 1. Homepage ────────────────────────────────────────────────────────────
  progress('Fetching homepage…')
  const homepageHtml = await proxiedFetch(base, 10000)
  const homeDoc = parseHtml(homepageHtml)

  const ldItems = extractJsonLd(homeDoc)
  const org = extractOrganization(ldItems)

  const name = org?.name || getTitle(homeDoc).split(/[-|·—]/)[0].trim() || hostname
  const description = getDescription(homeDoc)
  const tagline = getMeta(homeDoc, 'og:description') || description.split('.')[0] || ''

  const homepageH1 = getHeadings(homeDoc, 'h1', 5).join(' · ')
  const homepageH2s = getHeadings(homeDoc, 'h2', 12)
  const homepageH3s = getHeadings(homeDoc, 'h3', 10)
  const keywords = [
    ...getKeywords(homeDoc),
    ...getMeta(homeDoc, 'article:tag').split(',').map(k => k.trim()).filter(Boolean),
  ]

  const socials = { ...extractSocials(homeDoc), ...(org?.sameAs || []).reduce((acc, url) => {
    for (const [label, pattern] of SOCIAL_PATTERNS) {
      if (pattern.test(url)) { acc[label] = url; break }
    }
    return acc
  }, {} as Record<string, string>) }

  const platform = detectPlatform(homeDoc)
  const language = getLanguage(homeDoc)

  // ── 2. Nav links → core pages ───────────────────────────────────────────
  progress('Mapping site structure…')
  const navUrls = extractNavLinks(homeDoc, hostname)

  // ── 3. Sitemap → blog posts + additional pages ─────────────────────────
  progress('Fetching sitemap…')
  let sitemapEntries: SitemapEntry[] = []
  let sitemapFound = false
  try {
    sitemapEntries = await fetchSitemap(base, progress)
    sitemapFound = sitemapEntries.length > 0
  } catch { /* optional */ }

  // Separate out blog posts vs other sitemap pages
  const blogEntries = sitemapEntries
    .filter(e => classifyPage(e.url) === 'blog-post')
    .sort((a, b) => {
      if (a.lastmod && b.lastmod) return b.lastmod.localeCompare(a.lastmod)
      return (b.priority ?? 0) - (a.priority ?? 0)
    })
    .slice(0, 8)

  // Also pick interesting non-blog sitemap pages not already in nav
  const navPathSet = new Set(navUrls.map(u => new URL(u).pathname.replace(/\/$/, '')))
  const extraSitemapPages = sitemapEntries
    .filter(e => {
      const t = classifyPage(e.url)
      if (t === 'blog-post' || t === 'home') return false
      const path = (() => { try { return new URL(e.url).pathname.replace(/\/$/, '') } catch { return '' } })()
      return !navPathSet.has(path)
    })
    .sort((a, b) => (b.priority ?? 0) - (a.priority ?? 0))
    .slice(0, 4)

  // ── 4. Fetch core pages ────────────────────────────────────────────────
  const allCoreUrls = [...new Set([...navUrls, ...extraSitemapPages.map(e => e.url)])]
  progress(`Analyzing ${allCoreUrls.length} key pages…`)

  const corePages: Page[] = []
  for (const url of allCoreUrls) {
    const page = await fetchPage(url, classifyPage(url))
    if (page) corePages.push(page)
  }

  // ── 5. Fetch blog posts ────────────────────────────────────────────────
  progress(`Reading ${blogEntries.length} recent blog posts…`)
  const blogPosts: Page[] = []
  // Batch in parallel (max 5 concurrent)
  const BATCH = 5
  for (let i = 0; i < blogEntries.length; i += BATCH) {
    const batch = blogEntries.slice(i, i + BATCH)
    const settled = await Promise.allSettled(batch.map(e => fetchPage(e.url, 'blog-post')))
    for (let j = 0; j < settled.length; j++) {
      const r = settled[j]
      if (r.status === 'fulfilled' && r.value) {
        // Carry lastmod from sitemap if page fetch didn't get datePublished
        const page = r.value
        if (!page.datePublished && batch[j].lastmod) page.datePublished = batch[j].lastmod?.slice(0, 10)
        blogPosts.push(page)
      }
    }
  }

  return {
    url: base,
    name,
    tagline,
    description,
    language,
    platform,
    keywords,
    homepageH1,
    homepageH2s,
    homepageH3s,
    organization: org,
    socials,
    corePages,
    blogPosts,
    sitemapFound,
    sitemapUrl: sitemapFound ? `${base}/sitemap.xml` : undefined,
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// llms.txt builder
// ─────────────────────────────────────────────────────────────────────────────

const PAGE_TYPE_SECTION: Partial<Record<PageType, string>> = {
  about: 'About',
  features: 'Features',
  pricing: 'Pricing & Plans',
  product: 'Products',
  service: 'Services',
  docs: 'Documentation',
  contact: 'Contact',
  'case-study': 'Case Studies & Customers',
  'blog-index': 'Blog',
  other: 'More',
}

export function buildLLMsTxt(a: SiteAnalysis): string {
  const lines: string[] = []

  // ── Header ──────────────────────────────────────────────────────────────
  lines.push(`# ${a.name}`)
  lines.push('')

  if (a.tagline && a.tagline !== a.description) {
    lines.push(`> ${a.tagline}`)
    lines.push('')
  }

  if (a.description) {
    lines.push(a.description)
    lines.push('')
  }

  // ── Homepage key messages (h2s as capabilities/topics) ─────────────────
  const usefulH2s = a.homepageH2s.filter(h =>
    h.length > 5 &&
    !/^(menu|navigation|cookie|footer|header|skip|search)/i.test(h) &&
    !/^[\d.]+$/.test(h)
  )
  if (usefulH2s.length > 0) {
    lines.push('## Key Highlights')
    lines.push('')
    for (const h of usefulH2s.slice(0, 8)) {
      lines.push(`- ${h}`)
    }
    lines.push('')
  }

  // ── Sub-topic signals from h3s ───────────────────────────────────────
  const usefulH3s = a.homepageH3s.filter(h =>
    h.length > 4 &&
    !/^(menu|navigation|cookie|footer)/i.test(h)
  )
  if (usefulH3s.length >= 4) {
    lines.push('## Features & Capabilities')
    lines.push('')
    for (const h of usefulH3s.slice(0, 10)) {
      lines.push(`- ${h}`)
    }
    lines.push('')
  }

  // ── Core pages grouped by type ──────────────────────────────────────────
  const grouped = new Map<string, Page[]>()
  for (const page of a.corePages) {
    const section = PAGE_TYPE_SECTION[page.type] ?? 'Pages'
    if (!grouped.has(section)) grouped.set(section, [])
    grouped.get(section)!.push(page)
  }

  if (grouped.size > 0) {
    lines.push('## Pages')
    lines.push('')

    // Core first, then rest alphabetically
    const sectionOrder = ['About', 'Features', 'Products', 'Services', 'Pricing & Plans', 'Documentation', 'Case Studies & Customers', 'Blog', 'Contact', 'More', 'Pages']
    const sortedSections = [...grouped.keys()].sort(
      (a, b) => (sectionOrder.indexOf(a) + 1 || 99) - (sectionOrder.indexOf(b) + 1 || 99)
    )

    for (const section of sortedSections) {
      const pages = grouped.get(section)!
      lines.push(`### ${section}`)
      lines.push('')
      for (const p of pages) {
        const desc = p.description || p.h1 || ''
        // Add h2 insight if description is thin
        const extra = (!desc || desc.length < 40) && p.h2s.length > 0
          ? ` Covers: ${p.h2s.slice(0, 3).join(', ')}.`
          : ''
        lines.push(`- [${p.title || p.url}](${p.url})${desc ? `: ${desc}${extra}` : extra ? `: ${extra}` : ''}`)
      }
      lines.push('')
    }
  }

  // ── Blog posts ──────────────────────────────────────────────────────────
  if (a.blogPosts.length > 0) {
    lines.push('## Blog & Articles')
    lines.push('')
    for (const post of a.blogPosts) {
      const meta: string[] = []
      if (post.author) meta.push(`by ${post.author}`)
      if (post.datePublished) meta.push(post.datePublished)
      const suffix = meta.length > 0 ? ` — ${meta.join(' · ')}` : ''
      const desc = post.description ? `: ${post.description}` : ''
      lines.push(`- [${post.title || post.url}](${post.url})${desc}${suffix}`)
    }
    lines.push('')
  }

  // ── Topics & Keywords ────────────────────────────────────────────────────
  const allTopics = [
    ...a.keywords,
    ...a.corePages.flatMap(p => p.keywords ?? []),
    ...a.blogPosts.flatMap(p => p.keywords ?? []),
  ]
  const uniqueTopics = [...new Set(allTopics.map(t => t.toLowerCase()))].filter(t => t.length > 2)
  if (uniqueTopics.length > 0) {
    lines.push('## Topics & Keywords')
    lines.push('')
    lines.push(uniqueTopics.join(', '))
    lines.push('')
  }

  // ── Organization ────────────────────────────────────────────────────────
  const org = a.organization
  if (org && (org.orgType || org.description || org.foundingDate || org.location)) {
    lines.push('## Organization')
    lines.push('')
    if (org.orgType) lines.push(`- **Type**: ${org.orgType}`)
    if (org.description && org.description !== a.description) lines.push(`- **About**: ${org.description}`)
    if (org.foundingDate) lines.push(`- **Founded**: ${org.foundingDate}`)
    if (org.location) lines.push(`- **Location**: ${org.location}`)
    if (org.applicationCategory) lines.push(`- **Category**: ${org.applicationCategory}`)
    if (org.offers) lines.push(`- **Pricing**: ${org.offers}`)
    if (a.url) lines.push(`- **Website**: ${a.url}`)
    lines.push('')
  }

  // ── Social & Connect ─────────────────────────────────────────────────────
  if (Object.keys(a.socials).length > 0) {
    lines.push('## Connect')
    lines.push('')
    for (const [platform, url] of Object.entries(a.socials)) {
      lines.push(`- [${platform}](${url})`)
    }
    lines.push('')
  }

  // ── Technical ────────────────────────────────────────────────────────────
  const techLines: string[] = []
  if (a.language) techLines.push(`- **Language**: ${a.language}`)
  if (a.platform) techLines.push(`- **Built with**: ${a.platform}`)
  if (a.sitemapFound) techLines.push(`- **Sitemap**: ${a.url}/sitemap.xml`)

  if (techLines.length > 0) {
    lines.push('## Technical')
    lines.push('')
    lines.push(...techLines)
    lines.push('')
  }

  return lines.join('\n')
}
