import { createBrowserRouter, type RouteObject } from 'react-router'
import { AppLayout } from './layouts/AppLayout'
import { AuthLayout } from './layouts/AuthLayout'
import { OnboardingLayout } from './layouts/OnboardingLayout'
import { lazyPage } from './lazy-page'
import { NotFoundPage } from './routes/ErrorPages'
import { GuestOnly, RequireAuth, RequireOnboarding, RequireRole } from './routes/guards'
import { LandingPage } from './routes/LandingPage'

// 依頁面切分程式碼：只有首頁直接載入，其餘頁面（包含登入頁）在第一次進入時才下載，
// 表單相關套件（React Hook Form、Zod）因此不會進入主程式。版面與路由守衛不切分。
const loginPage = lazyPage(async () => (await import('@/features/auth/LoginPage')).LoginPage)
const registerPage = lazyPage(async () => (await import('@/features/auth/RegisterPage')).RegisterPage)
const forgotPasswordPage = lazyPage(
  async () => (await import('@/features/auth/ForgotPasswordPage')).ForgotPasswordPage,
)
const checkEmailPage = lazyPage(async () => (await import('@/features/auth/CheckEmailPage')).CheckEmailPage)
const confirmEmailPage = lazyPage(
  async () => (await import('@/features/auth/ConfirmEmailPage')).ConfirmEmailPage,
)
const resetPasswordPage = lazyPage(
  async () => (await import('@/features/auth/ResetPasswordPage')).ResetPasswordPage,
)
const onboardingPage = lazyPage(
  async () => (await import('@/features/onboarding/OnboardingPage')).OnboardingPage,
)
const dashboardPage = lazyPage(async () => (await import('@/features/dashboard/DashboardPage')).DashboardPage)
const settingsPage = lazyPage(async () => (await import('@/features/settings/SettingsPage')).SettingsPage)
const adminPage = lazyPage(async () => (await import('@/features/admin/AdminPage')).AdminPage)

export const routes: RouteObject[] = [
  {
    element: <GuestOnly />,
    children: [
      { path: '/', element: <LandingPage /> },
      {
        element: <AuthLayout />,
        children: [
          { path: '/login', element: loginPage },
          { path: '/register', element: registerPage },
          { path: '/forgot-password', element: forgotPasswordPage },
        ],
      },
    ],
  },
  {
    // 不論是否登入都可開啟（信件連結）。
    element: <AuthLayout />,
    children: [
      { path: '/register/check-email', element: checkEmailPage },
      { path: '/confirm-email', element: confirmEmailPage },
      { path: '/reset-password', element: resetPasswordPage },
    ],
  },
  {
    element: <RequireAuth />,
    children: [
      {
        element: <OnboardingLayout />,
        children: [{ path: '/onboarding', element: onboardingPage }],
      },
      {
        element: <AppLayout />,
        children: [
          {
            element: <RequireOnboarding />,
            children: [{ path: '/dashboard', element: dashboardPage }],
          },
          { path: '/settings', element: settingsPage },
          {
            element: <RequireRole role="Admin" />,
            children: [{ path: '/admin', element: adminPage }],
          },
        ],
      },
    ],
  },
  {
    element: <AuthLayout />,
    children: [{ path: '*', element: <NotFoundPage /> }],
  },
]

export function createAppRouter() {
  return createBrowserRouter(routes)
}
