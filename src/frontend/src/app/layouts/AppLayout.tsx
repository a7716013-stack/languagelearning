import { LayoutDashboard, LogOut, Settings, ShieldCheck } from 'lucide-react'
import { NavLink, Outlet } from 'react-router'
import { AppFooter } from '@/components/layout/AppFooter'
import { BrandLogo } from '@/components/layout/BrandLogo'
import { NetworkStatusBanner } from '@/components/layout/NetworkStatusBanner'
import { SkipLink } from '@/components/layout/SkipLink'
import { ThemeToggle } from '@/components/layout/ThemeToggle'
import { Button } from '@/components/ui/button'
import { useAuthenticatedUser } from '@/hooks/use-current-user'
import { useLogout } from '@/hooks/use-logout'
import { cn } from '@/lib/utils'

interface NavItem {
  to: string
  label: string
  icon: typeof LayoutDashboard
  adminOnly?: boolean
}

const NAV_ITEMS: NavItem[] = [
  { to: '/dashboard', label: '學習首頁', icon: LayoutDashboard },
  { to: '/settings', label: '設定', icon: Settings },
  { to: '/admin', label: '管理後台', icon: ShieldCheck, adminOnly: true },
]

/** 登入後的主要版面：桌面左側導覽，手機底部導航。 */
export function AppLayout() {
  const user = useAuthenticatedUser()
  const logout = useLogout()
  const items = NAV_ITEMS.filter((item) => !item.adminOnly || user.roles.includes('Admin'))

  return (
    <div className="min-h-dvh">
      <SkipLink />
      <NetworkStatusBanner />

      <aside className="fixed inset-y-0 left-0 z-30 hidden w-64 flex-col border-r bg-sidebar md:flex">
        <div className="px-5 py-5">
          <BrandLogo to="/dashboard" />
        </div>
        <nav aria-label="主要導覽" className="flex-1 space-y-1 px-3">
          {items.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) =>
                cn(
                  'flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition-colors hover:bg-accent hover:text-accent-foreground',
                  isActive ? 'bg-accent text-accent-foreground' : 'text-muted-foreground',
                )
              }
            >
              <item.icon className="size-5" aria-hidden="true" />
              {item.label}
            </NavLink>
          ))}
        </nav>
        <div className="space-y-3 border-t p-4">
          <div className="min-w-0">
            <p className="truncate text-sm font-medium">{user.displayName}</p>
            <p className="truncate text-xs text-muted-foreground">{user.email}</p>
          </div>
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              className="flex-1"
              onClick={() => logout.mutate()}
              disabled={logout.isPending}
            >
              <LogOut aria-hidden="true" />
              登出
            </Button>
            <ThemeToggle />
          </div>
        </div>
      </aside>

      <header className="sticky top-0 z-30 flex items-center justify-between border-b bg-background/95 px-4 py-3 backdrop-blur md:hidden">
        <BrandLogo to="/dashboard" />
        <div className="flex items-center gap-1">
          <ThemeToggle />
          <Button
            variant="ghost"
            size="icon"
            onClick={() => logout.mutate()}
            disabled={logout.isPending}
            aria-label="登出"
          >
            <LogOut />
          </Button>
        </div>
      </header>

      <main id="main-content" tabIndex={-1} className="pb-24 md:pb-0 md:pl-64">
        <div className="mx-auto max-w-5xl px-4 py-6 sm:px-6 md:py-10">
          <Outlet />
          <AppFooter />
        </div>
      </main>

      <nav
        aria-label="主要導覽（行動版）"
        className="fixed inset-x-0 bottom-0 z-30 border-t bg-background/95 pb-[env(safe-area-inset-bottom)] backdrop-blur md:hidden"
      >
        <ul className="grid" style={{ gridTemplateColumns: `repeat(${items.length}, minmax(0, 1fr))` }}>
          {items.map((item) => (
            <li key={item.to}>
              <NavLink
                to={item.to}
                className={({ isActive }) =>
                  cn(
                    'flex flex-col items-center gap-1 py-2.5 text-xs font-medium',
                    isActive ? 'text-primary' : 'text-muted-foreground',
                  )
                }
              >
                <item.icon className="size-5" aria-hidden="true" />
                {item.label}
              </NavLink>
            </li>
          ))}
        </ul>
      </nav>

    </div>
  )
}
