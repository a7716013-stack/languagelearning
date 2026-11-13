import { describe, expect, it } from 'vitest'
import { createGoalSchema } from './goal-schema'

const toeic = { minScore: 10, maxScore: 990, scoreStep: 5 }
const schema = createGoalSchema(toeic, '2026-11-05')

describe('createGoalSchema', () => {
  it('接受有效目標', () => {
    expect(schema.safeParse({ targetScore: 850, examDate: '2027-01-10', dailyStudyMinutes: 60 }).success).toBe(true)
  })

  it.each([5, 803, 995])('拒絕無效分數 %d', (targetScore) => {
    expect(schema.safeParse({ targetScore, examDate: '2027-01-10', dailyStudyMinutes: 60 }).success).toBe(false)
  })

  it('拒絕早於今天的考試日期，但接受今天', () => {
    expect(schema.safeParse({ targetScore: 700, examDate: '2026-11-04', dailyStudyMinutes: 60 }).success).toBe(false)
    expect(schema.safeParse({ targetScore: 700, examDate: '2026-11-05', dailyStudyMinutes: 60 }).success).toBe(true)
  })

  it('每日學習時間須介於 10 到 600 分鐘', () => {
    expect(schema.safeParse({ targetScore: 700, examDate: '2027-01-10', dailyStudyMinutes: 5 }).success).toBe(false)
    expect(schema.safeParse({ targetScore: 700, examDate: '2027-01-10', dailyStudyMinutes: 601 }).success).toBe(false)
  })

  it('支援 0.5 級距（IELTS 類型）', () => {
    const ielts = createGoalSchema({ minScore: 0, maxScore: 9, scoreStep: 0.5 }, '2026-11-05')
    expect(ielts.safeParse({ targetScore: 6.5, examDate: '2027-01-10', dailyStudyMinutes: 60 }).success).toBe(true)
    expect(ielts.safeParse({ targetScore: 6.25, examDate: '2027-01-10', dailyStudyMinutes: 60 }).success).toBe(false)
  })
})
