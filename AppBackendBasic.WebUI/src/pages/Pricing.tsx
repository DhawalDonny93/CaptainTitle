import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Check } from 'lucide-react'
import Header from '../components/Header'
import Footer from '../components/Footer'
import { useAuth } from '../contexts/AuthContext'
import { captionsApi } from '../features/captions/captionsApi'
import { formatPrice, isSubscription, planCta, planFeatures, purchaseLimitNote, usePlanCheckout } from '../features/captions/lib/plans'
import type { CaptionPlan, PlanCatalog } from '../features/captions/types'

const Features = ({ items }: { items: string[] }) => (
  <ul className="mt-6 grid flex-1 content-start gap-3 text-sm">
    {items.map((f) => <li key={f} className="flex gap-2"><Check className="h-5 w-5 flex-shrink-0 text-mint" strokeWidth={3} />{f}</li>)}
  </ul>
)

export default function Pricing() {
  const navigate = useNavigate()
  const { isAuthenticated } = useAuth()
  const [catalog, setCatalog] = useState<PlanCatalog | null>(null)
  const { busy, buy } = usePlanCheckout(() => navigate('/studio'))

  useEffect(() => { captionsApi.paymentCatalog().then(setCatalog).catch(() => setCatalog(null)) }, [])

  const choose = (plan: CaptionPlan) => (isAuthenticated ? buy(plan) : navigate('/login?mode=signup'))
  const available = catalog?.available ?? true
  const free = catalog?.free

  return (
    <div className="min-h-screen bg-paper text-ink" style={{ fontFamily: "'IBM Plex Sans', system-ui, sans-serif" }}>
      <Header />
      <main className="mx-auto max-w-6xl px-4 py-16 sm:px-6">
        <h1 className="font-display text-5xl font-extrabold tracking-tight sm:text-6xl">Pricing</h1>
        <p className="mt-4 max-w-2xl text-lg font-medium">Start free. Pay only when you need more videos or longer clips.</p>
        {catalog?.unlimited_testing && <p className="mt-6 rounded-lg border-2 border-ink bg-sun p-4 text-sm font-medium">Unlimited testing is active. No video credits are deducted right now.</p>}

        <div className="mt-12 grid gap-6 md:grid-cols-3">
          {free && free.videos > 0 && (
            <div className="flex flex-col rounded-xl border-2 border-ink bg-white p-7 shadow-hard">
              <p className="font-display text-sm font-extrabold uppercase tracking-widest">Free</p>
              <p className="mt-3 font-display text-5xl font-extrabold">₹0</p>
              <Features items={[`${free.videos} captioned video${free.videos === 1 ? '' : 's'}`, `Up to ${free.max_seconds} seconds`, 'All caption styles', 'MP4 and SRT export']} />
              <button onClick={() => navigate(isAuthenticated ? '/studio' : '/login?mode=signup')}
                className="mt-8 rounded-lg border-2 border-ink bg-white py-3 font-bold shadow-hard-sm transition-all hover:-translate-y-0.5 hover:shadow-hard">
                {isAuthenticated ? 'Open studio' : 'Start free'}
              </button>
            </div>
          )}

          {catalog?.items.map((p) => {
            const note = purchaseLimitNote(p)
            return (
              <div key={p.id} className={`flex flex-col rounded-xl border-2 border-ink p-7 shadow-hard ${p.featured ? 'bg-sun' : 'bg-white'}`}>
                {p.featured && <span className="mb-3 w-fit rounded-md border-2 border-ink bg-cherry px-2.5 py-0.5 text-xs font-extrabold uppercase tracking-widest text-white">Best for creators</span>}
                <p className="font-display text-sm font-extrabold uppercase tracking-widest">{p.name}</p>
                <p className="mt-3 font-display text-5xl font-extrabold">{formatPrice(p)}{isSubscription(p) && <span className="text-lg font-semibold"> /month</span>}</p>
                <Features items={planFeatures(p)} />
                {note && <p className="mt-3 text-xs font-medium">{note}</p>}
                <button onClick={() => choose(p)} disabled={!available || busy === p.id}
                  className="mt-8 rounded-lg border-2 border-ink bg-cherry py-3 font-bold text-white shadow-hard transition-all hover:-translate-y-0.5 hover:shadow-hard-lg disabled:opacity-60">
                  {busy === p.id ? 'Opening payment...' : planCta(p)}
                </button>
              </div>
            )
          })}
        </div>

        {!available && <p className="mt-8 rounded-lg border-2 border-ink bg-blush p-4 text-sm font-medium">Online payments are not switched on yet. You can still use your free video.</p>}
        <p className="mt-10 text-sm text-gray-700">Prices in INR are paid securely with Razorpay (UPI, cards, netbanking, wallets). Payments from outside India use Stripe.</p>
      </main>
      <Footer />
    </div>
  )
}
