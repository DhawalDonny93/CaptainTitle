import { useCallback, useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '@/contexts/AuthContext'
import { CaptionCatalogProvider } from '../CaptionCatalogContext'
import { captionsApi } from '../captionsApi'
import { EditorHeader } from '../components/EditorHeader'
import { UploadStage } from '../components/UploadStage'
import { CaptionStudio } from '../components/CaptionStudio'
import { PaywallDialog } from '../components/PaywallDialog'
import type { CaptionProject, CaptionUsage } from '../types'
import '../studio.css'

function StudioShell() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const [usage, setUsage] = useState<CaptionUsage | null>(null)
  const [project, setProject] = useState<CaptionProject | null>(null)
  const [pricing, setPricing] = useState(false)

  const loadUsage = useCallback(() => { captionsApi.usage().then(setUsage).catch(() => setUsage(null)) }, [])
  useEffect(() => { loadUsage() }, [loadUsage])

  const created = (next: CaptionProject) => { setProject(next); loadUsage() }
  const signOut = async () => { await logout(); navigate('/login') }
  const displayName = [user?.firstName, user?.lastName].filter(Boolean).join(' ') || user?.email || 'Creator'

  return (
    <div className="editor-app ct-scope" data-testid="captain-title-editor-app">
      <EditorHeader userName={displayName} userPicture={user?.profilePhotoUrl} usage={usage} onPricing={() => setPricing(true)} onLogout={signOut} />
      {project
        ? <CaptionStudio key={project.project_id} initialProject={project} onNew={() => setProject(null)} />
        : <UploadStage onCreated={created} onPaywall={() => setPricing(true)} />}
      <PaywallDialog open={pricing} onOpenChange={setPricing} usage={usage} onPaid={loadUsage} />
    </div>
  )
}

/** Captain Title caption studio (route /studio). Replaces the old app's EditorPage. */
export default function StudioPage() {
  return <CaptionCatalogProvider><StudioShell /></CaptionCatalogProvider>
}
