// Free CORS proxies tried in order — falls back on error/bad status.

interface ProxyDef {
  build: (url: string) => string
  format: 'text' | 'allorigins-json'
}

const PROXIES: ProxyDef[] = [
  {
    // corsproxy.io — most reliable, returns raw body
    build: (url) => `https://corsproxy.io/?${encodeURIComponent(url)}`,
    format: 'text',
  },
  {
    // allorigins JSON — gives us the upstream status code so we can detect failures
    build: (url) => `https://api.allorigins.win/get?url=${encodeURIComponent(url)}`,
    format: 'allorigins-json',
  },
  {
    // codetabs fallback
    build: (url) => `https://api.codetabs.com/v1/proxy?quest=${encodeURIComponent(url)}`,
    format: 'text',
  },
]

function makeController(timeoutMs: number): { signal: AbortSignal; clear: () => void } {
  const controller = new AbortController()
  const timer = setTimeout(() => controller.abort(), timeoutMs)
  return { signal: controller.signal, clear: () => clearTimeout(timer) }
}

export async function proxiedFetch(url: string, timeoutMs = 12000): Promise<string> {
  let lastError = ''

  for (const proxy of PROXIES) {
    const { signal, clear } = makeController(timeoutMs)
    try {
      const res = await fetch(proxy.build(url), { signal })
      clear()

      if (!res.ok) {
        lastError = `Proxy returned ${res.status}`
        continue
      }

      if (proxy.format === 'allorigins-json') {
        const data = await res.json()
        const code = data?.status?.http_code ?? 0
        if (code >= 200 && code < 300 && data?.contents?.trim()) {
          return data.contents as string
        }
        lastError = `Upstream returned ${code}`
        continue
      }

      const text = await res.text()
      if (text.trim().length > 50) return text
      lastError = 'Empty response from proxy'
    } catch (err: any) {
      clear()
      lastError = err?.name === 'AbortError' ? 'Timed out' : String(err?.message ?? err)
    }
  }

  throw new Error(
    `Could not reach ${new URL(url).hostname}. Make sure the domain is publicly accessible. (${lastError})`
  )
}
