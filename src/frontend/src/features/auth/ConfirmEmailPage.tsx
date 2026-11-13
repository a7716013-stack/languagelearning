import { useQuery } from '@tanstack/react-query'
import { CircleCheck, CircleX, LoaderCircle } from 'lucide-react'
import { Link, useSearchParams } from 'react-router'
import { Button } from '@/components/ui/button'
import { authApi } from '@/services/api'
import { AuthCard } from './AuthCard'
import { errorMessage } from './server-errors'

/** 讀取信件連結參數並送出驗證。以 query 呈現，避免 StrictMode 下重複送出。 */
export function ConfirmEmailPage() {
  const [params] = useSearchParams()
  const userId = params.get('userId') ?? ''
  const token = params.get('token') ?? ''
  const complete = Boolean(userId && token)

  const confirmation = useQuery({
    queryKey: ['confirm-email', userId, token],
    queryFn: () => authApi.confirmEmail({ userId, token }),
    enabled: complete,
    retry: false,
    staleTime: Infinity,
    gcTime: 0,
  })

  return (
    <AuthCard title="Email 驗證">
      <div className="flex flex-col items-center gap-4 py-2 text-center" aria-live="polite">
        {!complete ? (
          <>
            <CircleX className="size-10 text-destructive" aria-hidden="true" />
            <p className="text-muted-foreground">驗證連結不完整，請重新開啟信中的連結。</p>
          </>
        ) : confirmation.isPending ? (
          <>
            <LoaderCircle className="size-10 animate-spin text-primary" aria-hidden="true" />
            <p className="text-muted-foreground">正在驗證你的 Email…</p>
          </>
        ) : confirmation.isError ? (
          <>
            <CircleX className="size-10 text-destructive" aria-hidden="true" />
            <p className="text-muted-foreground">{errorMessage(confirmation.error)}</p>
            <Button asChild variant="outline">
              <Link to="/login">前往登入並重新寄送</Link>
            </Button>
          </>
        ) : (
          <>
            <CircleCheck className="size-10 text-success" aria-hidden="true" />
            <p>{confirmation.data.message}</p>
            <Button asChild>
              <Link to="/login">前往登入</Link>
            </Button>
          </>
        )}
      </div>
    </AuthCard>
  )
}
