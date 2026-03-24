# Domain Rules

## Application/Scope invariants

- Every Application must reference at least one Scope during create/update.
- A Scope can be:
  - Global (`applications = []`), or
  - Application-specific (`applications = [app1, app2]`).
- Scope update/delete operations are blocked if they would leave any existing application with zero effective scopes.
- `redirectUris` and `postLogoutRedirectUris` are only valid for `AuthorizationWithPKCE` applications.
