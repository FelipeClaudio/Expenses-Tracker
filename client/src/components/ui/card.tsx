import type { HTMLAttributes } from 'react'
import { cn } from '../../lib/cn'

export function Card({ className, ...props }: HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn('rounded-2xl bg-white p-4 shadow-sm dark:bg-navy-800 dark:shadow-none', className)}
      {...props}
    />
  )
}

// Dark, high-contrast feature card — mirrors the reference's "Hi, Olivia" /
// "Total Balance" cards. Bespoke layouts (greeting, stat pills, big
// figures) live in the callers rather than in CardTitle/CardContent, since
// those assume the light Card's color scheme.
export function HeroCard({ className, ...props }: HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn('rounded-3xl bg-navy-900 p-6 text-white shadow-lg dark:bg-navy-700', className)}
      {...props}
    />
  )
}

export function CardHeader({ className, ...props }: HTMLAttributes<HTMLDivElement>) {
  return <div className={cn('mb-2 flex items-center justify-between gap-2', className)} {...props} />
}

export function CardTitle({ className, ...props }: HTMLAttributes<HTMLHeadingElement>) {
  return <h3 className={cn('text-base font-semibold text-navy-900 dark:text-white', className)} {...props} />
}

export function CardContent({ className, ...props }: HTMLAttributes<HTMLDivElement>) {
  return <div className={cn('text-sm text-navy-400 dark:text-navy-300', className)} {...props} />
}
