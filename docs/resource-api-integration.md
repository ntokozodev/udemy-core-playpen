# Resource API Integration

[← Back to docs index](./README.md)


## Validation model

Resource APIs should validate bearer tokens; they should not store client secrets for calling applications.

Recommended default:
- Local JWT validation using OIDC discovery + JWKS.
  - In SDK terms: `ValidationMode = AuthApiTokenValidationMode.Jwt` and `Authority` points to AuthApi.

Optional for higher-security needs:
- Introspection (`/connect/introspect`) for near real-time revocation checks.

## Validation tradeoffs

| Mode | Calls Auth API each check | Revocation freshness | Latency |
|---|---|---|---|
| `Jwt` | No (after metadata/JWKS cache) | Eventual | Lowest |
| `Introspection` | Yes | Near real-time | Higher |

## Integration path 1: Custom stack resource APIs

This path is stack-agnostic and works for any resource API implementation.

Required integration points:
- Obtain tokens from AuthApi `/connect/token` (for machine-to-machine/client-credentials use cases).
- Validate access tokens locally using OIDC discovery and JWKS from AuthApi (`/.well-known/openid-configuration`, `/.well-known/jwks`).
- Enforce your API permissions/scopes in your framework's authorization middleware.

Optional integration point:
- Call `/connect/introspect` when near real-time token state checks are needed (at the cost of per-call dependency/latency).

In short: AdminApp/AuthApi remain your central dynamic source of truth for applications/scopes regardless of API runtime.

## Integration path 2 (optional): ResourceApi.Sdk convenience package

`src/AuthPlaypen.ResourceApi.Sdk` is an optional convenience package. Teams can use it, or integrate directly using path 1.

The SDK provides:
- `AddAuthApiResourceAuthentication(...)` for JWT or introspection validation setup.
- Mode switch: `AuthApiTokenValidationMode.Jwt` / `AuthApiTokenValidationMode.Introspection`.
- Dynamic permission-alias authorization: `AddAuthApiPermissionAliasAuthorization(...)` + `RequirePermissionAlias(...)`.
- Scope policy helper (alternative option): `RequireAnyScope(...)` (OR semantics across provided scopes).
- Auth API client wrapper registration: `AddAuthApiClient(...)`.
- Runtime auth client interface: `IAuthApiClient` for token and introspection calls.

### SDK defaults and requirements

- `Authority` defaults to `https://localhost:5100` via `AuthApiResourceAuthDefaults.DefaultAuthority`.
- `Audience` is required for both JWT and introspection validation.
- `MetadataEndpoint` is required when using the default HTTP permission metadata source.
- For introspection mode, `IntrospectionClientId` and `IntrospectionClientSecret` are required.
- Introspection uses built-in response caching (`CacheDuration = 2 minutes`) in the package configuration.

### Typical SDK setup (dynamic alias recommended)

```csharp
builder.Services.AddAuthApiResourceAuthentication(options =>
{
    // Optional: defaults to https://localhost:5100
    // options.Authority = "https://localhost:5100";

    options.Audience = "resource-b";
    options.ValidationMode = AuthApiTokenValidationMode.Jwt; // or Introspection

    // Required only for Introspection mode
    options.IntrospectionClientId = "resource-b-introspection";
    options.IntrospectionClientSecret = "change-me";
});

builder.Services.AddAuthApiPermissionAliasAuthorization(options =>
{
    options.Authority = "https://localhost:5100";
    options.MetadataEndpoint = "/.well-known/authplaypen/permissions";
    options.CacheDuration = TimeSpan.FromMinutes(5);
    options.HardcodedFallbackMappings["orders.read"] = ["resource-b.orders.read"];
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("orders.read", policy =>
        policy.RequireAuthenticatedUser().RequirePermissionAlias("orders.read"));
});
```

`/.well-known/authplaypen/permissions` is not an OIDC standard endpoint; it is an AuthPlaypen-specific
metadata endpoint exposed by AuthApi.

Hardcoded scope checks are also a viable option:

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("orders.read", policy =>
        policy.RequireAuthenticatedUser().RequireAnyScope("resource-b.orders.read"));
});
```

## Policy names are static; scope values are the dynamic contract (SDK example)

`[Authorize(Policy = "orders.read")]` tells ASP.NET that this endpoint must pass a named authorization
rule before it can be accessed.

Do not confuse policy names with AdminApp scopes: `orders.read` is your API policy alias, while
`resource-b.orders.read` is an OAuth scope value in tokens. Policy names can be renamed as long as the
policy still checks the right scope claim(s).

## Authorization enforcement options (recommended order)

1. **Dynamic permission alias resolution (recommended):**
   - Endpoint policy references a permission alias (e.g., `orders.read`).
   - API resolves alias -> scopes from AuthApi metadata with local caching.
   - Best fit for fast-moving scope contracts and high-throughput APIs.
2. **Hybrid with fallback mapping:**
   - Same dynamic model, but with a local fallback map for resilience during metadata outages.
3. **Hardcoded scopes (viable option):**
   - Use explicit scope strings in API policy (e.g., `RequireAnyScope("resource-b.orders.read")`).
   - Acceptable for simple/static APIs, but requires manual updates when scope contracts change.

## What changes if you remove `[Authorize(...)]` from an endpoint?

There is a major security difference:

- `app.MapGet("/orders", [Authorize(Policy = "orders.read")] () => Results.Ok("read ok"))`
  - Requires a valid access token.
  - Requires the token to satisfy the mapped policy/scope requirement.
  - Returns `401` (no/invalid token) or `403` (token present, missing required scope).
- `app.MapGet("/orders", () => Results.Ok("read ok"))`
  - Public endpoint by default.
  - No token or scope is required for access.

So the significance is not about “hardcoding strings”; it is about whether the endpoint is protected and
which permissions are required.

## Optional SDK-style Auth API client

Use this when a resource API also needs to request tokens or call introspection directly:

```csharp
builder.Services.AddAuthApiClient(options =>
{
    options.Authority = "https://localhost:5100";
    options.ClientId = "resource-b-introspection";
    options.ClientSecret = "change-me";
});
```

Then inject `IAuthApiClient` and call:
- `RequestClientCredentialsTokenAsync(...)`
- `IntrospectTokenAsync(...)`

## First-use rollout checklist (for this new implementation)

This is the first rollout of dynamic permission-alias authorization, so prefer a staged release.

1. **AuthApi endpoint availability**
   - Confirm `GET /.well-known/authplaypen/permissions` returns HTTP 200 in each environment.
   - Confirm payload includes expected aliases/scopes for at least one real API permission.
2. **JWT local validation baseline**
   - Confirm resource APIs validate tokens with OIDC discovery/JWKS from AuthApi.
   - Keep validation mode as `Jwt` unless introspection is explicitly required.
3. **Permission alias policy wiring**
   - Confirm protected endpoints use `[Authorize(Policy = "...")]` and that each policy uses `RequirePermissionAlias("...")`.
   - Keep aliases stable (e.g., `orders.read`) even if backing scopes evolve.
4. **Cache and resiliency settings**
   - Set `CacheDuration` for each API based on acceptable staleness (for example 1-5 minutes).
   - Decide whether to configure `HardcodedFallbackMappings` for critical permissions during initial launch.
5. **Smoke tests before publish**
   - Positive: token with required scope can access protected endpoint.
   - Negative: token missing scope returns `403`.
   - Negative: missing/invalid token returns `401`.
6. **Operational visibility**
   - Add logs/metrics around permission metadata fetch failures and cache refresh behavior.
   - Add a dashboard/alert for repeated metadata fetch failures.
7. **Safe cutover plan**
   - Start with one low-risk API route or one pilot service.
   - Expand gradually after observing stable auth success/error rates.
