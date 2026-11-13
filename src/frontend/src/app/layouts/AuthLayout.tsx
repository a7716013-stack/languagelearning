import { Outlet } from 'react-router'
import { BrandLogo } from '@/components/layout/BrandLogo'
import { NetworkStatusBanner } from '@/components/layout/NetworkStatusBanner'
import { SkipLink } from '@/components/layout/SkipLink'
import { ThemeToggle } from '@/components/layout/ThemeToggle'

/** 登入、註冊等頁面：置中卡片版面。 */
export function AuthLayout() {
  return (
    <div className="flex min-h-dvh flex-col bg-gradient-to-b from-secondary/60 to-background">
      <SkipLink />
      <NetworkStatusBanner />
      <header className="flex items-center justify-between px-4 py-4 sm:px-6">
        <BrandLogo />
        <ThemeToggle />
      </header>
      <main id="main-content" tabIndex={-1} className="flex flex-1 items-start justify-center px-4 pt-6 pb-12 sm:pt-12">
        <div className="w-full max-w-md">
          <Outlet />
        </div>
      </main>
    </div>
  )
}
