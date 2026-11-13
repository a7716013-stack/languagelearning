import { createBrowserRouter, type RouteObject } from 'react-router'
import { AdminPage } from '@/features/admin/AdminPage'
import { CheckEmailPage } from '@/features/auth/CheckEmailPage'
import { ConfirmEmailPage } from '@/features/auth/ConfirmEmailPage'
import { ForgotPasswordPage } from '@/features/auth/ForgotPasswordPage'
import { LoginPage } from '@/features/auth/LoginPage'
import { RegisterPage } from '@/features/auth/RegisterPage'
import { ResetPasswordPage } from '@/features/auth/ResetPasswordPage'
import { DashboardPage } from '@/features/dashboard/DashboardPage'
import { OnboardingPage } from '@/features/onboarding/OnboardingPage'
import { SettingsPage } from '@/features/settings/SettingsPage'
import { AppLayout } from './layouts/AppLayout'
import { AuthLayout } from './layouts/AuthLayout'
import { OnboardingLayout } from './layouts/OnboardingLayout'
import { NotFoundPage } from './routes/ErrorPages'
import { GuestOnly, RequireAuth, RequireOnboarding, RequireRole } from './routes/guards'
import { LandingPage } from './routes/LandingPage'

export const routes: RouteObject[] = [
  {
    element: <GuestOnly />,
    children: [
      { path: '/', element: <LandingPage /> },
      {
        element: <AuthLayout />,
        children: [
          { path: '/login', element: <LoginPage /> },
          { path: '/register', element: <RegisterPage /> },
          { path: '/forgot-password', element: <ForgotPasswordPage /> },
        ],
      },
    ],
  },
  {
    // 不論是否登入都可開啟（信件連結）。
    element: <AuthLayout />,
    children: [
      { path: '/register/check-email', element: <CheckEmailPage /> },
      { path: '/confirm-email', element: <ConfirmEmailPage /> },
      { path: '/reset-password', element: <ResetPasswordPage /> },
    ],
  },
  {
    element: <RequireAuth />,
    children: [
      {
        element: <OnboardingLayout />,
        children: [{ path: '/onboarding', element: <OnboardingPage /> }],
      },
      {
        element: <AppLayout />,
        children: [
          {
            element: <RequireOnboarding />,
            children: [{ path: '/dashboard', element: <DashboardPage /> }],
          },
          { path: '/settings', element: <SettingsPage /> },
          {
            element: <RequireRole role="Admin" />,
            children: [{ path: '/admin', element: <AdminPage /> }],
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
