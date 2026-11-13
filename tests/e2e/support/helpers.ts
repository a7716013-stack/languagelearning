import { expect, type Page } from '@playwright/test'
import { waitForEmailLink } from './mailpit'

export const PASSWORD = 'Student1Pass'

export function uniqueEmail(prefix = 'e2e'): string {
  return `${prefix}-${Date.now()}-${Math.random().toString(36).slice(2, 8)}@example.com`
}

export async function register(page: Page, email: string, displayName = 'E2E 學生', password = PASSWORD) {
  await page.goto('/register')
  await page.getByLabel('顯示名稱').fill(displayName)
  await page.getByLabel('Email').fill(email)
  await page.getByLabel('密碼', { exact: true }).fill(password)
  await page.getByLabel('確認密碼').fill(password)
  await page.getByRole('button', { name: '註冊' }).click()
  await expect(page.getByRole('heading', { name: '請查看你的信箱' })).toBeVisible()
}

export async function confirmEmail(page: Page, email: string) {
  const link = await waitForEmailLink(email, '/confirm-email')
  await page.goto(link)
  await expect(page.getByText('Email 驗證完成')).toBeVisible()
}

export async function login(page: Page, email: string, password = PASSWORD) {
  await page.goto('/login')
  await page.getByLabel('Email').fill(email)
  await page.getByLabel('密碼', { exact: true }).fill(password)
  await page.getByRole('button', { name: '登入', exact: true }).click()
}

export async function completeOnboarding(page: Page, targetScore = '850') {
  await expect(page).toHaveURL(/\/onboarding$/)
  await page.getByRole('radio', { name: /TOEIC/ }).click()
  await page.getByRole('button', { name: '下一步' }).click()
  await page.getByLabel('目標分數').fill(targetScore)
  const examDate = new Date(Date.now() + 90 * 24 * 60 * 60 * 1000).toISOString().slice(0, 10)
  await page.getByLabel('預計考試日期').fill(examDate)
  await page.getByRole('button', { name: '60 分鐘' }).click()
  await page.getByRole('button', { name: '儲存目標' }).click()
  await page.getByRole('button', { name: '完成設定，前往學習首頁' }).click()
  await expect(page).toHaveURL(/\/dashboard$/)
}

/** 建立已驗證並完成初次設定的學生，停在學習首頁。 */
export async function createReadyStudent(page: Page): Promise<string> {
  const email = uniqueEmail()
  await register(page, email)
  await confirmEmail(page, email)
  await login(page, email)
  await completeOnboarding(page)
  return email
}
