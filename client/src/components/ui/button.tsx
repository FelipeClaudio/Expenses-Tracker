import { type ButtonHTMLAttributes, forwardRef } from 'react'
import { cva, type VariantProps } from 'class-variance-authority'
import { cn } from '../../lib/cn'

export const buttonVariants = cva(
  'inline-flex items-center justify-center gap-2 whitespace-nowrap rounded-full text-sm font-semibold transition-colors disabled:pointer-events-none disabled:opacity-50 min-h-11 px-5',
  {
    variants: {
      variant: {
        default: 'bg-mint-500 text-navy-900 hover:bg-mint-400 dark:bg-mint-400 dark:text-navy-950 dark:hover:bg-mint-300',
        outline: 'border border-navy-100 bg-transparent text-navy-900 hover:bg-navy-50 dark:border-navy-700 dark:text-white dark:hover:bg-navy-800',
        ghost: 'bg-transparent text-navy-900 hover:bg-navy-50 dark:text-white dark:hover:bg-navy-800',
      },
    },
    defaultVariants: {
      variant: 'default',
    },
  },
)

export interface ButtonProps
  extends ButtonHTMLAttributes<HTMLButtonElement>,
    VariantProps<typeof buttonVariants> {}

export const Button = forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, variant, type = 'button', ...props }, ref) => (
    <button ref={ref} type={type} className={cn(buttonVariants({ variant }), className)} {...props} />
  ),
)
Button.displayName = 'Button'
