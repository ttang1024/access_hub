import { relativeTime } from '../lib/format'
import type { SiteSyncStatus } from '../lib/types'
import { Badge } from './Badge'
import { Panel } from './Panel'

export function SiteStatusPanel({ sites, error }: { sites: SiteSyncStatus[]; error: Error | null }) {
  return (
    <Panel title="Sites">
      {error && <p className="p-4 text-sm text-red-700">{error.message}</p>}
      <table className="w-full text-sm">
        <thead className="text-left text-xs text-slate-500">
          <tr>
            <th className="px-4 py-2 font-medium">Site</th>
            <th className="px-2 py-2 font-medium">Status</th>
            <th className="px-2 py-2 font-medium">Last seen</th>
            <th className="px-2 py-2 text-right font-medium">Lag</th>
          </tr>
        </thead>
        <tbody>
          {sites.map((s) => (
            <tr key={s.siteId} className="border-t border-slate-100">
              <td className="px-4 py-1.5 text-slate-800">{s.name}</td>
              <td className="px-2 py-1.5">
                <Badge tone={s.isConnected ? 'green' : 'red'}>{s.isConnected ? 'connected' : 'disconnected'}</Badge>
              </td>
              <td className="px-2 py-1.5 text-slate-600">{relativeTime(s.lastSeenAt)}</td>
              <td className={`px-2 py-1.5 text-right tabular-nums ${s.lag > 0 ? 'text-amber-700' : 'text-slate-600'}`}>{s.lag}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </Panel>
  )
}
