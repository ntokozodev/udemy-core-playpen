import { getBooleanConfigValue, getConfigValue } from "./runtimeConfig";

export const isOidcAuthEnabled = getBooleanConfigValue("VITE_ENABLE_OIDC_AUTH");

export type OidcConfig = {
  authority: string;
  clientId: string;
  scope: string;
  redirectUri: string;
  postLogoutRedirectUri: string;
};

const DEFAULT_OIDC_SCOPE = "openid profile offline_access";

function normalizePath(path: string, fallback: string): string {
  const value = path?.trim() || fallback;
  return value.startsWith("/") ? value : `/${value}`;
}

export function getOidcConfig(): OidcConfig {
  const authority = getConfigValue("VITE_API_OIDC_AUTHORITY") || window.location.origin;
  const clientId = getConfigValue("VITE_API_OIDC_CLIENT_ID");

  if (!clientId) {
    throw new Error(
      "OIDC auth is enabled but no API/OpenIddict client id is configured. Set VITE_API_OIDC_CLIENT_ID.",
    );
  }

  const redirectPath = normalizePath(getConfigValue("VITE_OIDC_REDIRECT_PATH"), "/admin/auth/callback");
  const postLogoutRedirectPath = normalizePath(getConfigValue("VITE_OIDC_POST_LOGOUT_REDIRECT_PATH"), "/admin/auth/logout-callback");

  return {
    authority,
    clientId,
    scope: getConfigValue("VITE_API_OIDC_SCOPE") || DEFAULT_OIDC_SCOPE,
    redirectUri: `${window.location.origin}${redirectPath}`,
    postLogoutRedirectUri: `${window.location.origin}${postLogoutRedirectPath}`,
  };
}
