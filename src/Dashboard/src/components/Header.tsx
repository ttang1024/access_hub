import { HubConnectionState } from '@microsoft/signalr'
import { useAuth } from 'react-oidc-context'
import { Badge } from './Badge'

export function Header({ hubState }: { hubState: HubConnectionState }) {
  const auth = useAuth()
  const live = hubState === HubConnectionState.Connected
  return (
    <header className="flex items-center justify-between border-b border-slate-200 bg-white px-6 py-3">
      <div className="flex items-center gap-3">
        <h1 className="text-lg font-semibold text-slate-900">AccessHub</h1>
        <Badge tone={live ? 'green' : 'amber'}>{live ? 'live' : hubState.toLowerCase()}</Badge>
      </div>
      <div className="flex items-center gap-3 text-sm text-slate-600">
        <span>{auth.user?.profile.preferred_username}</span>
        <button className="rounded border border-slate-300 px-2 py-1 hover:bg-slate-100" onClick={() => void auth.signoutRedirect()}>
          Sign out
        </button>
      </div>
    </header>
  )
}
