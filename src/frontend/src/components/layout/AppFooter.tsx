import { APP_VERSION } from '@/lib/version'

export function AppFooter() {
  return (
    <footer className="mt-12 border-t py-6 text-center text-xs text-muted-foreground">
      <p>LanguageLearning v{APP_VERSION}</p>
      <p className="mt-1">
        TOEIC 為 ETS 的註冊商標。本平台與 ETS 無隸屬或合作關係，所有練習內容均為自行編寫。
      </p>
    </footer>
  )
}
