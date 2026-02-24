import { useNavigate } from "@solidjs/router";
import { createSignal } from "solid-js";
import { Breadcrumbs } from "@/components/Breadcrumbs";
import { SearchMultiSelect } from "@/components/SearchMultiSelect";
import { useCreateApplication } from "@/queries/applicationQueries";
import { useSearchScopes } from "@/queries/scopeQueries";
import type { ApplicationFlow } from "@/types/models";

function slugify(value: string) { return value.toLowerCase().trim().replace(/[^a-z0-9\s-]/g, "").replace(/\s+/g, "-").replace(/-+/g, "-"); }
function generateSecret() { return crypto.randomUUID().replace(/-/g, ""); }

export function CreateApplication() {
  const navigate = useNavigate();
  const createApplication = useCreateApplication();
  const [displayName, setDisplayName] = createSignal("");
  const [clientId, setClientId] = createSignal("");
  const [clientSecret, setClientSecret] = createSignal(generateSecret());
  const [flow, setFlow] = createSignal<ApplicationFlow>("ClientCredentials");
  const [postLogoutRedirectUris, setPostLogoutRedirectUris] = createSignal("");
  const [redirectUris, setRedirectUris] = createSignal("");
  const [scopeIds, setScopeIds] = createSignal<string[]>([]);
  const [scopeSearchTerm, setScopeSearchTerm] = createSignal("");
  const [showClientId, setShowClientId] = createSignal(false);
  const scopeSearch = useSearchScopes(scopeSearchTerm, () => true);

  const ensureUniqueClientId = (baseValue: string) => slugify(baseValue);

  return (
    <div class="space-y-4">
      <Breadcrumbs items={[{ href: "/", label: "Home" }, { href: "/applications", label: "Applications" }, { label: "Create Application" }]} />
      <h1 class="text-2xl font-semibold">Create Application</h1>
      <label class="block"><span class="text-sm">Display Name</span><input class="mt-1 w-full rounded border p-2" value={displayName()} onInput={(e)=>{const name=e.currentTarget.value;setDisplayName(name);setClientId(ensureUniqueClientId(name));}}/></label>
      <label class="block">
        <span class="text-sm">Client ID</span>
        <div class="mt-1 flex gap-2">
          <input class="w-full rounded border p-2" type={showClientId() ? "text" : "password"} value={clientId()} onInput={(e)=>setClientId(e.currentTarget.value)} onBlur={(e)=>setClientId(ensureUniqueClientId(e.currentTarget.value))} />
          <button class="rounded border px-3 py-2" type="button" onClick={()=>setShowClientId(!showClientId())}>{showClientId()?"Hide":"Show"}</button>
          <button class="rounded border px-3 py-2" type="button" onClick={()=>navigator.clipboard.writeText(clientId())}>Copy</button>
        </div>
      </label>
      <label class="block"><span class="text-sm">Client Secret</span><div class="mt-1 flex gap-2"><input class="w-full rounded border p-2" value={clientSecret()} onInput={(e)=>setClientSecret(e.currentTarget.value)} /><button class="rounded border px-3 py-2" type="button" onClick={()=>setClientSecret(generateSecret())}>Regenerate</button></div></label>
      <label class="block"><span class="text-sm">Flow</span><select class="mt-1 w-full rounded border p-2" value={flow()} onChange={(e)=>setFlow(e.currentTarget.value as ApplicationFlow)}><option value="ClientCredentials">ClientCredentials</option><option value="AuthorizationWithPKCE">AuthorizationWithPKCE</option></select></label>
      <label class="block"><span class="text-sm">Redirect Uris</span><input class="mt-1 w-full rounded border p-2" value={redirectUris()} onInput={(e)=>setRedirectUris(e.currentTarget.value)} /></label>
      <label class="block"><span class="text-sm">Post Logout Redirect Uris</span><input class="mt-1 w-full rounded border p-2" value={postLogoutRedirectUris()} onInput={(e)=>setPostLogoutRedirectUris(e.currentTarget.value)} /></label>
      <SearchMultiSelect label="Scopes" searchTerm={scopeSearchTerm()} onSearchTermChange={setScopeSearchTerm} options={(scopeSearch.data ?? []).map((s) => ({ id: s.id, label: `${s.displayName} (${s.scopeName})` }))} selected={scopeIds()} onChange={setScopeIds} />
      <button class="rounded bg-blue-700 px-3 py-2 text-white" onClick={()=>createApplication.mutate({displayName:displayName(),clientId:ensureUniqueClientId(clientId()),clientSecret:clientSecret(),flow:flow(),redirectUris:redirectUris(),postLogoutRedirectUris:postLogoutRedirectUris(),scopeIds:scopeIds()},{onSuccess:()=>navigate('/applications')})}>Create</button>
    </div>
  );
}
