export const config = {
  gatewayUrl: (import.meta.env.VITE_GATEWAY_URL as string | undefined) ?? 'http://localhost:5100',
  oidcAuthority: (import.meta.env.VITE_OIDC_AUTHORITY as string | undefined) ?? 'http://localhost:8080/realms/accesshub',
  oidcClientId: (import.meta.env.VITE_OIDC_CLIENT_ID as string | undefined) ?? 'dashboard',
}

export const MAX_FEED_EVENTS = 100
