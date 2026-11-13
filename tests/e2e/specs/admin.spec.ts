import { expect, test } from '@playwright/test'
import { createReadyStudent, login } from '../support/helpers'

const adminEmail = process.env.E2E_ADMIN_EMAIL ?? 'admin@languagelearning.test'
const adminPassword = process.env.E2E_ADMIN_PASSWORD

test.describe('管理後台', () => {
  test('學生無法進入管理後台', async ({ page }) => {
    await createReadyStudent(page)

    await page.goto('/admin')

    await expect(page.getByRole('heading', { name: '沒有權限' })).toBeVisible()
  })

  test('管理者可以查看使用者與稽核紀錄', async ({ page }) => {
    test.skip(!adminPassword, '未設定 E2E_ADMIN_PASSWORD，略過管理者測試。')

    await login(page, adminEmail, adminPassword!)
    await expect(page).toHaveURL(/\/(admin|dashboard)$/)
    await page.goto('/admin')

    await expect(page.getByRole('heading', { name: '管理後台' })).toBeVisible()
    await expect(page.getByText('使用者總數')).toBeVisible()

    await page.getByRole('tab', { name: '使用者' }).click()
    await expect(page.getByRole('table')).toBeVisible()

    await page.getByRole('tab', { name: '稽核紀錄' }).click()
    await expect(page.getByRole('table')).toContainText('auth.login')
  })
})
