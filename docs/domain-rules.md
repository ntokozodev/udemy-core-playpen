# Domain Rules (Current Implementation)

This page reflects the current behavior in `ApplicationService` and `ScopeService`.

## Uniqueness

- `Application.ClientId` must be unique.
- `Scope.ScopeName` must be unique.
- Uniqueness is enforced in services and backed by DB unique indexes.

## Relationship ID validation

- Application create/update validates `scopeIds`:
  - every referenced scope ID must exist.
- Scope create/update validates `applicationIds`:
  - every referenced application ID must exist.

## Flow/URI validation

- `redirectUris` and `postLogoutRedirectUris` are only allowed when `flow = AuthorizationWithPKCE`.

## Scope assignment behavior (as implemented)

- Scope globalness is derived (`IsGlobal`) from links:
  - no `ApplicationScopes` links => global scope
  - one or more links => app-specific scope
- During Application create/update:
  - global scopes are assignable
  - app-specific scopes are assignable only if they already link to that application ID
- Practical implication for create:
  - a newly created application usually cannot be assigned app-specific scopes in the same create request, because those scopes cannot already reference the not-yet-existing application ID.

## Not currently enforced

- No invariant currently forces an application to have at least one scope.
- No invariant currently blocks scope update/delete based on possible downstream orphaning behavior.
