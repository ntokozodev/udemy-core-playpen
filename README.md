# dotnet-auth-playpen API

ASP.NET Core 8 Web API + PostgreSQL that supports CRUD for `Application` and `Scope`.


## Project layout

The solution is now organized into layered projects:

- `AuthPlaypen.Api` - HTTP surface and startup/composition root only.
- `AuthPlaypen.Application` - DTOs and application services/use-cases.
- `AuthPlaypen.Data` - all EF Core persistence concerns.
- `AuthPlaypen.Domain` - domain entities and enums.

## Strict layer ownership

This repository follows strict layer ownership:

- `AuthPlaypen.Api`
  - Owns controllers, request/response wiring, DI registration, and host startup.
  - Must not own persistence artifacts (no migrations, no `DbContext` factory).
- `AuthPlaypen.Application`
  - Owns use-case orchestration and contracts.
  - Must not contain HTTP or EF Core infrastructure details.
- `AuthPlaypen.Data`
  - Owns persistence end-to-end: `DbContext`, entity mapping, design-time factory, and EF migrations.
- `AuthPlaypen.Domain`
  - Owns core business model and enums with no infrastructure dependencies.

In short: if a change is data/persistence-specific, it belongs in `AuthPlaypen.Data`.

## Domain rules implemented

- Every application must reference at least one scope when created or updated.
- A scope can be global (applies to all applications) or application-specific.
  - `applications = []` in scope payload means **global scope**.
  - `applications = [app1, app2]` means **scope only for those applications**.
- Scope update/delete operations are blocked when they would leave any existing application with zero effective scopes.
- `redirectUris` and `postLogoutRedirectUris` are only valid when `flow` is `AuthorizationWithPKCE`.

## Run with Docker

```bash
docker compose up --build
```

API: `http://localhost:8080`
Swagger UI: `http://localhost:8080/swagger`

## AdminApp frontend auth flow (feature-flagged)

The admin frontend includes an `oidc-client-ts` based auth flow that is intentionally disabled by default.

- `VITE_ENABLE_OIDC_AUTH=false` keeps current behavior (no enforced sign-in).
- Set `VITE_ENABLE_OIDC_AUTH=true` to require authentication for admin routes.
- Callback route is `/admin/auth/callback`.
- Unauthenticated users are redirected to API/OpenIddict with PKCE (`response_type=code`).
- OIDC scope is intentionally fixed in frontend (`openid profile`) and treated as API/OpenIddict contract, not a per-frontend env setting.
- Frontend OIDC user state is stored in `sessionStorage` (not `localStorage`).
- The admin app should point to **API/OpenIddict authority only** and should not target Azure directly.

Runtime configuration for `src/AuthPlaypen.Api/AdminApp` is **100% API-served** (no Vite `.env` fallback in frontend).

## Environment strategy for QA/Staging/Live (API-served runtime config)

Vite env vars are build-time and frozen in the bundle, so the admin app now loads config only from `GET /app-config` at startup.

### How it works

- Admin frontend boots and calls `GET /app-config`.
- API returns runtime settings from `IConfiguration`.
- Frontend maps those values into runtime config and then starts the app.
- Even when mock data is enabled, runtime config still comes from `/app-config`, so the API must be running for the admin app to boot.

Server shape (implemented in `Program.cs`):

```csharp
app.MapGet("/app-config", (IConfiguration config, IWebHostEnvironment environment) =>
{
    var useMockData = config["AdminApp:UseMockData"];
    var enableOidcAuth = config["AdminApp:Oidc:EnableAuth"];
    var authority = config["AdminApp:Oidc:Authority"];
    var clientId = config["AdminApp:Oidc:ClientId"];
    var redirectPath = config["AdminApp:Oidc:RedirectPath"];
    var postLogoutRedirectPath = config["AdminApp:Oidc:PostLogoutRedirectPath"];

    var useLocalMockDefaults = environment.IsDevelopment() && string.Equals(useMockData, "true", StringComparison.OrdinalIgnoreCase);

    if (useLocalMockDefaults)
    {
        authority = "https://localhost:5100";
        clientId = "gatekeeper-web-admin";
        redirectPath = "/auth/callback";
        postLogoutRedirectPath = "/";
    }

    return Results.Ok(new
    {
        useMockData,
        enableOidcAuth,
        authority,
        clientId,
        redirectPath,
        postLogoutRedirectPath
    });
});
```

### Configure staging/live on Linux server

Set environment variables for the API process (systemd/container/app service), for example:

```bash
AdminApp__UseMockData=false
AdminApp__Oidc__EnableAuth=true
AdminApp__Oidc__Authority=https://login.qa.example.com
AdminApp__Oidc__ClientId=gatekeeper-web-admin
AdminApp__Oidc__RedirectPath=/auth/callback
AdminApp__Oidc__PostLogoutRedirectPath=/
```

Use API environment variables (`AdminApp__...`) for QA/Staging/Live instead of frontend `VITE_...` values. `VITE_*` variables are build-time and become fixed in the bundled assets, while `/app-config` keeps config runtime-driven per environment.

### Local mock mode (development only)

Local defaults are now defined in `src/AuthPlaypen.Api/appsettings.Development.json` under `AdminApp`:

```json
"AdminApp": {
  "UseMockData": "true",
  "Oidc": {
    "EnableAuth": "false",
    "Authority": "https://localhost:5100",
    "ClientId": "gatekeeper-web-admin",
    "RedirectPath": "/auth/callback",
    "PostLogoutRedirectPath": "/"
  }
}
```

The `/app-config` endpoint reads these values in Development, so local mock mode works without extra env setup.

`UseMockData=true` only switches admin CRUD/data requests to the in-memory mock API. It does not bypass runtime config loading.



## OpenIddict v7 + Redis storage

The API is wired to OpenIddict Core v7 using custom Redis-backed stores for:

- Applications (`IOpenIddictApplicationStore`)
- Scopes (`IOpenIddictScopeStore`)
- Tokens (`IOpenIddictTokenStore`)

Admin CRUD remains in PostgreSQL. After admin writes, the application services call sync services that upsert/delete OpenIddict entities in Redis.

Configuration:

- `ConnectionStrings:Postgres` for admin data
- `ConnectionStrings:Redis` for OpenIddict stores (default `localhost:6379`)

## API contracts

### ApplicationFlow
- `ClientCredentials`
- `AuthorizationWithPKCE`

### Application payload shape

```json
{
  "id": "guid",
  "displayName": "Admin App",
  "clientId": "admin-client",
  "clientSecret": "secret",
  "flow": "AuthorizationWithPKCE",
  "postLogoutRedirectUris": "https://example.com/logout",
  "redirectUris": "https://example.com/callback",
  "scopes": [
    {
      "id": "guid",
      "displayName": "Read Users",
      "scopeName": "users.read",
      "description": "Read user profile data"
    }
  ]
}
```

### Scope payload shape

```json
{
  "id": "guid",
  "displayName": "Read Users",
  "scopeName": "users.read",
  "description": "Read user profile data",
  "applications": []
}
```

`applications: []` means the scope is global.

## Migrations and design-time EF tooling

Under strict ownership, EF Core migrations and `AuthPlaypenDbContextFactory` should live in `AuthPlaypen.Data`.

Use EF commands with explicit target/startup projects:

```bash
dotnet ef migrations add <Name> \
  --project src/AuthPlaypen.Data \
  --startup-project src/AuthPlaypen.Api
```

```bash
dotnet ef database update \
  --project src/AuthPlaypen.Data \
  --startup-project src/AuthPlaypen.Api
```

At runtime, API remains the composition root and continues to apply migrations through `Database.Migrate()` during startup.

## Client Credentials flow (Application A -> token -> external resource APIs)

Auth API acts as the token orchestrator. Resource APIs (resource-b/resource-c/resource-d/etc.) are independent services that trust tokens issued by Auth API. Client authentication and token persistence use OpenIddict Redis stores (not the SQL admin CRUD database).

This repo provides:

- `POST /connect/token` (client credentials grant)

### 1) Register scopes and clients

In AdminApp:

- Register resource API scopes (for example: `resource-b.read`, `resource-c.write`, `resource-d.read`).
- Create **Application A** with flow `ClientCredentials`.
- Assign the scopes Application A is allowed to use.

> Important: scopes are assigned in AdminApp and synchronized to OpenIddict application permissions. Token requests do not need a `scope` parameter, and `/connect/token` resolves scopes from OpenIddict stores.

### 2) Request token from Application A

```bash
curl -X POST "http://localhost:8080/connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "client_id=<application-a-client-id>" \
  -d "client_secret=<application-a-client-secret>"
```

Response contains:

- `access_token`
- `token_type` (`Bearer`)
- `expires_in`
- `scope` (all scopes assigned to the client)

### 3) Call any external resource API with bearer token

```bash
curl "https://resource-b.example.com/api/orders" \
  -H "Authorization: Bearer <access_token>"
```

### OpenIddict idiomatic token endpoint notes

`POST /connect/token` is implemented as an OpenIddict server passthrough endpoint:

- OpenIddict server is configured with `AllowClientCredentialsFlow()` and token endpoint URI `/connect/token`.
- Controller reads `HttpContext.GetOpenIddictServerRequest()` and returns `SignIn(..., OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)`.
- Client and scope permissions are resolved from OpenIddict application permissions synced by AdminApp.

This keeps behavior close to OpenIddict defaults while still allowing custom checks when needed.

### How each external API validates token from Application A

When `Application A` uses its own `client_id/client_secret`, it authenticates to Auth API (token issuer). The resulting JWT is then sent to resource APIs.

Each resource API validates the JWT by checking:

> A scope-authorization middleware on each resource API is a good place to enforce that token scopes are allowed for that client/application.


1. **Signature** using the issuer signing key.
2. **Issuer** (`iss`) matches trusted Auth API issuer.
3. **Audience** (`aud`) matches expected audience.
4. **Expiry** (`exp`) and token lifetime claims.
5. **Scope claims** contain the permission(s) that API endpoint requires.

So resource APIs never need Application A's secret. They only trust the token issuer and enforce their own required scopes.
