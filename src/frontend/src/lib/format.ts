/** 以使用者時區格式化 UTC 時間。 */
export function formatDateTime(isoUtc: string | null | undefined, timeZone?: string): string {
  if (!isoUtc) return '—'
  const date = new Date(isoUtc.endsWith('Z') || /[+-]\d\d:\d\d$/.test(isoUtc) ? isoUtc : `${isoUtc}Z`)
  if (Number.isNaN(date.getTime())) return '—'
  return new Intl.DateTimeFormat('zh-TW', { dateStyle: 'medium', timeStyle: 'short', timeZone }).format(date)
}

/** 格式化 yyyy-MM-dd 日期（不做時區轉換）。 */
export function formatDateOnly(value: string | null | undefined): string {
  if (!value) return '—'
  const [year, month, day] = value.split('-').map(Number)
  if (!year || !month || !day) return value
  return `${year} 年 ${month} 月 ${day} 日`
}

/** 指定時區的今天（yyyy-MM-dd）。 */
export function todayInTimeZone(timeZone?: string): string {
  const parts = new Intl.DateTimeFormat('en-CA', {
    timeZone,
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  }).formatToParts(new Date())
  const get = (type: string) => parts.find((part) => part.type === type)?.value ?? ''
  return `${get('year')}-${get('month')}-${get('day')}`
}

export function formatScore(value: number): string {
  return Number.isInteger(value) ? String(value) : value.toFixed(1)
}
