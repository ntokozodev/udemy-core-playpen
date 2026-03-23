type RuntimeConfig = Partial<{
  VITE_USE_MOCK_DATA: string;
  VITE_ENABLE_OIDC_AUTH: string;
  VITE_API_OIDC_AUTHORITY: string;
  VITE_API_OIDC_CLIENT_ID: string;
  VITE_OIDC_REDIRECT_PATH: string;
  VITE_OIDC_POST_LOGOUT_REDIRECT_PATH: string;
}>;

type AppConfigResponse = Partial<{
  useMockData: boolean;
  enableOidcAuth: boolean;
  authority: string;
  clientId: string;
  redirectPath: string;
  postLogoutRedirectPath: string;
}>;

declare global {
  interface Window {
    __AUTH_PLAYPEN_CONFIG__?: RuntimeConfig;
  }
}

function readRuntimeConfigValue(key: keyof RuntimeConfig): string | undefined {
  return window.__AUTH_PLAYPEN_CONFIG__?.[key]?.trim();
}

function setRuntimeConfigValue(
  runtimeConfig: RuntimeConfig,
  key: keyof RuntimeConfig,
  value: string | boolean | undefined,
): void {
  if (value === undefined) {
    return;
  }

  const normalizedValue = String(value).trim();

  if (!normalizedValue) {
    return;
  }

  runtimeConfig[key] = normalizedValue;
}

function applyApiRuntimeConfig(config: AppConfigResponse): void {
  window.__AUTH_PLAYPEN_CONFIG__ ??= {};
  const runtimeConfig = window.__AUTH_PLAYPEN_CONFIG__;

  setRuntimeConfigValue(runtimeConfig, "VITE_USE_MOCK_DATA", config.useMockData);
  setRuntimeConfigValue(runtimeConfig, "VITE_ENABLE_OIDC_AUTH", config.enableOidcAuth);
  setRuntimeConfigValue(runtimeConfig, "VITE_API_OIDC_AUTHORITY", config.authority);
  setRuntimeConfigValue(runtimeConfig, "VITE_API_OIDC_CLIENT_ID", config.clientId);
  setRuntimeConfigValue(runtimeConfig, "VITE_OIDC_REDIRECT_PATH", config.redirectPath);
  setRuntimeConfigValue(runtimeConfig, "VITE_OIDC_POST_LOGOUT_REDIRECT_PATH", config.postLogoutRedirectPath);
}

export async function loadRuntimeConfig(): Promise<void> {
  const response = await fetch("/app-config", { cache: "no-store" });

  if (!response.ok) {
    throw new Error(`Failed to load runtime config: ${response.status}`);
  }

  const apiConfig = (await response.json()) as AppConfigResponse;
  applyApiRuntimeConfig(apiConfig);
}

export function getConfigValue(key: keyof RuntimeConfig): string | undefined {
  return readRuntimeConfigValue(key);
}

export function getBooleanConfigValue(key: keyof RuntimeConfig): boolean {
  return getConfigValue(key) === "true";
}
