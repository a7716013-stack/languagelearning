import { useQuery } from '@tanstack/react-query'
import { examApi, userApi } from '@/services/api'
import { queryKeys } from '@/services/query-keys'

export function useExamTypes() {
  return useQuery({
    queryKey: queryKeys.examTypes,
    queryFn: ({ signal }) => examApi.types(signal),
    staleTime: 10 * 60_000,
  })
}

export function useExamGoals() {
  return useQuery({
    queryKey: queryKeys.examGoals,
    queryFn: ({ signal }) => userApi.goals(signal),
  })
}
