import { beforeEach, describe, expect, it } from 'vitest'
import { csrfHandler, json, mockFetch } from '@/test/fetch-mock'
import { ApiError, apiRequest, clearCsrfToken, CSRF_HEADER } from './api-client'

function headerOf(init: RequestInit | undefined, name: string): string | undefined {
  return (init?.headers as Record<string, string> | undefined)?.[name]
}

describe('apiRequest', () => {
  beforeEach(() => clearCsrfToken())

  it('GET 請求不需要 CSRF Token 並回傳 JSON', async () => {
    const fetchMock = mockFetch({ 'GET /api/exam-types': () => json([{ code: 'TOEIC' }]) })

    const result = await apiRequest<{ code: string }[]>('/api/exam-types')

    expect(result).toEqual([{ code: 'TOEIC' }])
    expect(fetchMock).toHaveBeenCalledTimes(1)
    expect(headerOf(fetchMock.mock.calls[0][1], CSRF_HEADER)).toBeUndefined()
    expect(fetchMock.mock.calls[0][1]?.credentials).toBe('same-origin')
  })

  it('變更狀態的請求會先取得 CSRF Token 並帶在 Header', async () => {
    const fetchMock = mockFetch({
      'GET /api/auth/csrf': csrfHandler,
      'POST /api/auth/forgot-password': () => json({ message: 'ok' }, 202),
    })

    await apiRequest('/api/auth/forgot-password', { method: 'POST', body: { email: 'a@example.com' } })

    const post = fetchMock.mock.calls.find(([, init]) => init?.method === 'POST')
    expect(headerOf(post?.[1], CSRF_HEADER)).toBe('test-csrf-token')
    expect(post?.[1]?.body).toBe(JSON.stringify({ email: 'a@example.com' }))
  })

  it('CSRF Token 失效時重新取得並重試一次', async () => {
    let tokenVersion = 0
    let attempts = 0
    const fetchMock = mockFetch({
      'GET /api/auth/csrf': () => json({ token: `token-${++tokenVersion}` }),
      'POST /api/users/me/onboarding/complete': ({ init }) => {
        attempts += 1
        return headerOf(init, CSRF_HEADER) === 'token-2'
          ? json({ id: '1' })
          : json({ code: 'csrf_invalid', detail: '安全驗證已失效' }, 400)
      },
    })

    const result = await apiRequest<{ id: string }>('/api/users/me/onboarding/complete', { method: 'POST' })

    expect(result).toEqual({ id: '1' })
    expect(attempts).toBe(2)
    expect(fetchMock.mock.calls.filter(([url]) => url === '/api/auth/csrf')).toHaveLength(2)
  })

  it('錯誤回應轉為 ApiError，包含錯誤代碼與欄位錯誤', async () => {
    mockFetch({
      'GET /api/auth/csrf': csrfHandler,
      'POST /api/auth/register': () =>
        json(
          {
            title: '資料驗證失敗',
            detail: '輸入資料有誤',
            code: 'validation_failed',
            errors: { email: ['Email 格式不正確。'] },
          },
          400,
        ),
    })

    const error = await apiRequest('/api/auth/register', { method: 'POST', body: {} }).catch((e: unknown) => e)

    expect(error).toBeInstanceOf(ApiError)
    const apiError = error as ApiError
    expect(apiError.status).toBe(400)
    expect(apiError.code).toBe('validation_failed')
    expect(apiError.fieldErrors.email).toEqual(['Email 格式不正確。'])
    expect(apiError.message).toBe('輸入資料有誤')
  })

  it('204 回應回傳 undefined', async () => {
    mockFetch({
      'GET /api/auth/csrf': csrfHandler,
      'POST /api/auth/logout': () => new Response(null, { status: 204 }),
    })

    await expect(apiRequest('/api/auth/logout', { method: 'POST' })).resolves.toBeUndefined()
  })

  it('網路錯誤轉為 status 0 的 ApiError', async () => {
    mockFetch({
      'GET /api/version': () => {
        throw new TypeError('Failed to fetch')
      },
    })

    const error = await apiRequest('/api/version').catch((e: unknown) => e)

    expect(error).toBeInstanceOf(ApiError)
    expect((error as ApiError).status).toBe(0)
  })
})
