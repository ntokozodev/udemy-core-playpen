import type { Application, ApplicationReference, Scope, ScopeReference } from "@/types/models";
import { mockApplications, MockScopes } from "@/services/mockData";

const state = {
  applications: structuredClone(mockApplications()),
  scopes: structuredClone(MockScopes()),
};

function applicationReference(app: Application): ApplicationReference {
  return {
    id: app.id,
    displayName: app.displayName,
    clientId: app.clientId,
  };
}

function scopeReference(scope: Scope): ScopeReference {
  return {
    id: scope.id,
    displayName: scope.displayName,
    scopeName: scope.scopeName,
    description: scope.description,
  };
}

function rebuildRelations() {
  state.scopes = state.scopes.map((scope) => ({ ...scope, applications: [] }));

  for (const app of state.applications) {
    app.scopes = app.scopes
      .map((scopeRef) => state.scopes.find((scope) => scope.id === scopeRef.id))
      .filter((scope): scope is Scope => Boolean(scope))
      .map(scopeReference);

    for (const scopeRef of app.scopes) {
      const scope = state.scopes.find((s) => s.id === scopeRef.id);
      if (!scope) continue;
      scope.applications.push(applicationReference(app));
    }
  }
}

rebuildRelations();

export const mockStore = {
  getApplications: () => state.applications,
  setApplications: (applications: Application[]) => {
    state.applications = applications;
    rebuildRelations();
  },
  getScopes: () => state.scopes,
  setScopes: (scopes: Scope[]) => {
    state.scopes = scopes;
    rebuildRelations();
  },
  rebuildRelations,
};
