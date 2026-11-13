export const shortId = (id: string | null | undefined) => (id ? id.slice(-6) : '–')

export function relativeTime(iso: string | null): string {
  if (!iso) return 'never'
  const seconds = Math.round((Date.now() - new Date(iso).getTime()) / 1000)
  if (seconds < 5) return 'just now'
  if (seconds < 60) return `${seconds}s ago`
  if (seconds < 3600) return `${Math.round(seconds / 60)}m ago`
  return `${Math.round(seconds / 3600)}h ago`
}

export const localTime = (iso: string) => new Date(iso).toLocaleTimeString()
