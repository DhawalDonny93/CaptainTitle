import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { Menu, X } from 'lucide-react'
import { useAuth } from '../contexts/AuthContext'

export function Logo({ onClick }: { onClick?: () => void }) {
  return (
    <button onClick={onClick} className="flex items-center gap-2.5" aria-label="Captain Title home">
      <span className="grid h-9 w-9 place-items-center rounded-lg border-2 border-ink bg-cherry font-display text-base font-extrabold text-white shadow-hard-sm">C</span>
      <span className="font-display text-xl font-extrabold tracking-tight text-ink">Captain Title</span>
    </button>
  )
}

/** Public site header (landing, pricing, legal pages). */
export default function Header() {
  const navigate = useNavigate()
  const { isAuthenticated } = useAuth()
  const [open, setOpen] = useState(false)

  const links = [
    { to: '/#how-it-works', label: 'How it works' },
    { to: '/#styles', label: 'Styles' },
    { to: '/pricing', label: 'Pricing' },
  ]

  return (
    <header className="sticky top-0 z-40 border-b-2 border-ink bg-paper">
      <div className="mx-auto flex h-16 max-w-6xl items-center justify-between px-4 sm:px-6">
        <Logo onClick={() => navigate('/')} />

        <nav className="hidden items-center gap-8 md:flex">
          {links.map((l) => (
            <Link key={l.label} to={l.to} className="font-semibold text-ink hover:text-cherry">{l.label}</Link>
          ))}
        </nav>

        <div className="hidden items-center gap-3 md:flex">
          {isAuthenticated ? (
            <button onClick={() => navigate('/studio')} className="rounded-lg border-2 border-ink bg-cherry px-5 py-2 font-bold text-white shadow-hard-sm transition-all hover:-translate-y-0.5 hover:shadow-hard">
              Open studio
            </button>
          ) : (
            <>
              <button onClick={() => navigate('/login')} className="px-3 py-2 font-semibold hover:text-cherry">Sign in</button>
              <button onClick={() => navigate('/login?mode=signup')} className="rounded-lg border-2 border-ink bg-cherry px-5 py-2 font-bold text-white shadow-hard-sm transition-all hover:-translate-y-0.5 hover:shadow-hard">
                Start free
              </button>
            </>
          )}
        </div>

        <button className="md:hidden" onClick={() => setOpen(!open)} aria-label="Menu">
          {open ? <X className="h-7 w-7" /> : <Menu className="h-7 w-7" />}
        </button>
      </div>

      {open && (
        <div className="border-t-2 border-ink bg-paper px-4 py-4 md:hidden">
          <div className="grid gap-3">
            {links.map((l) => (
              <Link key={l.label} to={l.to} onClick={() => setOpen(false)} className="font-semibold">{l.label}</Link>
            ))}
            <button onClick={() => navigate(isAuthenticated ? '/studio' : '/login')} className="rounded-lg border-2 border-ink bg-cherry py-2.5 font-bold text-white shadow-hard-sm">
              {isAuthenticated ? 'Open studio' : 'Start free'}
            </button>
          </div>
        </div>
      )}
    </header>
  )
}
