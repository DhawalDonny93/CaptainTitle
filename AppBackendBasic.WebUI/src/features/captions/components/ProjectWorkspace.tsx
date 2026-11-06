import { useCallback, useEffect, useState } from 'react'
import { toast } from '@/utils/toast'
import { CtButton } from './ui'
import { CaptionStudio } from './CaptionStudio'
import { captionsApi, errorMessage, httpStatus } from '../captionsApi'
import type { CaptionProject } from '../types'

const POLL_MS = 2500
const isGenerating = (project: CaptionProject) => project.status === 'queued' || project.status === 'processing'

interface Props {
  projectId: number
  onExit: () => void
  onUsageChanged: () => void
  onPaywall: () => void
}

/** The page of one saved project (/studio/:id): whatever state it's in, this is where the user lands when they come back to it. */
export const ProjectWorkspace = ({ projectId, onExit, onUsageChanged, onPaywall }: Props) => {
  const [project, setProject] = useState<CaptionProject | null>(null)
  // Bumped when the server's copy replaces the content, so the editor re-reads everything from it.
  const [generation, setGeneration] = useState(0)
  const [pending, setPending] = useState(false)
  const [latestVersionId, setLatestVersionId] = useState<number | null>(null)

  const replace = useCallback((next: CaptionProject) => {
    setProject(next)
    setGeneration((current) => current + 1)
  }, [])

  useEffect(() => {
    let cancelled = false
    captionsApi.get(projectId).then((loaded) => { if (!cancelled) setProject(loaded) }).catch((error) => {
      if (cancelled) return
      toast.error(httpStatus(error) === 404 ? 'That project no longer exists' : errorMessage(error))
      onExit()
    })
    return () => { cancelled = true }
  }, [projectId, onExit])

  // Follow the transcription job. A failed poll (e.g. a network blip) just waits for the next one: the job doesn't depend on this page.
  const generating = project != null && isGenerating(project)
  useEffect(() => {
    if (!generating) return undefined
    let cancelled = false
    let timer: number
    const poll = async () => {
      try {
        const next = await captionsApi.get(projectId)
        if (cancelled) return
        setProject(next)
        if (!isGenerating(next)) {
          onUsageChanged()
          if (next.status !== 'failed') toast.success('Hinglish captions are ready')
          return
        }
      } catch { /* retried below */ }
      if (!cancelled) timer = window.setTimeout(poll, POLL_MS)
    }
    timer = window.setTimeout(poll, POLL_MS)
    return () => { cancelled = true; window.clearTimeout(timer) }
  }, [generating, projectId, onUsageChanged])

  const failed = project?.status === 'failed'
  useEffect(() => {
    if (!failed) return undefined
    let cancelled = false
    captionsApi.versions(projectId).then((versions) => { if (!cancelled) setLatestVersionId(versions[0]?.version_id ?? null) }).catch(() => undefined)
    return () => { cancelled = true }
  }, [failed, projectId])

  const run = async (action: () => Promise<void>) => {
    setPending(true)
    try { await action() }
    catch (error) {
      if (httpStatus(error) === 402) onPaywall()
      else toast.error(errorMessage(error))
    }
    finally { setPending(false) }
  }
  const retry = () => run(async () => { setProject(await captionsApi.retranscribe(projectId, 'auto')) })
  const restore = () => run(async () => {
    if (latestVersionId == null) return
    replace(await captionsApi.restoreVersion(projectId, latestVersionId))
    toast.success('Your previous edits are back')
  })
  const remove = () => run(async () => { await captionsApi.remove(projectId); toast.success('Project deleted'); onExit() })

  if (!project) return <main className="project-stage" data-testid="project-loading"><p>Opening your project…</p></main>

  if (generating) {
    return (
      <main className="project-stage" data-testid="project-processing">
        <section>
          <h1 data-testid="project-processing-name">{project.name}</h1>
          <div className="processing-progress" data-testid="caption-processing-progress">
            <div><span data-testid="caption-processing-label">{project.processing_label || 'Generating captions'}</span><b data-testid="caption-processing-percent">{project.progress}%</b></div>
            <i><em style={{ width: `${project.progress}%` }} /></i>
            <p data-testid="caption-processing-note">This keeps running on our servers. You can close this tab and come back to it from your projects.</p>
          </div>
          <div className="upload-buttons"><CtButton variant="outline" onClick={onExit} data-testid="project-processing-back-button">Back to projects</CtButton></div>
        </section>
      </main>
    )
  }

  if (failed) {
    return (
      <main className="project-stage" data-testid="project-failed">
        <section>
          <h1>{project.name}</h1>
          <p data-testid="project-failed-message">{project.error_message || 'Caption processing could not finish. Please try again, and contact the admin if it keeps happening.'}</p>
          <div className="upload-buttons">
            <CtButton disabled={pending} onClick={retry} data-testid="project-retry-button">Try again</CtButton>
            {latestVersionId != null && <CtButton variant="outline" disabled={pending} onClick={restore} data-testid="project-restore-button">Restore my previous edits</CtButton>}
            <CtButton variant="outline" disabled={pending} onClick={remove} data-testid="project-delete-button">Delete</CtButton>
            <CtButton variant="ghost" onClick={onExit} data-testid="project-failed-back-button">Back to projects</CtButton>
          </div>
        </section>
      </main>
    )
  }

  return <CaptionStudio key={generation} initialProject={project} onNew={onExit} onReplaced={replace} />
}
