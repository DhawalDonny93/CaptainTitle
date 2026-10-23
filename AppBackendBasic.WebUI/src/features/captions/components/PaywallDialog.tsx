import { Crown, Lightning, X } from '@phosphor-icons/react'
import { toast } from '@/utils/toast'
import { CtButton, CtDialog } from './ui'
import { errorMessage } from '../captionsApi'
import { startCheckout } from '../lib/checkout'
import type { CaptionPlan, CaptionUsage } from '../types'

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  usage: CaptionUsage | null
  /** Called after a payment is confirmed in-page (Razorpay) so the usage meter can refresh. */
  onPaid?: () => void
}

const price = (plan: CaptionPlan) => `${plan.currency === 'INR' ? '₹' : plan.currency + ' '}${plan.price}`

export const PaywallDialog = ({ open, onOpenChange, usage, onPaid }: Props) => {
  const checkout = async (packageId: string) => {
    try {
      const outcome = await startCheckout(packageId)
      if (outcome === 'paid') {
        toast.success('Payment received - your credits are ready')
        onPaid?.()
        onOpenChange(false)
      }
    } catch (error) { toast.error(errorMessage(error)) }
  }
  const plans = usage?.plans ?? []
  const capped = plans.find((p) => p.max_purchases != null)
  return (
    <CtDialog open={open} onOpenChange={onOpenChange} className="pricing-dialog" data-testid="pricing-dialog">
      <header>
        <p className="eyebrow" data-testid="pricing-dialog-label"><Crown size={17} /> CREATOR ACCESS</p>
        <h2 data-testid="pricing-dialog-title">Keep your captions moving</h2>
      </header>
      <p className="pricing-intro" data-testid="pricing-dialog-description">Your free video is included. Choose a pass or unlock a monthly creator batch.</p>
      {usage?.unlimited_testing && <p className="testing-mode" data-testid="unlimited-testing-status">Unlimited testing is active. No video credits are deducted right now.</p>}
      <div className="pricing-options">
        {plans.map((plan) => plan.kind === 'subscription'
          ? <div key={plan.id} className="pricing-option featured" data-testid={`${plan.id}-plan-card`}>
            <Crown size={23} weight="fill" />{plan.featured && <span className="popular">BEST FOR CREATORS</span>}
            <h3>{plan.name}</h3><strong>{price(plan)}<small>/month</small></strong><p>{plan.description ?? `${plan.videos} videos · up to ${plan.max_seconds} seconds each`}</p>
            <CtButton onClick={() => checkout(plan.id)} data-testid={`buy-${plan.id}-button`}>Choose {plan.name}</CtButton>
          </div>
          : <div key={plan.id} className="pricing-option" data-testid={`${plan.id}-plan-card`}>
            <Lightning size={23} />
            <h3>{plan.name}</h3><strong>{price(plan)}</strong><p>{plan.description ?? `${plan.videos} video · up to ${plan.max_seconds} seconds`}</p>
            <CtButton variant="outline" onClick={() => checkout(plan.id)} data-testid={`buy-${plan.id}-button`}>Buy video pass</CtButton>
          </div>)}
      </div>
      {capped && <p className="pricing-note" data-testid="oneoff-purchase-limit-note">{capped.name} passes can be purchased up to {capped.max_purchases} times; then a subscription is required.</p>}
      {usage?.subscription_active && <p className="active-plan" data-testid="active-plan-status">Plan active</p>}
      <button className="dialog-close-label" onClick={() => onOpenChange(false)} data-testid="pricing-dialog-close-button"><X size={16} /> Back to studio</button>
    </CtDialog>
  )
}
