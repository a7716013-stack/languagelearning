import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation } from '@tanstack/react-query'
import { LoaderCircle } from 'lucide-react'
import { useForm } from 'react-hook-form'
import { Link, useNavigate } from 'react-router'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { fieldProps, FormField } from '@/components/ui/form-field'
import { Input } from '@/components/ui/input'
import { authApi } from '@/services/api'
import { AuthCard } from './AuthCard'
import { PasswordInput } from './PasswordInput'
import { registerSchema, type RegisterValues } from './schemas'
import { applyServerFieldErrors, errorMessage } from './server-errors'

export function RegisterPage() {
  const navigate = useNavigate()
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<RegisterValues>({
    resolver: zodResolver(registerSchema),
    defaultValues: { displayName: '', email: '', password: '', confirmPassword: '' },
  })

  const mutation = useMutation({
    mutationFn: (values: RegisterValues) =>
      authApi.register({ email: values.email, password: values.password, displayName: values.displayName }),
    onSuccess: (_, values) => {
      void navigate('/register/check-email', { state: { email: values.email } })
    },
    onError: (error) => {
      applyServerFieldErrors(error, setError)
    },
  })

  return (
    <AuthCard
      title="建立帳號"
      description="免費註冊，設定你的 TOEIC 目標分數與考試日期。"
      footer={
        <span>
          已經有帳號了？{' '}
          <Link to="/login" className="font-medium text-primary underline-offset-4 hover:underline">
            登入
          </Link>
        </span>
      }
    >
      <form className="grid gap-4" noValidate onSubmit={handleSubmit((values) => mutation.mutate(values))}>
        {mutation.isError && Object.keys(errors).length === 0 ? (
          <Alert variant="destructive">
            <AlertTitle>註冊失敗</AlertTitle>
            <AlertDescription>{errorMessage(mutation.error)}</AlertDescription>
          </Alert>
        ) : null}

        <FormField id="displayName" label="顯示名稱" error={errors.displayName?.message}>
          <Input autoComplete="nickname" {...fieldProps('displayName', errors.displayName?.message)} {...register('displayName')} />
        </FormField>

        <FormField id="email" label="Email" error={errors.email?.message}>
          <Input
            type="email"
            autoComplete="email"
            inputMode="email"
            {...fieldProps('email', errors.email?.message)}
            {...register('email')}
          />
        </FormField>

        <FormField
          id="password"
          label="密碼"
          error={errors.password?.message}
          description="至少 10 個字元，需包含大寫、小寫英文字母與數字。"
        >
          <PasswordInput
            autoComplete="new-password"
            {...fieldProps('password', errors.password?.message, true)}
            {...register('password')}
          />
        </FormField>

        <FormField id="confirmPassword" label="確認密碼" error={errors.confirmPassword?.message}>
          <PasswordInput
            autoComplete="new-password"
            {...fieldProps('confirmPassword', errors.confirmPassword?.message)}
            {...register('confirmPassword')}
          />
        </FormField>

        <Button type="submit" size="lg" disabled={mutation.isPending}>
          {mutation.isPending ? <LoaderCircle className="animate-spin" aria-hidden="true" /> : null}
          註冊
        </Button>
      </form>
    </AuthCard>
  )
}
