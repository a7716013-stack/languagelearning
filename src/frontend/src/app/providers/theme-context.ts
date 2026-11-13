import { createContext, useContext } from 'react'
import type { UiTheme } from '@/types/api'

export const THEME_STORAGE_KEY = 'll-theme'

export interface ThemeContextValue {
  theme: UiTheme
  resolvedTheme: 'light' | 'dark'
  setTheme: (theme: UiTheme) => void
}

export const ThemeContext = createContext<ThemeContextValue | null>(null)

export function useTheme(): ThemeContextValue {
  const context = useContext(ThemeContext)
  if (!context) throw new Error('useTheme 必須在 ThemeProvider 內使用。')
  return context
}

export function readStoredTheme(): UiTheme {
  try {
    const stored = window.localStorage.getItem(THEME_STORAGE_KEY)
    return stored === 'light' || stored === 'dark' ? stored : 'system'
  } catch {
    return 'system'
  }
}
