import { A } from "@solidjs/router";
import { createSignal, onMount, Show } from "solid-js";
import { completeSignoutRedirect, ensureAuthenticated } from "@/services/authService";
import { isOidcAuthEnabled } from "@/services/authConfig";

export function AuthLogoutCallback() {
  const [isProcessing, setIsProcessing] = createSignal(true);
  const [error, setError] = createSignal<string | null>(null);

  onMount(async () => {
    try {
      await completeSignoutRedirect();
      setIsProcessing(false);
    } catch (callbackError) {
      console.error("Logout callback failed", callbackError);
      setError("Logout callback failed. Please retry.");
      setIsProcessing(false);
    }
  });

  const handleSignInAgain = async () => {
    try {
      await ensureAuthenticated();
    } catch (signInError) {
      console.error("Unable to start sign-in", signInError);
      setError("Unable to start sign-in. Please check auth configuration.");
    }
  };

  return (
    <div class="rounded border border-slate-200 bg-white p-6">
      <Show when={isProcessing()} fallback={
        <>
          <h1 class="text-lg font-semibold text-slate-900">You are signed out</h1>
          <p class="mt-2 text-sm text-slate-600">
            Your AdminApp session has ended.
          </p>
          <div class="mt-4 flex gap-3">
            <Show when={isOidcAuthEnabled}>
              <button
                class="rounded bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700"
                onClick={() => void handleSignInAgain()}
                type="button"
              >
                Sign in again
              </button>
            </Show>
            <A class="rounded border border-slate-300 px-4 py-2 text-sm text-slate-700 hover:bg-slate-100" href="/">
              Go to dashboard
            </A>
          </div>
        </>
      }>
        <h1 class="text-lg font-semibold text-slate-900">Signing you out...</h1>
      </Show>
      {error() && <p class="mt-3 text-sm text-red-600">{error()}</p>}
    </div>
  );
}
