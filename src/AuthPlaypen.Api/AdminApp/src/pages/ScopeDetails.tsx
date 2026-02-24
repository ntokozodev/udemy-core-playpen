import { A, useNavigate, useParams } from "@solidjs/router";
import { For, Show, createSignal } from "solid-js";
import { Breadcrumbs } from "@/components/Breadcrumbs";
import { DeleteConfirmationModal } from "@/components/DeleteConfirmationModal";
import { useDeleteScope, useScopeById } from "@/queries/scopeQueries";

export function ScopeDetails() {
  const params = useParams();
  const navigate = useNavigate();
  const query = useScopeById(() => params.id);
  const remove = useDeleteScope();
  const [showDelete, setShowDelete] = createSignal(false);

  return <div class="space-y-4"><Breadcrumbs items={[{href:'/',label:'Home'},{href:'/scopes',label:'Scopes'},{label:'Scope Details'}]} />
    <div class="flex items-center justify-between"><h1 class="text-2xl font-semibold">{query.data?.displayName ?? 'Scope Details'}</h1><A class="text-sm text-blue-700" href={`/scopes/${params.id}/edit`}>Edit</A></div>
    <div class="rounded border p-4 space-y-2"><p><strong>Scope Name:</strong> {query.data?.scopeName}</p><p><strong>Description:</strong> {query.data?.description}</p>
      <div>
        <strong>Applications:</strong>
        <ul class="ml-5 list-disc text-sm text-slate-700">
          <For each={query.data?.applications ?? []}>{(app) => <li>{app.displayName} ({app.clientId})</li>}</For>
        </ul>
      </div>
    </div>
    <details class="rounded border p-3"><summary class="cursor-pointer">Metadata</summary><Show when={query.data}><div class="mt-2 text-sm text-slate-700"><p>Created By: {query.data!.metadata.createdBy}</p><p>Created At: {new Date(query.data!.metadata.createdAt).toLocaleString()}</p><p>Updated By: {query.data!.metadata.updatedBy}</p><p>Updated At: {new Date(query.data!.metadata.updatedAt).toLocaleString()}</p></div></Show></details>
    <button class="rounded bg-red-700 px-3 py-2 text-white" type="button" onClick={() => setShowDelete(true)}>Delete Scope</button>
    <DeleteConfirmationModal isOpen={showDelete()} title="Delete scope" description="This will permanently remove the scope and clear it from linked applications." isDeleting={remove.isPending} onCancel={()=>setShowDelete(false)} onConfirm={()=>{remove.mutate(params.id,{onSuccess:()=>navigate('/scopes')});setShowDelete(false);}} />
  </div>;
}
