import type { CurrentUser } from '@/types/api'

/** 登入後的預設頁面：未完成初次設定的學生先到 Onboarding；管理者可直接進入後台。 */
export function homePathFor(user: Pick<CurrentUser, 'onboardingCompleted' | 'roles'>): string {
  if (user.onboardingCompleted) return '/dashboard'
  return user.roles.includes('Admin') ? '/admin' : '/onboarding'
}
