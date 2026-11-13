import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Check, GraduationCap, Info, Lock } from 'lucide-react'
import { useState } from 'react'
import { useNavigate } from 'react-router'
import { toast } from 'sonner'
import { ErrorState, LoadingState } from '@/components/layout/states'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { errorMessage } from '@/features/auth/server-errors'
import { GoalForm } from '@/features/exams/GoalForm'
import { useExamGoals, useExamTypes } from '@/features/exams/use-exam-data'
import { useAuthenticatedUser } from '@/hooks/use-current-user'
import { useDocumentTitle } from '@/hooks/use-document-title'
import { cn } from '@/lib/utils'
import { userApi } from '@/services/api'
import { queryKeys } from '@/services/query-keys'

const STEPS = ['選擇檢定', '設定目標', '完成'] as const

export function OnboardingPage() {
  useDocumentTitle('初次設定')
  const user = useAuthenticatedUser()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const examTypes = useExamTypes()
  const goals = useExamGoals()
  const [selectedCode, setSelectedCode] = useState<string | null>(null)
  const [step, setStep] = useState(0)

  const complete = useMutation({
    mutationFn: userApi.completeOnboarding,
    onSuccess: (updated) => {
      queryClient.setQueryData(queryKeys.me, updated)
      toast.success('設定完成，開始你的學習之旅！')
      void navigate('/dashboard', { replace: true })
    },
    onError: (error) => toast.error(errorMessage(error)),
  })

  if (examTypes.isPending || goals.isPending) return <LoadingState />
  if (examTypes.isError || goals.isError) {
    return (
      <ErrorState
        onRetry={() => {
          void examTypes.refetch()
          void goals.refetch()
        }}
      />
    )
  }

  const selected = examTypes.data.find((type) => type.code === selectedCode)
  const existingGoal = goals.data.find((goal) => goal.examTypeCode === selectedCode)

  return (
    <div className="space-y-6">
      <div className="space-y-2">
        <h1 className="text-2xl font-bold tracking-tight md:text-3xl">嗨，{user.displayName}！先設定你的學習目標</h1>
        <p className="text-muted-foreground">只需要一分鐘，我們會依你的目標安排學習首頁。</p>
      </div>

      <ol className="flex items-center gap-2 text-sm" aria-label="設定步驟">
        {STEPS.map((label, index) => (
          <li key={label} className="flex items-center gap-2" aria-current={index === step ? 'step' : undefined}>
            <span
              className={cn(
                'flex size-7 items-center justify-center rounded-full border text-xs font-semibold',
                index < step && 'border-primary bg-primary text-primary-foreground',
                index === step && 'border-primary text-primary',
                index > step && 'text-muted-foreground',
              )}
            >
              {index < step ? <Check className="size-4" aria-hidden="true" /> : index + 1}
            </span>
            <span className={cn(index === step ? 'font-medium' : 'text-muted-foreground')}>{label}</span>
            {index < STEPS.length - 1 ? <span className="mx-1 h-px w-6 bg-border" aria-hidden="true" /> : null}
          </li>
        ))}
      </ol>

      {step === 0 ? (
        <section aria-labelledby="choose-exam" className="space-y-4">
          <h2 id="choose-exam" className="text-lg font-semibold">
            你準備參加哪一種檢定？
          </h2>
          <div className="grid gap-3 sm:grid-cols-3" role="radiogroup" aria-labelledby="choose-exam">
            {examTypes.data.map((type) => {
              const active = selectedCode === type.code
              return (
                <button
                  key={type.code}
                  type="button"
                  role="radio"
                  aria-checked={active}
                  disabled={!type.isAvailable}
                  onClick={() => setSelectedCode(type.code)}
                  className={cn(
                    'flex flex-col items-start gap-2 rounded-xl border p-4 text-left transition-colors focus-visible:ring-[3px] focus-visible:ring-ring/50 focus-visible:outline-none',
                    active ? 'border-primary bg-accent' : 'hover:bg-accent/60',
                    !type.isAvailable && 'cursor-not-allowed opacity-60 hover:bg-transparent',
                  )}
                >
                  <span className="flex w-full items-center justify-between gap-2">
                    <span className="font-semibold">{type.code}</span>
                    {type.isAvailable ? (
                      <Badge variant="success">已開放</Badge>
                    ) : (
                      <Badge variant="outline">
                        <Lock aria-hidden="true" />
                        尚未開放
                      </Badge>
                    )}
                  </span>
                  <span className="text-sm text-muted-foreground">{type.name}</span>
                </button>
              )
            })}
          </div>
          <div className="flex justify-end">
            <Button size="lg" disabled={!selected} onClick={() => setStep(1)}>
              下一步
            </Button>
          </div>
        </section>
      ) : null}

      {step === 1 && selected ? (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <GraduationCap className="size-5 text-primary" aria-hidden="true" />
              {selected.name}
            </CardTitle>
            <CardDescription>設定目標分數、預計考試日期與每日可學習時間。</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <GoalForm
              examType={selected}
              existing={existingGoal}
              timeZoneId={user.timeZoneId}
              submitLabel="儲存目標"
              onSaved={() => setStep(2)}
            />
            <Button variant="ghost" onClick={() => setStep(0)}>
              上一步
            </Button>
          </CardContent>
        </Card>
      ) : null}

      {step === 2 ? (
        <Card>
          <CardHeader>
            <CardTitle>目標已儲存</CardTitle>
            <CardDescription>之後可以隨時在「設定」頁調整。</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <Alert>
              <Info aria-hidden="true" />
              <AlertTitle>基礎程度診斷</AlertTitle>
              <AlertDescription>程度診斷將於後續版本開放，屆時會依結果推薦合適的練習。</AlertDescription>
            </Alert>
            <Button size="lg" className="w-full" disabled={complete.isPending} onClick={() => complete.mutate()}>
              完成設定，前往學習首頁
            </Button>
          </CardContent>
        </Card>
      ) : null}
    </div>
  )
}
