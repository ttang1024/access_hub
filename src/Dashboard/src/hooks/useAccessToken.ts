import { useAuth } from 'react-oidc-context'

export function useAccessToken() {
  return useAuth().user?.access_token
}

/** Roles come from the flat `roles` claim emitted by the Keycloak realm. */
export function useCanOperate() {
  const roles = (useAuth().user?.profile.roles as string[] | undefined) ?? []
  return roles.includes('admin') || roles.includes('operator')
}
