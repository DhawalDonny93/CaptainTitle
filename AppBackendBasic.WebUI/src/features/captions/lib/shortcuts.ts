/**
 * Studio keyboard shortcuts: the list of actions, their default keys, and how a key press is turned into a combo string and shown back.
 *
 * A combo is a canonical string such as "Mod+Shift+Z" or "Alt+ArrowUp". "Mod" is ⌘ on a Mac and Ctrl everywhere else. Letter and digit keys
 * are read from KeyboardEvent.code (not .key), so ⌥K on a Mac is still "Alt+K" instead of the "˚" character the Option key types.
 */

export type ShortcutGroup = 'General' | 'Playback' | 'Captions' | 'Timeline' | 'Assistant'

export type ShortcutId =
  | 'shortcuts.show' | 'undo' | 'redo' | 'element.remove'
  | 'play.toggle' | 'play' | 'pause' | 'frame.prev' | 'frame.next' | 'word.prev' | 'word.next' | 'caption.prev' | 'caption.next'
  | 'volume.up' | 'volume.down' | 'speed.down' | 'speed.up'
  | 'caption.search' | 'caption.reveal' | 'caption.editPrev' | 'caption.editNext' | 'caption.playFromStart' | 'caption.split'
  | 'caption.joinPrev' | 'caption.joinNext'
  | 'selection.clear' | 'selection.cut'
  | 'assistant.toggle'

export interface ShortcutAction {
  id: ShortcutId
  group: ShortcutGroup
  label: string
  hint?: string
  defaults: string[]
  /** Also fires while typing in a text field - only for combos that use Mod, so plain typing is never swallowed. */
  inInput?: boolean
  /** Fires again while the key is held down (seeking, volume). */
  repeat?: boolean
}

export const SHORTCUT_GROUPS: ShortcutGroup[] = ['General', 'Playback', 'Captions', 'Timeline', 'Assistant']

export const SHORTCUT_ACTIONS: ShortcutAction[] = [
  { id: 'shortcuts.show', group: 'General', label: 'Show keyboard shortcuts', defaults: ['Shift+Slash', 'Mod+Slash'], inInput: true },
  { id: 'undo', group: 'General', label: 'Undo', defaults: ['Mod+Z'] },
  { id: 'redo', group: 'General', label: 'Redo', defaults: ['Mod+Shift+Z', 'Mod+Y'] },
  { id: 'element.remove', group: 'General', label: 'Remove the picked sticker or text',
    hint: 'Takes it off the video. Undo brings it back.', defaults: ['Delete', 'Backspace'] },

  { id: 'play.toggle', group: 'Playback', label: 'Play or pause', defaults: ['Space', 'Alt+K'] },
  { id: 'play', group: 'Playback', label: 'Play', defaults: [] },
  { id: 'pause', group: 'Playback', label: 'Pause', defaults: [] },
  { id: 'frame.prev', group: 'Playback', label: 'Previous frame', defaults: ['ArrowLeft'], repeat: true },
  { id: 'frame.next', group: 'Playback', label: 'Next frame', defaults: ['ArrowRight'], repeat: true },
  { id: 'word.prev', group: 'Playback', label: 'Previous word', defaults: ['Alt+ArrowLeft'], repeat: true },
  { id: 'word.next', group: 'Playback', label: 'Next word', defaults: ['Alt+ArrowRight'], repeat: true },
  { id: 'caption.prev', group: 'Playback', label: 'Previous caption', defaults: ['Shift+ArrowLeft'], repeat: true },
  { id: 'caption.next', group: 'Playback', label: 'Next caption', defaults: ['Shift+ArrowRight'], repeat: true },
  { id: 'volume.up', group: 'Playback', label: 'Volume up', hint: 'Preview only. The export uses the volume set in Audio.', defaults: ['ArrowUp'], repeat: true },
  { id: 'volume.down', group: 'Playback', label: 'Volume down', hint: 'Preview only. The export uses the volume set in Audio.', defaults: ['ArrowDown'], repeat: true },
  { id: 'speed.down', group: 'Playback', label: 'Slower', defaults: ['Comma'] },
  { id: 'speed.up', group: 'Playback', label: 'Faster', defaults: ['Period'] },

  { id: 'caption.search', group: 'Captions', label: 'Search captions', hint: 'In the Script panel and the Timeline.', defaults: ['Mod+F'], inInput: true },
  { id: 'caption.reveal', group: 'Captions', label: 'Go to the caption under the playhead', hint: 'In the Script panel and the Timeline.', defaults: ['Mod+G'] },
  { id: 'caption.editPrev', group: 'Captions', label: 'Edit the previous caption', hint: 'Moves the playhead to it too.', defaults: ['Alt+ArrowUp'], repeat: true },
  { id: 'caption.editNext', group: 'Captions', label: 'Edit the next caption', hint: 'Moves the playhead to it too.', defaults: ['Alt+ArrowDown'], repeat: true },
  { id: 'caption.playFromStart', group: 'Captions', label: 'Play from the start of the caption',
    hint: 'The caption being edited, or the one under the playhead.', defaults: ['Alt+Enter'] },
  { id: 'caption.split', group: 'Captions', label: 'Split the caption at the picked word',
    hint: 'Or at the playhead when no word is picked.', defaults: ['Mod+Enter'] },
  { id: 'caption.joinPrev', group: 'Captions', label: 'Join with the previous caption', defaults: ['Alt+Shift+ArrowUp'] },
  { id: 'caption.joinNext', group: 'Captions', label: 'Join with the next caption', defaults: ['Alt+Shift+ArrowDown'] },

  { id: 'selection.clear', group: 'Timeline', label: 'Let go of the selection',
    hint: 'With nothing selected, switches the Timeline from Words back to Lines.', defaults: ['Escape'] },
  { id: 'selection.cut', group: 'Timeline', label: 'Cut the selection', hint: 'Deletes the picked word. Undo brings it back.', defaults: ['Delete', 'Backspace'] },

  { id: 'assistant.toggle', group: 'Assistant', label: 'Show or hide the AI tools', defaults: ['Mod+K'], inInput: true },
]

export type ShortcutBindings = Record<ShortcutId, string[]>

/** The keys are fixed: users can look them up in the shortcuts dialog but not change them. */
export const SHORTCUT_BINDINGS: ShortcutBindings =
  Object.fromEntries(SHORTCUT_ACTIONS.map((action) => [action.id, action.defaults])) as ShortcutBindings

export const IS_MAC = typeof navigator !== 'undefined' && /Mac|iPhone|iPad|iPod/i.test(navigator.platform || navigator.userAgent)

const MODIFIER_KEYS = new Set(['Shift', 'Meta', 'Control', 'Alt', 'AltGraph', 'CapsLock', 'Fn', 'OS'])

/** The combo string for a key press, or null for a bare modifier (still being held while the user builds a combo). */
export function comboFromEvent(e: KeyboardEvent): string | null {
  if (MODIFIER_KEYS.has(e.key)) return null
  let key = e.code || e.key
  if (/^Key[A-Z]$/.test(key)) key = key.slice(3)
  else if (/^Digit\d$/.test(key)) key = key.slice(5)
  else if (key === 'NumpadEnter') key = 'Enter'
  if (!key || key === 'Unidentified') return null
  const parts: string[] = []
  if (IS_MAC ? e.metaKey : e.ctrlKey) parts.push('Mod')
  if (IS_MAC && e.ctrlKey) parts.push('Ctrl')
  if (e.altKey) parts.push('Alt')
  if (e.shiftKey) parts.push('Shift')
  parts.push(key)
  return parts.join('+')
}

const MODIFIER_LABELS: Record<string, string> = IS_MAC
  ? { Mod: '⌘', Ctrl: '⌃', Alt: '⌥', Shift: '⇧' }
  : { Mod: 'Ctrl', Ctrl: 'Ctrl', Alt: 'Alt', Shift: 'Shift' }

const KEY_LABELS: Record<string, string> = {
  ArrowLeft: '←', ArrowRight: '→', ArrowUp: '↑', ArrowDown: '↓', Enter: '↩', Backspace: '⌫', Delete: '⌦', Escape: 'Esc', Space: 'Space',
  Tab: 'Tab', Comma: ',', Period: '.', Slash: '/', Backslash: '\\', Semicolon: ';', Quote: "'", BracketLeft: '[', BracketRight: ']',
  Minus: '-', Equal: '=', Backquote: '`', PageUp: 'PgUp', PageDown: 'PgDn', Home: 'Home', End: 'End',
}

/** The keycaps to draw for one combo, e.g. "Alt+Shift+ArrowUp" -> ["⌥", "⇧", "↓"]. Shift+/ is shown as the "?" it types. */
export function comboKeys(combo: string): string[] {
  if (combo === 'Shift+Slash') return ['?']
  const parts = combo.split('+')
  const key = parts.pop() ?? ''
  return [...parts.map((part) => MODIFIER_LABELS[part] ?? part), KEY_LABELS[key] ?? key.replace(/^Numpad/, 'Num ')]
}

/** One line for a tooltip, e.g. "⌘Z" on a Mac and "Ctrl+Z" elsewhere. */
export const comboText = (combo: string) => comboKeys(combo).join(IS_MAC ? '' : '+')

export const SHORTCUT_BY_ID = Object.fromEntries(SHORTCUT_ACTIONS.map((action) => [action.id, action])) as Record<ShortcutId, ShortcutAction>
