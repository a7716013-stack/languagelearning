import type { AdminUserQuery, AuditLogQuery } from './api'

export const queryKeys = {
  me: ['me'] as const,
  examTypes: ['exam-types'] as const,
  examGoals: ['exam-goals'] as const,
  version: ['version'] as const,
  adminSummary: ['admin', 'summary'] as const,
  adminUsers: (params: AdminUserQuery) => ['admin', 'users', params] as const,
  adminAuditLogs: (params: AuditLogQuery) => ['admin', 'audit-logs', params] as const,
}
