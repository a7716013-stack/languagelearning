import { RefreshCw } from 'lucide-react'
import { useRegisterSW } from 'virtual:pwa-register/react'
import { Button } from '@/components/ui/button'

/** Service Worker 有新版本時提示使用者重新載入。 */
export function UpdatePrompt() {
  const {
    needRefresh: [needRefresh, setNeedRefresh],
    updateServiceWorker,
  } = useRegisterSW()

  if (!needRefresh) return null

  return (
    <div
      role="status"
      aria-live="polite"
      className="fixed inset-x-4 bottom-20 z-50 mx-auto flex max-w-md items-center gap-3 rounded-xl border bg-card p-4 text-card-foreground shadow-lg md:bottom-6"
    >
      <RefreshCw className="size-5 shrink-0 text-primary" aria-hidden="true" />
      <p className="flex-1 text-sm">有新版本可以使用。</p>
      <Button size="sm" variant="ghost" onClick={() => setNeedRefresh(false)}>
        稍後
      </Button>
      <Button size="sm" onClick={() => void updateServiceWorker(true)}>
        重新載入
      </Button>
    </div>
  )
}
