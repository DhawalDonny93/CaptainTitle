import { ClosedCaptioning, FilmSlate, MusicNotes, Shapes, Sparkle, SlidersHorizontal, TextT, type Icon } from '@phosphor-icons/react'

export type LeftTab = 'media' | 'audio' | 'text' | 'elements' | 'captions' | 'adjustment' | 'ai'

const sections: { id: LeftTab; label: string; icon: Icon }[] = [
  { id: 'media', label: 'Media', icon: FilmSlate },
  { id: 'audio', label: 'Audio', icon: MusicNotes },
  { id: 'text', label: 'Text', icon: TextT },
  { id: 'elements', label: 'Elements', icon: Shapes },
  { id: 'captions', label: 'Captions', icon: ClosedCaptioning },
  { id: 'adjustment', label: 'Adjustment', icon: SlidersHorizontal },
  { id: 'ai', label: 'AI tools', icon: Sparkle },
]

interface Props { active: LeftTab; onSelect: (tab: LeftTab) => void }

export const LeftRail = ({ active, onSelect }: Props) => (
  <nav className="left-rail" data-testid="left-rail">
    {sections.map(({ id, label, icon: Icon }) => (
      <button key={id} className={active === id ? 'active' : ''} onClick={() => onSelect(id)} title={label} data-testid={`left-rail-${id}-button`}>
        <Icon size={21} weight={active === id ? 'fill' : 'regular'} />
        <span>{label}</span>
      </button>
    ))}
  </nav>
)
