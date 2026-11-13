import { CalendarDays, ChartLine, NotebookPen, ShieldCheck, Target, Trophy } from 'lucide-react'
import { Link } from 'react-router'
import { BrandLogo } from '@/components/layout/BrandLogo'
import { SkipLink } from '@/components/layout/SkipLink'
import { ThemeToggle } from '@/components/layout/ThemeToggle'
import { APP_VERSION } from '@/lib/version'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { useDocumentTitle } from '@/hooks/use-document-title'

const AVAILABLE = [
  { icon: Target, title: '設定 TOEIC 目標', description: '依 10–990 分制設定目標分數與預計考試日期。' },
  { icon: CalendarDays, title: '考試倒數', description: '以你所在的時區計算，清楚掌握剩餘準備天數。' },
  { icon: ShieldCheck, title: '帳號安全', description: 'Email 驗證、登入保護與跨裝置登入管理。' },
]

const UPCOMING = [
  { icon: NotebookPen, title: 'Part 1–7 練習與模擬考' },
  { icon: Trophy, title: '單字 SRS 複習與錯題本' },
  { icon: ChartLine, title: '學習分析與 AI 解題' },
]

export function LandingPage() {
  useDocumentTitle('')
  return (
    <div className="min-h-dvh bg-gradient-to-b from-secondary/70 via-background to-background">
      <SkipLink />
      <header className="mx-auto flex max-w-5xl items-center justify-between px-4 py-4 sm:px-6">
        <BrandLogo />
        <div className="flex items-center gap-2">
          <ThemeToggle />
          <Button asChild variant="ghost">
            <Link to="/login">登入</Link>
          </Button>
        </div>
      </header>

      <main id="main-content" tabIndex={-1} className="mx-auto max-w-5xl px-4 pb-16 sm:px-6">
        <section className="py-12 text-center sm:py-20">
          <Badge variant="secondary" className="mb-4">
            TOEIC Listening &amp; Reading
          </Badge>
          <h1 className="text-4xl font-bold tracking-tight text-balance sm:text-5xl">
            準備英文檢定，<span className="text-primary">從設定目標開始</span>
          </h1>
          <p className="mx-auto mt-4 max-w-2xl text-lg text-muted-foreground">
            LanguageLearning 是為檢定考生設計的學習平台。先設定目標分數與考試日期，接下來的練習、單字與分析功能將陸續開放。
          </p>
          <div className="mt-8 flex flex-col justify-center gap-3 sm:flex-row">
            <Button asChild size="lg">
              <Link to="/register">免費註冊</Link>
            </Button>
            <Button asChild size="lg" variant="outline">
              <Link to="/login">我已經有帳號</Link>
            </Button>
          </div>
        </section>

        <section aria-labelledby="available-heading" className="space-y-4">
          <h2 id="available-heading" className="text-xl font-semibold">
            現在可以使用
          </h2>
          <ul className="grid gap-4 sm:grid-cols-3">
            {AVAILABLE.map((feature) => (
              <li key={feature.title} className="rounded-xl border bg-card p-5 shadow-xs">
                <feature.icon className="size-6 text-primary" aria-hidden="true" />
                <h3 className="mt-3 font-semibold">{feature.title}</h3>
                <p className="mt-1 text-sm text-muted-foreground">{feature.description}</p>
              </li>
            ))}
          </ul>
        </section>

        <section aria-labelledby="upcoming-heading" className="mt-12 space-y-4">
          <h2 id="upcoming-heading" className="text-xl font-semibold">
            即將推出
          </h2>
          <ul className="grid gap-3 sm:grid-cols-3">
            {UPCOMING.map((feature) => (
              <li key={feature.title} className="flex items-center gap-3 rounded-xl border border-dashed p-4 text-sm">
                <feature.icon className="size-5 text-muted-foreground" aria-hidden="true" />
                {feature.title}
              </li>
            ))}
          </ul>
        </section>

        <footer className="mt-16 border-t pt-6 text-center text-xs text-muted-foreground">
          <p>LanguageLearning v{APP_VERSION}</p>
          <p className="mt-1">TOEIC 為 ETS 的註冊商標。本平台與 ETS 無隸屬或合作關係，所有練習內容均為自行編寫。</p>
        </footer>
      </main>
    </div>
  )
}
