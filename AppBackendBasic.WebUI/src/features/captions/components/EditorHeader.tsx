import { CaretDown, Check, Crown, FolderSimple, Palette, SignOut, UserCircle } from '@phosphor-icons/react'
import type { CaptionUsage } from '../types'
import { STUDIO_THEMES, useStudioTheme } from '../hooks/useStudioTheme'
import { useDismissable } from '../hooks/useDismissable'

interface Props {
  userName: string
  userEmail?: string
  userPicture?: string
  usage: CaptionUsage | null
  /** true on the project library (no project open): the left side shows a title instead of the project's slot content */
  home: boolean
  onHome: () => void
  onPricing: () => void
  onLogout: () => void
  /** DOM slots the open project portals its back link / name / save state (left) and Export button (right) into */
  leftSlotRef: (el: HTMLDivElement | null) => void
  rightSlotRef: (el: HTMLDivElement | null) => void
}

const ThemeMenu = () => {
  const { theme, setTheme } = useStudioTheme()
  const { open, setOpen, ref } = useDismissable()
  const current = STUDIO_THEMES.find((t) => t.id === theme) ?? STUDIO_THEMES[0]
  return (
    <div className="header-menu" ref={ref}>
      <button className="utility-chip icon-only" onClick={() => setOpen((v) => !v)} aria-haspopup="menu" aria-expanded={open}
        title={`Theme: ${current.label}`} aria-label={`Studio colour theme (${current.label})`} data-testid="studio-theme-button">
        <Palette size={18} />
      </button>
      {open && <div className="header-menu-popover" role="menu" data-testid="studio-theme-menu">
        <p className="header-menu-caption">Theme</p>
        {STUDIO_THEMES.map((t) => (
          <button key={t.id} role="menuitemradio" aria-checked={t.id === theme} className={`header-menu-item${t.id === theme ? ' active' : ''}`}
            onClick={() => { setTheme(t.id); setOpen(false) }} data-testid={`studio-theme-${t.id}`}>
            <i className="theme-swatch" style={{ background: t.swatch[0] }}><b style={{ background: t.swatch[1] }} /></i>
            <span>{t.label}</span>
            {t.id === theme && <Check size={14} weight="bold" />}
          </button>
        ))}
      </div>}
    </div>
  )
}

const UserMenu = ({ userName, userEmail, userPicture, credits, onHome, onPricing, onLogout }: {
  userName: string; userEmail?: string; userPicture?: string; credits: string; onHome: () => void; onPricing: () => void; onLogout: () => void
}) => {
  const { open, setOpen, ref } = useDismissable()
  const pick = (action: () => void) => () => { setOpen(false); action() }
  return (
    <div className="header-menu" ref={ref}>
      <button className="user-chip" onClick={() => setOpen((v) => !v)} aria-haspopup="menu" aria-expanded={open} data-testid="signed-in-user">
        {userPicture ? <img src={userPicture} alt="" data-testid="user-avatar-image" /> : <UserCircle size={26} />}
        <span data-testid="signed-in-user-name">{userName}</span>
        <CaretDown size={12} />
      </button>
      {open && <div className="header-menu-popover user-menu" role="menu" data-testid="user-menu">
        <div className="user-menu-head">
          <b>{userName}</b>
          {userEmail && <span>{userEmail}</span>}
        </div>
        <button className="header-menu-item" role="menuitem" onClick={pick(onHome)} data-testid="user-menu-projects"><FolderSimple size={16} /><span>All projects</span></button>
        <button className="header-menu-item" role="menuitem" onClick={pick(onPricing)} data-testid="view-plans-button"><Crown size={16} /><span>Plans &amp; credits</span><em>{credits}</em></button>
        <hr />
        <button className="header-menu-item danger" role="menuitem" onClick={pick(onLogout)} data-testid="logout-button"><SignOut size={16} /><span>Sign out</span></button>
      </div>}
    </div>
  )
}

export const EditorHeader = ({ userName, userEmail, userPicture, usage, home, onHome, onPricing, onLogout, leftSlotRef, rightSlotRef }: Props) => {
  const credits = (usage?.free_remaining || 0) + (usage?.oneoff_credits || 0) + (usage?.basic_credits || 0)
  const creditLabel = !usage ? 'Checking access…' : usage.unlimited_testing ? 'Unlimited tests' : `${credits} credits`
  return (
    <header className="editor-header" data-testid="editor-header">
      <div className="header-left" ref={leftSlotRef}>
        {home && <h1 className="header-title" data-testid="editor-home-title">All projects</h1>}
      </div>
      <div className="header-actions">
        <div className="header-slot" ref={rightSlotRef} />
        <button className="usage-meter" onClick={onPricing} data-testid="usage-meter-button">
          <span data-testid="usage-credit-count">{creditLabel}</span>
          <i style={{ width: !usage ? '18%' : usage.unlimited_testing ? '100%' : `${Math.min(100, credits * 10)}%` }} />
        </button>
        <ThemeMenu />
        <UserMenu userName={userName} userEmail={userEmail} userPicture={userPicture} credits={creditLabel} onHome={onHome} onPricing={onPricing} onLogout={onLogout} />
      </div>
    </header>
  )
}
