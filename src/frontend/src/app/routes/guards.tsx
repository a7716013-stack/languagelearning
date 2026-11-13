import { Navigate, Outlet, useLocation } from 'react-router'
import { ErrorState, FullPageLoading } from '@/components/layout/states'
import { useCurrentUser } from '@/hooks/use-current-user'
import { homePathFor } from '@/lib/home-path'
import { ForbiddenPage } from './ErrorPages'

/** 需要登入；未登入時導向登入頁並保留原本要前往的路徑。 */
export function RequireAuth() {
  const { data: user, isPending, isError, refetch } = useCurrentUser()
  const location = useLocation()

  if (isPending) return <FullPageLoading />
  if (isError) {
    return (
      <div className="mx-auto max-w-md p-6">
        <ErrorState title="無法確認登入狀態" onRetry={() => void refetch()} />
      </div>
    )
  }
  if (!user) {
    const returnTo = encodeURIComponent(location.pathname + location.search)
    return <Navigate to={`/login?returnTo=${returnTo}`} replace />
  }
  return <Outlet />
}

/** 已登入使用者不需要看到登入 / 註冊頁。 */
export function GuestOnly() {
  const { data: user, isPending } = useCurrentUser()
  if (isPending) return <FullPageLoading />
  if (user) return <Navigate to={homePathFor(user)} replace />
  return <Outlet />
}

/** 尚未完成初次設定時導向 Onboarding。 */
export function RequireOnboarding() {
  const { data: user } = useCurrentUser()
  if (user && !user.onboardingCompleted) return <Navigate to="/onboarding" replace />
  return <Outlet />
}

export function RequireRole({ role }: { role: string }) {
  const { data: user } = useCurrentUser()
  if (!user?.roles.includes(role)) return <ForbiddenPage />
  return <Outlet />
}
