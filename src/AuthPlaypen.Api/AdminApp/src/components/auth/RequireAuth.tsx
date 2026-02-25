import { JSX, Show, createSignal, onMount } from "solid-js";
import { ensureAuthenticated } from "@/services/authService";

type RequireAuthProps = {
  children: JSX.Element;
};

export function RequireAuth(props: RequireAuthProps) {
  const [isReady, setIsReady] = createSignal(false);
  const [error, setError] = createSignal<string | null>(null);

  onMount(async () => {
    try {
      const isAuthenticated = await ensureAuthenticated();
      setIsReady(isAuthenticated);
    } catch (authError) {
      console.error("Failed to validate authentication state", authError);
      setError("Authentication is enabled but not configured correctly.");
    }
  });

  return (
    <Show
      when={isReady()}
      fallback={
        <div class="p-6 text-sm text-slate-600">
          {error() || "Redirecting to sign in..."}
        </div>
      }
    >
      {props.children}
    </Show>
  );
}
