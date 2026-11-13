import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { LoaderCircle } from 'lucide-react'
import { useMemo } from 'react'
import { useForm } from 'react-hook-form'
import { toast } from 'sonner'
import { z } from 'zod'
import { useTheme } from '@/app/providers/theme-context'
import { Button } from '@/components/ui/button'
import { fieldProps, FormField } from '@/components/ui/form-field'
import { Input } from '@/components/ui/input'
import { NativeSelect } from '@/components/ui/native-select'
import { displayNameField } from '@/features/auth/schemas'
import { applyServerFieldErrors, errorMessage } from '@/features/auth/server-errors'
import { userApi } from '@/services/api'
import { queryKeys } from '@/services/query-keys'
import type { CurrentUser, UiTheme } from '@/types/api'

const profileSchema = z.object({
  displayName: displayNameField,
  timeZoneId: z.string().min(1, '請選擇時區。'),
  uiTheme: z.enum(['system', 'light', 'dark']),
})

type ProfileValues = z.infer<typeof profileSchema>

const THEME_OPTIONS: { value: UiTheme; label: string }[] = [
  { value: 'system', label: '跟隨系統' },
  { value: 'light', label: '淺色' },
  { value: 'dark', label: '深色' },
]

function availableTimeZones(current: string): string[] {
  const zones = typeof Intl.supportedValuesOf === 'function' ? Intl.supportedValuesOf('timeZone') : []
  const list = zones.length > 0 ? zones : ['Asia/Taipei', 'Asia/Tokyo', 'Asia/Hong_Kong', 'Asia/Singapore', 'UTC']
  return list.includes(current) ? list : [current, ...list]
}

export function ProfileForm({ user }: { user: CurrentUser }) {
  const queryClient = useQueryClient()
  const { setTheme } = useTheme()
  const timeZones = useMemo(() => availableTimeZones(user.timeZoneId), [user.timeZoneId])

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isDirty },
    reset,
  } = useForm<ProfileValues>({
    resolver: zodResolver(profileSchema),
    defaultValues: { displayName: user.displayName, timeZoneId: user.timeZoneId, uiTheme: user.uiTheme },
  })

  const mutation = useMutation({
    mutationFn: userApi.updateProfile,
    onSuccess: (updated) => {
      queryClient.setQueryData(queryKeys.me, updated)
      void queryClient.invalidateQueries({ queryKey: queryKeys.examGoals })
      setTheme(updated.uiTheme)
      reset({ displayName: updated.displayName, timeZoneId: updated.timeZoneId, uiTheme: updated.uiTheme })
      toast.success('個人資料已更新。')
    },
    onError: (error) => {
      if (!applyServerFieldErrors(error, setError)) toast.error(errorMessage(error))
    },
  })

  return (
    <form className="grid gap-4" noValidate onSubmit={handleSubmit((values) => mutation.mutate(values))}>
      <FormField id="profile-email" label="Email">
        <Input id="profile-email" value={user.email} readOnly disabled />
      </FormField>
      <FormField id="displayName" label="顯示名稱" error={errors.displayName?.message}>
        <Input {...fieldProps('displayName', errors.displayName?.message)} {...register('displayName')} />
      </FormField>
      <FormField
        id="timeZoneId"
        label="時區"
        error={errors.timeZoneId?.message}
        description="用來計算考試倒數與每日任務的「今天」。"
      >
        <NativeSelect {...fieldProps('timeZoneId', errors.timeZoneId?.message, true)} {...register('timeZoneId')}>
          {timeZones.map((zone) => (
            <option key={zone} value={zone}>
              {zone}
            </option>
          ))}
        </NativeSelect>
      </FormField>
      <FormField id="uiTheme" label="介面主題" error={errors.uiTheme?.message}>
        <NativeSelect {...fieldProps('uiTheme', errors.uiTheme?.message)} {...register('uiTheme')}>
          {THEME_OPTIONS.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </NativeSelect>
      </FormField>
      <div>
        <Button type="submit" disabled={mutation.isPending || !isDirty}>
          {mutation.isPending ? <LoaderCircle className="animate-spin" aria-hidden="true" /> : null}
          儲存變更
        </Button>
      </div>
    </form>
  )
}
