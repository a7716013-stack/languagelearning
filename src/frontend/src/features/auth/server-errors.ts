import type { FieldValues, Path, UseFormSetError } from 'react-hook-form'
import { isApiError } from '@/services/api-client'

/** 將後端 ValidationProblem 的欄位錯誤套用到表單；回傳是否有對應到欄位。 */
export function applyServerFieldErrors<T extends FieldValues>(
  error: unknown,
  setError: UseFormSetError<T>,
  fieldMap: Partial<Record<string, Path<T>>> = {},
): boolean {
  if (!isApiError(error)) return false
  let applied = false
  for (const [field, messages] of Object.entries(error.fieldErrors)) {
    const target = fieldMap[field] ?? (field as Path<T>)
    if (messages.length > 0) {
      setError(target, { type: 'server', message: messages[0] })
      applied = true
    }
  }
  return applied
}

export function errorMessage(error: unknown, fallback = '發生錯誤，請稍後再試。'): string {
  if (isApiError(error)) return error.message
  return fallback
}
