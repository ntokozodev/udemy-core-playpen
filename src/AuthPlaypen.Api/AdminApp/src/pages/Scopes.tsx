import { A } from "@solidjs/router";
import { For, Show } from "solid-js";
import { Breadcrumbs } from "@/components/Breadcrumbs";
import { LoadingSpinner } from "@/components/LoadingSpinner";
import { useScopes } from "@/queries/scopeQueries";

function getErrorMessage(error: unknown): string {
  return error instanceof Error ? error.message : "Unable to load scopes. Please try again.";
}

export function Scopes() {
  const query = useScopes();

  return (
    <div class="space-y-4">
      <Breadcrumbs items={[{ href: "/", label: "Home" }, { label: "Scopes" }]} />
      <div class="flex items-center justify-between">
        <h1 class="text-2xl font-semibold">Scopes</h1>
        <A class="rounded bg-blue-700 px-3 py-2 text-sm font-semibold text-white" href="/scopes/create">
          + Create
        </A>
      </div>
      <Show when={query.isPending}>
        <LoadingSpinner label="Loading scopes..." />
      </Show>
      <Show when={query.isError}>
        <div class="bg-red-50 border border-red-200 rounded-md p-4 mb-6">
          <h3 class="text-sm font-medium text-red-800">Could not fetch scopes</h3>
          <p class="mt-2 text-sm text-red-700">{getErrorMessage(query.error)}</p>
        </div>
      </Show>
      <For each={query.data ?? []}>
        {(scope) => (
          <div class="flex items-start justify-between rounded border border-slate-200 bg-white p-4">
            <div>
              <div class="font-semibold">{scope.displayName}</div>
              <div class="text-sm text-slate-600">{scope.scopeName}</div>
            </div>
            <A class="text-sm text-blue-700" href={`/scopes/${scope.id}/edit`}>
              Edit
            </A>
          </div>
        )}
      </For>
    </div>
  );
}
