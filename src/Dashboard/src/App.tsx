import { useMemo } from 'react'
import { useAuth } from 'react-oidc-context'
import { AlarmsPanel } from './components/AlarmsPanel'
import { EventFeed } from './components/EventFeed'
import { Header } from './components/Header'
import { SiteStatusPanel } from './components/SiteStatusPanel'
import { useAlarmHub } from './hooks/useAlarmHub'
import { useSiteStatus } from './hooks/useSiteStatus'

function Dashboard() {
  const { events, state } = useAlarmHub()
  const sites = useSiteStatus()
  const siteNames = useMemo(() => new Map((sites.data ?? []).map((s) => [s.siteId, s.name])), [sites.data])

  return (
    <div className="flex h-screen flex-col">
      <Header hubState={state} />
      <main className="grid min-h-0 flex-1 grid-cols-1 gap-4 p-4 lg:grid-cols-3">
        <div className="min-h-0 lg:col-span-2">
          <EventFeed events={events} siteNames={siteNames} />
        </div>
        <div className="flex min-h-0 flex-col gap-4">
          <AlarmsPanel siteNames={siteNames} />
          <SiteStatusPanel sites={sites.data ?? []} error={sites.error} />
        </div>
      </main>
    </div>
  )
}

export default function App() {
  const auth = useAuth()
  if (auth.isLoading) {
    return <p className="p-8 text-slate-500">Signing in…</p>
  }
  if (auth.error) {
    return <p className="p-8 text-red-700">Sign-in failed: {auth.error.message}</p>
  }
  if (!auth.isAuthenticated) {
    return (
      <div className="flex h-screen items-center justify-center">
        <div className="rounded-lg border border-slate-200 bg-white p-8 text-center shadow-sm">
          <h1 className="mb-2 text-xl font-semibold text-slate-900">AccessHub</h1>
          <p className="mb-6 text-sm text-slate-600">Access control operations dashboard</p>
          <button className="rounded bg-sky-600 px-4 py-2 text-white hover:bg-sky-700" onClick={() => void auth.signinRedirect()}>
            Sign in
          </button>
        </div>
      </div>
    )
  }
  return <Dashboard />
}
