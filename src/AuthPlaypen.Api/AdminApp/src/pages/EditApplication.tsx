import { useNavigate, useParams } from "@solidjs/router";
import { createEffect, createSignal } from "solid-js";
import { Breadcrumbs } from "@/components/Breadcrumbs";
import { DeleteConfirmationModal } from "@/components/DeleteConfirmationModal";
import { MultiSelect } from "@/components/MultiSelect";
import { useApplications, useDeleteApplication, useUpdateApplication } from "@/queries/applicationQueries";
import { useScopes } from "@/queries/scopeQueries";
import type { ApplicationFlow } from "@/types/models";
import { mapSelectionToIds } from "@/utils/selection";

function slugify(value: string) {
  return value
    .toLowerCase()
    .trim()
    .replace(/[^a-z0-9\s-]/g, "")
    .replace(/\s+/g, "-")
    .replace(/-+/g, "-");
}

function randomSuffix() {
  return Math.random().toString(36).slice(2, 7);
}

function generateSecret() {
  return crypto.randomUUID().replace(/-/g, "");
}

export function EditApplication() {
  const params = useParams();
  const navigate = useNavigate();
  const apps = useApplications();
  const scopes = useScopes();
  const update = useUpdateApplication();
  const remove = useDeleteApplication();

  const [displayName, setDisplayName] = createSignal("");
  const [clientId, setClientId] = createSignal("");
  const [clientSecret, setClientSecret] = createSignal("");
  const [flow, setFlow] = createSignal<ApplicationFlow>("ClientCredentials");
  const [postLogoutRedirectUris, setPostLogoutRedirectUris] = createSignal("");
  const [redirectUris, setRedirectUris] = createSignal("");
  const [selectedScopeIds, setSelectedScopeIds] = createSignal<string[]>([]);
  const [showDeleteModal, setShowDeleteModal] = createSignal(false);

  const selectedApp = () => apps.data?.find((a) => a.id === params.id);

  function ensureUniqueClientId(baseValue: string) {
    const normalized = slugify(baseValue);
    if (!normalized) return "";

    const existingIds = new Set((apps.data ?? []).filter((app) => app.id !== params.id).map((app) => app.clientId));
    if (!existingIds.has(normalized)) {
      return normalized;
    }

    let uniqueValue = `${normalized}-${randomSuffix()}`;
    while (existingIds.has(uniqueValue)) {
      uniqueValue = `${normalized}-${randomSuffix()}`;
    }

    return uniqueValue;
  }

  createEffect(() => {
    const app = selectedApp();
    if (!app) return;
    setDisplayName(app.displayName);
    setClientId(app.clientId);
    setClientSecret(app.clientSecret || generateSecret());
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
        <input
          class="mt-1 w-full rounded border p-2"
          value={displayName()}
          onInput={(e) => {
            const name = e.currentTarget.value;
            setDisplayName(name);
            setClientId(ensureUniqueClientId(name));
          }}
        />
      </label>
      <label class="block">
        <span class="text-sm">Client ID</span>
        <input
          class="mt-1 w-full rounded border p-2"
          value={clientId()}
          onInput={(e) => setClientId(e.currentTarget.value)}
          onBlur={(e) => setClientId(ensureUniqueClientId(e.currentTarget.value))}
        />
      </label>
      <label class="block">
        <span class="text-sm">Client Secret</span>
        <div class="mt-1 flex gap-2">
          <input class="w-full rounded border p-2" value={clientSecret()} onInput={(e) => setClientSecret(e.currentTarget.value)} />
          <button class="rounded border px-3 py-2" type="button" onClick={() => setClientSecret(generateSecret())}>
            Regenerate
          </button>
        </div>
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
              clientId: ensureUniqueClientId(clientId()),
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
      <button class="rounded bg-red-700 px-3 py-2 text-white" type="button" onClick={() => setShowDeleteModal(true)}>
        Delete Application
      </button>
      <DeleteConfirmationModal
        isOpen={showDeleteModal()}
        title="Delete application"
        description="This will permanently remove the application and all linked scope assignments."
        isDeleting={remove.isPending}
        onCancel={() => setShowDeleteModal(false)}
        onConfirm={() => {
          const app = selectedApp();
          if (!app) return;
          remove.mutate(app.id, {
            onSuccess: () => navigate("/applications"),
          });
          setShowDeleteModal(false);
        }}
      />
    </div>
  );
}
