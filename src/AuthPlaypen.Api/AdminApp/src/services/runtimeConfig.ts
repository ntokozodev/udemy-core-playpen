type RuntimeConfig = Partial<{
  VITE_USE_MOCK_DATA: string;
  VITE_ENABLE_OIDC_AUTH: string;
  VITE_API_OIDC_AUTHORITY: string;
  VITE_API_OIDC_CLIENT_ID: string;
  VITE_OIDC_REDIRECT_PATH: string;
  VITE_OIDC_POST_LOGOUT_REDIRECT_PATH: string;
  VITE_LOCAL_RUN_MODE: string;
}>;

type AppConfigResponse = Partial<{
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

const buildTimeRuntimeConfig: RuntimeConfig = {
  VITE_USE_MOCK_DATA: import.meta.env.VITE_USE_MOCK_DATA,
  VITE_ENABLE_OIDC_AUTH: import.meta.env.VITE_ENABLE_OIDC_AUTH,
  VITE_API_OIDC_AUTHORITY: import.meta.env.VITE_API_OIDC_AUTHORITY,
  VITE_API_OIDC_CLIENT_ID: import.meta.env.VITE_API_OIDC_CLIENT_ID,
  VITE_OIDC_REDIRECT_PATH: import.meta.env.VITE_OIDC_REDIRECT_PATH,
  VITE_OIDC_POST_LOGOUT_REDIRECT_PATH: import.meta.env.VITE_OIDC_POST_LOGOUT_REDIRECT_PATH,
  VITE_LOCAL_RUN_MODE: import.meta.env.VITE_LOCAL_RUN_MODE,
};

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

  setRuntimeConfigValue(runtimeConfig, "VITE_ENABLE_OIDC_AUTH", config.enableOidcAuth);
  setRuntimeConfigValue(runtimeConfig, "VITE_API_OIDC_AUTHORITY", config.authority);
  setRuntimeConfigValue(runtimeConfig, "VITE_API_OIDC_CLIENT_ID", config.clientId);
  setRuntimeConfigValue(runtimeConfig, "VITE_OIDC_REDIRECT_PATH", config.redirectPath);
  setRuntimeConfigValue(runtimeConfig, "VITE_OIDC_POST_LOGOUT_REDIRECT_PATH", config.postLogoutRedirectPath);
}

function initializeRuntimeConfig(): void {
  if (window.__AUTH_PLAYPEN_CONFIG__) {
    return;
  }

  window.__AUTH_PLAYPEN_CONFIG__ = {};
  const runtimeConfig = window.__AUTH_PLAYPEN_CONFIG__;

  for (const key of Object.keys(buildTimeRuntimeConfig) as Array<keyof RuntimeConfig>) {
    setRuntimeConfigValue(runtimeConfig, key, buildTimeRuntimeConfig[key]);
  }
}

function isLocalRunModeEnabled(): boolean {
  return getConfigValue("VITE_LOCAL_RUN_MODE") === "true";
}

export async function loadRuntimeConfig(): Promise<void> {
  initializeRuntimeConfig();

  try {
    const response = await fetch("/app-config", { cache: "no-store" });

    if (!response.ok) {
      throw new Error(`Failed to load runtime config: ${response.status}`);
    }

    const apiConfig = (await response.json()) as AppConfigResponse;
    applyApiRuntimeConfig(apiConfig);
  } catch (error) {
    if (isLocalRunModeEnabled()) {
      return;
    }

    throw error;
  }
}

export function getConfigValue(key: keyof RuntimeConfig): string | undefined {
  initializeRuntimeConfig();
  return readRuntimeConfigValue(key);
}

export function getBooleanConfigValue(key: keyof RuntimeConfig): boolean {
  return getConfigValue(key) === "true";
}
