export type ApplicationFlow = "ClientCredentials" | "AuthorizationWithPKCE";

export type EntityMetadata = {
  createdBy: string;
  createdAt: string;
  updatedBy: string;
  updatedAt: string;
};

export type ApplicationReference = {
  id: string;
  displayName: string;
  clientId: string;
};

export type ScopeReference = {
  id: string;
  displayName: string;
  scopeName: string;
  description: string;
};

export type Application = {
  id: string;
  displayName: string;
  clientId: string;
  clientSecret: string;
  scopes: ScopeReference[];
  flow: ApplicationFlow;
  postLogoutRedirectUris?: string;
  redirectUris?: string;
  metadata: EntityMetadata;
};

export type Scope = {
  id: string;
  displayName: string;
  scopeName: string;
  description: string;
  applications: ApplicationReference[];
  metadata: EntityMetadata;
};

export type CursorPage<T> = {
  items: T[];
  nextCursor?: string;
};

export type CreateApplicationRequest = Omit<Application, "id" | "scopes" | "metadata"> & { scopeIds: string[] };
export type CreateScopeRequest = Omit<Scope, "id" | "applications" | "metadata"> & { applicationIds: string[] };
