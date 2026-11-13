import AxeBuilder from '@axe-core/playwright'
import { expect, test, type Page } from '@playwright/test'
import { createReadyStudent } from '../support/helpers'

async function expectNoSeriousViolations(page: Page) {
  const results = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa']).analyze()
  const serious = results.violations.filter((violation) => ['serious', 'critical'].includes(violation.impact ?? ''))
  expect(
    serious.map((violation) => `${violation.id}: ${violation.nodes.map((node) => node.target.join(' ')).join(', ')}`),
  ).toEqual([])
}

test.describe('無障礙檢查', () => {
  for (const path of ['/', '/login', '/register', '/forgot-password']) {
    test(`公開頁面 ${path}`, async ({ page }) => {
      await page.goto(path)
      await page.waitForLoadState('networkidle')
      await expectNoSeriousViolations(page)
    })
  }

  test('登入後的頁面（淺色與深色）', async ({ page }) => {
    await createReadyStudent(page)
    await expectNoSeriousViolations(page)

    await page.goto('/settings')
    await expect(page.getByRole('heading', { name: '設定', exact: true })).toBeVisible()
    await expectNoSeriousViolations(page)

    await page.evaluate(() => window.localStorage.setItem('ll-theme', 'dark'))
    await page.goto('/dashboard')
    await expect(page.locator('html')).toHaveClass(/dark/)
    await expectNoSeriousViolations(page)
  })

  test('可以用鍵盤跳到主要內容', async ({ page }) => {
    await page.goto('/login')
    await expect(page.getByRole('heading', { name: '登入' })).toBeVisible()
    await page.keyboard.press('Tab')
    await expect(page.getByRole('link', { name: '跳到主要內容' })).toBeFocused()
  })
})
