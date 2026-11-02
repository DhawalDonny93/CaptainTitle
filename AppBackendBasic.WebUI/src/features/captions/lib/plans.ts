import { useState } from 'react'
import { toast } from '@/utils/toast'
import { errorMessage } from '../captionsApi'
import { startCheckout } from './checkout'
import type { CaptionPlan } from '../types'

/**
 * Shared plan presentation + purchase flow for every place that sells caption plans (studio paywall, /pricing).
 * Plans always come from the API (CaptionPlans table, editable in Admin), never from the page.
 */

export const formatPrice = (plan: CaptionPlan) => `${plan.currency === 'INR' ? '₹' : plan.currency + ' '}${plan.price}`

export const isSubscription = (plan: CaptionPlan) => plan.kind === 'subscription'

/** One-line summary: the admin-written description, else videos + length. */
export const planSummary = (plan: CaptionPlan) =>
  plan.description ?? `${plan.videos} video${plan.videos === 1 ? '' : 's'}${isSubscription(plan) ? ' every month' : ''} · up to ${plan.max_seconds} seconds each`

export const planFeatures = (plan: CaptionPlan) => [
  `${plan.videos} video${plan.videos === 1 ? '' : 's'}${isSubscription(plan) ? ' every month' : ''}`,
  `Up to ${plan.max_seconds} seconds each`,
  'All caption styles',
  'MP4 and SRT export',
]

export const planCta = (plan: CaptionPlan) => (isSubscription(plan) ? `Subscribe to ${plan.name}` : `Buy ${plan.name}`)

export const purchaseLimitNote = (plan: CaptionPlan) =>
  plan.max_purchases != null ? `${plan.name} can be purchased up to ${plan.max_purchases} times; then a subscription is required.` : null

/** Starts checkout for a plan and reports the outcome; `busy` is the plan id being bought. */
export function usePlanCheckout(onPaid: () => void) {
  const [busy, setBusy] = useState<string | null>(null)
  const buy = async (plan: CaptionPlan) => {
    setBusy(plan.id)
    try {
      if (await startCheckout(plan.id) === 'paid') {
        toast.success(`${plan.name} is active - your credits are ready`)
        onPaid()
      }
    } catch (error) { toast.error(errorMessage(error)) }
    finally { setBusy(null) }
  }
  return { busy, buy }
}
