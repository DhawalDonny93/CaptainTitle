import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Check } from 'lucide-react'
import Header from '../components/Header'
import Footer from '../components/Footer'
import { useAuth } from '../contexts/AuthContext'
import { toast } from '../utils/toast'
import { captionsApi, errorMessage } from '../features/captions/captionsApi'
import { startCheckout } from '../features/captions/lib/checkout'
import type { CaptionPlan } from '../features/captions/types'

const money = (p: CaptionPlan) => `${p.currency === 'INR' ? '₹' : p.currency + ' '}${p.price}`

export default function Pricing() {
  const navigate = useNavigate()
  const { isAuthenticated } = useAuth()
  const [plans, setPlans] = useState<CaptionPlan[]>([])
  const [available, setAvailable] = useState(true)
  const [busy, setBusy] = useState<string | null>(null)

  useEffect(() => {
    captionsApi.paymentCatalog().then((c) => { setPlans(c.items); setAvailable(c.available) }).catch(() => setPlans([]))
  }, [])

  const buy = async (plan: CaptionPlan) => {
    if (!isAuthenticated) { navigate('/login?mode=signup'); return }
    setBusy(plan.id)
    try {
      const outcome = await startCheckout(plan.id)
      if (outcome === 'paid') { toast.success('Payment received - your credits are ready'); navigate('/studio') }
    } catch (error) { toast.error(errorMessage(error)) }
    finally { setBusy(null) }
  }

  return (
    <div className="min-h-screen bg-paper text-ink" style={{ fontFamily: "'IBM Plex Sans', system-ui, sans-serif" }}>
      <Header />
      <main className="mx-auto max-w-6xl px-4 py-16 sm:px-6">
        <h1 className="font-display text-5xl font-extrabold tracking-tight sm:text-6xl">Pricing</h1>
        <p className="mt-4 max-w-2xl text-lg font-medium">Start free. Pay only when you need more videos or longer clips.</p>

        <div className="mt-12 grid gap-6 md:grid-cols-3">
          <div className="flex flex-col rounded-xl border-2 border-ink bg-white p-7 shadow-hard">
            <p className="font-display text-sm font-extrabold uppercase tracking-widest">Free</p>
            <p className="mt-3 font-display text-5xl font-extrabold">₹0</p>
            <ul className="mt-6 grid flex-1 content-start gap-3 text-sm">
              {['1 captioned video', 'Up to 45 seconds', 'All caption styles', 'MP4 and SRT export'].map((f) => (
                <li key={f} className="flex gap-2"><Check className="h-5 w-5 flex-shrink-0 text-mint" strokeWidth={3} />{f}</li>
              ))}
            </ul>
            <button onClick={() => navigate(isAuthenticated ? '/studio' : '/login?mode=signup')}
              className="mt-8 rounded-lg border-2 border-ink bg-white py-3 font-bold shadow-hard-sm transition-all hover:-translate-y-0.5 hover:shadow-hard">
              {isAuthenticated ? 'Open studio' : 'Start free'}
            </button>
          </div>

          {plans.map((p) => (
            <div key={p.id} className={`flex flex-col rounded-xl border-2 border-ink p-7 shadow-hard ${p.featured ? 'bg-sun' : 'bg-white'}`}>
              {p.featured && <span className="mb-3 w-fit rounded-md border-2 border-ink bg-cherry px-2.5 py-0.5 text-xs font-extrabold uppercase tracking-widest text-white">Best for creators</span>}
              <p className="font-display text-sm font-extrabold uppercase tracking-widest">{p.name}</p>
              <p className="mt-3 font-display text-5xl font-extrabold">{money(p)}{p.kind === 'subscription' && <span className="text-lg font-semibold"> /month</span>}</p>
              <ul className="mt-6 grid flex-1 content-start gap-3 text-sm">
                {[`${p.videos} video${p.videos === 1 ? '' : 's'}${p.kind === 'subscription' ? ' every month' : ''}`, `Up to ${p.max_seconds} seconds each`, 'All caption styles', 'MP4 and SRT export'].map((f) => (
                  <li key={f} className="flex gap-2"><Check className="h-5 w-5 flex-shrink-0 text-mint" strokeWidth={3} />{f}</li>
                ))}
                {p.max_purchases != null && <li className="text-xs font-medium">Up to {p.max_purchases} purchases, then choose a subscription.</li>}
              </ul>
              <button onClick={() => buy(p)} disabled={!available || busy === p.id}
                className="mt-8 rounded-lg border-2 border-ink bg-cherry py-3 font-bold text-white shadow-hard transition-all hover:-translate-y-0.5 hover:shadow-hard-lg disabled:opacity-60">
                {busy === p.id ? 'Opening payment...' : p.kind === 'subscription' ? `Subscribe to ${p.name}` : `Buy ${p.name.toLowerCase()}`}
              </button>
            </div>
          ))}
        </div>

        {!available && <p className="mt-8 rounded-lg border-2 border-ink bg-blush p-4 text-sm font-medium">Online payments are not switched on yet. You can still use your free video.</p>}
        <p className="mt-10 text-sm text-gray-700">Prices in INR are paid securely with Razorpay (UPI, cards, netbanking, wallets). Payments from outside India use Stripe.</p>
      </main>
      <Footer />
    </div>
  )
}
