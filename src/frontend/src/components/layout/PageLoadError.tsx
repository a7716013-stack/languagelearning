import { reloadPage } from '@/lib/reload-page'
import { ErrorState } from './states'

/** 頁面程式碼下載失敗（例如剛好斷線）時顯示，保留原本的版面與網路狀態提示。 */
export function PageLoadError() {
  return (
    <ErrorState
      className="m-6"
      title="無法載入頁面"
      message="請確認網路連線後重新載入。"
      onRetry={reloadPage}
    />
  )
}
