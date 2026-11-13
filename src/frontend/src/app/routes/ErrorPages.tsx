import { Search, ShieldCheck } from 'lucide-react'
import type { ReactNode } from 'react'
import { Link } from 'react-router'
import { Button } from '@/components/ui/button'
import { useDocumentTitle } from '@/hooks/use-document-title'

function ErrorPage({ code, title, message, icon }: { code: string; title: string; message: string; icon: ReactNode }) {
  useDocumentTitle(title)
  return (
    <div className="mx-auto flex min-h-[60dvh] max-w-md flex-col items-center justify-center gap-4 p-6 text-center">
      <div className="flex size-14 items-center justify-center rounded-full bg-secondary text-secondary-foreground">
        {icon}
      </div>
      <p className="text-sm font-semibold text-primary">{code}</p>
      <h1 className="text-2xl font-bold">{title}</h1>
      <p className="text-muted-foreground">{message}</p>
      <Button asChild>
        <Link to="/">回到首頁</Link>
      </Button>
    </div>
  )
}

export function NotFoundPage() {
  return (
    <ErrorPage
      code="404"
      title="找不到頁面"
      message="您要找的頁面不存在或已移除。"
      icon={<Search className="size-6" aria-hidden="true" />}
    />
  )
}

export function ForbiddenPage() {
  return (
    <ErrorPage
      code="403"
      title="沒有權限"
      message="此頁面僅限管理者使用。"
      icon={<ShieldCheck className="size-6" aria-hidden="true" />}
    />
  )
}
