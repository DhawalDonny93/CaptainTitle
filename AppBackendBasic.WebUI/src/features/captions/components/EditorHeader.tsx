import { ClosedCaptioning, Crown, Palette, SignOut, UserCircle } from '@phosphor-icons/react'
import { CtButton } from './ui'
import type { CaptionUsage } from '../types'
import { STUDIO_THEMES, useStudioTheme, type StudioThemeId } from '../hooks/useStudioTheme'

interface Props {
  userName: string
  userPicture?: string
  usage: CaptionUsage | null
  onPricing: () => void
  onLogout: () => void
}

export const EditorHeader = ({ userName, userPicture, usage, onPricing, onLogout }: Props) => {
  const { theme, setTheme } = useStudioTheme()
  const credits = (usage?.free_remaining || 0) + (usage?.oneoff_credits || 0) + (usage?.basic_credits || 0)
  return (
    <header className="editor-header" data-testid="editor-header">
      <div className="brand-lockup" data-testid="editor-brand"><span className="brand-mark"><ClosedCaptioning size={22} weight="fill" /></span> CAPTAIN TITLE</div>
      <div className="header-actions">
        <label className="theme-switcher" title="Studio colour theme">
          <Palette size={16} />
          <select className="quality-select" value={theme} onChange={(e) => setTheme(e.target.value as StudioThemeId)} aria-label="Studio colour theme" data-testid="studio-theme-select">
            {STUDIO_THEMES.map((t) => <option key={t.id} value={t.id}>{t.label}</option>)}
          </select>
        </label>
        <button className="usage-meter" onClick={onPricing} data-testid="usage-meter-button">
          <span data-testid="usage-credit-count">{!usage ? 'Checking access…' : usage.unlimited_testing ? 'Unlimited tests' : `${credits} credits`}</span>
          <i style={{ width: !usage ? '18%' : usage.unlimited_testing ? '100%' : `${Math.min(100, credits * 10)}%` }} />
        </button>
        <CtButton variant="outline" onClick={onPricing} data-testid="view-plans-button"><Crown size={17} /> Plans</CtButton>
        <div className="user-chip" data-testid="signed-in-user">
          {userPicture ? <img src={userPicture} alt="" data-testid="user-avatar-image" /> : <UserCircle size={24} />}
          <span data-testid="signed-in-user-name">{userName}</span>
        </div>
        <CtButton size="icon" variant="ghost" onClick={onLogout} title="Sign out" data-testid="logout-button"><SignOut size={19} /></CtButton>
      </div>
    </header>
  )
}
