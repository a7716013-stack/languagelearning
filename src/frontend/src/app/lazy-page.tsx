import { type ComponentType, lazy, Suspense } from 'react'
import { PageLoadError } from '@/components/layout/PageLoadError'
import { LoadingState } from '@/components/layout/states'

/**
 * 延遲載入頁面元件，第一次進入該頁時才下載程式碼。
 * 下載失敗（例如剛好斷線）時顯示可重試的錯誤訊息，而不是讓整個應用程式進入錯誤畫面；
 * 版面（含網路狀態提示）因此仍然保留。
 */
export function lazyPage(load: () => Promise<ComponentType>) {
  const Page = lazy(async () => {
    try {
      return { default: await load() }
    } catch {
      return { default: PageLoadError }
    }
  })

  return (
    <Suspense fallback={<LoadingState className="p-6" />}>
      <Page />
    </Suspense>
  )
}
