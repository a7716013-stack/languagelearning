import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { reloadPage } from '@/lib/reload-page'
import { lazyPage } from './lazy-page'

vi.mock('@/lib/reload-page', () => ({ reloadPage: vi.fn() }))

describe('lazyPage', () => {
  it('下載完成後顯示頁面', async () => {
    render(lazyPage(async () => () => <h1>練習頁</h1>))

    expect(await screen.findByRole('heading', { name: '練習頁' })).toBeInTheDocument()
  })

  it('下載失敗時顯示可重新載入的錯誤訊息', async () => {
    render(lazyPage(() => Promise.reject(new TypeError('Failed to fetch dynamically imported module'))))

    const alert = await screen.findByRole('alert')
    expect(alert).toHaveTextContent('無法載入頁面')
    expect(alert).toHaveTextContent('請確認網路連線後重新載入。')

    await userEvent.click(screen.getByRole('button', { name: '重試' }))
    expect(reloadPage).toHaveBeenCalledOnce()
  })
})
