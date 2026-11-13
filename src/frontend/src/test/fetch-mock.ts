import { vi } from 'vitest'

export type MockHandler = (request: { url: string; init: RequestInit | undefined }) => Response | Promise<Response>

export function json(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': status >= 400 ? 'application/problem+json' : 'application/json' },
  })
}

/** 以「METHOD path」為 key 模擬 fetch，例如 'GET /api/users/me'。 */
export function mockFetch(handlers: Record<string, MockHandler>) {
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = typeof input === 'string' ? input : input instanceof URL ? input.toString() : input.url
    const method = (init?.method ?? 'GET').toUpperCase()
    const path = url.split('?')[0]
    const handler = handlers[`${method} ${path}`] ?? handlers[`${method} ${url}`]
    if (!handler) return json({ title: 'not mocked', code: 'not_mocked' }, 404)
    return handler({ url, init })
  })
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

export const csrfHandler: MockHandler = () => json({ token: 'test-csrf-token' })
