import { User, UserManager, WebStorageStateStore } from "oidc-client-ts";
import { getOidcConfig, isOidcAuthEnabled } from "@/services/authConfig";

let userManager: UserManager | null = null;

function getUserManager(): UserManager {
  if (!userManager) {
    const config = getOidcConfig();

    userManager = new UserManager({
      authority: config.authority,
      client_id: config.clientId,
      response_type: "code",
      redirect_uri: config.redirectUri,
      post_logout_redirect_uri: config.postLogoutRedirectUri,
      scope: config.scope,
      userStore: new WebStorageStateStore({ store: window.sessionStorage }),
      automaticSilentRenew: false,
      monitorSession: false,
      loadUserInfo: false,
    });
  }

  return userManager;
}

function isValidUser(user: User | null): user is User {
  return !!user && !user.expired;
}

export async function ensureAuthenticated(): Promise<boolean> {
  if (!isOidcAuthEnabled) {
    return true;
  }

  const manager = getUserManager();
  const user = await manager.getUser();

  if (isValidUser(user)) {
    return true;
  }

  await manager.signinRedirect();
  return false;
}

export async function completeSigninRedirect(): Promise<void> {
  if (!isOidcAuthEnabled) {
    return;
  }

  const manager = getUserManager();
  await manager.signinRedirectCallback();
}
