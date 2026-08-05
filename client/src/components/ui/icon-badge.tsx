import type { HTMLAttributes } from 'react'
import { cva, type VariantProps } from 'class-variance-authority'
import { cn } from '../../lib/cn'

const iconBadgeVariants = cva('flex size-10 shrink-0 items-center justify-center rounded-full', {
  variants: {
    variant: {
      positive: 'bg-mint-100 text-mint-600 dark:bg-mint-500/20 dark:text-mint-300',
      negative: 'bg-rose-100 text-rose-600 dark:bg-rose-500/20 dark:text-rose-300',
      neutral: 'bg-navy-100 text-navy-700 dark:bg-navy-700 dark:text-navy-100',
    },
  },
  defaultVariants: {
    variant: 'neutral',
  },
})

export interface IconBadgeProps extends HTMLAttributes<HTMLSpanElement>, VariantProps<typeof iconBadgeVariants> {}

// Circular tinted badge for list rows (transactions, balances, subtopics) —
// mirrors the reference design's per-row icon chips.
export function IconBadge({ className, variant, ...props }: IconBadgeProps) {
  return <span className={cn(iconBadgeVariants({ variant }), className)} {...props} />
}

const glyphProps = {
  viewBox: '0 0 24 24',
  fill: 'none',
  stroke: 'currentColor',
  strokeWidth: 2.5,
  strokeLinecap: 'round' as const,
  strokeLinejoin: 'round' as const,
  className: 'size-4',
  'aria-hidden': true,
}

export function ArrowUpGlyph() {
  return (
    <svg {...glyphProps}>
      <path d="M12 19V5M5 12l7-7 7 7" />
    </svg>
  )
}

export function ArrowDownGlyph() {
  return (
    <svg {...glyphProps}>
      <path d="M12 5v14M19 12l-7 7-7-7" />
    </svg>
  )
}
