import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation } from '@tanstack/react-query'
import { LoaderCircle } from 'lucide-react'
import { useForm } from 'react-hook-form'
import { Link, useSearchParams } from 'react-router'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { fieldProps, FormField } from '@/components/ui/form-field'
import { AuthCard } from './AuthCard'
import { PasswordInput } from './PasswordInput'
import { resetPasswordSchema, type ResetPasswordValues } from './schemas'
import { applyServerFieldErrors, errorMessage } from './server-errors'
import { authApi } from '@/services/api'

export function ResetPasswordPage() {
  const [params] = useSearchParams()
  const userId = params.get('userId') ?? ''
  const token = params.get('token') ?? ''
  const linkComplete = Boolean(userId && token)

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<ResetPasswordValues>({
    resolver: zodResolver(resetPasswordSchema),
    defaultValues: { newPassword: '', confirmPassword: '' },
  })

  const mutation = useMutation({
    mutationFn: (values: ResetPasswordValues) =>
      authApi.resetPassword({ userId, token, newPassword: values.newPassword }),
    onError: (error) => {
      applyServerFieldErrors(error, setError)
    },
  })

  return (
    <AuthCard
      title="重設密碼"
      description="請設定新的密碼。"
      footer={<Link to="/login" className="text-primary hover:underline">返回登入</Link>}
    >
      {!linkComplete ? (
        <Alert variant="destructive">
          <AlertTitle>連結不完整</AlertTitle>
          <AlertDescription>
            請重新開啟信中的連結，或 <Link to="/forgot-password" className="underline">重新申請</Link>。
          </AlertDescription>
        </Alert>
      ) : mutation.isSuccess ? (
        <Alert variant="success">
          <AlertTitle>密碼已重設</AlertTitle>
          <AlertDescription>
            <p>{mutation.data.message}</p>
            <Button asChild className="mt-3">
              <Link to="/login">前往登入</Link>
            </Button>
          </AlertDescription>
        </Alert>
      ) : (
        <form className="grid gap-4" noValidate onSubmit={handleSubmit((values) => mutation.mutate(values))}>
          {mutation.isError && Object.keys(errors).length === 0 ? (
            <Alert variant="destructive">
              <AlertTitle>無法重設密碼</AlertTitle>
              <AlertDescription>
                <p>{errorMessage(mutation.error)}</p>
                <Link to="/forgot-password" className="underline">
                  重新申請重設連結
                </Link>
              </AlertDescription>
            </Alert>
          ) : null}
          <FormField
            id="newPassword"
            label="新密碼"
            error={errors.newPassword?.message}
            description="至少 10 個字元，需包含大寫、小寫英文字母與數字。"
          >
            <PasswordInput
              autoComplete="new-password"
              {...fieldProps('newPassword', errors.newPassword?.message, true)}
              {...register('newPassword')}
            />
          </FormField>
          <FormField id="confirmPassword" label="確認新密碼" error={errors.confirmPassword?.message}>
            <PasswordInput
              autoComplete="new-password"
              {...fieldProps('confirmPassword', errors.confirmPassword?.message)}
              {...register('confirmPassword')}
            />
          </FormField>
          <Button type="submit" size="lg" disabled={mutation.isPending}>
            {mutation.isPending ? <LoaderCircle className="animate-spin" aria-hidden="true" /> : null}
            重設密碼
          </Button>
        </form>
      )}
    </AuthCard>
  )
}
