import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router'
import { toast } from 'sonner'
import { authApi } from '@/services/api'
import { queryKeys } from '@/services/query-keys'

export function useLogout() {
  const queryClient = useQueryClient()
  const navigate = useNavigate()

  return useMutation({
    mutationFn: authApi.logout,
    onSettled: () => {
      // 清除所有快取的個人資料。
      queryClient.clear()
      queryClient.setQueryData(queryKeys.me, null)
      toast.success('已登出。')
      void navigate('/login', { replace: true })
    },
  })
}
