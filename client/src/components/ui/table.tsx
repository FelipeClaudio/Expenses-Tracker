import type { HTMLAttributes, TdHTMLAttributes, ThHTMLAttributes } from 'react'
import { cn } from '../../lib/cn'

export function Table({ className, ...props }: HTMLAttributes<HTMLTableElement>) {
  return (
    <table className={cn('w-full border-collapse text-left text-sm', className)} {...props} />
  )
}

export function TableHeader({ className, ...props }: HTMLAttributes<HTMLTableSectionElement>) {
  return <thead className={cn('border-b border-neutral-200 dark:border-neutral-800', className)} {...props} />
}

export function TableBody({ className, ...props }: HTMLAttributes<HTMLTableSectionElement>) {
  return <tbody className={className} {...props} />
}

export function TableRow({ className, ...props }: HTMLAttributes<HTMLTableRowElement>) {
  return <tr className={cn('border-b border-neutral-100 last:border-0 dark:border-neutral-900', className)} {...props} />
}

export function TableHead({ className, ...props }: ThHTMLAttributes<HTMLTableCellElement>) {
  return <th className={cn('px-3 py-2 font-medium text-neutral-500 dark:text-neutral-400', className)} {...props} />
}

export function TableCell({ className, ...props }: TdHTMLAttributes<HTMLTableCellElement>) {
  return <td className={cn('px-3 py-2', className)} {...props} />
}
