import { useEffect } from 'react'

export function useDocumentTitle(title: string) {
  useEffect(() => {
    document.title = title ? `${title}｜LanguageLearning` : 'LanguageLearning — AI 英文檢定學習平台'
  }, [title])
}
