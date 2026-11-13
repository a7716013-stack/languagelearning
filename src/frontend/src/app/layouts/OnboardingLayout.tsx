import { LogOut } from 'lucide-react'
import { Outlet } from 'react-router'
import { BrandLogo } from '@/components/layout/BrandLogo'
import { NetworkStatusBanner } from '@/components/layout/NetworkStatusBanner'
import { SkipLink } from '@/components/layout/SkipLink'
import { ThemeToggle } from '@/components/layout/ThemeToggle'
import { Button } from '@/components/ui/button'
import { useLogout } from '@/hooks/use-logout'

export function OnboardingLayout() {
  const logout = useLogout()
  return (
    <div className="flex min-h-dvh flex-col">
      <SkipLink />
      <NetworkStatusBanner />
      <header className="flex items-center justify-between border-b px-4 py-3 sm:px-6">
        <BrandLogo to="/onboarding" />
        <div className="flex items-center gap-1">
          <ThemeToggle />
          <Button variant="ghost" size="sm" onClick={() => logout.mutate()} disabled={logout.isPending}>
            <LogOut aria-hidden="true" />
            登出
          </Button>
        </div>
      </header>
      <main id="main-content" tabIndex={-1} className="flex-1 px-4 py-8 sm:px-6">
        <div className="mx-auto max-w-2xl">
          <Outlet />
        </div>
      </main>
    </div>
  )
}
