# Runtime Configuration (AdminApp + API)

[← Back to docs index](./README.md)


## Why runtime config exists

`VITE_*` values are build-time only. QA/Staging/Live environments should use API-served runtime config via `GET /app-config`.

## Runtime flow

1. AdminApp starts.
2. AdminApp calls `GET /app-config`.
3. API returns OIDC runtime settings from `IConfiguration`.
4. Frontend boots with runtime values.

## Typical API environment variables

```bash
AdminApp__Oidc__EnableAuth=true
AdminApp__Oidc__Authority=https://login.qa.example.com
AdminApp__Oidc__ClientId=authkeeper-web-admin
AdminApp__Oidc__RedirectPath=/auth/callback
AdminApp__Oidc__PostLogoutRedirectPath=/

AzureAd__TenantId=<tenant-guid-or-common>
AzureAd__ClientId=<azure-app-client-id>
AzureAd__ClientSecret=<azure-app-client-secret>
AzureAd__CallbackPath=/signin-oidc
```

## Local frontend-only mode

For isolated UI work:

```bash
npm run dev:local-mock
```

This mode allows boot without `/app-config` and forces mock API usage.
