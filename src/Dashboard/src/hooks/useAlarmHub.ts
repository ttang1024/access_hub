import { useEffect, useRef, useState } from 'react'
import { HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr'
import { useQueryClient } from '@tanstack/react-query'
import { useAuth } from 'react-oidc-context'
import { config, MAX_FEED_EVENTS } from '../lib/config'
import type { Alarm, Paged, SiteEvent } from '../lib/types'
import { OPEN_ALARMS_KEY } from './useAlarms'

/** Live site events and alarm changes over SignalR (through the gateway). */
export function useAlarmHub() {
  const auth = useAuth()
  const client = useQueryClient()
  const [events, setEvents] = useState<SiteEvent[]>([])
  const [state, setState] = useState<HubConnectionState>(HubConnectionState.Disconnected)
  const tokenRef = useRef(auth.user?.access_token)

  useEffect(() => {
    tokenRef.current = auth.user?.access_token
  }, [auth.user?.access_token])

  const isAuthenticated = auth.isAuthenticated
  useEffect(() => {
    if (!isAuthenticated) return
    const connection = new HubConnectionBuilder()
      .withUrl(`${config.gatewayUrl}/hubs/alarms`, { accessTokenFactory: () => tokenRef.current ?? '' })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()

    connection.on('siteEvent', (event: SiteEvent) =>
      setEvents((current) => [event, ...current.filter((e) => e.eventId !== event.eventId)].slice(0, MAX_FEED_EVENTS)),
    )
    connection.on('alarmChanged', (alarm: Alarm) =>
      client.setQueryData<Paged<Alarm>>(OPEN_ALARMS_KEY, (page) => {
        if (!page) return page
        const others = page.items.filter((a) => a.id !== alarm.id)
        const items = alarm.state === 'Closed' ? others : [alarm, ...others]
        items.sort((a, b) => b.raisedAt.localeCompare(a.raisedAt))
        return { ...page, items, totalCount: items.length }
      }),
    )
    connection.onreconnecting(() => setState(HubConnectionState.Reconnecting))
    connection.onreconnected(() => setState(HubConnectionState.Connected))
    connection.onclose(() => setState(HubConnectionState.Disconnected))

    let disposed = false
    connection
      .start()
      .then(() => !disposed && setState(HubConnectionState.Connected))
      .catch(() => !disposed && setState(HubConnectionState.Disconnected))

    return () => {
      disposed = true
      void connection.stop()
    }
  }, [isAuthenticated, client])

  return { events, state }
}
