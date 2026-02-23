import { useParams } from "@solidjs/router";
import { createEffect, createSignal } from "solid-js";
import { Breadcrumbs } from "@/components/Breadcrumbs";
import { MultiSelect } from "@/components/MultiSelect";
import { useApplications, useUpdateApplication } from "@/queries/applicationQueries";
import { useScopes } from "@/queries/scopeQueries";
import type { ApplicationFlow } from "@/types/models";
import { mapSelectionToIds } from "@/utils/selection";

export function EditApplication() {
  const params = useParams();
  const apps = useApplications();
  const scopes = useScopes();
  const update = useUpdateApplication();

  const [displayName, setDisplayName] = createSignal("");
  const [clientId, setClientId] = createSignal("");
  const [clientSecret, setClientSecret] = createSignal("");
  const [flow, setFlow] = createSignal<ApplicationFlow>("ClientCredentials");
  const [postLogoutRedirectUris, setPostLogoutRedirectUris] = createSignal("");
  const [redirectUris, setRedirectUris] = createSignal("");
  const [selectedScopeIds, setSelectedScopeIds] = createSignal<string[]>([]);

  const selectedApp = () => apps.data?.find((a) => a.id === params.id);

  createEffect(() => {
    const app = selectedApp();
    if (!app) return;
    setDisplayName(app.displayName);
    setClientId(app.clientId);
    setClientSecret(app.clientSecret);
    setFlow(app.flow);
    setPostLogoutRedirectUris(app.postLogoutRedirectUris ?? "");
    setRedirectUris(app.redirectUris ?? "");
    setSelectedScopeIds(app.scopes.map((s) => s.id));
  });

  return (
    <div class="space-y-4">
      <Breadcrumbs items={[{ href: "/", label: "Home" }, { href: "/applications", label: "Applications" }, { label: "Edit Application" }]} />
      <h1 class="text-2xl font-semibold">Edit Application</h1>
      <label class="block">
        <span class="text-sm">Display Name</span>
        <input class="mt-1 w-full rounded border p-2" value={displayName()} onInput={(e) => setDisplayName(e.currentTarget.value)} />
      </label>
      <label class="block">
        <span class="text-sm">Client ID</span>
        <input class="mt-1 w-full rounded border p-2" value={clientId()} onInput={(e) => setClientId(e.currentTarget.value)} />
      </label>
      <label class="block">
        <span class="text-sm">Client Secret</span>
        <input class="mt-1 w-full rounded border p-2" value={clientSecret()} onInput={(e) => setClientSecret(e.currentTarget.value)} />
      </label>
      <label class="block">
        <span class="text-sm">Flow</span>
        <select class="mt-1 w-full rounded border p-2" value={flow()} onChange={(e) => setFlow(e.currentTarget.value as ApplicationFlow)}>
          <option value="ClientCredentials">ClientCredentials</option>
          <option value="AuthorizationWithPKCE">AuthorizationWithPKCE</option>
        </select>
      </label>
      <label class="block">
        <span class="text-sm">Redirect Uris</span>
        <input class="mt-1 w-full rounded border p-2" value={redirectUris()} onInput={(e) => setRedirectUris(e.currentTarget.value)} />
      </label>
      <label class="block">
        <span class="text-sm">Post Logout Redirect Uris</span>
        <input
          class="mt-1 w-full rounded border p-2"
          value={postLogoutRedirectUris()}
          onInput={(e) => setPostLogoutRedirectUris(e.currentTarget.value)}
        />
      </label>
      <MultiSelect
        label="Scopes"
        options={(scopes.data ?? []).map((s) => ({ id: s.id, label: `${s.displayName} (${s.scopeName})` }))}
        selected={selectedScopeIds()}
        onChange={setSelectedScopeIds}
      />
      <button
        class="rounded bg-blue-700 px-3 py-2 text-white"
        onClick={() => {
          const app = selectedApp();
          if (!app) return;
          update.mutate({
            id: app.id,
            payload: {
              displayName: displayName(),
              clientId: clientId(),
              clientSecret: clientSecret(),
              flow: flow(),
              redirectUris: redirectUris(),
              postLogoutRedirectUris: postLogoutRedirectUris(),
              scopeIds: mapSelectionToIds(selectedScopeIds(), (scopes.data ?? []).map((scope) => scope.id)),
            },
          });
        }}
      >
        Save
      </button>
    </div>
  );
}
