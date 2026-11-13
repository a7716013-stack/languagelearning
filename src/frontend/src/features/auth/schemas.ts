import { z } from 'zod'

// 與後端 FluentValidation 規則一致（後端為準，前端僅提升使用體驗）。
const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

export const emailField = z
  .string()
  .trim()
  .min(1, '請輸入 Email。')
  .max(256, 'Email 長度不可超過 256 字元。')
  .refine((value) => EMAIL_PATTERN.test(value), 'Email 格式不正確。')

export const strongPasswordField = z
  .string()
  .min(10, '密碼至少需要 10 個字元。')
  .max(128, '密碼長度不可超過 128 字元。')
  .refine((value) => /[A-Z]/.test(value), '密碼需包含至少一個大寫英文字母。')
  .refine((value) => /[a-z]/.test(value), '密碼需包含至少一個小寫英文字母。')
  .refine((value) => /[0-9]/.test(value), '密碼需包含至少一個數字。')

export const displayNameField = z
  .string()
  .trim()
  .min(1, '請輸入顯示名稱。')
  .max(50, '顯示名稱不可超過 50 字元。')

export const loginSchema = z.object({
  email: emailField,
  password: z.string().min(1, '請輸入密碼。').max(128, '密碼長度不正確。'),
  rememberMe: z.boolean(),
})

export const registerSchema = z
  .object({
    displayName: displayNameField,
    email: emailField,
    password: strongPasswordField,
    confirmPassword: z.string().min(1, '請再次輸入密碼。'),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: '兩次輸入的密碼不一致。',
    path: ['confirmPassword'],
  })

export const forgotPasswordSchema = z.object({ email: emailField })

export const resetPasswordSchema = z
  .object({
    newPassword: strongPasswordField,
    confirmPassword: z.string().min(1, '請再次輸入密碼。'),
  })
  .refine((data) => data.newPassword === data.confirmPassword, {
    message: '兩次輸入的密碼不一致。',
    path: ['confirmPassword'],
  })

export const changePasswordSchema = z
  .object({
    currentPassword: z.string().min(1, '請輸入目前的密碼。'),
    newPassword: strongPasswordField,
    confirmPassword: z.string().min(1, '請再次輸入新密碼。'),
  })
  .refine((data) => data.newPassword === data.confirmPassword, {
    message: '兩次輸入的密碼不一致。',
    path: ['confirmPassword'],
  })
  .refine((data) => data.newPassword !== data.currentPassword, {
    message: '新密碼不可與目前的密碼相同。',
    path: ['newPassword'],
  })

export type LoginValues = z.infer<typeof loginSchema>
export type RegisterValues = z.infer<typeof registerSchema>
export type ForgotPasswordValues = z.infer<typeof forgotPasswordSchema>
export type ResetPasswordValues = z.infer<typeof resetPasswordSchema>
export type ChangePasswordValues = z.infer<typeof changePasswordSchema>
