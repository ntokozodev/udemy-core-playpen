import { http } from "./http";
import type { CreateScopeRequest, CursorPage, Scope, ScopeReference } from "@/types/models";

export const scopeService = {
  getPaged: (cursor?: string, pageSize = 10) =>
    http<CursorPage<Scope>>(`/scopes?pageSize=${pageSize}${cursor ? `&cursor=${cursor}` : ""}`),
  getById: (id: string) => http<Scope>(`/scopes/${id}`),
  search: (term: string, pageSize = 20) =>
    http<ScopeReference[]>(`/scopes/search?term=${encodeURIComponent(term)}&pageSize=${pageSize}`),
  create: (payload: CreateScopeRequest) => http<Scope>("/scopes", { method: "POST", body: JSON.stringify(payload) }),
  update: (id: string, payload: CreateScopeRequest) =>
    http<Scope>(`/scopes/${id}`, { method: "PUT", body: JSON.stringify(payload) }),
  delete: (id: string) => http<void>(`/scopes/${id}`, { method: "DELETE" }),
};
