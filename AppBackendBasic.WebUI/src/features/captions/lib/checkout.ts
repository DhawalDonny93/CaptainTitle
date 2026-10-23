import { captionsApi } from '../captionsApi'
import type { RazorpayCheckoutInfo } from '../types'

interface RazorpaySuccess { razorpay_payment_id: string; razorpay_order_id?: string; razorpay_subscription_id?: string; razorpay_signature: string }

interface RazorpayOptions {
  key: string
  name: string
  description: string
  currency?: string
  amount?: number
  order_id?: string
  subscription_id?: string
  prefill?: { email?: string; name?: string }
  theme?: { color: string }
  handler: (response: RazorpaySuccess) => void
  modal?: { ondismiss?: () => void }
}

declare global {
  interface Window { Razorpay?: new (options: RazorpayOptions) => { open: () => void } }
}

let scriptPromise: Promise<void> | null = null

/** Loads Razorpay Checkout.js once, only when a payment is started. */
function loadRazorpay(): Promise<void> {
  if (window.Razorpay) return Promise.resolve()
  if (!scriptPromise) {
    scriptPromise = new Promise((resolve, reject) => {
      const s = document.createElement('script')
      s.src = 'https://checkout.razorpay.com/v1/checkout.js'
      s.async = true
      s.onload = () => resolve()
      s.onerror = () => { scriptPromise = null; reject(new Error('Could not load the payment window. Check your connection and try again.')) }
      document.body.appendChild(s)
    })
  }
  return scriptPromise
}

function openRazorpay(info: RazorpayCheckoutInfo): Promise<RazorpaySuccess | null> {
  return loadRazorpay().then(() => new Promise((resolve) => {
    if (!window.Razorpay) { resolve(null); return }
    const options: RazorpayOptions = {
      key: info.key_id,
      name: info.name,
      description: info.description,
      prefill: { email: info.prefill_email, name: info.prefill_name },
      theme: { color: '#FF2D4D' },
      handler: (response) => resolve(response),
      modal: { ondismiss: () => resolve(null) },
    }
    if (info.subscription_id) options.subscription_id = info.subscription_id
    else { options.order_id = info.order_id; options.amount = info.amount; options.currency = info.currency }
    new window.Razorpay(options).open()
  }))
}

export type CheckoutOutcome = 'paid' | 'cancelled' | 'redirected'

/**
 * Starts a payment for a plan. INR plans use Razorpay (modal + signature verification on the server),
 * other currencies use Stripe (redirect to the hosted checkout page and back to /payment/success).
 */
export async function startCheckout(planId: string): Promise<CheckoutOutcome> {
  const res = await captionsApi.checkout(planId)
  if (res.provider === 'stripe' && res.checkout_url) {
    window.location.href = res.checkout_url
    return 'redirected'
  }
  if (res.provider === 'razorpay' && res.razorpay) {
    const paid = await openRazorpay(res.razorpay)
    if (!paid) return 'cancelled'
    const verified = await captionsApi.verifyRazorpay({
      razorpay_order_id: paid.razorpay_order_id,
      razorpay_subscription_id: paid.razorpay_subscription_id,
      razorpay_payment_id: paid.razorpay_payment_id,
      razorpay_signature: paid.razorpay_signature,
    })
    return verified.payment_status === 'paid' ? 'paid' : 'cancelled'
  }
  throw new Error('Unexpected payment response')
}
