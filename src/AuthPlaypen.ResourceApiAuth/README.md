# AuthPlaypen.ResourceApiAuth

Shared authentication helpers for resource APIs that validate access tokens issued by AuthPlaypen Auth API.

## Features

- Local JWT validation via OIDC discovery/JWKS.
- Optional introspection mode for APIs that need active-token checks.
- Scope policy helper (`RequireAnyScope`).

## Example (registration + runtime enforcement)

```csharp
using AuthPlaypen.ResourceApiAuth;
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
