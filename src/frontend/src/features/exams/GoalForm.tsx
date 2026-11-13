import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { LoaderCircle } from 'lucide-react'
import { useMemo, useState } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { toast } from 'sonner'
import { applyServerFieldErrors, errorMessage } from '@/features/auth/server-errors'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { fieldProps, FormField } from '@/components/ui/form-field'
import { Input } from '@/components/ui/input'
import { todayInTimeZone } from '@/lib/format'
import { userApi } from '@/services/api'
import { queryKeys } from '@/services/query-keys'
import type { ExamGoal, ExamType } from '@/types/api'
import { createGoalSchema, DAILY_MINUTE_PRESETS, type GoalValues } from './goal-schema'

interface GoalFormProps {
  examType: ExamType
  existing?: ExamGoal
  timeZoneId: string
  submitLabel?: string
  onSaved?: (goal: ExamGoal) => void
}

/** 學習目標表單（Onboarding 與設定頁共用）。 */
export function GoalForm({ examType, existing, timeZoneId, submitLabel = '儲存目標', onSaved }: GoalFormProps) {
  const queryClient = useQueryClient()
  const [today] = useState(() => todayInTimeZone(timeZoneId))
  const schema = useMemo(() => createGoalSchema(examType, today), [examType, today])

  const {
    register,
    handleSubmit,
    setError,
    setValue,
    control,
    formState: { errors },
  } = useForm<GoalValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      targetScore: existing?.targetScore ?? Math.min(examType.maxScore, 750),
      examDate: existing?.examDate ?? '',
      dailyStudyMinutes: existing?.dailyStudyMinutes ?? 30,
    },
  })
  const dailyMinutes = useWatch({ control, name: 'dailyStudyMinutes' })

  const mutation = useMutation({
    mutationFn: (values: GoalValues) =>
      userApi.saveGoal(examType.code, { ...values, version: existing?.version ?? null }),
    onSuccess: (goal) => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.examGoals })
      onSaved?.(goal)
    },
    onError: (error) => {
      if (!applyServerFieldErrors(error, setError)) toast.error(errorMessage(error))
    },
  })

  return (
    <form className="grid gap-5" noValidate onSubmit={handleSubmit((values) => mutation.mutate(values))}>
      {mutation.isError && Object.keys(errors).length === 0 ? (
        <Alert variant="destructive">
          <AlertTitle>無法儲存</AlertTitle>
          <AlertDescription>{errorMessage(mutation.error)}</AlertDescription>
        </Alert>
      ) : null}

      <FormField
        id="targetScore"
        label="目標分數"
        error={errors.targetScore?.message}
        description={`${examType.name}：${examType.minScore}–${examType.maxScore} 分，以 ${examType.scoreStep} 分為單位。`}
      >
        <Input
          type="number"
          inputMode="numeric"
          min={examType.minScore}
          max={examType.maxScore}
          step={examType.scoreStep}
          {...fieldProps('targetScore', errors.targetScore?.message, true)}
          {...register('targetScore', { valueAsNumber: true })}
        />
      </FormField>

      <FormField id="examDate" label="預計考試日期" error={errors.examDate?.message}>
        <Input type="date" min={today} {...fieldProps('examDate', errors.examDate?.message)} {...register('examDate')} />
      </FormField>

      <fieldset className="grid gap-2">
        <legend className="mb-2 text-sm font-medium">每日可學習時間</legend>
        <div className="flex flex-wrap gap-2" role="group" aria-label="快速選擇每日學習時間">
          {DAILY_MINUTE_PRESETS.map((minutes) => (
            <Button
              key={minutes}
              size="sm"
              variant={dailyMinutes === minutes ? 'default' : 'outline'}
              aria-pressed={dailyMinutes === minutes}
              onClick={() => setValue('dailyStudyMinutes', minutes, { shouldValidate: true, shouldDirty: true })}
            >
              {minutes} 分鐘
            </Button>
          ))}
        </div>
        <FormField id="dailyStudyMinutes" label="自訂（分鐘）" error={errors.dailyStudyMinutes?.message}>
          <Input
            type="number"
            inputMode="numeric"
            min={10}
            max={600}
            step={5}
            {...fieldProps('dailyStudyMinutes', errors.dailyStudyMinutes?.message)}
            {...register('dailyStudyMinutes', { valueAsNumber: true })}
          />
        </FormField>
      </fieldset>

      <Button type="submit" size="lg" disabled={mutation.isPending}>
        {mutation.isPending ? <LoaderCircle className="animate-spin" aria-hidden="true" /> : null}
        {submitLabel}
      </Button>
    </form>
  )
}
