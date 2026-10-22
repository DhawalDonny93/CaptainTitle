import { api } from '@/lib/api'
import type {
  CaptionCatalog, CaptionPlan, CaptionProject, CaptionUsage, CaptionWord, ProjectEditPayload,
} from './types'

const base = '/captions'

/** Extracts a user-facing message from an axios error (backend returns { message }). */
export function errorMessage(error: unknown): string {
  const e = error as { response?: { data?: { message?: string; detail?: string; title?: string } }; message?: string }
  return e?.response?.data?.message || e?.response?.data?.detail || e?.response?.data?.title || e?.message || 'Something went wrong'
}

export function httpStatus(error: unknown): number | undefined {
  return (error as { response?: { status?: number } })?.response?.status
}

export const captionsApi = {
  catalog: () => api.get<CaptionCatalog>(`${base}/catalog`).then((r) => r.data),
  usage: () => api.get<CaptionUsage>(`${base}/usage`).then((r) => r.data),
  process: (file: File, theme: string, speechLanguage: string) => {
    const form = new FormData()
    form.append('file', file)
    form.append('theme', theme)
    form.append('speech_language', speechLanguage)
    // Let the browser set the multipart boundary; the shared client defaults to JSON.
    return api.post<CaptionProject>(`${base}/projects/process`, form, { headers: { 'Content-Type': undefined as unknown as string }, timeout: 0 }).then((r) => r.data)
  },
  get: (id: number) => api.get<CaptionProject>(`${base}/projects/${id}`).then((r) => r.data),
  save: (id: number, payload: ProjectEditPayload) => api.put(`${base}/projects/${id}`, payload).then((r) => r.data),
  retranscribe: (id: number, speechLanguage: string) =>
    api.post<CaptionProject>(`${base}/projects/${id}/retranscribe`, { speech_language: speechLanguage }).then((r) => r.data),
  reassignEmphasis: (id: number) => api.post<{ words: CaptionWord[] }>(`${base}/projects/${id}/ai/emphasis`).then((r) => r.data),
  render: (id: number) => api.post<{ status: string; download_url: string }>(`${base}/projects/${id}/render`, undefined, { timeout: 0 }).then((r) => r.data),
  mediaUrl: (id: number) => api.post<{ url: string }>(`${base}/projects/${id}/media-url`).then((r) => r.data.url),
  downloadBlob: (id: number) => api.get<Blob>(`${base}/projects/${id}/download`, { responseType: 'blob' }).then((r) => r.data),
  srtBlob: (id: number) => api.get<Blob>(`${base}/projects/${id}/srt`, { responseType: 'blob' }).then((r) => r.data),
  paymentCatalog: () => api.get<{ items: CaptionPlan[]; available: boolean }>(`${base}/payments/catalog`).then((r) => r.data),
  checkout: (packageId: string) =>
    api.post<{ checkout_url: string; session_id: string }>(`${base}/payments/checkout`, { package_id: packageId, origin_url: window.location.origin }).then((r) => r.data),
  paymentStatus: (sessionId: string) =>
    api.get<{ session_id: string; status: string; payment_status: string }>(`${base}/payments/status/${encodeURIComponent(sessionId)}`).then((r) => r.data),
}

/** Absolute URL for a backend-relative media path (the media endpoint is token-authenticated, no bearer header). */
export function absoluteApiUrl(relative: string): string {
  const baseUrl = (api.defaults.baseURL || '').replace(/\/api\/?$/, '')
  return `${baseUrl}${relative}`
}

export function saveBlob(blob: Blob, filename: string) {
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = filename
  anchor.click()
  URL.revokeObjectURL(url)
}
