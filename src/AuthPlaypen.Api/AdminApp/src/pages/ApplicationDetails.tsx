import { A, useNavigate, useParams } from "@solidjs/router";
import { For, Show, createSignal } from "solid-js";
import { Breadcrumbs } from "@/components/Breadcrumbs";
import { DeleteConfirmationModal } from "@/components/DeleteConfirmationModal";
import { useApplicationById, useDeleteApplication } from "@/queries/applicationQueries";

function DetailRow(props: { label: string; value?: string | null }) {
  return (
    <div class="rounded-md border border-slate-200 bg-slate-50 px-3 py-2">
      <p class="text-xs font-medium uppercase tracking-wide text-slate-500">{props.label}</p>
      <p class="mt-1 text-sm text-slate-900">{props.value?.trim() ? props.value : "-"}</p>
    </div>
  );
}

export function ApplicationDetails() {
  const params = useParams();
  const navigate = useNavigate();
  const query = useApplicationById(() => params.id);
  const remove = useDeleteApplication();
  const [showMeta, setShowMeta] = createSignal(false);
  const [showDelete, setShowDelete] = createSignal(false);

  return (
    <div class="space-y-5">
      <Breadcrumbs items={[{ href: "/", label: "Home" }, { href: "/applications", label: "Applications" }, { label: "Application Details" }]} />

      <div class="rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
        <div class="flex flex-wrap items-start justify-between gap-3">
          <div>
            <p class="text-xs font-medium uppercase tracking-wide text-slate-500">Application</p>
            <h1 class="mt-1 text-2xl font-semibold text-slate-900">{query.data?.displayName ?? "Application Details"}</h1>
          </div>
          <A class="rounded border border-slate-300 px-3 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-50" href={`/applications/${params.id}/edit`}>
            Edit
          </A>
        </div>

        <div class="mt-4 grid gap-3 sm:grid-cols-2">
          <DetailRow label="Client ID" value={query.data?.clientId} />
          <DetailRow label="Client Secret" value={query.data?.clientSecret} />
          <DetailRow label="Flow" value={query.data?.flow} />
          <DetailRow label="Redirect Uris" value={query.data?.redirectUris} />
          <DetailRow label="Post Logout Redirect Uris" value={query.data?.postLogoutRedirectUris} />
        </div>

        <div class="mt-4 rounded-md border border-slate-200 p-3">
          <p class="text-sm font-medium text-slate-700">Scopes</p>
          <div class="mt-2 flex flex-wrap gap-2">
            <Show
              when={(query.data?.scopes?.length ?? 0) > 0}
              fallback={<p class="text-sm text-slate-500">No scopes assigned.</p>}
            >
              <For each={query.data?.scopes ?? []}>
                {(scope) => (
                  <span class="rounded-full bg-slate-100 px-3 py-1 text-xs font-medium text-slate-700">
                    {scope.displayName} ({scope.scopeName})
                  </span>
                )}
              </For>
            </Show>
          </div>
        </div>
      </div>

      <div class="rounded-lg border border-slate-200 bg-white p-4 shadow-sm">
        <button class="flex w-full items-center justify-between text-left" type="button" onClick={() => setShowMeta(!showMeta())}>
          <span class="text-sm font-semibold text-slate-900">Metadata</span>
          <span class="text-xs text-slate-500">{showMeta() ? "Hide" : "Show"}</span>
        </button>
        <Show when={showMeta() && query.data}>
          <div class="mt-3 grid gap-2 sm:grid-cols-2">
            <DetailRow label="Created By" value={query.data!.metadata.createdBy} />
            <DetailRow label="Created At" value={new Date(query.data!.metadata.createdAt).toLocaleString()} />
            <DetailRow label="Updated By" value={query.data!.metadata.updatedBy} />
            <DetailRow label="Updated At" value={new Date(query.data!.metadata.updatedAt).toLocaleString()} />
          </div>
        </Show>
      </div>

      <button class="rounded bg-red-700 px-3 py-2 text-white" type="button" onClick={() => setShowDelete(true)}>
        Delete Application
      </button>

      <DeleteConfirmationModal
        isOpen={showDelete()}
        title="Delete application"
        description="This will permanently remove the application and all linked scope assignments."
        isDeleting={remove.isPending}
        onCancel={() => setShowDelete(false)}
        onConfirm={() => {
          remove.mutate(params.id, { onSuccess: () => navigate("/applications") });
          setShowDelete(false);
        }}
      />
    </div>
  );
}
