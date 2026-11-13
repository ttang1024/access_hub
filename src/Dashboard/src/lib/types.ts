export interface Alarm {
  id: string
  rule: string
  priority: 'High' | 'Medium'
  state: 'Raised' | 'Acknowledged' | 'Closed'
  escalated: boolean
  siteId: string
  doorId: string | null
  cardNumber: string | null
  occurrenceCount: number
  raisedAt: string
  lastTriggeredAt: string
  acknowledgedBy: string | null
  acknowledgedAt: string | null
  closedBy: string | null
  closedAt: string | null
  closeNote: string | null
}

export interface Paged<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
}

export interface SiteEvent {
  eventId: string
  siteId: string
  doorId: string
  type: string
  cardNumber: string | null
  denyReason: string | null
  occurredAt: string
  recordedAt: string
}

export interface SiteSyncStatus {
  siteId: string
  name: string
  isConnected: boolean
  lastConnectedAt: string | null
  lastSeenAt: string | null
  latestSequence: number
  lastDeliveredSequence: number
  lag: number
}
