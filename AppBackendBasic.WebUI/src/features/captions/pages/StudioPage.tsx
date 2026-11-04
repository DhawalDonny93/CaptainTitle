import { useCallback, useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useAuth } from '@/contexts/AuthContext'
import { CaptionCatalogProvider } from '../CaptionCatalogContext'
import { captionsApi } from '../captionsApi'
import { EditorHeader } from '../components/EditorHeader'
import { UploadStage } from '../components/UploadStage'
import { ProjectLibrary } from '../components/ProjectLibrary'
import { ProjectWorkspace } from '../components/ProjectWorkspace'
import { PaywallDialog } from '../components/PaywallDialog'
import type { CaptionProject, CaptionUsage } from '../types'
import '../studio.css'

function StudioShell() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const [usage, setUsage] = useState<CaptionUsage | null>(null)
  const { projectId } = useParams()
  const openProjectId = projectId && /^\d+$/.test(projectId) ? Number(projectId) : null
  const [pricing, setPricing] = useState(false)

  const loadUsage = useCallback(() => { captionsApi.usage().then(setUsage).catch(() => setUsage(null)) }, [])
  useEffect(() => { loadUsage() }, [loadUsage])

  const openProject = useCallback((id: number) => navigate(`/studio/${id}`), [navigate])
  const showHome = useCallback(() => navigate('/studio'), [navigate])
  const created = (next: CaptionProject) => { openProject(next.project_id); loadUsage() }
  const signOut = async () => { await logout(); navigate('/login') }
  const displayName = [user?.firstName, user?.lastName].filter(Boolean).join(' ') || user?.email || 'Creator'

  return (
    <div className="editor-app ct-scope" data-testid="captain-title-editor-app">
      <EditorHeader userName={displayName} userPicture={user?.profilePhotoUrl} usage={usage} onPricing={() => setPricing(true)} onLogout={signOut} />
      {openProjectId != null
        ? <ProjectWorkspace key={openProjectId} projectId={openProjectId} onExit={showHome} onUsageChanged={loadUsage} onPaywall={() => setPricing(true)} />
        : <div className="studio-home" data-testid="studio-home">
            <ProjectLibrary onOpen={openProject} />
            <UploadStage onCreated={created} onPaywall={() => setPricing(true)} />
          </div>}
      <PaywallDialog open={pricing} onOpenChange={setPricing} usage={usage} onPaid={loadUsage} />
    </div>
  )
}

/** Captain Title caption studio (route /studio). Replaces the old app's EditorPage. */
export default function StudioPage() {
  return <CaptionCatalogProvider><StudioShell /></CaptionCatalogProvider>
}
