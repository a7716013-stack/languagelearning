import { screen, waitFor } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { json, mockFetch } from '@/test/fetch-mock'
import { renderRoutes } from '@/test/render'
import type { CurrentUser } from '@/types/api'
import { RequireAuth, RequireOnboarding, RequireRole } from './guards'

function makeUser(overrides: Partial<CurrentUser> = {}): CurrentUser {
  return {
    id: '1',
    email: 'student@example.com',
    displayName: '小明',
    roles: ['Student'],
    timeZoneId: 'Asia/Taipei',
    uiTheme: 'system',
    emailConfirmed: true,
    onboardingCompleted: true,
    createdAtUtc: '2026-11-01T00:00:00Z',
    ...overrides,
  }
}

const routes = [
  { path: '/login', element: <h1>登入頁</h1> },
  { path: '/onboarding', element: <h1>初次設定頁</h1> },
  {
    element: <RequireAuth />,
    children: [
      {
        element: <RequireOnboarding />,
        children: [{ path: '/dashboard', element: <h1>學習首頁</h1> }],
      },
      {
        element: <RequireRole role="Admin" />,
        children: [{ path: '/admin', element: <h1>管理後台</h1> }],
      },
    ],
  },
]

describe('route guards', () => {
  it('未登入時導向登入頁並保留 returnTo', async () => {
    mockFetch({ 'GET /api/users/me': () => json({ code: 'not_authenticated' }, 401) })
    const { router } = renderRoutes(routes, '/dashboard')

    expect(await screen.findByRole('heading', { name: '登入頁' })).toBeInTheDocument()
    expect(router.state.location.search).toBe('?returnTo=%2Fdashboard')
  })

  it('已登入且完成設定時顯示頁面', async () => {
    mockFetch({ 'GET /api/users/me': () => json(makeUser()) })
    renderRoutes(routes, '/dashboard')

    expect(await screen.findByRole('heading', { name: '學習首頁' })).toBeInTheDocument()
  })

  it('尚未完成初次設定時導向 Onboarding', async () => {
    mockFetch({ 'GET /api/users/me': () => json(makeUser({ onboardingCompleted: false })) })
    const { router } = renderRoutes(routes, '/dashboard')

    expect(await screen.findByRole('heading', { name: '初次設定頁' })).toBeInTheDocument()
    expect(router.state.location.pathname).toBe('/onboarding')
  })

  it('非管理者進入後台顯示 403', async () => {
    mockFetch({ 'GET /api/users/me': () => json(makeUser()) })
    renderRoutes(routes, '/admin')

    expect(await screen.findByRole('heading', { name: '沒有權限' })).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: '管理後台' })).not.toBeInTheDocument()
  })

  it('管理者可以進入後台', async () => {
    mockFetch({ 'GET /api/users/me': () => json(makeUser({ roles: ['Admin'] })) })
    renderRoutes(routes, '/admin')

    await waitFor(() => expect(screen.getByRole('heading', { name: '管理後台' })).toBeInTheDocument())
  })

  it('伺服器錯誤時顯示可重試的錯誤狀態', async () => {
    mockFetch({ 'GET /api/users/me': () => json({ code: 'server_error' }, 500) })
    renderRoutes(routes, '/dashboard')

    expect(await screen.findByText('無法確認登入狀態')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: '重試' })).toBeInTheDocument()
  })
})
