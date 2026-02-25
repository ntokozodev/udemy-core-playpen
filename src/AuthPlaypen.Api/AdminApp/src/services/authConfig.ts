export const isOidcAuthEnabled = import.meta.env.VITE_ENABLE_OIDC_AUTH === "true";

export type OidcConfig = {
  authority: string;
  clientId: string;
  scope: string;
  redirectUri: string;
  postLogoutRedirectUri: string;
};

const DEFAULT_OIDC_SCOPE = "openid profile";

function normalizePath(path: string, fallback: string): string {
  const value = path?.trim() || fallback;
  return value.startsWith("/") ? value : `/${value}`;
}

export function getOidcConfig(): OidcConfig {
  const authority = import.meta.env.VITE_API_OIDC_AUTHORITY?.trim() || window.location.origin;
  const clientId = import.meta.env.VITE_API_OIDC_CLIENT_ID?.trim();

  if (!clientId) {
    throw new Error(
      "OIDC auth is enabled but no API/OpenIddict client id is configured. Set VITE_API_OIDC_CLIENT_ID.",
    );
  }

  const redirectPath = normalizePath(import.meta.env.VITE_OIDC_REDIRECT_PATH, "/admin/auth/callback");
  const postLogoutRedirectPath = normalizePath(import.meta.env.VITE_OIDC_POST_LOGOUT_REDIRECT_PATH, "/admin");

  return {
    authority,
    clientId,
    scope: DEFAULT_OIDC_SCOPE,
    redirectUri: `${window.location.origin}${redirectPath}`,
    postLogoutRedirectUri: `${window.location.origin}${postLogoutRedirectPath}`,
  };
}
