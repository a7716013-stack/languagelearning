import { useQuery } from '@tanstack/react-query'
import { userApi } from '@/services/api'
import { queryKeys } from '@/services/query-keys'

/** 目前登入者；未登入時 data 為 null。 */
export function useCurrentUser() {
  return useQuery({
    queryKey: queryKeys.me,
    queryFn: ({ signal }) => userApi.me(signal),
    staleTime: 60_000,
  })
}

/** 只能在 RequireAuth 之內使用：保證已登入。 */
export function useAuthenticatedUser() {
  const { data } = useCurrentUser()
  if (!data) throw new Error('useAuthenticatedUser 只能在已登入的路由中使用。')
  return data
}
