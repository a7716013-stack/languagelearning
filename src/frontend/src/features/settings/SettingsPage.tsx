import { toast } from 'sonner'
import { APP_VERSION } from '@/lib/version'
import { PageHeader } from '@/components/layout/PageHeader'
import { ErrorState, LoadingState } from '@/components/layout/states'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { GoalForm } from '@/features/exams/GoalForm'
import { useExamGoals, useExamTypes } from '@/features/exams/use-exam-data'
import { useAuthenticatedUser } from '@/hooks/use-current-user'
import { useDocumentTitle } from '@/hooks/use-document-title'
import { formatDateTime } from '@/lib/format'
import { ChangePasswordForm } from './ChangePasswordForm'
import { ProfileForm } from './ProfileForm'

function GoalsSection({ timeZoneId }: { timeZoneId: string }) {
  const examTypes = useExamTypes()
  const goals = useExamGoals()

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

  const available = examTypes.data.filter((type) => type.isAvailable)
  return (
    <div className="space-y-8">
      {available.map((type) => {
        const existing = goals.data.find((goal) => goal.examTypeCode === type.code)
        return (
          <div key={`${type.code}-${existing?.version ?? 'new'}`} className="space-y-3">
            <h3 className="font-medium">{type.name}</h3>
            <GoalForm
              examType={type}
              existing={existing}
              timeZoneId={timeZoneId}
              onSaved={() => toast.success('學習目標已更新。')}
            />
          </div>
        )
      })}
    </div>
  )
}

export function SettingsPage() {
  useDocumentTitle('設定')
  const user = useAuthenticatedUser()

  return (
    <div className="space-y-6">
      <PageHeader title="設定" description="管理個人資料、學習目標與帳號安全。" />

      <Card>
        <CardHeader>
          <CardTitle>個人資料</CardTitle>
          <CardDescription>註冊時間：{formatDateTime(user.createdAtUtc, user.timeZoneId)}</CardDescription>
        </CardHeader>
        <CardContent>
          <ProfileForm user={user} />
        </CardContent>
      </Card>

      <Card id="goals" className="scroll-mt-24">
        <CardHeader>
          <CardTitle>學習目標</CardTitle>
          <CardDescription>目前開放 TOEIC Listening &amp; Reading，IELTS 與 TOEFL 將於後續版本開放。</CardDescription>
        </CardHeader>
        <CardContent>
          <GoalsSection timeZoneId={user.timeZoneId} />
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>變更密碼</CardTitle>
          <CardDescription>變更後，其他裝置上的登入狀態將會失效。</CardDescription>
        </CardHeader>
        <CardContent>
          <ChangePasswordForm />
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>關於</CardTitle>
        </CardHeader>
        <CardContent className="text-sm text-muted-foreground">
          <p>
            版本 <span className="font-medium text-foreground">{APP_VERSION}</span>
          </p>
        </CardContent>
      </Card>
    </div>
  )
}
