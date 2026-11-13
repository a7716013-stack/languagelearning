import { act, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { ThemeToggle } from '@/components/layout/ThemeToggle'
import { THEME_STORAGE_KEY, useTheme } from './theme-context'
import { ThemeProvider } from './ThemeProvider'

function ThemeProbe() {
  const { theme, resolvedTheme, setTheme } = useTheme()
  return (
    <div>
      <span data-testid="theme">{theme}</span>
      <span data-testid="resolved">{resolvedTheme}</span>
      <button type="button" onClick={() => setTheme('dark')}>
        dark
      </button>
    </div>
  )
}

describe('ThemeProvider', () => {
  it('預設跟隨系統（測試環境為淺色）', () => {
    render(
      <ThemeProvider>
        <ThemeProbe />
      </ThemeProvider>,
    )

    expect(screen.getByTestId('theme')).toHaveTextContent('system')
    expect(screen.getByTestId('resolved')).toHaveTextContent('light')
    expect(document.documentElement).not.toHaveClass('dark')
  })

  it('切換為深色時套用 class 並保存偏好', async () => {
    render(
      <ThemeProvider>
        <ThemeProbe />
      </ThemeProvider>,
    )

    await userEvent.click(screen.getByRole('button', { name: 'dark' }))

    expect(document.documentElement).toHaveClass('dark')
    expect(window.localStorage.getItem(THEME_STORAGE_KEY)).toBe('dark')
  })

  it('讀取已保存的偏好', () => {
    window.localStorage.setItem(THEME_STORAGE_KEY, 'dark')
    render(
      <ThemeProvider>
        <ThemeProbe />
      </ThemeProvider>,
    )

    expect(screen.getByTestId('resolved')).toHaveTextContent('dark')
  })

  it('ThemeToggle 依序切換並有無障礙名稱', async () => {
    render(
      <ThemeProvider>
        <ThemeToggle />
        <ThemeProbe />
      </ThemeProvider>,
    )

    const toggle = screen.getByRole('button', { name: /介面主題：跟隨系統/ })
    await act(async () => {
      await userEvent.click(toggle)
    })

    expect(screen.getByTestId('theme')).toHaveTextContent('light')
    expect(screen.getByRole('button', { name: /介面主題：淺色/ })).toBeInTheDocument()
  })
})
