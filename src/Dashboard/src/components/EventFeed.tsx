import { localTime, shortId } from '../lib/format'
import type { SiteEvent } from '../lib/types'
import { Badge, type Tone } from './Badge'
import { Panel } from './Panel'

const toneFor: Record<string, Tone> = {
  AccessGranted: 'green',
  AccessDenied: 'amber',
  DoorForced: 'red',
  DoorHeldOpen: 'red',
}

export function EventFeed({ events, siteNames }: { events: SiteEvent[]; siteNames: Map<string, string> }) {
  return (
    <Panel title="Live events" aside={<span className="text-xs text-slate-500">last {events.length}</span>}>
      {events.length === 0 ? (
        <p className="p-4 text-sm text-slate-500">Waiting for site events…</p>
      ) : (
        <table className="w-full text-sm">
          <tbody>
            {events.map((e) => (
              <tr key={e.eventId} className="border-b border-slate-100">
                <td className="px-4 py-1.5 tabular-nums text-slate-500">{localTime(e.occurredAt)}</td>
                <td className="px-2 py-1.5">
                  <Badge tone={toneFor[e.type] ?? 'slate'}>{e.type}</Badge>
                </td>
                <td className="px-2 py-1.5 text-slate-700">{siteNames.get(e.siteId) ?? shortId(e.siteId)}</td>
                <td className="px-2 py-1.5 text-slate-500">door {shortId(e.doorId)}</td>
                <td className="px-2 py-1.5 font-mono text-xs text-slate-600">{e.cardNumber ?? ''}</td>
                <td className="px-2 py-1.5 text-xs text-slate-500">{e.denyReason ?? ''}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </Panel>
  )
}
