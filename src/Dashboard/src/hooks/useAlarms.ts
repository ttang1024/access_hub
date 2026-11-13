import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiFetch } from '../lib/api'
import type { Alarm, Paged } from '../lib/types'
import { useAccessToken } from './useAccessToken'

export const OPEN_ALARMS_KEY = ['alarms', 'open'] as const

export function useOpenAlarms() {
  const token = useAccessToken()
  return useQuery({
    queryKey: OPEN_ALARMS_KEY,
    queryFn: () => apiFetch<Paged<Alarm>>('/api/v1/alarms?state=open&pageSize=100', token),
    enabled: !!token,
  })
}

export function useAlarmCommand() {
  const token = useAccessToken()
  const client = useQueryClient()
  return useMutation({
    mutationFn: ({ id, action, note }: { id: string; action: 'acknowledge' | 'close'; note?: string }) =>
      apiFetch<void>(`/api/v1/alarms/${id}/${action}`, token, {
        method: 'POST',
        body: action === 'close' ? JSON.stringify({ note }) : undefined,
      }),
    // The hub pushes the change; refetching as well keeps the list right if the push is missed.
    onSettled: () => client.invalidateQueries({ queryKey: OPEN_ALARMS_KEY }),
  })
}
