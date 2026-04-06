# Resource API Integration

[← Back to docs index](./README.md)


## Validation model

Resource APIs should validate bearer tokens; they should not store client secrets for calling applications.

Recommended default:
- Local JWT validation using OIDC discovery + JWKS.

Optional for higher-security needs:
- Introspection (`/connect/introspect`) for near real-time revocation checks.

## Validation tradeoffs

| Mode | Calls Auth API each check | Revocation freshness | Latency |
|---|---|---|---|
| `Jwt` | No (after metadata/JWKS cache) | Eventual | Lowest |
| `Introspection` | Yes | Near real-time | Higher |

## Reusable package in this repo

`src/AuthPlaypen.ResourceApi.Sdk` provides:
- `AddAuthApiResourceAuthentication(...)` for JWT or introspection validation setup.
- Mode switch: `AuthApiTokenValidationMode.Jwt` / `AuthApiTokenValidationMode.Introspection`.
- Scope policy helper: `RequireAnyScope(...)` (OR semantics across provided scopes).
- Auth API client wrapper registration: `AddAuthApiClient(...)`.
- Runtime auth client interface: `IAuthApiClient` for token and introspection calls.

## Important defaults and requirements

- `Authority` defaults to `https://localhost:5100` via `AuthApiResourceAuthDefaults.DefaultAuthority`.
- `Audience` is required for both JWT and introspection validation.
- For introspection mode, `IntrospectionClientId` and `IntrospectionClientSecret` are required.
- Introspection uses built-in response caching (`CacheDuration = 2 minutes`) in the package configuration.

## Typical setup

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

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("orders.read", policy =>
        policy.RequireAuthenticatedUser().RequireAnyScope("resource-b.orders.read"));
});
```

## Policy names are static; scope values are the dynamic contract

`[Authorize(Policy = "orders.read")]` tells ASP.NET that this endpoint must pass a named authorization
rule before it can be accessed.

Do not confuse policy names with AdminApp scopes: `orders.read` is your API policy alias, while
`resource-b.orders.read` is an OAuth scope value in tokens. Policy names can be renamed as long as the
policy still checks the right scope claim(s).

## What changes if you remove `[Authorize(...)]` from an endpoint?

There is a major security difference:

- `app.MapGet("/orders", [Authorize(Policy = "orders.read")] ...)`
  - Requires a valid access token.
  - Requires the token to satisfy the mapped policy/scope requirement.
  - Returns `401` (no/invalid token) or `403` (token present, missing required scope).
- `app.MapGet("/orders", () => ...)`
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
