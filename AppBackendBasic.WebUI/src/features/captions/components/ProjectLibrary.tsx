import { useCallback, useEffect, useState } from 'react'
import { FilmStrip, Trash } from '@phosphor-icons/react'
import { toast } from '@/utils/toast'
import { absoluteApiUrl, captionsApi, errorMessage } from '../captionsApi'
import { timeAgo } from '../lib/timeAgo'
import type { CaptionProjectSummary } from '../types'

const POLL_MS = 5000
const isActive = (status: string) => ['queued', 'processing', 'rendering'].includes(status)

function statusLabel(project: CaptionProjectSummary) {
  if (project.status === 'failed') return { text: 'Failed', className: 'failed' }
  if (project.status === 'rendering') return { text: 'Exporting', className: '' }
  if (isActive(project.status)) return { text: `${project.progress}%`, className: '' }
  if (project.export_current) return { text: 'Exported', className: 'exported' }
  return null
}

function formatDuration(seconds: number) {
  const whole = Math.round(seconds)
  return `${Math.floor(whole / 60)}:${String(whole % 60).padStart(2, '0')}`
}

const ProjectCard = ({ project, onOpen, onDeleted }: { project: CaptionProjectSummary; onOpen: (id: number) => void; onDeleted: (id: number) => void }) => {
  const [thumbnailFailed, setThumbnailFailed] = useState(false)
  const [confirming, setConfirming] = useState(false)
  const [deleting, setDeleting] = useState(false)
  const status = statusLabel(project)

  const remove = async () => {
    setDeleting(true)
    try {
      await captionsApi.remove(project.project_id)
      onDeleted(project.project_id)
    } catch (error) {
      toast.error(errorMessage(error))
      setDeleting(false)
      setConfirming(false)
    }
  }

  return (
    <article className="project-card" data-testid={`project-card-${project.project_id}`}>
      <button className="project-card-open" onClick={() => onOpen(project.project_id)} aria-label={`Open ${project.name}`} data-testid={`project-open-${project.project_id}`}>
        <div className="project-card-thumb">
          {project.thumbnail_url && !thumbnailFailed
            ? <img src={absoluteApiUrl(project.thumbnail_url)} alt="" loading="lazy" onError={() => setThumbnailFailed(true)} />
            : <FilmStrip size={34} weight="duotone" />}
        </div>
        <div className="project-card-body">
          <b title={project.name}>{project.name}</b>
          <span>{formatDuration(project.duration)} · Edited {timeAgo(project.last_edited_at)}</span>
        </div>
      </button>
      {status && <span className={`project-card-status ${status.className}`} data-testid={`project-status-${project.project_id}`}>{status.text}</span>}
      {!isActive(project.status) && <div className={`project-card-actions ${confirming ? 'confirming' : ''}`}>
        {confirming
          ? <>
              <button className="danger" disabled={deleting} onClick={remove} data-testid={`project-delete-confirm-${project.project_id}`}>{deleting ? 'Deleting…' : 'Delete'}</button>
              <button disabled={deleting} onClick={() => setConfirming(false)}>Keep</button>
            </>
          : <button aria-label={`Delete ${project.name}`} title="Delete project" onClick={() => setConfirming(true)} data-testid={`project-delete-${project.project_id}`}><Trash size={15} /></button>}
      </div>}
    </article>
  )
}

/** "Continue where you left off": renders nothing until the user has a project. */
export const ProjectLibrary = ({ onOpen }: { onOpen: (id: number) => void }) => {
  const [projects, setProjects] = useState<CaptionProjectSummary[]>([])
  const [nextCursor, setNextCursor] = useState<string | null>(null)
  const [loadingMore, setLoadingMore] = useState(false)

  useEffect(() => {
    let cancelled = false
    captionsApi.list()
      .then((page) => { if (!cancelled) { setProjects(page.items); setNextCursor(page.next_cursor) } })
      .catch((error) => { if (!cancelled) toast.error(errorMessage(error)) })
    return () => { cancelled = true }
  }, [])

  const loadMore = async () => {
    if (!nextCursor) return
    setLoadingMore(true)
    try {
      const page = await captionsApi.list(nextCursor)
      setProjects((current) => [...current, ...page.items.filter((item) => !current.some((known) => known.project_id === item.project_id))])
      setNextCursor(page.next_cursor)
    } catch (error) { toast.error(errorMessage(error)) }
    finally { setLoadingMore(false) }
  }

  // While a job runs, keep its card live by refreshing the first page and merging it in.
  const hasActive = projects.some((project) => isActive(project.status))
  useEffect(() => {
    if (!hasActive) return undefined
    const timer = window.setInterval(() => {
      captionsApi.list().then((page) => setProjects((current) => {
        const fresh = new Map(page.items.map((item) => [item.project_id, item]))
        return current.map((project) => fresh.get(project.project_id) ?? project)
      })).catch(() => undefined)
    }, POLL_MS)
    return () => window.clearInterval(timer)
  }, [hasActive])

  const onDeleted = useCallback((id: number) => {
    setProjects((current) => current.filter((project) => project.project_id !== id))
    toast.success('Project deleted')
  }, [])

  if (projects.length === 0) return null
  return (
    <section className="project-library" data-testid="project-library">
      <div className="project-library-head"><h2>Continue where you left off</h2></div>
      <div className="project-grid">
        {projects.map((project) => <ProjectCard key={project.project_id} project={project} onOpen={onOpen} onDeleted={onDeleted} />)}
      </div>
      {nextCursor && <button className="project-library-more" disabled={loadingMore} onClick={loadMore} data-testid="project-library-more">{loadingMore ? 'Loading…' : 'Show more'}</button>}
    </section>
  )
}
