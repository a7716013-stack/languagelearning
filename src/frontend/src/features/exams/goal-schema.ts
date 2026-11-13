import { z } from 'zod'
import type { ExamType } from '@/types/api'

export const DAILY_MINUTES_MIN = 10
export const DAILY_MINUTES_MAX = 600
export const DAILY_MINUTE_PRESETS = [15, 30, 60, 90, 120] as const

/** 依考試類型（分數範圍與級距）與使用者時區的「今天」建立目標驗證規則。 */
export function createGoalSchema(examType: Pick<ExamType, 'minScore' | 'maxScore' | 'scoreStep'>, today: string) {
  return z.object({
    targetScore: z
      .number({ error: '請輸入目標分數。' })
      .min(examType.minScore, `目標分數不可低於 ${examType.minScore}。`)
      .max(examType.maxScore, `目標分數不可高於 ${examType.maxScore}。`)
      .refine(
        (value) => examType.scoreStep <= 0 || Math.abs(((value - examType.minScore) / examType.scoreStep) % 1) < 1e-9,
        `目標分數需以 ${examType.scoreStep} 分為單位。`,
      ),
    examDate: z
      .string()
      .regex(/^\d{4}-\d{2}-\d{2}$/, '請選擇考試日期。')
      .refine((value) => value >= today, '考試日期不可早於今天。'),
    dailyStudyMinutes: z
      .number({ error: '請輸入每日學習時間。' })
      .int('請輸入整數分鐘。')
      .min(DAILY_MINUTES_MIN, `每日學習時間至少 ${DAILY_MINUTES_MIN} 分鐘。`)
      .max(DAILY_MINUTES_MAX, `每日學習時間最多 ${DAILY_MINUTES_MAX} 分鐘。`),
  })
}

export type GoalValues = z.infer<ReturnType<typeof createGoalSchema>>
