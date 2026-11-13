import { useMutation } from '@tanstack/react-query'
import { MailCheck } from 'lucide-react'
import { Link, useLocation } from 'react-router'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { authApi } from '@/services/api'
import { AuthCard } from './AuthCard'
import { errorMessage } from './server-errors'

export function CheckEmailPage() {
  const location = useLocation()
  const email = (location.state as { email?: string } | null)?.email

  const resend = useMutation({
    mutationFn: (address: string) => authApi.resendConfirmation(address),
    onSuccess: (response) => toast.success(response.message),
    onError: (error) => toast.error(errorMessage(error)),
  })

  return (
    <AuthCard title="請查看你的信箱" footer={<Link to="/login" className="text-primary hover:underline">返回登入</Link>}>
      <div className="flex flex-col items-center gap-4 text-center">
        <div className="flex size-14 items-center justify-center rounded-full bg-secondary text-secondary-foreground">
          <MailCheck className="size-7" aria-hidden="true" />
        </div>
        <p className="text-sm leading-relaxed text-muted-foreground">
          {email ? (
            <>
              我們已寄出驗證信到 <strong className="text-foreground">{email}</strong>。
            </>
          ) : (
            '我們已寄出驗證信。'
          )}
          請點擊信中的連結完成 Email 驗證，驗證後即可登入。
        </p>
        <p className="text-xs text-muted-foreground">沒收到信？請檢查垃圾郵件匣，或重新寄送。</p>
        {email ? (
          <Button
            variant="outline"
            onClick={() => resend.mutate(email)}
            disabled={resend.isPending || resend.isSuccess}
          >
            {resend.isSuccess ? '已重新寄出' : '重新寄送驗證信'}
          </Button>
        ) : null}
      </div>
    </AuthCard>
  )
}
