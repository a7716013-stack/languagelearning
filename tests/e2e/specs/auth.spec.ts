import { expect, test } from '@playwright/test'
import { waitForEmailLink } from '../support/mail'
import { completeOnboarding, confirmEmail, login, PASSWORD, register, uniqueEmail } from '../support/helpers'

test.describe('帳號流程', () => {
  test('註冊 → Email 驗證 → 登入 → 初次設定 → 個人資料 → 登出', async ({ page }) => {
    const email = uniqueEmail('flow')

    await register(page, email, '流程測試')
    await confirmEmail(page, email)

    await login(page, email)
    await completeOnboarding(page, '850')

    await expect(page.getByRole('heading', { name: '哈囉，流程測試' })).toBeVisible()
    await expect(page.getByText('850', { exact: true })).toBeVisible()
    await expect(page.getByText(/還有 \d+ 天/)).toBeVisible()

    // 重新整理後仍保持登入（Cookie），資料由伺服器提供。
    await page.reload()
    await expect(page.getByRole('heading', { name: '哈囉，流程測試' })).toBeVisible()

    await page.getByRole('navigation', { name: '主要導覽' }).getByRole('link', { name: '設定' }).click()
    await page.getByLabel('顯示名稱').fill('改過的名字')
    await page.getByRole('button', { name: '儲存變更' }).click()
    await expect(page.getByText('個人資料已更新。')).toBeVisible()

    await page.getByRole('button', { name: '登出' }).first().click()
    await expect(page).toHaveURL(/\/login$/)

    await page.goto('/dashboard')
    await expect(page).toHaveURL(/\/login\?returnTo=%2Fdashboard$/)
  })

  test('未驗證 Email 時無法登入，並可重寄驗證信', async ({ page }) => {
    const email = uniqueEmail('unconfirmed')
    await register(page, email)

    await login(page, email)

    await expect(page.getByRole('alert')).toContainText('尚未完成 Email 驗證')
    await page.getByRole('button', { name: /重新寄送驗證信/ }).click()
    await expect(page.getByRole('button', { name: /已重新寄出驗證信/ })).toBeDisabled()
  })

  test('錯誤密碼顯示一致的錯誤訊息', async ({ page }) => {
    await login(page, uniqueEmail('nobody'), 'WrongPass123')

    await expect(page.getByRole('alert')).toContainText('Email 或密碼不正確')
  })

  test('忘記密碼 → 重設 → 以新密碼登入', async ({ page }) => {
    const email = uniqueEmail('reset')
    const newPassword = 'Brand1NewPass'
    await register(page, email)
    await confirmEmail(page, email)

    await page.goto('/forgot-password')
    await page.getByLabel('Email').fill(email)
    await page.getByRole('button', { name: '寄送重設連結' }).click()
    await expect(page.getByText('請查看你的信箱')).toBeVisible()

    const link = await waitForEmailLink(email, '/reset-password')
    await page.goto(link)
    await page.getByLabel('新密碼', { exact: true }).fill(newPassword)
    await page.getByLabel('確認新密碼').fill(newPassword)
    await page.getByRole('button', { name: '重設密碼' }).click()
    await expect(page.getByText('密碼已重設', { exact: true })).toBeVisible()

    await login(page, email, PASSWORD)
    await expect(page.getByRole('alert')).toContainText('Email 或密碼不正確')

    await login(page, email, newPassword)
    await expect(page).toHaveURL(/\/onboarding$/)
  })
})
