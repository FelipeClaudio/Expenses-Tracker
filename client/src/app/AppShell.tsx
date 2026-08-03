import type { ReactNode } from 'react'
import { Link } from 'react-router'

const navItems = [{ to: '/topics', label: 'Topics' }]

function NavLinks({ itemClassName }: { itemClassName: string }) {
  return (
    <>
      {navItems.map((item) => (
        <Link key={item.to} to={item.to} className={itemClassName}>
          {item.label}
        </Link>
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
        className="hidden md:flex md:w-56 md:shrink-0 md:flex-col md:gap-1 md:border-r md:border-neutral-200 md:p-4 dark:md:border-neutral-800"
      >
        <NavLinks itemClassName="min-h-11 flex items-center rounded-md px-3 text-sm font-medium hover:bg-neutral-100 dark:hover:bg-neutral-800" />
      </nav>

      <main className="flex-1 p-4 pb-20 md:pb-4">{children}</main>

      <nav
        data-testid="bottom-nav"
        aria-label="Primary"
        className="fixed inset-x-0 bottom-0 flex border-t border-neutral-200 bg-white md:hidden dark:border-neutral-800 dark:bg-neutral-950"
      >
        <NavLinks itemClassName="min-h-11 min-w-11 flex flex-1 items-center justify-center text-sm font-medium" />
      </nav>
    </div>
  )
}
