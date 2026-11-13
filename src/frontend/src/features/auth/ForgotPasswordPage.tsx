import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation } from '@tanstack/react-query'
import { LoaderCircle } from 'lucide-react'
import { useForm } from 'react-hook-form'
import { Link } from 'react-router'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { fieldProps, FormField } from '@/components/ui/form-field'
import { Input } from '@/components/ui/input'
import { authApi } from '@/services/api'
import { AuthCard } from './AuthCard'
import { forgotPasswordSchema, type ForgotPasswordValues } from './schemas'
import { applyServerFieldErrors, errorMessage } from './server-errors'

export function ForgotPasswordPage() {
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<ForgotPasswordValues>({
    resolver: zodResolver(forgotPasswordSchema),
    defaultValues: { email: '' },
  })

  const mutation = useMutation({
    mutationFn: (values: ForgotPasswordValues) => authApi.forgotPassword(values.email),
    onError: (error) => {
      applyServerFieldErrors(error, setError)
    },
  })

  return (
    <AuthCard
      title="忘記密碼"
      description="輸入註冊時的 Email，我們會寄送重設密碼連結。"
      footer={<Link to="/login" className="text-primary hover:underline">返回登入</Link>}
    >
      {mutation.isSuccess ? (
        <Alert variant="success">
          <AlertTitle>請查看你的信箱</AlertTitle>
          <AlertDescription>{mutation.data.message}</AlertDescription>
        </Alert>
      ) : (
        <form className="grid gap-4" noValidate onSubmit={handleSubmit((values) => mutation.mutate(values))}>
          {mutation.isError && !errors.email ? (
            <Alert variant="destructive">
              <AlertTitle>無法送出</AlertTitle>
              <AlertDescription>{errorMessage(mutation.error)}</AlertDescription>
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
          <Button type="submit" size="lg" disabled={mutation.isPending}>
            {mutation.isPending ? <LoaderCircle className="animate-spin" aria-hidden="true" /> : null}
            寄送重設連結
          </Button>
        </form>
      )}
    </AuthCard>
  )
}
