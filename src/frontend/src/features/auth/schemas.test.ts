import { describe, expect, it } from 'vitest'
import { changePasswordSchema, loginSchema, registerSchema } from './schemas'

describe('registerSchema', () => {
  const valid = {
    displayName: '小明',
    email: 'student@example.com',
    password: 'Correct1Horse',
    confirmPassword: 'Correct1Horse',
  }

  it('接受有效資料', () => {
    expect(registerSchema.safeParse(valid).success).toBe(true)
  })

  it.each([
    ['Short1A', '密碼至少需要 10 個字元。'],
    ['alllowercase1', '密碼需包含至少一個大寫英文字母。'],
    ['ALLUPPERCASE1', '密碼需包含至少一個小寫英文字母。'],
    ['NoDigitsHere', '密碼需包含至少一個數字。'],
  ])('拒絕弱密碼 %s', (password, message) => {
    const result = registerSchema.safeParse({ ...valid, password, confirmPassword: password })
    expect(result.success).toBe(false)
    expect(result.error?.issues.map((issue) => issue.message)).toContain(message)
  })

  it('確認密碼必須一致', () => {
    const result = registerSchema.safeParse({ ...valid, confirmPassword: 'Different1Pass' })
    expect(result.success).toBe(false)
    expect(result.error?.issues[0].path).toEqual(['confirmPassword'])
  })

  it('Email 會去除前後空白並檢查格式', () => {
    expect(registerSchema.safeParse({ ...valid, email: '  student@example.com ' }).data?.email).toBe('student@example.com')
    expect(registerSchema.safeParse({ ...valid, email: 'not-an-email' }).success).toBe(false)
  })
})

describe('loginSchema', () => {
  it('需要 Email 與密碼', () => {
    const result = loginSchema.safeParse({ email: '', password: '', rememberMe: false })
    expect(result.success).toBe(false)
    expect(result.error?.issues.map((issue) => issue.path[0])).toEqual(expect.arrayContaining(['email', 'password']))
  })
})

describe('changePasswordSchema', () => {
  it('新密碼不可與目前密碼相同', () => {
    const result = changePasswordSchema.safeParse({
      currentPassword: 'Correct1Horse',
      newPassword: 'Correct1Horse',
      confirmPassword: 'Correct1Horse',
    })
    expect(result.success).toBe(false)
    expect(result.error?.issues.some((issue) => issue.path[0] === 'newPassword')).toBe(true)
  })
})
