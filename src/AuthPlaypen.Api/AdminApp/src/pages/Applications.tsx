import { A } from "@solidjs/router";
import { For, Show } from "solid-js";
import { Breadcrumbs } from "@/components/Breadcrumbs";
import { LoadingSpinner } from "@/components/LoadingSpinner";
import { useApplications } from "@/queries/applicationQueries";

function getErrorMessage(error: unknown): string {
  return error instanceof Error ? error.message : "Unable to load applications. Please try again.";
}

export function Applications() {
  const query = useApplications();

  return (
    <div class="space-y-4">
      <Breadcrumbs items={[{ href: "/", label: "Home" }, { label: "Applications" }]} />
      <div class="flex items-center justify-between">
        <h1 class="text-2xl font-semibold">Applications</h1>
        <A class="rounded bg-blue-700 px-3 py-2 text-sm font-semibold text-white" href="/applications/create">
          + Create
        </A>
      </div>
      <Show when={query.isPending}>
        <LoadingSpinner label="Loading applications..." />
      </Show>
      <Show when={query.isError}>
        <div class="bg-red-50 border border-red-200 rounded-md p-4 mb-6">
          <h3 class="text-sm font-medium text-red-800">Could not fetch applications</h3>
          <p class="mt-2 text-sm text-red-700">{getErrorMessage(query.error)}</p>
        </div>
      </Show>
      <For each={query.data ?? []}>
        {(app) => (
          <div class="flex items-start justify-between rounded border border-slate-200 bg-white p-4">
            <div>
              <div class="font-semibold">{app.displayName}</div>
              <div class="text-sm text-slate-600">{app.clientId}</div>
            </div>
            <A class="text-sm text-blue-700" href={`/applications/${app.id}`}>
              Details
            </A>
          </div>
        )}
      </For>
      <Show when={query.hasNextPage}>
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
