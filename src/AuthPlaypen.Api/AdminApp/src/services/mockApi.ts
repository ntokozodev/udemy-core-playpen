import { mockStore } from "@/services/mockStore";
import type { Application, ApplicationReference, CursorPage, EntityMetadata, Scope, ScopeReference } from "@/types/models";

function toCursorPage<T extends { id: string }>(items: T[], cursor?: string, pageSize = 10): CursorPage<T> {
  const start = cursor ? items.findIndex((item) => item.id === cursor) + 1 : 0;
  const safeStart = Math.max(start, 0);
  const pageItems = items.slice(safeStart, safeStart + pageSize);
  const last = pageItems.at(-1);
  const nextCursor = safeStart + pageSize < items.length && last ? last.id : undefined;
  return { items: pageItems, nextCursor };
}

function query(path: string) {
  const [pathname, raw = ""] = path.split("?");
  return { pathname, params: new URLSearchParams(raw) };
}

function appToReference(app: Application): ApplicationReference {
  return {
    id: app.id,
    displayName: app.displayName,
    clientId: app.clientId,
  };
}

function scopeToReference(scope: Scope): ScopeReference {
  return {
    id: scope.id,
    displayName: scope.displayName,
    scopeName: scope.scopeName,
    description: scope.description,
  };
}

function createMetadata(createdBy = "mock-system"): EntityMetadata {
  const timestamp = new Date().toISOString();

  return {
    createdBy,
    createdAt: timestamp,
    updatedBy: createdBy,
    updatedAt: timestamp,
  };
}

function touchMetadata(current: EntityMetadata, updatedBy = "mock-system"): EntityMetadata {
  return {
    ...current,
    updatedBy,
    updatedAt: new Date().toISOString(),
  };
}

export async function mockHttp<T>(path: string, init?: RequestInit): Promise<T> {
  const method = (init?.method ?? "GET").toUpperCase();
  const { pathname, params } = query(path);

  if (method === "GET" && pathname === "/user") {
    return {
      displayName: "System",
      email: null,
      isSystem: true,
    } as T;
  }

  if (method === "GET" && pathname === "/applications") {
    const pageSize = Number(params.get("pageSize") ?? "10");
    const cursor = params.get("cursor") ?? undefined;
    const items = [...mockStore.getApplications()].sort((a, b) => a.id.localeCompare(b.id));
    return toCursorPage(items, cursor, pageSize) as T;
  }

  if (method === "GET" && pathname === "/applications/search") {
    const term = (params.get("term") ?? "").trim().toLowerCase();
    const pageSize = Number(params.get("pageSize") ?? "20");
    const items = mockStore
      .getApplications()
      .filter((item) => !term || item.displayName.toLowerCase().includes(term) || item.clientId.toLowerCase().includes(term))
      .slice(0, pageSize)
      .map(appToReference);
    return items as T;
  }

  if (method === "GET" && pathname.startsWith("/applications/")) {
    const id = pathname.split("/")[2];
    const app = mockStore.getApplications().find((item) => item.id === id);
    if (!app) throw new Error(`Application ${id} not found`);
    return app as T;
  }

  if (method === "POST" && pathname === "/applications") {
    const payload = JSON.parse(init?.body as string);
    const scopeIds = new Set(payload.scopeIds ?? []);
    const scopes = mockStore.getScopes().filter((s) => scopeIds.has(s.id)).map(scopeToReference);

    const created: Application = {
      ...payload,
      id: crypto.randomUUID(),
      scopes,
      metadata: createMetadata(),
    };

    mockStore.setApplications([...mockStore.getApplications(), created]);
    return created as T;
  }

  if (method === "PUT" && pathname.startsWith("/applications/")) {
    const id = pathname.split("/")[2];
    const payload = JSON.parse(init?.body as string);
    const current = mockStore.getApplications();
    const index = current.findIndex((a) => a.id === id);
    if (index < 0) throw new Error(`Application ${id} not found`);
    const scopeIds = new Set(payload.scopeIds ?? []);
    const scopes = mockStore.getScopes().filter((s) => scopeIds.has(s.id)).map(scopeToReference);
    const updated: Application = {
      ...current[index],
      ...payload,
      id,
      scopes,
      metadata: touchMetadata(current[index].metadata),
    };
    const next = [...current];
    next[index] = updated;
    mockStore.setApplications(next);
    return updated as T;
  }

  if (method === "DELETE" && pathname.startsWith("/applications/")) {
    const id = pathname.split("/")[2];
    mockStore.setApplications(mockStore.getApplications().filter((a) => a.id !== id));
    return undefined as T;
  }

  if (method === "GET" && pathname === "/scopes") {
    const pageSize = Number(params.get("pageSize") ?? "10");
    const cursor = params.get("cursor") ?? undefined;
    const items = [...mockStore.getScopes()].sort((a, b) => a.id.localeCompare(b.id));
    return toCursorPage(items, cursor, pageSize) as T;
  }

  if (method === "GET" && pathname === "/scopes/search") {
    const term = (params.get("term") ?? "").trim().toLowerCase();
    const pageSize = Number(params.get("pageSize") ?? "20");
    const items = mockStore
      .getScopes()
      .filter((item) => !term || item.displayName.toLowerCase().includes(term) || item.scopeName.toLowerCase().includes(term))
      .slice(0, pageSize)
      .map(scopeToReference);
    return items as T;
  }

  if (method === "GET" && pathname.startsWith("/scopes/")) {
    const id = pathname.split("/")[2];
    const scope = mockStore.getScopes().find((item) => item.id === id);
    if (!scope) throw new Error(`Scope ${id} not found`);
    return scope as T;
  }

  if (method === "POST" && pathname === "/scopes") {
    const payload = JSON.parse(init?.body as string);
    const appIds = new Set(payload.applicationIds ?? []);
    const applications = mockStore.getApplications().filter((a) => appIds.has(a.id)).map(appToReference);

    const created: Scope = {
      ...payload,
      id: crypto.randomUUID(),
      applications,
      metadata: createMetadata(),
    };

    mockStore.setScopes([...mockStore.getScopes(), created]);
    return created as T;
  }

  if (method === "PUT" && pathname.startsWith("/scopes/")) {
    const id = pathname.split("/")[2];
    const payload = JSON.parse(init?.body as string);
    const current = mockStore.getScopes();
    const index = current.findIndex((s) => s.id === id);
    if (index < 0) throw new Error(`Scope ${id} not found`);
    const appIds = new Set(payload.applicationIds ?? []);
    const applications = mockStore.getApplications().filter((a) => appIds.has(a.id)).map(appToReference);
    const updated: Scope = {
      ...current[index],
      ...payload,
      id,
      applications,
      metadata: touchMetadata(current[index].metadata),
    };
    const next = [...current];
    next[index] = updated;
    mockStore.setScopes(next);
    return updated as T;
  }

  if (method === "DELETE" && pathname.startsWith("/scopes/")) {
    const id = pathname.split("/")[2];
    mockStore.setScopes(mockStore.getScopes().filter((s) => s.id !== id));
    return undefined as T;
  }

  throw new Error(`Mock API route not implemented: ${method} ${path}`);
}
