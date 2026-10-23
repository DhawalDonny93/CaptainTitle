import { Link } from 'react-router-dom'
import { Logo } from './Header'

export default function Footer() {
  return (
    <footer className="border-t-2 border-ink bg-ink text-paper">
      <div className="mx-auto flex max-w-6xl flex-col gap-8 px-4 py-12 sm:px-6 md:flex-row md:items-start md:justify-between">
        <div className="max-w-sm">
          <div className="[&_span:last-child]:text-paper"><Logo /></div>
          <p className="mt-4 text-sm text-paper/80">Word-by-word Hinglish captions for creators, podcasters and editors.</p>
        </div>
        <div className="grid grid-cols-2 gap-10 text-sm">
          <div className="grid content-start gap-2">
            <p className="font-display font-bold uppercase tracking-widest text-sun">Product</p>
            <Link to="/#how-it-works" className="hover:text-sun">How it works</Link>
            <Link to="/#styles" className="hover:text-sun">Styles</Link>
            <Link to="/pricing" className="hover:text-sun">Pricing</Link>
          </div>
          <div className="grid content-start gap-2">
            <p className="font-display font-bold uppercase tracking-widest text-sun">Legal</p>
            <Link to="/terms" className="hover:text-sun">Terms</Link>
            <Link to="/privacy" className="hover:text-sun">Privacy</Link>
          </div>
        </div>
      </div>
      <div className="border-t border-paper/20 py-4 text-center text-xs text-paper/70">
        &copy; {new Date().getFullYear()} Captain Title. All rights reserved.
      </div>
    </footer>
  )
}
