/**
 * 只允許站內相對路徑作為登入後的導向目標，避免 Open Redirect。
 */
export function sanitizeReturnTo(value: string | null | undefined, fallback = '/dashboard'): string {
  if (!value) return fallback
  if (!value.startsWith('/') || value.startsWith('//') || value.startsWith('/\\')) return fallback
  if (/^\/(login|register|forgot-password|reset-password|confirm-email)(\/|\?|$)/.test(value)) return fallback
  return value
}
