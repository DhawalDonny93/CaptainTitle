import { PaymentResult } from '../components/PaymentResult'
import '../studio.css'

export function CaptionPaymentSuccess() { return <PaymentResult success /> }
export function CaptionPaymentCancel() { return <PaymentResult /> }
