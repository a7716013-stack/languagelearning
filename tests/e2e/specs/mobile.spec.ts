import { expect, test } from '@playwright/test'
import { createReadyStudent } from '../support/helpers'

test('手機版使用底部導航，隱藏桌面側欄', async ({ page }) => {
  await page.setViewportSize({ width: 393, height: 851 })
  await createReadyStudent(page)

  const bottomNav = page.getByRole('navigation', { name: '主要導覽（行動版）' })
  await expect(bottomNav).toBeVisible()
  await expect(page.getByRole('navigation', { name: '主要導覽', exact: true })).toBeHidden()

  await bottomNav.getByRole('link', { name: '設定' }).click()
  await expect(page.getByRole('heading', { name: '設定', exact: true })).toBeVisible()
})
