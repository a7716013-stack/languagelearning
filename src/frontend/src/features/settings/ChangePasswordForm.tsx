import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation } from '@tanstack/react-query'
import { LoaderCircle } from 'lucide-react'
import { useForm } from 'react-hook-form'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { fieldProps, FormField } from '@/components/ui/form-field'
import { PasswordInput } from '@/features/auth/PasswordInput'
import { changePasswordSchema, type ChangePasswordValues } from '@/features/auth/schemas'
import { applyServerFieldErrors, errorMessage } from '@/features/auth/server-errors'
import { authApi } from '@/services/api'

export function ChangePasswordForm() {
  const {
    register,
    handleSubmit,
    setError,
    reset,
    formState: { errors },
  } = useForm<ChangePasswordValues>({
    resolver: zodResolver(changePasswordSchema),
    defaultValues: { currentPassword: '', newPassword: '', confirmPassword: '' },
  })

  const mutation = useMutation({
    mutationFn: (values: ChangePasswordValues) =>
      authApi.changePassword({ currentPassword: values.currentPassword, newPassword: values.newPassword }),
    onSuccess: () => {
      reset()
      toast.success('密碼已變更，其他裝置將需要重新登入。')
    },
    onError: (error) => {
      if (!applyServerFieldErrors(error, setError)) toast.error(errorMessage(error))
    },
  })

  return (
    <form className="grid gap-4" noValidate onSubmit={handleSubmit((values) => mutation.mutate(values))}>
      <FormField id="currentPassword" label="目前的密碼" error={errors.currentPassword?.message}>
        <PasswordInput
          autoComplete="current-password"
          {...fieldProps('currentPassword', errors.currentPassword?.message)}
          {...register('currentPassword')}
        />
      </FormField>
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
      <FormField id="confirmNewPassword" label="確認新密碼" error={errors.confirmPassword?.message}>
        <PasswordInput
          autoComplete="new-password"
          {...fieldProps('confirmNewPassword', errors.confirmPassword?.message)}
          {...register('confirmPassword')}
        />
      </FormField>
      <div>
        <Button type="submit" disabled={mutation.isPending}>
          {mutation.isPending ? <LoaderCircle className="animate-spin" aria-hidden="true" /> : null}
          變更密碼
        </Button>
      </div>
    </form>
  )
}
