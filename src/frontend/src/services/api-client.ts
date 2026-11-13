import type { CsrfTokenResponse, ProblemDetails } from '@/types/api'

export const CSRF_HEADER = 'X-XSRF-TOKEN'

/** API 錯誤，包含後端的穩定錯誤代碼與欄位錯誤。 */
export class ApiError extends Error {
  readonly status: number
  readonly code: string | undefined
  readonly fieldErrors: Record<string, string[]>
  readonly traceId: string | undefined

  constructor(status: number, problem: ProblemDetails | null, cause?: unknown) {
    super(problem?.detail ?? problem?.title ?? defaultMessage(status), cause === undefined ? undefined : { cause })
    this.name = 'ApiError'
    this.status = status
    this.code = problem?.code
    this.fieldErrors = problem?.errors ?? {}
    this.traceId = problem?.traceId
  }
}

function defaultMessage(status: number): string {
  if (status === 0) return '無法連線到伺服器，請檢查網路後再試。'
  if (status === 401) return '請先登入。'
  if (status === 403) return '您沒有權限執行此操作。'
  if (status === 404) return '找不到要求的資料。'
  if (status === 429) return '請求太頻繁，請稍後再試。'
  if (status >= 500) return '伺服器發生錯誤，請稍後再試。'
  return '發生錯誤，請稍後再試。'
}

// CSRF Token 只存在記憶體中（不使用 localStorage）。Token 綁定使用者身分，登入 / 登出後需重新取得。
let csrfToken: string | null = null
let csrfRequest: Promise<string> | null = null

export function clearCsrfToken(): void {
  csrfToken = null
  csrfRequest = null
}

async function getCsrfToken(forceRefresh = false): Promise<string> {
  if (!forceRefresh && csrfToken) return csrfToken
  if (!forceRefresh && csrfRequest) return csrfRequest

  csrfRequest = (async () => {
    const response = await fetch('/api/auth/csrf', {
      method: 'GET',
      credentials: 'same-origin',
      headers: { Accept: 'application/json' },
      cache: 'no-store',
    })
    if (!response.ok) throw await toApiError(response)
    const body = (await response.json()) as CsrfTokenResponse
    csrfToken = body.token
    return body.token
  })()

  try {
    return await csrfRequest
  } finally {
    csrfRequest = null
  }
}

async function readProblem(response: Response): Promise<ProblemDetails | null> {
  const contentType = response.headers.get('content-type') ?? ''
  if (!contentType.includes('json')) return null
  try {
    return (await response.json()) as ProblemDetails
  } catch {
    return null
  }
}

async function toApiError(response: Response): Promise<ApiError> {
  return new ApiError(response.status, await readProblem(response))
}

export type HttpMethod = 'GET' | 'POST' | 'PUT' | 'DELETE'

export interface RequestOptions {
  method?: HttpMethod
  body?: unknown
  signal?: AbortSignal
}

/**
 * 呼叫同源 API。變更狀態的請求自動帶上 CSRF Token；Token 失效時自動重新取得並重試一次。
 */
export async function apiRequest<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const method = options.method ?? 'GET'
  const unsafe = method !== 'GET'

  const send = async (token: string | null) => {
    const headers: Record<string, string> = { Accept: 'application/json' }
    if (options.body !== undefined) headers['Content-Type'] = 'application/json'
    if (token) headers[CSRF_HEADER] = token

    try {
      return await fetch(path, {
        method,
        credentials: 'same-origin',
        headers,
        body: options.body === undefined ? undefined : JSON.stringify(options.body),
        signal: options.signal,
      })
    } catch (error) {
      if (error instanceof DOMException && error.name === 'AbortError') throw error
      throw new ApiError(0, null, error)
    }
  }

  let response = await send(unsafe ? await getCsrfToken() : null)

  if (unsafe && response.status === 400) {
    const problem = await readProblem(response.clone())
    if (problem?.code === 'csrf_invalid') {
      response = await send(await getCsrfToken(true))
    }
  }

  if (!response.ok) throw await toApiError(response)
  if (response.status === 204) return undefined as T

  const text = await response.text()
  return (text ? JSON.parse(text) : undefined) as T
}

export function isApiError(error: unknown, status?: number): error is ApiError {
  return error instanceof ApiError && (status === undefined || error.status === status)
}
