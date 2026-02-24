import { A } from "@solidjs/router";
import { For, Show } from "solid-js";
import { Breadcrumbs } from "@/components/Breadcrumbs";
import { LoadingSpinner } from "@/components/LoadingSpinner";
import { useScopes, useSearchScopes } from "@/queries/scopeQueries";
import { createSignal } from "solid-js";

function getErrorMessage(error: unknown): string {
  return error instanceof Error ? error.message : "Unable to load scopes. Please try again.";
}

export function Scopes() {
  const query = useScopes();
  const [searchTerm, setSearchTerm] = createSignal("");
  const searchQuery = useSearchScopes(searchTerm, () => searchTerm().trim().length > 1);
  const isSearching = () => searchTerm().trim().length > 1;
  const scopes = () => (isSearching() ? searchQuery.data ?? [] : query.data ?? []);

  return (
    <div class="space-y-4">
      <Breadcrumbs items={[{ href: "/", label: "Home" }, { label: "Scopes" }]} />
      <div class="flex items-center justify-between">
        <h1 class="text-2xl font-semibold">Scopes</h1>
        <A class="rounded bg-blue-700 px-3 py-2 text-sm font-semibold text-white" href="/scopes/create">
          + Create
        </A>
      </div>
      <input
        class="w-full rounded border border-slate-300 px-3 py-2 text-sm"
        onInput={(event) => setSearchTerm(event.currentTarget.value)}
        placeholder="Search by display name or scope name"
        type="text"
        value={searchTerm()}
      />
      <Show when={query.isPending}>
        <LoadingSpinner label="Loading scopes..." />
      </Show>
      <Show when={searchQuery.isPending && isSearching()}>
        <LoadingSpinner label="Searching scopes..." />
      </Show>
      <Show when={query.isError}>
        <div class="bg-red-50 border border-red-200 rounded-md p-4 mb-6">
          <h3 class="text-sm font-medium text-red-800">Could not fetch scopes</h3>
          <p class="mt-2 text-sm text-red-700">{getErrorMessage(query.error)}</p>
        </div>
      </Show>
      <For each={scopes()}>
        {(scope) => (
          <div class="flex items-start justify-between rounded border border-slate-200 bg-white p-4">
            <div>
              <div class="font-semibold">{scope.displayName}</div>
              <div class="text-sm text-slate-600">{scope.scopeName}</div>
            </div>
            <A class="text-sm text-blue-700" href={`/scopes/${scope.id}`}>
              Details
            </A>
          </div>
        )}
      </For>
      <Show when={query.hasNextPage && !isSearching()}>
        <button
          class="rounded border border-slate-300 px-3 py-2 text-sm font-medium text-slate-700 disabled:cursor-not-allowed disabled:opacity-60"
          disabled={query.isFetchingNextPage}
          onClick={() => query.fetchNextPage()}
          type="button"
        >
          {query.isFetchingNextPage ? "Loading more..." : "Load more"}
        </button>
      </Show>
    </div>
  );
}
