import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { LoaderCircle, Mail } from 'lucide-react'
import { useForm } from 'react-hook-form'
import { Link, useNavigate, useSearchParams } from 'react-router'
import { useTheme } from '@/app/providers/theme-context'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { fieldProps, FormField } from '@/components/ui/form-field'
import { Input } from '@/components/ui/input'
import { homePathFor } from '@/lib/home-path'
import { sanitizeReturnTo } from '@/lib/safe-redirect'
import { authApi } from '@/services/api'
import { isApiError } from '@/services/api-client'
import { queryKeys } from '@/services/query-keys'
import { AuthCard } from './AuthCard'
import { PasswordInput } from './PasswordInput'
import { loginSchema, type LoginValues } from './schemas'
import { applyServerFieldErrors, errorMessage } from './server-errors'

export function LoginPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { setTheme } = useTheme()
  const [searchParams] = useSearchParams()
  const returnTo = searchParams.get('returnTo')

  const form = useForm<LoginValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: '', password: '', rememberMe: false },
  })
  const {
    register,
    handleSubmit,
    getValues,
    setError,
    formState: { errors },
  } = form

  const login = useMutation({
    mutationFn: authApi.login,
    onSuccess: (user) => {
      queryClient.setQueryData(queryKeys.me, user)
      setTheme(user.uiTheme)
      const target = returnTo ? sanitizeReturnTo(returnTo, homePathFor(user)) : homePathFor(user)
      void navigate(target, { replace: true })
    },
    onError: (error) => {
      applyServerFieldErrors(error, setError)
    },
  })

  const resend = useMutation({ mutationFn: authApi.resendConfirmation })
  const notConfirmed = isApiError(login.error) && login.error.code === 'email_not_confirmed'
  const hasFieldErrors = isApiError(login.error) && Object.keys(login.error.fieldErrors).length > 0
  const generalError = login.error && !hasFieldErrors ? login.error : null

  return (
    <AuthCard
      title="登入"
      description="歡迎回來！登入後繼續你的檢定準備。"
      footer={
        <span>
          還沒有帳號？{' '}
          <Link to="/register" className="font-medium text-primary underline-offset-4 hover:underline">
            免費註冊
          </Link>
        </span>
      }
    >
      <form className="grid gap-4" noValidate onSubmit={handleSubmit((values) => login.mutate(values))}>
        {generalError ? (
          <Alert variant="destructive">
            <AlertTitle>{notConfirmed ? '尚未完成 Email 驗證' : '登入失敗'}</AlertTitle>
            <AlertDescription>
              <p>{errorMessage(generalError)}</p>
              {notConfirmed ? (
                <Button
                  variant="link"
                  className="h-auto p-0"
                  disabled={resend.isPending || resend.isSuccess}
                  onClick={() => resend.mutate(getValues('email'))}
                >
                  <Mail aria-hidden="true" />
                  {resend.isSuccess ? '已重新寄出驗證信' : '重新寄送驗證信'}
                </Button>
              ) : null}
            </AlertDescription>
          </Alert>
        ) : null}

        <FormField id="email" label="Email" error={errors.email?.message}>
          <Input
            type="email"
            autoComplete="email"
            inputMode="email"
            {...fieldProps('email', errors.email?.message)}
            {...register('email')}
          />
        </FormField>

        <FormField id="password" label="密碼" error={errors.password?.message}>
          <PasswordInput
            autoComplete="current-password"
            {...fieldProps('password', errors.password?.message)}
            {...register('password')}
          />
        </FormField>

        <div className="-mt-2 flex justify-end">
          <Link to="/forgot-password" className="text-sm text-primary underline-offset-4 hover:underline">
            忘記密碼？
          </Link>
        </div>

        <label className="flex items-center gap-2 text-sm">
          <input type="checkbox" className="size-4 accent-[var(--primary)]" {...register('rememberMe')} />
          在這台裝置上保持登入
        </label>

        <Button type="submit" size="lg" disabled={login.isPending}>
          {login.isPending ? <LoaderCircle className="animate-spin" aria-hidden="true" /> : null}
          登入
        </Button>
      </form>
    </AuthCard>
  )
}
