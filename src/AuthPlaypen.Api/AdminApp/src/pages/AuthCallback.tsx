import { useNavigate } from "@solidjs/router";
import { createSignal, onMount } from "solid-js";
import { completeSigninRedirect } from "@/services/authService";

export function AuthCallback() {
  const navigate = useNavigate();
  const [error, setError] = createSignal<string | null>(null);

  onMount(async () => {
    try {
      await completeSigninRedirect();
      navigate("/", { replace: true });
    } catch (callbackError) {
      console.error("Authentication callback failed", callbackError);
      setError("Authentication callback failed. Please retry.");
    }
  });

  return (
    <div class="rounded border border-slate-200 bg-white p-6">
      <h1 class="text-lg font-semibold text-slate-900">Signing you in...</h1>
      {error() && <p class="mt-3 text-sm text-red-600">{error()}</p>}
    </div>
  );
}
