import { Crown, Lightning, X } from '@phosphor-icons/react'
import { CtButton, CtDialog } from './ui'
import { formatPrice, isSubscription, planCta, planSummary, purchaseLimitNote, usePlanCheckout } from '../lib/plans'
import type { CaptionUsage } from '../types'

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  usage: CaptionUsage | null
  /** Called after a plan is confirmed in-page (Razorpay, or free in Development) so the usage meter can refresh. */
  onPaid?: () => void
}

export const PaywallDialog = ({ open, onOpenChange, usage, onPaid }: Props) => {
  const { busy, buy } = usePlanCheckout(() => { onPaid?.(); onOpenChange(false) })
  const plans = usage?.plans ?? []
  const limitNote = plans.map(purchaseLimitNote).find(Boolean)
  return (
    <CtDialog open={open} onOpenChange={onOpenChange} className="pricing-dialog" data-testid="pricing-dialog">
      <header>
        <p className="eyebrow" data-testid="pricing-dialog-label"><Crown size={17} /> CREATOR ACCESS</p>
        <h2 data-testid="pricing-dialog-title">Keep your captions moving</h2>
      </header>
      <p className="pricing-intro" data-testid="pricing-dialog-description">Your free video is included. Choose a pass or unlock a monthly creator batch.</p>
      {usage?.unlimited_testing && <p className="testing-mode" data-testid="unlimited-testing-status">Unlimited testing is active. No video credits are deducted right now.</p>}
      <div className="pricing-options">
        {plans.map((plan) => (
          <div key={plan.id} className={`pricing-option${isSubscription(plan) ? ' featured' : ''}`} data-testid={`${plan.id}-plan-card`}>
            {isSubscription(plan) ? <Crown size={23} weight="fill" /> : <Lightning size={23} />}
            {plan.featured && <span className="popular">BEST FOR CREATORS</span>}
            <h3>{plan.name}</h3>
            <strong>{formatPrice(plan)}{isSubscription(plan) && <small>/month</small>}</strong>
            <p>{planSummary(plan)}</p>
            <CtButton variant={isSubscription(plan) ? undefined : 'outline'} disabled={busy === plan.id} onClick={() => buy(plan)} data-testid={`buy-${plan.id}-button`}>
              {busy === plan.id ? 'Opening payment...' : planCta(plan)}
            </CtButton>
          </div>
        ))}
      </div>
      {limitNote && <p className="pricing-note" data-testid="oneoff-purchase-limit-note">{limitNote}</p>}
      {usage?.subscription_active && <p className="active-plan" data-testid="active-plan-status">Plan active</p>}
      <button className="dialog-close-label" onClick={() => onOpenChange(false)} data-testid="pricing-dialog-close-button"><X size={16} /> Back to studio</button>
    </CtDialog>
  )
}
