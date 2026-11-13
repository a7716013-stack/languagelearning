import { describe, expect, it } from 'vitest'
import { homePathFor } from './home-path'
import { sanitizeReturnTo } from './safe-redirect'

describe('sanitizeReturnTo', () => {
  it.each([
    ['/settings', '/settings'],
    ['/admin?tab=users', '/admin?tab=users'],
    [null, '/dashboard'],
    ['', '/dashboard'],
    ['https://evil.example.com', '/dashboard'],
    ['//evil.example.com', '/dashboard'],
    ['/\\evil.example.com', '/dashboard'],
    ['/login', '/dashboard'],
    ['/login?returnTo=/x', '/dashboard'],
  ])('%s → %s', (input, expected) => {
    expect(sanitizeReturnTo(input)).toBe(expected)
  })
})

describe('homePathFor', () => {
  it('未完成初次設定的學生前往 Onboarding', () => {
    expect(homePathFor({ onboardingCompleted: false, roles: ['Student'] })).toBe('/onboarding')
  })

  it('管理者可直接進入後台', () => {
    expect(homePathFor({ onboardingCompleted: false, roles: ['Admin'] })).toBe('/admin')
  })

  it('已完成設定前往學習首頁', () => {
    expect(homePathFor({ onboardingCompleted: true, roles: ['Student'] })).toBe('/dashboard')
  })
})
