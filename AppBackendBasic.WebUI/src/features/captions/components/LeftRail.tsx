import { ArrowsOutCardinal, ClosedCaptioning, FrameCorners, MusicNotes, Palette, Rows, Sparkle, SquaresFour, TextAa, type Icon } from '@phosphor-icons/react'

// 'adjustment' is the Color panel: the id is kept so editor states saved before the rename still open it.
// 'templates' | 'customize' | 'transform' are the caption style tools that used to be the right-hand panel's tabs.
// 'text' is the single Text tool: it edits both a picked caption word and the free text overlays (see TextPanel's mode switch).
export type CaptionStyleTab = 'templates' | 'customize' | 'transform'
export type LeftTab = 'captions' | 'text' | 'audio' | 'ratio' | 'adjustment' | 'ai' | CaptionStyleTab

interface Section { id: LeftTab; label: string; icon: Icon }

// Two groups, split by a divider: the caption and text tools, then everything else on the video.
const groups: Section[][] = [
  [
    { id: 'captions', label: 'Captions', icon: ClosedCaptioning },
    { id: 'templates', label: 'Templates', icon: SquaresFour },
    { id: 'customize', label: 'Customize', icon: Rows },
    { id: 'text', label: 'Text', icon: TextAa },
    { id: 'transform', label: 'Transform', icon: ArrowsOutCardinal },
  ],
  [
    { id: 'audio', label: 'Audio', icon: MusicNotes },
    { id: 'ratio', label: 'Ratio', icon: FrameCorners },
    { id: 'adjustment', label: 'Color', icon: Palette },
    { id: 'ai', label: 'AI tools', icon: Sparkle },
  ],
]

interface Props { active: LeftTab; onSelect: (tab: LeftTab) => void }

export const LeftRail = ({ active, onSelect }: Props) => (
  <nav className="left-rail" data-testid="left-rail">
    {groups.map((sections, groupIndex) => [
      groupIndex > 0 && <i key={`divider-${groupIndex}`} className="left-rail-divider" aria-hidden="true" />,
      ...sections.map(({ id, label, icon: Icon }) => (
        <button key={id} className={active === id ? 'active' : ''} onClick={() => onSelect(id)} title={label} data-testid={`left-rail-${id}-button`}>
          <Icon size={20} weight={active === id ? 'fill' : 'regular'} />
          <span>{label}</span>
        </button>
      )),
    ])}
  </nav>
)
