import { useNavigate, useParams } from "@solidjs/router";
import { createEffect, createSignal } from "solid-js";
import { Breadcrumbs } from "@/components/Breadcrumbs";
import { SearchMultiSelect } from "@/components/SearchMultiSelect";
import { useApplicationById, useUpdateApplication } from "@/queries/applicationQueries";
import { useScopes } from "@/queries/scopeQueries";
import type { ApplicationFlow } from "@/types/models";

function slugify(value: string) { return value.toLowerCase().trim().replace(/[^a-z0-9\s-]/g, "").replace(/\s+/g, "-").replace(/-+/g, "-"); }
function generateSecret() { return crypto.randomUUID().replace(/-/g, ""); }

export function EditApplication() {
  const params = useParams();
  const navigate = useNavigate();
  const appQuery = useApplicationById(() => params.id);
  const update = useUpdateApplication();

  const [displayName, setDisplayName] = createSignal("");
  const [clientId, setClientId] = createSignal("");
  const [clientSecret, setClientSecret] = createSignal("");
  const [flow, setFlow] = createSignal<ApplicationFlow>("ClientCredentials");
  const [postLogoutRedirectUris, setPostLogoutRedirectUris] = createSignal("");
  const [redirectUris, setRedirectUris] = createSignal("");
  const [selectedScopeIds, setSelectedScopeIds] = createSignal<string[]>([]);
  const [scopeSearchTerm, setScopeSearchTerm] = createSignal("");
  const [showClientId, setShowClientId] = createSignal(false);
  const scopesQuery = useScopes();

  createEffect(() => {
    const app = appQuery.data;
    if (!app) return;
    setDisplayName(app.displayName); setClientId(app.clientId); setClientSecret(app.clientSecret || generateSecret());
    setFlow(app.flow); setPostLogoutRedirectUris(app.postLogoutRedirectUris ?? ""); setRedirectUris(app.redirectUris ?? "");
    setSelectedScopeIds(app.scopes.map((s) => s.id));
  });

  return (
    <div class="space-y-4">
      <Breadcrumbs items={[{ href: "/", label: "Home" }, { href: "/applications", label: "Applications" }, { href: `/applications/${params.id}`, label: "Application Details" }, { label: "Edit Application" }]} />
      <h1 class="text-2xl font-semibold">Edit Application</h1>
      <label class="block"><span class="text-sm">Display Name</span><input class="mt-1 w-full rounded border p-2" value={displayName()} onInput={(e)=>{const name=e.currentTarget.value;setDisplayName(name);setClientId(slugify(name));}}/></label>
      <label class="block"><span class="text-sm">Client ID</span><div class="mt-1 flex gap-2"><input class="w-full rounded border p-2" type={showClientId() ? 'text':'password'} value={clientId()} onInput={(e)=>setClientId(e.currentTarget.value)} onBlur={(e)=>setClientId(slugify(e.currentTarget.value))} /><button class="rounded border px-3 py-2" type="button" onClick={()=>setShowClientId(!showClientId())}>{showClientId()?"Hide":"Show"}</button><button class="rounded border px-3 py-2" type="button" onClick={()=>navigator.clipboard.writeText(clientId())}>Copy</button></div></label>
      <label class="block"><span class="text-sm">Client Secret</span><div class="mt-1 flex gap-2"><input class="w-full rounded border p-2" value={clientSecret()} onInput={(e)=>setClientSecret(e.currentTarget.value)} /><button class="rounded border px-3 py-2" type="button" onClick={()=>setClientSecret(generateSecret())}>Regenerate</button></div></label>
      <label class="block"><span class="text-sm">Flow</span><select class="mt-1 w-full rounded border p-2" value={flow()} onChange={(e)=>setFlow(e.currentTarget.value as ApplicationFlow)}><option value="ClientCredentials">ClientCredentials</option><option value="AuthorizationWithPKCE">AuthorizationWithPKCE</option></select></label>
      <label class="block"><span class="text-sm">Redirect Uris</span><input class="mt-1 w-full rounded border p-2" value={redirectUris()} onInput={(e)=>setRedirectUris(e.currentTarget.value)} /></label>
      <label class="block"><span class="text-sm">Post Logout Redirect Uris</span><input class="mt-1 w-full rounded border p-2" value={postLogoutRedirectUris()} onInput={(e)=>setPostLogoutRedirectUris(e.currentTarget.value)} /></label>
      <SearchMultiSelect label="Scopes" searchTerm={scopeSearchTerm()} onSearchTermChange={setScopeSearchTerm} options={(scopesQuery.data ?? []).map((s) => ({ id: s.id, label: `${s.displayName} (${s.scopeName})` }))} selected={selectedScopeIds()} onChange={setSelectedScopeIds} initialVisibleCount={6} />
      <button class="rounded bg-blue-700 px-3 py-2 text-white" onClick={()=>{const app=appQuery.data;if(!app)return;update.mutate({id:app.id,payload:{displayName:displayName(),clientId:slugify(clientId()),clientSecret:clientSecret(),flow:flow(),redirectUris:redirectUris(),postLogoutRedirectUris:postLogoutRedirectUris(),scopeIds:selectedScopeIds()}},{onSuccess:()=>navigate(`/applications/${app.id}`)});}}>Save</button>
    </div>
  );
}
