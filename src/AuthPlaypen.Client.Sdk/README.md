# AuthPlaypen.Client.Sdk

NuGet package ID: `AuthPlaypen.Client.Sdk`  
Code namespace: `AuthPlaypen.Client.Sdk`  
Target framework: `netstandard2.0`

`AuthPlaypen.Client.Sdk` is the official lightweight .NET client for calling AuthPlaypen endpoints from service-to-service workloads.

---

## What this SDK includes

- OAuth client credentials token acquisition (`/connect/token`)
- OAuth token introspection (`/connect/introspect`) when enabled by deployment
- Permission alias metadata retrieval (`/.well-known/authplaypen/permissions`)
- OpenID discovery retrieval (`/.well-known/openid-configuration`)
- JWKS document retrieval using `jwks_uri` from discovery

---

## Project layout (professional conventions)

### Why some types are in `Models/` and some are not

- **`Models/`** contains DTOs representing remote wire payloads.
  - Examples: `AuthApiTokenResponse`, `AuthApiIntrospectionResponse`, `OpenIdConfigurationDocument`, `JsonWebKeySetDocument`, `JsonWebKeyDocument`.
- **Root folder** contains SDK infrastructure and API surface.
  - Examples: `AuthApiClient`, `IAuthApiClient`, DI registration, options, exception types.

This separation keeps payload contracts isolated from transport/client orchestration code and makes the package easier to navigate and maintain.

---

## Installation

```bash
dotnet add package AuthPlaypen.Client.Sdk
```

---

## Quick start

```csharp
using AuthPlaypen.Client.Sdk;

builder.Services.AddAuthPlaypenClientSdk(options =>
{
    options.Authority = "https://localhost:5100";
    options.ClientId = "orders-api";
    options.ClientSecret = "orders-api-secret";
});
```

---

## JWKS support: implementation status and scope

### Current status

JWKS support is **implemented as SDK retrieval utilities**, not as a full JWT validation framework.

Implemented:

1. `GetOpenIdConfigurationAsync()` fetches discovery metadata.
2. `GetJsonWebKeySetAsync()` reads `jwks_uri` from discovery and fetches the JWKS document.
3. Typed DTO mapping for discovery/JWKS payloads.

Not implemented (by design):

- Signature validation pipeline
- JWT lifetime/audience/issuer validation
- Automatic key rotation cache/refresh policy
- ASP.NET authorization policy integration

You use this SDK to fetch metadata/documents, then plug those into your own validation stack (e.g., Microsoft identity/token validation middleware).

---

## Expected use cases

### Use case 1: machine-to-machine token acquisition

Background workers or service APIs obtaining access tokens for downstream APIs.

### Use case 2: server-authoritative token activity check

Operational paths that require introspection-backed active/inactive token checks.

### Use case 3: local resource API validation bootstrap

Resource APIs that need discovery and JWKS material as input into their JWT validation configuration.

### Use case 4: permission-to-scope policy bootstrapping

Applications that consume alias metadata for policy or authorization setup.

---

## Test coverage snapshot (unit tests)

The SDK test project currently covers:

- token request payload construction and response parsing
- scope omission behavior when scopes are not passed
- introspection error shaping
- permission map parsing and deduplication
- JWKS retrieval via discovery `jwks_uri`
- error case when `jwks_uri` is missing

---

## Important: introspection availability

Introspection is an **optional** deployment capability.

Before calling `IntrospectTokenAsync`, verify introspection is enabled in the target environment (dev/stage/prod). If disabled, prefer local JWT validation based on discovery/JWKS.
