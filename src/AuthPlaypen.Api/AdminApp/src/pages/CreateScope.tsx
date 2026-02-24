import { useNavigate } from "@solidjs/router";
import { createSignal } from "solid-js";
import { Breadcrumbs } from "@/components/Breadcrumbs";
import { SearchMultiSelect } from "@/components/SearchMultiSelect";
import { useCreateScope } from "@/queries/scopeQueries";
import { useSearchApplications } from "@/queries/applicationQueries";

export function CreateScope() {
  const navigate = useNavigate();
  const createScope = useCreateScope();
  const [displayName, setDisplayName] = createSignal("");
  const [scopeName, setScopeName] = createSignal("");
  const [description, setDescription] = createSignal("");
  const [selectedAppIds, setSelectedAppIds] = createSignal<string[]>([]);
  const [searchTerm, setSearchTerm] = createSignal("");
  const appSearch = useSearchApplications(searchTerm, () => true);

  return <div class="space-y-4"><Breadcrumbs items={[{ href: "/", label: "Home" }, { href: "/scopes", label: "Scopes" }, { label: "Create Scope" }]} /><h1 class="text-2xl font-semibold">Create Scope</h1>
    <label class="block"><span class="text-sm">Display Name</span><input class="mt-1 w-full rounded border p-2" value={displayName()} onInput={(e)=>setDisplayName(e.currentTarget.value)} /></label>
    <label class="block"><span class="text-sm">Scope Name</span><input class="mt-1 w-full rounded border p-2" value={scopeName()} onInput={(e)=>setScopeName(e.currentTarget.value)} /></label>
    <label class="block"><span class="text-sm">Description</span><input class="mt-1 w-full rounded border p-2" value={description()} onInput={(e)=>setDescription(e.currentTarget.value)} /></label>
    <SearchMultiSelect label="Applications" searchTerm={searchTerm()} onSearchTermChange={setSearchTerm} options={(appSearch.data ?? []).map((a)=>({id:a.id,label:`${a.displayName} (${a.clientId})`}))} selected={selectedAppIds()} onChange={setSelectedAppIds} />
    <button class="rounded bg-blue-700 px-3 py-2 text-white" onClick={()=>createScope.mutate({displayName:displayName(),scopeName:scopeName(),description:description(),applicationIds:selectedAppIds()},{onSuccess:()=>navigate('/scopes')})}>Create</button>
  </div>;
}
