import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { clearCsrfToken } from '@/services/api-client'
import { csrfHandler, json, mockFetch } from '@/test/fetch-mock'
import { renderRoutes } from '@/test/render'
import type { CurrentUser } from '@/types/api'
import { LoginPage } from './LoginPage'

const user: CurrentUser = {
  id: '1',
  email: 'student@example.com',
  displayName: '小明',
  roles: ['Student'],
  timeZoneId: 'Asia/Taipei',
  uiTheme: 'dark',
  emailConfirmed: true,
  onboardingCompleted: true,
  createdAtUtc: '2026-11-01T00:00:00Z',
}

const routes = [
  { path: '/login', element: <LoginPage /> },
  { path: '/dashboard', element: <h1>學習首頁內容</h1> },
  { path: '/settings', element: <h1>設定內容</h1> },
]

describe('LoginPage', () => {
  it('空白送出時顯示欄位錯誤，且不呼叫 API', async () => {
    const fetchMock = mockFetch({})
    renderRoutes(routes, '/login')

    await userEvent.click(screen.getByRole('button', { name: '登入' }))

    expect(await screen.findByText('請輸入 Email。')).toBeInTheDocument()
    expect(screen.getByText('請輸入密碼。')).toBeInTheDocument()
    expect(screen.getByLabelText('Email')).toHaveAttribute('aria-invalid', 'true')
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('登入成功後套用主題並導向 returnTo', async () => {
    clearCsrfToken()
    mockFetch({
      'GET /api/auth/csrf': csrfHandler,
      'POST /api/auth/login': () => json(user),
    })
    const { router } = renderRoutes(routes, '/login?returnTo=%2Fsettings')

    await userEvent.type(screen.getByLabelText('Email'), 'student@example.com')
    await userEvent.type(screen.getByLabelText('密碼'), 'Correct1Horse')
    await userEvent.click(screen.getByRole('button', { name: '登入' }))

    expect(await screen.findByRole('heading', { name: '設定內容' })).toBeInTheDocument()
    expect(router.state.location.pathname).toBe('/settings')
    expect(document.documentElement).toHaveClass('dark')
  })

  it('拒絕站外 returnTo，改導向學習首頁', async () => {
    clearCsrfToken()
    mockFetch({
      'GET /api/auth/csrf': csrfHandler,
      'POST /api/auth/login': () => json(user),
    })
    const { router } = renderRoutes(routes, '/login?returnTo=%2F%2Fevil.example.com')

    await userEvent.type(screen.getByLabelText('Email'), 'student@example.com')
    await userEvent.type(screen.getByLabelText('密碼'), 'Correct1Horse')
    await userEvent.click(screen.getByRole('button', { name: '登入' }))

    await waitFor(() => expect(router.state.location.pathname).toBe('/dashboard'))
  })

  it('顯示伺服器回傳的錯誤訊息', async () => {
    clearCsrfToken()
    mockFetch({
      'GET /api/auth/csrf': csrfHandler,
      'POST /api/auth/login': () =>
        json({ status: 401, code: 'invalid_credentials', detail: 'Email 或密碼不正確。' }, 401),
    })
    renderRoutes(routes, '/login')

    await userEvent.type(screen.getByLabelText('Email'), 'student@example.com')
    await userEvent.type(screen.getByLabelText('密碼'), 'WrongPass123')
    await userEvent.click(screen.getByRole('button', { name: '登入' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(/Email 或密碼不正確/)
  })

  it('尚未驗證 Email 時提供重新寄送驗證信', async () => {
    clearCsrfToken()
    mockFetch({
      'GET /api/auth/csrf': csrfHandler,
      'POST /api/auth/login': () =>
        json({ status: 403, code: 'email_not_confirmed', detail: '此帳號尚未完成 Email 驗證。' }, 403),
      'POST /api/auth/resend-confirmation': () => json({ message: '已重新寄出' }, 202),
    })
    renderRoutes(routes, '/login')

    await userEvent.type(screen.getByLabelText('Email'), 'student@example.com')
    await userEvent.type(screen.getByLabelText('密碼'), 'Correct1Horse')
    await userEvent.click(screen.getByRole('button', { name: '登入' }))
    await userEvent.click(await screen.findByRole('button', { name: /重新寄送驗證信/ }))

    expect(await screen.findByRole('button', { name: /已重新寄出驗證信/ })).toBeDisabled()
  })
})
