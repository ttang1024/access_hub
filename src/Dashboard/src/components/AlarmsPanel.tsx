import { useOpenAlarms } from '../hooks/useAlarms'
import { AlarmRow } from './AlarmRow'
import { Panel } from './Panel'

export function AlarmsPanel({ siteNames }: { siteNames: Map<string, string> }) {
  const { data, isLoading, error } = useOpenAlarms()
  const alarms = data?.items ?? []
  return (
    <Panel title="Open alarms" aside={<span className="text-xs text-slate-500">{alarms.length}</span>}>
      {isLoading && <p className="p-4 text-sm text-slate-500">Loading…</p>}
      {error && <p className="p-4 text-sm text-red-700">{error.message}</p>}
      {!isLoading && alarms.length === 0 && <p className="p-4 text-sm text-slate-500">No open alarms.</p>}
      <ul>
        {alarms.map((a) => (
          <AlarmRow key={a.id} alarm={a} siteName={siteNames.get(a.siteId) ?? a.siteId.slice(-6)} />
        ))}
      </ul>
    </Panel>
  )
}
