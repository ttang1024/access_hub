import { useState } from 'react'
import { relativeTime, shortId } from '../lib/format'
import type { Alarm } from '../lib/types'
import { useAlarmCommand } from '../hooks/useAlarms'
import { useCanOperate } from '../hooks/useAccessToken'
import { Badge } from './Badge'

export function AlarmRow({ alarm, siteName }: { alarm: Alarm; siteName: string }) {
  const canOperate = useCanOperate()
  const command = useAlarmCommand()
  const [closing, setClosing] = useState(false)
  const [note, setNote] = useState('')
  const subject = alarm.cardNumber ? `card ${alarm.cardNumber}` : `door ${shortId(alarm.doorId)}`

  return (
    <li className="border-b border-slate-100 px-4 py-2">
      <div className="flex items-center gap-2">
        <Badge tone={alarm.priority === 'High' ? 'red' : 'amber'}>{alarm.priority}</Badge>
        <span className="font-medium text-slate-800">{alarm.rule}</span>
        {alarm.escalated && <Badge tone="red">escalated</Badge>}
        <Badge tone={alarm.state === 'Raised' ? 'blue' : 'slate'}>{alarm.state}</Badge>
        <span className="ml-auto text-xs text-slate-500">{relativeTime(alarm.lastTriggeredAt)}</span>
      </div>
      <div className="mt-1 flex items-center gap-2 text-xs text-slate-600">
        <span>
          {siteName} · {subject} · ×{alarm.occurrenceCount}
        </span>
        {canOperate && alarm.state === 'Raised' && (
          <button
            className="ml-auto rounded bg-sky-600 px-2 py-0.5 text-white hover:bg-sky-700 disabled:opacity-50"
            disabled={command.isPending}
            onClick={() => command.mutate({ id: alarm.id, action: 'acknowledge' })}
          >
            Acknowledge
          </button>
        )}
        {canOperate && alarm.state === 'Acknowledged' && !closing && (
          <button className="ml-auto rounded bg-slate-700 px-2 py-0.5 text-white hover:bg-slate-800" onClick={() => setClosing(true)}>
            Close…
          </button>
        )}
      </div>
      {closing && (
        <form
          className="mt-2 flex gap-2"
          onSubmit={(e) => {
            e.preventDefault()
            command.mutate({ id: alarm.id, action: 'close', note }, { onSuccess: () => setClosing(false) })
          }}
        >
          <input
            className="flex-1 rounded border border-slate-300 px-2 py-1 text-sm"
            placeholder="What happened? (required)"
            value={note}
            onChange={(e) => setNote(e.target.value)}
            maxLength={2000}
            required
            autoFocus
          />
          <button className="rounded bg-slate-700 px-2 py-1 text-sm text-white disabled:opacity-50" disabled={!note.trim() || command.isPending}>
            Close alarm
          </button>
        </form>
      )}
      {command.error && <p className="mt-1 text-xs text-red-700">{command.error.message}</p>}
    </li>
  )
}
