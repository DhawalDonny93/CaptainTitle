import { forwardRef, useEffect, type ButtonHTMLAttributes, type InputHTMLAttributes, type ReactNode } from 'react'
import { cn } from '@/lib/utils'

// Small, dependency-free primitives for the studio (the old app used shadcn/Radix; the template does not ship those).

type Variant = 'default' | 'outline' | 'ghost'
interface CtButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant
  size?: 'default' | 'icon'
}

export const CtButton = forwardRef<HTMLButtonElement, CtButtonProps>(({ className, variant = 'default', size = 'default', ...props }, ref) => (
  <button
    ref={ref}
    className={cn(
      'inline-flex items-center justify-center gap-2 rounded-md text-sm font-semibold transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--ct-accent)] disabled:pointer-events-none disabled:opacity-50',
      variant === 'default' && 'bg-[var(--ct-accent)] text-[var(--ct-on-accent)] hover:brightness-110',
      variant === 'outline' && 'border border-[var(--ct-border)] bg-transparent text-[var(--ct-text)] hover:bg-[var(--ct-hover)]',
      variant === 'ghost' && 'bg-transparent text-[var(--ct-text)] hover:bg-[var(--ct-hover)]',
      size === 'default' ? 'h-10 px-4' : 'h-10 w-10',
      className,
    )}
    {...props}
  />
))
CtButton.displayName = 'CtButton'

export const CtInput = forwardRef<HTMLInputElement, InputHTMLAttributes<HTMLInputElement>>(({ className, ...props }, ref) => (
  <input
    ref={ref}
    className={cn('h-10 w-full rounded-md border border-[var(--ct-border)] bg-[var(--ct-field)] px-3 text-sm text-[var(--ct-text)] placeholder:text-[var(--ct-faint)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--ct-accent)]', className)}
    {...props}
  />
))
CtInput.displayName = 'CtInput'

interface CtSliderProps {
  min: number
  max: number
  step?: number
  value: number[]
  disabled?: boolean
  onValueChange: (value: number[]) => void
  'data-testid'?: string
}

/** Single-thumb slider with the same `value=[n]` / `onValueChange([n])` contract the ported panels were written against. */
export function CtSlider({ min, max, step = 1, value, disabled, onValueChange, ...rest }: CtSliderProps) {
  return (
    <input
      type="range"
      className="ct-slider"
      min={min}
      max={max}
      step={step}
      value={value[0]}
      disabled={disabled}
      onChange={(e) => onValueChange([Number(e.target.value)])}
      data-testid={rest['data-testid']}
    />
  )
}

interface CtDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  className?: string
  children: ReactNode
  'data-testid'?: string
}

export function CtDialog({ open, onOpenChange, className, children, ...rest }: CtDialogProps) {
  useEffect(() => {
    if (!open) return undefined
    const onKey = (e: KeyboardEvent) => { if (e.key === 'Escape') onOpenChange(false) }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [open, onOpenChange])
  if (!open) return null
  return (
    <div className="fixed inset-0 z-[60] grid place-items-center bg-black/70 backdrop-blur-sm p-4" onMouseDown={() => onOpenChange(false)}>
      <div role="dialog" aria-modal="true" className={cn('ct-scope max-h-[90vh] w-full max-w-3xl overflow-auto rounded-lg border border-[var(--ct-border)] bg-[var(--ct-panel)] p-6 text-[var(--ct-text)]', className)}
        onMouseDown={(e) => e.stopPropagation()} data-testid={rest['data-testid']}>
        {children}
      </div>
    </div>
  )
}
