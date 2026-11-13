import { useQuery } from '@tanstack/react-query'
import { apiFetch } from '../lib/api'
import type { SiteSyncStatus } from '../lib/types'
import { useAccessToken } from './useAccessToken'

export function useSiteStatus() {
  const token = useAccessToken()
  return useQuery({
    queryKey: ['sites', 'sync-status'],
    queryFn: () => apiFetch<SiteSyncStatus[]>('/api/v1/sites/sync-status', token),
    enabled: !!token,
    refetchInterval: 5000,
  })
}
