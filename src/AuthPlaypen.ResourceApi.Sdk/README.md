# AuthPlaypen.ResourceApi.Sdk

NuGet package ID: `AuthPlaypen.ResourceApi.Sdk`
Code namespace: `AuthPlaypen.ResourceApi`

Shared authentication utilities for **resource APIs** that consume AuthPlaypen-issued access tokens.
It now covers two related concerns:

1. **Inbound token validation** for your API (`AddAuthApiResourceAuthentication`).
2. **Outbound calls to the Auth API** for token/introspection workflows (`AddAuthApiClient`).

## Features

- Local JWT validation via OIDC discovery/JWKS.
- Optional introspection mode for APIs that need active-token checks.
- Scope policy helper (`RequireAnyScope`).
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

`RequireAnyScope(...)` uses OR semantics across provided scopes.
`Authority` is optional in this package and defaults to `https://localhost:5100`. Override it only if your Auth API host differs.

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
