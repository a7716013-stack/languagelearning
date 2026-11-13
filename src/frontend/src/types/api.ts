/** 與後端 DTO 對應的型別（JSON 為 camelCase）。 */

export type UiTheme = 'system' | 'light' | 'dark'

export interface CurrentUser {
  id: string
  email: string
  displayName: string
  roles: string[]
  timeZoneId: string
  uiTheme: UiTheme
  emailConfirmed: boolean
  onboardingCompleted: boolean
  createdAtUtc: string
}

export interface MessageResponse {
  message: string
}

export interface CsrfTokenResponse {
  token: string
}

export interface ExamType {
  code: string
  name: string
  description: string
  isAvailable: boolean
  minScore: number
  maxScore: number
  scoreStep: number
}

export interface ExamVersion {
  code: string
  examTypeCode: string
  name: string
  description: string
  isCurrent: boolean
  effectiveFrom: string | null
}

export interface ExamGoal {
  examTypeCode: string
  examTypeName: string
  targetScore: number
  examDate: string
  dailyStudyMinutes: number
  daysUntilExam: number
  updatedAtUtc: string
  version: string
}

export interface UpsertExamGoalRequest {
  targetScore: number
  examDate: string
  dailyStudyMinutes: number
  version: string | null
}

export interface UpdateProfileRequest {
  displayName: string
  timeZoneId: string
  uiTheme: UiTheme
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface AdminSummary {
  totalUsers: number
  confirmedUsers: number
  adminCount: number
  newUsersLast7Days: number
  successfulLoginsLast24Hours: number
  failedLoginsLast24Hours: number
}

export interface AdminUser {
  id: string
  email: string
  displayName: string
  roles: string[]
  emailConfirmed: boolean
  isLockedOut: boolean
  createdAtUtc: string
  lastLoginAtUtc: string | null
}

export interface AuditLogEntry {
  id: number
  occurredAtUtc: string
  actorUserId: string | null
  actorEmail: string | null
  action: string
  entityType: string | null
  entityId: string | null
  succeeded: boolean
  ipAddress: string | null
  details: string | null
}

export interface VersionInfo {
  version: string
  environment: string
  commit: string | null
}

export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  code?: string
  traceId?: string
  errors?: Record<string, string[]>
}
