import { ButtonHTMLAttributes, forwardRef } from 'react'
import { cn } from '@/lib/utils'

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'default' | 'outline' | 'ghost' | 'link' | 'sun'
  size?: 'default' | 'sm' | 'lg'
}

/** Flat, high-contrast button: solid fill, ink outline, hard offset shadow. No gradients. */
const Button = forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, variant = 'default', size = 'default', ...props }, ref) => {
    return (
      <button
        className={cn(
          'inline-flex items-center justify-center gap-2 rounded-lg font-semibold transition-all focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ink focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50',
          {
            'border-2 border-ink bg-cherry text-white shadow-hard-sm hover:-translate-y-0.5 hover:shadow-hard active:translate-y-0 active:shadow-none':
              variant === 'default',
            'border-2 border-ink bg-sun text-ink shadow-hard-sm hover:-translate-y-0.5 hover:shadow-hard active:translate-y-0 active:shadow-none':
              variant === 'sun',
            'border-2 border-ink bg-white text-ink shadow-hard-sm hover:-translate-y-0.5 hover:shadow-hard active:translate-y-0 active:shadow-none':
              variant === 'outline',
            'text-ink hover:bg-butter': variant === 'ghost',
            'text-cherry underline underline-offset-4 hover:text-ink': variant === 'link',
          },
          {
            'h-10 px-5 py-2 text-sm': size === 'default',
            'h-9 rounded-md px-4 text-sm': size === 'sm',
            'h-12 px-7 text-base': size === 'lg',
          },
          className
        )}
        ref={ref}
        {...props}
      />
    )
  }
)
Button.displayName = 'Button'

export { Button }
export default Button
