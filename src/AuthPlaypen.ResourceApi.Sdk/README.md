# AuthPlaypen.ResourceApi.Sdk

NuGet package ID: `AuthPlaypen.ResourceApi.Sdk`
Code namespace: `AuthPlaypen.ResourceApi`

Shared authentication utilities for **resource APIs** that consume AuthPlaypen-issued access tokens.
It now covers two related concerns:

1. **Inbound token validation** for your API (`AddAuthApiResourceAuthentication`).
2. **Outbound calls to the Auth API** for token/introspection workflows (`AddAuthApiClient`).


## Package layout

The SDK is organized by responsibility so navigation is self-documenting:

```text
AuthPlaypen.ResourceApi.Sdk/
├── ResourceAuthentication/   # inbound bearer token validation setup (JWT/introspection)
├── AuthApiClient/            # outbound Auth API client registration + implementation
│   └── Models/               # token/introspection response contracts
└── Authorization/            # scope policy helpers
```

## Features

- Local JWT validation via OIDC discovery/JWKS.
- Optional introspection mode for APIs that need active-token checks.
- Scope policy helper (`RequireAnyScope`).
- Dynamic permission-alias authorization with cached metadata resolution (`RequirePermissionAlias`).
- Auth API client wrapper (`IAuthApiClient`) for token + introspection endpoints.

## Example (registration + runtime enforcement)

```csharp
using AuthPlaypen.ResourceApi;
using Microsoft.AspNetCore.Authorization;

builder.Services.AddAuthApiResourceAuthentication(options =>
{
    // optional: defaults to https://localhost:5100
    // options.Authority = "https://localhost:5100";
    options.Audience = "resource-b";
    options.ValidationMode = AuthApiTokenValidationMode.Jwt; // or Introspection

    // required when ValidationMode = Introspection
    options.IntrospectionClientId = "resource-b-introspection";
    options.IntrospectionClientSecret = "change-me";
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("orders.read", policy =>
        policy.RequireAuthenticatedUser().RequireAnyScope("resource-b.orders.read"));

    options.AddPolicy("orders.write", policy =>
        policy.RequireAuthenticatedUser().RequireAnyScope(
            "resource-b.orders.write",
            "resource-b.orders.admin"));
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/orders", [Authorize(Policy = "orders.read")] () => Results.Ok("read ok"));
app.MapPost("/orders", [Authorize(Policy = "orders.write")] () => Results.Ok("write ok"));
```

## Dynamic permission alias authorization (recommended)

Use this to avoid hardcoding scope strings directly in endpoint policies:

```csharp
builder.Services.AddAuthApiPermissionAliasAuthorization(options =>
{
    options.Authority = "https://localhost:5100";
    options.MetadataEndpoint = "/.well-known/authplaypen/permissions";
    options.CacheDuration = TimeSpan.FromMinutes(5);

    // Optional fallback map (last-resort resilience)
    options.HardcodedFallbackMappings["orders.read"] = ["resource-b.orders.read"];
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("orders.read", policy =>
        policy.RequireAuthenticatedUser().RequirePermissionAlias("orders.read"));
});
```

`/.well-known/authplaypen/permissions` is **not** an OpenID Connect standard endpoint.
It is an AuthPlaypen-specific endpoint exposed by AuthApi.

If you prefer, you can still use hardcoded scope values with `RequireAnyScope(...)`. That remains a viable
option for stable/low-change APIs.

`RequireAnyScope(...)` uses OR semantics across provided scopes.
`Authority` is optional in this package and defaults to `https://localhost:5100`. Override it only if your Auth API host differs.

When `ValidationMode = AuthApiTokenValidationMode.Jwt`, the SDK uses the configured `Authority` for
OIDC discovery (`/.well-known/openid-configuration`) and JWKS (`/.well-known/jwks`) so token validation
happens locally in the resource API.

### Static policy names vs dynamic AdminApp/OpenIddict data

`[Authorize(Policy = "orders.read")]` means this endpoint must satisfy a named ASP.NET authorization rule.

Do not confuse policy names with AdminApp scopes: `orders.read` is your API policy alias, while
`resource-b.orders.read` is an OAuth scope value in access tokens. Policy names can be renamed as long as
the policy enforces the correct scope claim(s).

### What if an endpoint has no `[Authorize(...)]`?

That endpoint is public by default:

- With `[Authorize(Policy = "orders.read")]`: caller must present a valid token with required scope(s).
- Without `[Authorize]`: anyone can call the route (no token/scope check by default).

In practice, the policy is what makes `/orders` protected and permission-aware, not just the route itself.

## Auth API SDK-style client wrapper

This library also includes an easy-to-use client wrapper for common Auth API endpoints:

- `POST /connect/token` (client credentials token issuance)
- `POST /connect/introspect` (opaque token introspection)

Register it:

```csharp
builder.Services.AddAuthApiClient(options =>
{
    options.Authority = "https://localhost:5100";
    options.ClientId = "resource-b-introspection";
    options.ClientSecret = "change-me";
});
```

Use it:

```csharp
public class TokenService(IAuthApiClient authApiClient)
{
    public async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        var token = await authApiClient.RequestClientCredentialsTokenAsync(
            ["resource-b.orders.read"],
            ct);

        return token.AccessToken;
    }
}
```

## Publishing to NuGet (or another package feed)

Yes — this project is already set up to be packaged as a NuGet package. The `.csproj` has `GeneratePackageOnBuild=true` and package metadata (`PackageId`, `Version`, etc.), so building/packing produces a `.nupkg`.

Typical flow:

```bash
# from repository root
dotnet pack src/AuthPlaypen.ResourceApi.Sdk/AuthPlaypen.ResourceApi.Sdk.csproj -c Release

# push to NuGet.org
dotnet nuget push src/AuthPlaypen.ResourceApi.Sdk/bin/Release/*.nupkg \
  --api-key <NUGET_API_KEY> \
  --source https://api.nuget.org/v3/index.json
```

You can also push to GitHub Packages, Azure Artifacts, or an internal feed by changing `--source`.
