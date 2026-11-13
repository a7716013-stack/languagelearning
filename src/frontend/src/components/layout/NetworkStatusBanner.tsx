import { WifiOff } from 'lucide-react'
import { useOnlineStatus } from '@/hooks/use-online-status'

export function NetworkStatusBanner() {
  const online = useOnlineStatus()
  if (online) return null

  return (
    <div
      role="status"
      aria-live="polite"
      className="sticky top-0 z-50 flex items-center justify-center gap-2 bg-warning px-4 py-2 text-sm font-medium text-black"
    >
      <WifiOff className="size-4" aria-hidden="true" />
      目前處於離線狀態，資料無法儲存。連線恢復後即可繼續使用。
    </div>
  )
}
