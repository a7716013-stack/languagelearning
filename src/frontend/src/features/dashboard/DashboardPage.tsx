import { BookOpen, CalendarDays, ChartLine, Clock, NotebookPen, Pencil, Target, Trophy } from 'lucide-react'
import { Link } from 'react-router'
import { PageHeader } from '@/components/layout/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '@/components/layout/states'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { useExamGoals } from '@/features/exams/use-exam-data'
import { useAuthenticatedUser } from '@/hooks/use-current-user'
import { useDocumentTitle } from '@/hooks/use-document-title'
import { formatDateOnly, formatScore } from '@/lib/format'
import type { ExamGoal } from '@/types/api'

const UPCOMING = [
  { title: '練習與模擬考', description: 'TOEIC Part 1–7 題型練習、計時模擬考與答題解析。', icon: NotebookPen },
  { title: '錯題本', description: '自動整理錯題，依題型與考點重新練習。', icon: BookOpen },
  { title: '單字複習', description: '以間隔重複（SRS）安排每日到期單字。', icon: Trophy },
  { title: '學習分析', description: '學習時間、正確率與弱點分類圖表。', icon: ChartLine },
] as const

function countdownText(days: number): string {
  if (days > 0) return `還有 ${days} 天`
  if (days === 0) return '就是今天！'
  return `已過 ${Math.abs(days)} 天`
}

function GoalCard({ goal }: { goal: ExamGoal }) {
  return (
    <Card className="overflow-hidden">
      <CardHeader>
        <div className="flex items-start justify-between gap-2">
          <div>
            <CardDescription>目前目標</CardDescription>
            <CardTitle className="text-xl">{goal.examTypeName}</CardTitle>
          </div>
          <Badge variant="secondary">{goal.examTypeCode}</Badge>
        </div>
      </CardHeader>
      <CardContent>
        <dl className="grid gap-4 sm:grid-cols-3">
          <div className="rounded-lg bg-secondary/60 p-4">
            <dt className="flex items-center gap-1.5 text-sm text-muted-foreground">
              <Target className="size-4" aria-hidden="true" />
              目標分數
            </dt>
            <dd className="mt-1 text-3xl font-bold text-primary">{formatScore(goal.targetScore)}</dd>
          </div>
          <div className="rounded-lg bg-secondary/60 p-4">
            <dt className="flex items-center gap-1.5 text-sm text-muted-foreground">
              <CalendarDays className="size-4" aria-hidden="true" />
              考試日期
            </dt>
            <dd className="mt-1">
              <span className="block text-lg font-semibold">{formatDateOnly(goal.examDate)}</span>
              <span className="text-sm text-muted-foreground">{countdownText(goal.daysUntilExam)}</span>
            </dd>
          </div>
          <div className="rounded-lg bg-secondary/60 p-4">
            <dt className="flex items-center gap-1.5 text-sm text-muted-foreground">
              <Clock className="size-4" aria-hidden="true" />
              每日學習時間
            </dt>
            <dd className="mt-1 text-lg font-semibold">{goal.dailyStudyMinutes} 分鐘</dd>
          </div>
        </dl>
      </CardContent>
    </Card>
  )
}

export function DashboardPage() {
  useDocumentTitle('學習首頁')
  const user = useAuthenticatedUser()
  const goals = useExamGoals()

  return (
    <div>
      <PageHeader
        title={`哈囉，${user.displayName}`}
        description="這是你的學習首頁。"
        actions={
          <Button asChild variant="outline">
            <Link to="/settings#goals">
              <Pencil aria-hidden="true" />
              調整目標
            </Link>
          </Button>
        }
      />

      <section aria-labelledby="goal-heading" className="space-y-4">
        <h2 id="goal-heading" className="sr-only">
          學習目標
        </h2>
        {goals.isPending ? (
          <LoadingState />
        ) : goals.isError ? (
          <ErrorState onRetry={() => void goals.refetch()} />
        ) : goals.data.length === 0 ? (
          <EmptyState
            title="尚未設定學習目標"
            description="設定目標分數與考試日期，就能看到考試倒數。"
            icon={<Target className="size-6" aria-hidden="true" />}
            action={
              <Button asChild>
                <Link to="/settings#goals">設定目標</Link>
              </Button>
            }
          />
        ) : (
          goals.data.map((goal) => <GoalCard key={goal.examTypeCode} goal={goal} />)
        )}
      </section>

      <section aria-labelledby="upcoming-heading" className="mt-10 space-y-4">
        <div>
          <h2 id="upcoming-heading" className="text-lg font-semibold">
            即將推出
          </h2>
          <p className="text-sm text-muted-foreground">以下功能正在開發中，開放後會在這裡顯示你的學習數據。</p>
        </div>
        <div className="grid gap-3 sm:grid-cols-2">
          {UPCOMING.map((feature) => (
            <EmptyState
              key={feature.title}
              title={feature.title}
              description={feature.description}
              icon={<feature.icon className="size-6" aria-hidden="true" />}
              className="items-start py-6 text-left [&>div:last-child]:text-left"
            />
          ))}
        </div>
      </section>
    </div>
  )
}
