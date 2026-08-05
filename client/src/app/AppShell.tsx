import type { ReactNode } from 'react'
import { NavLink } from 'react-router'
import { cn } from '../lib/cn'

const navItems = [{ to: '/topics', label: 'Topics' }]

function NavLinks({ itemClassName, activeClassName }: { itemClassName: string; activeClassName: string }) {
  return (
    <>
      {navItems.map((item) => (
        <NavLink
          key={item.to}
          to={item.to}
          className={({ isActive }) => cn(itemClassName, isActive && activeClassName)}
        >
          {item.label}
        </NavLink>
      ))}
    </>
  )
}

export function AppShell({ children }: { children: ReactNode }) {
  return (
    <div className="flex min-h-svh flex-col md:flex-row">
      <nav
        data-testid="sidebar-nav"
        aria-label="Primary"
        className="hidden md:flex md:w-56 md:shrink-0 md:flex-col md:gap-1 md:border-r md:border-navy-100 md:bg-white md:p-4 dark:md:border-navy-800 dark:md:bg-navy-900"
      >
        <div className="mb-6 flex items-center gap-2 px-3">
          <span className="size-2.5 rounded-full bg-mint-500" />
          <span className="text-sm font-bold tracking-wide text-navy-900 dark:text-white">EXPENSES</span>
        </div>
        <NavLinks
          itemClassName="min-h-11 flex items-center rounded-full px-4 text-sm font-medium text-navy-400 hover:bg-navy-50 dark:text-navy-300 dark:hover:bg-navy-800"
          activeClassName="!bg-mint-100 !text-navy-900 dark:!bg-mint-500/20 dark:!text-mint-300"
        />
      </nav>

      <main className="flex-1 p-4 pb-24 md:pb-4">{children}</main>

      <nav
        data-testid="bottom-nav"
        aria-label="Primary"
        className="fixed inset-x-4 bottom-4 flex justify-center rounded-full bg-white shadow-lg md:hidden dark:bg-navy-900"
      >
        <NavLinks
          itemClassName="min-h-11 min-w-11 flex flex-1 items-center justify-center rounded-full text-sm font-medium text-navy-400 dark:text-navy-300"
          activeClassName="!bg-mint-100 !text-navy-900 dark:!bg-mint-500/20 dark:!text-mint-300"
        />
      </nav>
    </div>
  )
}
