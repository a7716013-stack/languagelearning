import { Monitor, Moon, Sun } from 'lucide-react'
import { useTheme } from '@/app/providers/theme-context'
import { Button } from '@/components/ui/button'
import type { UiTheme } from '@/types/api'

const ORDER: UiTheme[] = ['system', 'light', 'dark']
const LABELS: Record<UiTheme, string> = { system: '跟隨系統', light: '淺色', dark: '深色' }

/** 依序切換：跟隨系統 → 淺色 → 深色。 */
export function ThemeToggle() {
  const { theme, setTheme } = useTheme()
  const next = ORDER[(ORDER.indexOf(theme) + 1) % ORDER.length]
  const Icon = theme === 'dark' ? Moon : theme === 'light' ? Sun : Monitor

  return (
    <Button
      variant="ghost"
      size="icon"
      onClick={() => setTheme(next)}
      aria-label={`介面主題：${LABELS[theme]}，按下切換為${LABELS[next]}`}
      title={`主題：${LABELS[theme]}`}
    >
      <Icon />
    </Button>
  )
}
