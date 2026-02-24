import { A, useNavigate, useParams } from "@solidjs/router";
import { For, Show } from "solid-js";
import { Breadcrumbs } from "@/components/Breadcrumbs";
import { DeleteConfirmationModal } from "@/components/DeleteConfirmationModal";
import { useApplicationById, useDeleteApplication } from "@/queries/applicationQueries";
import { createSignal } from "solid-js";

export function ApplicationDetails() {
  const params = useParams();
  const navigate = useNavigate();
  const query = useApplicationById(() => params.id);
  const remove = useDeleteApplication();
  const [showMeta, setShowMeta] = createSignal(false);
  const [showDelete, setShowDelete] = createSignal(false);

  return <div class="space-y-4"><Breadcrumbs items={[{href:'/',label:'Home'},{href:'/applications',label:'Applications'},{label:'Application Details'}]} />
    <div class="flex items-center justify-between"><h1 class="text-2xl font-semibold">{query.data?.displayName ?? 'Application Details'}</h1><A class="text-sm text-blue-700" href={`/applications/${params.id}/edit`}>Edit</A></div>
    <div class="rounded border p-4 space-y-2">
      <p><strong>Client ID:</strong> {query.data?.clientId}</p>
      <p><strong>Client Secret:</strong> {query.data?.clientSecret}</p>
      <p><strong>Flow:</strong> {query.data?.flow}</p>
      <p><strong>Redirect Uris:</strong> {query.data?.redirectUris || "-"}</p>
      <p><strong>Post Logout Redirect Uris:</strong> {query.data?.postLogoutRedirectUris || "-"}</p>
      <div>
        <strong>Scopes:</strong>
        <ul class="ml-5 list-disc text-sm text-slate-700">
          <For each={query.data?.scopes ?? []}>{(scope) => <li>{scope.displayName} ({scope.scopeName})</li>}</For>
        </ul>
      </div>
    </div>
    <details class="rounded border p-3" open={showMeta()}><summary class="cursor-pointer" onClick={()=>setShowMeta(!showMeta())}>Metadata</summary><Show when={query.data}><div class="mt-2 text-sm text-slate-700"><p>Created By: {query.data!.metadata.createdBy}</p><p>Created At: {new Date(query.data!.metadata.createdAt).toLocaleString()}</p><p>Updated By: {query.data!.metadata.updatedBy}</p><p>Updated At: {new Date(query.data!.metadata.updatedAt).toLocaleString()}</p></div></Show></details>
    <button class="rounded bg-red-700 px-3 py-2 text-white" type="button" onClick={() => setShowDelete(true)}>Delete Application</button>
    <DeleteConfirmationModal isOpen={showDelete()} title="Delete application" description="This will permanently remove the application and all linked scope assignments." isDeleting={remove.isPending} onCancel={()=>setShowDelete(false)} onConfirm={()=>{remove.mutate(params.id,{onSuccess:()=>navigate('/applications')});setShowDelete(false);}} />
  </div>;
}
