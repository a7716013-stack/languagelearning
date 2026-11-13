import { expect, test } from '@playwright/test'
import { createReadyStudent } from '../support/helpers'

test.describe('PWA', () => {
  test('提供 Manifest 與圖示', async ({ page, request }) => {
    await page.goto('/')
    const href = await page.locator('link[rel="manifest"]').getAttribute('href')
    expect(href).toBeTruthy()

    const response = await request.get(href!)
    expect(response.ok()).toBeTruthy()
    const manifest = (await response.json()) as { name: string; lang: string; display: string; icons: unknown[] }
    expect(manifest.lang).toBe('zh-Hant-TW')
    expect(manifest.display).toBe('standalone')
    expect(manifest.icons.length).toBeGreaterThanOrEqual(3)
  })

  test('註冊 Service Worker，且不快取 API 回應', async ({ page }) => {
    await page.goto('/')
    await expect
      .poll(() => page.evaluate(async () => Boolean(await navigator.serviceWorker.getRegistration())), {
        timeout: 20_000,
      })
      .toBe(true)

    await createReadyStudent(page)

    await expect
      .poll(() => page.evaluate(async () => Boolean(await navigator.serviceWorker.getRegistration())), {
        timeout: 20_000,
      })
      .toBe(true)

    await page.reload()
    await expect(page.getByRole('heading', { name: /哈囉/ })).toBeVisible()

    const cachedApiUrls = await page.evaluate(async () => {
      const urls: string[] = []
      for (const name of await caches.keys()) {
        const cache = await caches.open(name)
        for (const request of await cache.keys()) {
          if (new URL(request.url).pathname.startsWith('/api/')) urls.push(request.url)
        }
      }
      return urls
    })
    expect(cachedApiUrls).toEqual([])
  })

  test('離線時顯示網路狀態提示', async ({ page, context }) => {
    await page.goto('/login')

    await context.setOffline(true)
    await expect(page.getByRole('status').filter({ hasText: '目前處於離線狀態' })).toBeVisible()

    await context.setOffline(false)
    await expect(page.getByText('目前處於離線狀態')).toBeHidden()
  })

  test('正式建置包含 Content-Security-Policy', async ({ page }) => {
    await page.goto('/')
    const csp = await page.locator('meta[http-equiv="Content-Security-Policy"]').getAttribute('content')
    expect(csp).toContain("default-src 'self'")
    expect(csp).toContain("script-src 'self'")
  })
})
