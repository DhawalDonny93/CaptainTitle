import { useEffect, useState } from 'react'
import { CheckCircle, XCircle } from '@phosphor-icons/react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { CtButton } from './ui'
import { captionsApi } from '../captionsApi'

export const PaymentResult = ({ success = false }: { success?: boolean }) => {
  const [params] = useSearchParams()
  const navigate = useNavigate()
  const [status, setStatus] = useState<'confirming' | 'paid' | 'cancelled' | 'missing' | 'error'>(success ? 'confirming' : 'cancelled')
  useEffect(() => {
    if (!success) return undefined
    const session = params.get('session_id')
    if (!session) { setStatus('missing'); return undefined }
    const timer = window.setInterval(() => {
      captionsApi.paymentStatus(session).then((data) => {
        if (data.payment_status === 'paid') { setStatus('paid'); window.clearInterval(timer) }
      }).catch(() => { setStatus('error'); window.clearInterval(timer) })
    }, 2000)
    return () => window.clearInterval(timer)
  }, [params, success])
  return <main className="payment-result ct-scope" data-testid="payment-result-page">
    {status === 'paid' ? <CheckCircle size={52} weight="fill" /> : <XCircle size={52} />}
    <h1 data-testid="payment-result-title">{status === 'paid' ? 'Credits unlocked' : status === 'confirming' ? 'Confirming payment…' : 'Payment not completed'}</h1>
    <p data-testid="payment-result-description">{status === 'paid' ? 'Your creator credits are ready in the studio.' : 'No credits were charged. You can return to your caption session.'}</p>
    <CtButton onClick={() => navigate('/studio')} data-testid="return-to-editor-button">Return to editor</CtButton>
  </main>
}
