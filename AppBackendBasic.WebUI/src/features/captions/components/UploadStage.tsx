import { useRef, useState } from 'react'
import { FilmStrip, UploadSimple, Waveform } from '@phosphor-icons/react'
import { toast } from '@/utils/toast'
import { CtButton } from './ui'
import { captionsApi, errorMessage, httpStatus } from '../captionsApi'
import { useCaptionCatalog } from '../catalogContext'
import type { CaptionProject } from '../types'

interface Props {
  onCreated: (project: CaptionProject) => void
  onPaywall: () => void
}

export const UploadStage = ({ onCreated, onPaywall }: Props) => {
  const { catalog } = useCaptionCatalog()
  const { limits } = catalog
  const input = useRef<HTMLInputElement>(null)
  const [file, setFile] = useState<File | null>(null)
  const [duration, setDuration] = useState<number | null>(null)
  const [probing, setProbing] = useState(false)
  const [processing, setProcessing] = useState(false)
  const [progress, setProgress] = useState(0)
  const [processingLabel, setProcessingLabel] = useState('')
  const [speechLanguage, setSpeechLanguage] = useState<'auto' | 'hi'>('auto')

  const extensionOk = (name: string) => limits.allowed_extensions.some((ext) => name.toLowerCase().endsWith(ext))
  const formatList = limits.allowed_extensions.map((e) => e.replace('.', '').toUpperCase()).join(' / ')

  const inspect = (selected?: File) => {
    if (!selected) return
    if (!extensionOk(selected.name)) { toast.error(`Choose an ${formatList.replace(' / ', ' or ')} video`); return }
    setFile(selected)
    setDuration(null)
    setProbing(true)
    const video = document.createElement('video')
    const finishProbe = () => setProbing(false)
    const probeTimer = window.setTimeout(finishProbe, 2500)
    video.preload = 'metadata'
    video.onloadedmetadata = () => {
      URL.revokeObjectURL(video.src)
      window.clearTimeout(probeTimer)
      finishProbe()
      if (video.duration > limits.absolute_max_seconds) { toast.error(`Video must be ${limits.absolute_max_seconds} seconds or shorter`); return }
      setDuration(video.duration)
    }
    video.onerror = () => { URL.revokeObjectURL(video.src); window.clearTimeout(probeTimer); finishProbe() }
    video.src = URL.createObjectURL(selected)
  }

  const generate = async () => {
    if (!file) return
    setProcessing(true)
    setProgress(2)
    setProcessingLabel('Uploading video')
    try {
      let project = await captionsApi.process(file, catalog.default_theme, speechLanguage)
      while (['queued', 'processing'].includes(project.status)) {
        setProgress(project.progress || 5)
        setProcessingLabel(project.processing_label || 'Generating captions')
        await new Promise((resolve) => window.setTimeout(resolve, 2500))
        project = await captionsApi.get(project.project_id)
      }
      if (project.status === 'failed') throw new Error(project.error_message || 'Caption processing failed')
      onCreated(project)
      toast.success('Hinglish captions are ready')
    } catch (error) {
      if (httpStatus(error) === 402) onPaywall()
      else toast.error(errorMessage(error))
    } finally {
      setProcessing(false)
      setProgress(0)
      setProcessingLabel('')
    }
  }

  return (
    <main className="upload-workspace" data-testid="video-upload-workspace">
      <section className="upload-copy">
        <p className="eyebrow" data-testid="workspace-status-label"><Waveform size={17} /> NEW CAPTION SESSION</p>
        <h1 data-testid="upload-page-heading">Drop a video.<br /><span>Captain the words.</span></h1>
        <p data-testid="upload-page-description">Whisper listens to the video first for word timing and Sarvam AI recognises the Hindi/Hinglish text. You’ll see exactly what was heard before AI converts it into Roman Hinglish and styles the key moments.</p>
        <div className="language-selector" data-testid="speech-language-selector">
          <span data-testid="speech-language-label">SPEECH MODE</span>
          <button className={speechLanguage === 'auto' ? 'active' : ''} onClick={() => setSpeechLanguage('auto')} data-testid="speech-language-auto-button">Adaptive Hinglish · Recommended</button>
          <button className={speechLanguage === 'hi' ? 'active' : ''} onClick={() => setSpeechLanguage('hi')} data-testid="speech-language-hinglish-button">Hindi only</button>
        </div>
        <div className="format-strip" data-testid="supported-video-formats">
          <span>{formatList}</span>
          <span>{limits.unlimited_testing ? 'UNLIMITED TESTING · ' : 'UP TO '}{limits.absolute_max_seconds} SEC</span>
          <span>RAW TRANSCRIPT VISIBLE</span>
        </div>
      </section>
      <section className={`upload-zone ${file ? 'has-file' : ''}`} onDragOver={(e) => e.preventDefault()} onDrop={(e) => { e.preventDefault(); inspect(e.dataTransfer.files[0]) }} data-testid="video-upload-dropzone">
        <input ref={input} type="file" accept="video/mp4,video/quicktime" onChange={(e) => inspect(e.target.files?.[0])} hidden data-testid="video-file-input" />
        <div className="upload-icon"><FilmStrip size={42} weight="duotone" /></div>
        {file ? <>
          <h2 data-testid="selected-video-name">{file.name}</h2>
          <p data-testid="selected-video-metadata">{duration ? `${duration.toFixed(1)} sec · ` : probing ? 'Checking duration · ' : 'Duration checked on upload · '}{file.size < 1048576 ? `${Math.max(1, Math.round(file.size / 1024))} KB` : `${(file.size / 1048576).toFixed(1)} MB`}</p>
          <div className="upload-buttons">
            <CtButton variant="outline" disabled={processing} onClick={() => input.current?.click()} data-testid="replace-video-button">Replace</CtButton>
            <CtButton onClick={generate} disabled={processing || probing} data-testid="generate-captions-button">{processing ? 'Transcribing & styling…' : probing ? 'Checking video…' : 'Generate Hinglish captions'}</CtButton>
          </div>
          {processing && <div className="processing-progress" data-testid="caption-processing-progress">
            <div><span data-testid="caption-processing-label">{processingLabel}</span><b data-testid="caption-processing-percent">{progress}%</b></div>
            <i><em style={{ width: `${progress}%` }} /></i>
            <p data-testid="caption-processing-note">You can keep this tab open—long videos continue safely in the background.</p>
          </div>}
        </> : <>
          <h2 data-testid="upload-dropzone-heading">Drag your video here</h2>
          <p data-testid="upload-dropzone-description">or choose a file from your device</p>
          <CtButton onClick={() => input.current?.click()} data-testid="choose-video-button"><UploadSimple size={19} /> Choose video</CtButton>
        </>}
      </section>
    </main>
  )
}
