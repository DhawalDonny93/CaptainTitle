import { api } from '@/lib/api'
import type {
  CaptionCatalog, CaptionProject, CaptionProjectList, CaptionProjectVersion, CaptionUsage, CaptionWord, CheckoutResponse, EditorState, PlanCatalog, ProjectEditPayload, SaveResult,
} from './types'

const base = '/captions'

/** Extracts a user-facing message from an axios error (backend returns { message }). */
export function errorMessage(error: unknown): string {
  const e = error as { response?: { data?: { message?: string; detail?: string; title?: string; errors?: unknown } }; message?: string }
  // ASP.NET model validation: its title ("One or more validation errors occurred") tells the user nothing.
  if (e?.response?.data?.errors) return 'Some captions have invalid content, for example an empty word'
  return e?.response?.data?.message || e?.response?.data?.detail || e?.response?.data?.title || e?.message || 'Something went wrong'
}

export function httpStatus(error: unknown): number | undefined {
  return (error as { response?: { status?: number } })?.response?.status
}

/** Machine-readable code from the backend's error body, e.g. REVISION_CONFLICT. */
export function errorCode(error: unknown): string | undefined {
  return (error as { response?: { data?: { code?: string } } })?.response?.data?.code
}

export const captionsApi = {
  catalog: () => api.get<CaptionCatalog>(`${base}/catalog`).then((r) => r.data),
  usage: () => api.get<CaptionUsage>(`${base}/usage`).then((r) => r.data),
  process: (file: File, theme: string, speechLanguage: string, onUploadPercent?: (percent: number) => void) => {
    const form = new FormData()
    form.append('file', file)
    form.append('theme', theme)
    form.append('speech_language', speechLanguage)
    // Let the browser set the multipart boundary; the shared client defaults to JSON.
    return api.post<CaptionProject>(`${base}/projects/process`, form, {
      headers: { 'Content-Type': undefined as unknown as string },
      timeout: 0,
      onUploadProgress: (e) => { if (e.total) onUploadPercent?.(Math.round((e.loaded / e.total) * 100)) },
    }).then((r) => r.data)
  },
  list: (cursor?: string | null, limit = 12) =>
    api.get<CaptionProjectList>(`${base}/projects`, { params: { limit, ...(cursor ? { cursor } : {}) } }).then((r) => r.data),
  get: (id: number) => api.get<CaptionProject>(`${base}/projects/${id}`).then((r) => r.data),
  save: (id: number, payload: ProjectEditPayload, baseRevision: number) =>
    api.put<SaveResult>(`${base}/projects/${id}`, { ...payload, base_revision: baseRevision }).then((r) => r.data),
  remove: (id: number) => api.delete(`${base}/projects/${id}`).then(() => undefined),
  saveEditorState: (id: number, state: EditorState) => api.put(`${base}/projects/${id}/editor-state`, state).then(() => undefined),
  /** For a page that is going away: keepalive lets the request outlive the tab, which axios can't do. Best effort by nature. */
  saveEditorStateOnExit: (id: number, state: EditorState) => {
    const token = localStorage.getItem('accessToken')
    void fetch(`${api.defaults.baseURL}${base}/projects/${id}/editor-state`, {
      method: 'PUT',
      keepalive: true,
      credentials: 'include',
      headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}) },
      body: JSON.stringify(state),
    }).catch(() => undefined)
  },
  versions: (id: number) => api.get<CaptionProjectVersion[]>(`${base}/projects/${id}/versions`).then((r) => r.data),
  restoreVersion: (id: number, versionId: number) =>
    api.post<CaptionProject>(`${base}/projects/${id}/versions/${versionId}/restore`).then((r) => r.data),
  retranscribe: (id: number, speechLanguage: string) =>
    api.post<CaptionProject>(`${base}/projects/${id}/retranscribe`, { speech_language: speechLanguage }).then((r) => r.data),
  reassignEmphasis: (id: number) => api.post<{ words: CaptionWord[]; revision: number }>(`${base}/projects/${id}/ai/emphasis`).then((r) => r.data),
  /** Queues the export (HTTP 202). Poll get() until status is 'rendered' (or 'ready' with error_message). */
  render: (id: number) => api.post<CaptionProject>(`${base}/projects/${id}/render`).then((r) => r.data),
  mediaUrl: (id: number) => api.post<{ url: string }>(`${base}/projects/${id}/media-url`).then((r) => r.data.url),
  downloadBlob: (id: number) => api.get<Blob>(`${base}/projects/${id}/download`, { responseType: 'blob' }).then((r) => r.data),
  srtBlob: (id: number) => api.get<Blob>(`${base}/projects/${id}/srt`, { responseType: 'blob' }).then((r) => r.data),
  paymentCatalog: () => api.get<PlanCatalog>(`${base}/payments/catalog`).then((r) => r.data),
  checkout: (packageId: string) =>
    api.post<CheckoutResponse>(`${base}/payments/checkout`, { package_id: packageId, origin_url: window.location.origin }).then((r) => r.data),
  verifyRazorpay: (payload: { razorpay_order_id?: string; razorpay_subscription_id?: string; razorpay_payment_id: string; razorpay_signature: string }) =>
    api.post<{ session_id: string; status: string; payment_status: string }>(`${base}/payments/razorpay/verify`, payload).then((r) => r.data),
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
