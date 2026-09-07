import { forwardRef, type ButtonHTMLAttributes, type ReactNode } from 'react'

type Variant = 'primary' | 'secondary' | 'danger'
type Size = 'small' | 'medium' | 'large'

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant
  size?: Size
  loading?: boolean
  children: ReactNode
}

const variantClasses: Record<Variant, string> = {
  primary: 'bg-primary text-white hover:bg-blue-700',
  secondary: 'bg-white text-neutral border border-neutral/40 hover:bg-neutral/10',
  danger: 'bg-error text-white hover:bg-red-700',
}

const sizeClasses: Record<Size, string> = {
  small: 'h-8 px-3 text-sm',
  medium: 'h-10 px-4 text-sm',
  large: 'h-12 px-6 text-base',
}

export const Button = forwardRef<HTMLButtonElement, ButtonProps>(
  ({ variant = 'primary', size = 'medium', loading = false, disabled, children, className = '', ...rest }, ref) => {
    return (
      <button
        ref={ref}
        disabled={disabled || loading}
        aria-busy={loading}
        className={[
          'inline-flex cursor-pointer items-center justify-center gap-2 rounded-md font-medium transition-colors',
          'disabled:cursor-not-allowed disabled:opacity-50',
          variantClasses[variant],
          sizeClasses[size],
          className,
        ].join(' ')}
        {...rest}
      >
        {loading && (
          <span
            role="status"
            aria-label="loading"
            className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent"
          />
        )}
        {children}
      </button>
    )
  },
)

Button.displayName = 'Button'
