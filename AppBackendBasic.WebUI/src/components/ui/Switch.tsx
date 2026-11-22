import { forwardRef, type ButtonHTMLAttributes } from 'react'
import { cn } from '@/lib/utils'

/**
 * iOS-style on/off switch: a pill track with a white knob that slides across.
 * Use it for anything that is simply on or off; named choices (e.g. "In front / Behind") stay as segmented buttons.
 *
 * Colours come from two CSS variables so each area can theme it without new props:
 *   --switch-on  (default iOS green #34C759)   --switch-off (default iOS light grey #E9E9EB)
 * The caption studio sets them from its theme (see studio.css, .ct-scope).
 *
 * It is a <button role="switch">, so Space/Enter toggle it, and wrapping it in a <label> makes the label text clickable too.
 */
export interface SwitchProps extends Omit<ButtonHTMLAttributes<HTMLButtonElement>, 'onChange' | 'role' | 'type'> {
  checked: boolean
  onCheckedChange: (checked: boolean) => void
  size?: 'sm' | 'md'
}

export const Switch = forwardRef<HTMLButtonElement, SwitchProps>(({ checked, onCheckedChange, size = 'md', className, onClick, ...props }, ref) => (
  <button
    ref={ref}
    type="button"
    role="switch"
    aria-checked={checked}
    data-state={checked ? 'on' : 'off'}
    onClick={(e) => {
      onClick?.(e)
      if (!e.defaultPrevented) onCheckedChange(!checked)
    }}
    className={cn(
      'relative inline-flex shrink-0 cursor-pointer items-center rounded-full p-[2px] align-middle transition-colors duration-200 ease-out',
      'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--switch-on,#34C759)] focus-visible:ring-offset-1',
      'disabled:cursor-not-allowed disabled:opacity-45',
      size === 'sm' ? 'h-[22px] w-[38px]' : 'h-[28px] w-[48px]',
      checked ? 'bg-[var(--switch-on,#34C759)]' : 'bg-[var(--switch-off,#E9E9EB)]',
      className,
    )}
    {...props}
  >
    <span
      aria-hidden="true"
      className={cn(
        'pointer-events-none block rounded-full bg-white shadow-[0_2px_4px_rgba(0,0,0,.22),0_0_1px_rgba(0,0,0,.2)] transition-transform duration-200 ease-out',
        size === 'sm' ? 'h-[18px] w-[18px]' : 'h-6 w-6',
        checked ? (size === 'sm' ? 'translate-x-4' : 'translate-x-5') : 'translate-x-0',
      )}
    />
  </button>
))
Switch.displayName = 'Switch'
