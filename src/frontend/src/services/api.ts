import type {
  AdminSummary,
  AdminUser,
  AuditLogEntry,
  CurrentUser,
  ExamGoal,
  ExamType,
  ExamVersion,
  MessageResponse,
  PagedResult,
  UpdateProfileRequest,
  UpsertExamGoalRequest,
  VersionInfo,
} from '@/types/api'
import { apiRequest, clearCsrfToken, isApiError } from './api-client'

function query(params: Record<string, string | number | undefined | null>): string {
  const search = new URLSearchParams()
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null && value !== '') search.set(key, String(value))
  }
  const text = search.toString()
  return text ? `?${text}` : ''
}

export const authApi = {
  register: (body: { email: string; password: string; displayName: string }) =>
    apiRequest<MessageResponse>('/api/auth/register', { method: 'POST', body }),

  confirmEmail: (body: { userId: string; token: string }) =>
    apiRequest<MessageResponse>('/api/auth/confirm-email', { method: 'POST', body }),

  resendConfirmation: (email: string) =>
    apiRequest<MessageResponse>('/api/auth/resend-confirmation', { method: 'POST', body: { email } }),

  login: async (body: { email: string; password: string; rememberMe: boolean }) => {
    const user = await apiRequest<CurrentUser>('/api/auth/login', { method: 'POST', body })
    clearCsrfToken()
    return user
  },

  logout: async () => {
    try {
      await apiRequest<void>('/api/auth/logout', { method: 'POST' })
    } finally {
      clearCsrfToken()
    }
  },

  forgotPassword: (email: string) =>
    apiRequest<MessageResponse>('/api/auth/forgot-password', { method: 'POST', body: { email } }),

  resetPassword: (body: { userId: string; token: string; newPassword: string }) =>
    apiRequest<MessageResponse>('/api/auth/reset-password', { method: 'POST', body }),

  changePassword: (body: { currentPassword: string; newPassword: string }) =>
    apiRequest<MessageResponse>('/api/auth/change-password', { method: 'POST', body }),
}

export const userApi = {
  /** 取得目前使用者；未登入時回傳 null。 */
  me: async (signal?: AbortSignal): Promise<CurrentUser | null> => {
    try {
      return await apiRequest<CurrentUser>('/api/users/me', { signal })
    } catch (error) {
      if (isApiError(error, 401)) return null
      throw error
    }
  },

  updateProfile: (body: UpdateProfileRequest) =>
    apiRequest<CurrentUser>('/api/users/me', { method: 'PUT', body }),

  goals: (signal?: AbortSignal) => apiRequest<ExamGoal[]>('/api/users/me/exam-goals', { signal }),

  saveGoal: (examTypeCode: string, body: UpsertExamGoalRequest) =>
    apiRequest<ExamGoal>(`/api/users/me/exam-goals/${encodeURIComponent(examTypeCode)}`, { method: 'PUT', body }),

  completeOnboarding: () => apiRequest<CurrentUser>('/api/users/me/onboarding/complete', { method: 'POST' }),
}

export const examApi = {
  types: (signal?: AbortSignal) => apiRequest<ExamType[]>('/api/exam-types', { signal }),
  versions: (examType?: string, signal?: AbortSignal) =>
    apiRequest<ExamVersion[]>(`/api/exam-versions${query({ examType })}`, { signal }),
}

export interface AdminUserQuery {
  page: number
  pageSize: number
  search?: string
}

export interface AuditLogQuery {
  page: number
  pageSize: number
  action?: string
}

export const adminApi = {
  summary: (signal?: AbortSignal) => apiRequest<AdminSummary>('/api/admin/summary', { signal }),
  users: (params: AdminUserQuery, signal?: AbortSignal) =>
    apiRequest<PagedResult<AdminUser>>(`/api/admin/users${query({ ...params })}`, { signal }),
  auditLogs: (params: AuditLogQuery, signal?: AbortSignal) =>
    apiRequest<PagedResult<AuditLogEntry>>(`/api/admin/audit-logs${query({ ...params })}`, { signal }),
}

export const systemApi = {
  version: (signal?: AbortSignal) => apiRequest<VersionInfo>('/api/version', { signal }),
}
