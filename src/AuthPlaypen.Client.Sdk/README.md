# AuthPlaypen.Client.Sdk

NuGet package ID: `AuthPlaypen.Client.Sdk`  
Code namespace: `AuthPlaypen.Client.Sdk`  
Target framework: `netstandard2.0`

`AuthPlaypen.Client.Sdk` is the official lightweight .NET client for calling AuthPlaypen endpoints from service-to-service workloads.

It gives you strongly-typed methods for:

- Getting client-credentials tokens (`/connect/token`)
- Introspecting bearer tokens (`/connect/introspect`) **when introspection is enabled in your AuthPlaypen deployment**
- Reading permission alias metadata (`/.well-known/authplaypen/permissions`)

---

## Installation

```bash
dotnet add package AuthPlaypen.Client.Sdk
```

---

## Quick start

### 1) Register the SDK in DI

```csharp
using AuthPlaypen.Client.Sdk;

builder.Services.AddAuthPlaypenClientSdk(options =>
{
    options.Authority = "https://localhost:5100";
    options.ClientId = "orders-api";
    options.ClientSecret = "orders-api-secret";
});
```

### 2) Inject `IAuthApiClient` and request a token

```csharp
using AuthPlaypen.Client.Sdk;

public sealed class DownstreamTokenService
{
    private readonly IAuthApiClient _authApiClient;

    public DownstreamTokenService(IAuthApiClient authApiClient)
    {
        _authApiClient = authApiClient;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var tokenResponse = await _authApiClient.RequestClientCredentialsTokenAsync(cancellationToken);

        return tokenResponse.AccessToken;
    }
}
```

---

## Full usage guide with tangible examples

## Configuration reference

`AuthApiClientOptions` supports:

- `Authority` (**required**): Base URL of AuthPlaypen (example: `https://auth.company.local`)
- `ClientId` (**required**): OAuth client id
- `ClientSecret` (**required**): OAuth client secret

Example appsettings:

```json
{
  "AuthPlaypen": {
    "Authority": "https://localhost:5100",
    "ClientId": "orders-api",
    "ClientSecret": "orders-api-secret"
  }
}
```

Binding example:

```csharp
using AuthPlaypen.Client.Sdk;

builder.Services.AddAuthPlaypenClientSdk(options =>
{
    var section = builder.Configuration.GetSection("AuthPlaypen");
    options.Authority = section["Authority"]!;
    options.ClientId = section["ClientId"]!;
    options.ClientSecret = section["ClientSecret"]!;
});
```


## Important: introspection availability

Token introspection is an **optional** capability and is commonly protected behind a feature toggle in AuthPlaypen deployments.

Before calling `IntrospectTokenAsync`, confirm that introspection is enabled in the target environment (local/dev/stage/prod).
If the feature is disabled, prefer local JWT validation/JWKS-based validation instead of introspection calls.

## Example A: call your own API using a freshly issued token

This pattern is common for background workers and service APIs.

```csharp
using System.Net.Http.Headers;
using AuthPlaypen.Client.Sdk;

public sealed class OrdersPublisher
{
    private readonly IAuthApiClient _authApiClient;
    private readonly HttpClient _httpClient;

    public OrdersPublisher(IAuthApiClient authApiClient, HttpClient httpClient)
    {
        _authApiClient = authApiClient;
        _httpClient = httpClient;
    }

    public async Task PublishAsync(object payload, CancellationToken cancellationToken)
    {
        var token = await _authApiClient.RequestClientCredentialsTokenAsync(cancellationToken);

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token.AccessToken);

        using var response = await _httpClient.PostAsJsonAsync("/v1/orders", payload, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
```

## Example B: token introspection before privileged action

Use introspection when you need server-authoritative token state, and only after confirming the introspection feature is enabled for that environment.

```csharp
using AuthPlaypen.Client.Sdk;

public sealed class TokenGate
{
    private readonly IAuthApiClient _authApiClient;

    public TokenGate(IAuthApiClient authApiClient)
    {
        _authApiClient = authApiClient;
    }

    public async Task<bool> CanExecuteDangerousOperationAsync(string bearerToken, CancellationToken cancellationToken)
    {
        var result = await _authApiClient.IntrospectTokenAsync(bearerToken, cancellationToken);

        if (!result.Active)
            return false;

        return result.Scope?.Split(' ').Contains("admin.execute") == true;
    }
}
```

## Example C: permission alias metadata for policy generation

```csharp
using AuthPlaypen.Client.Sdk;

public sealed class PermissionPolicyBootstrapper
{
    private readonly IAuthApiClient _authApiClient;

    public PermissionPolicyBootstrapper(IAuthApiClient authApiClient)
    {
        _authApiClient = authApiClient;
    }

    public async Task<IDictionary<string, string[]>> LoadMapAsync(CancellationToken cancellationToken)
    {
        var map = await _authApiClient.GetPermissionAliasMapAsync(cancellationToken);

        // Example result:
        // {
        //   "orders.read": ["api.orders.read"],
        //   "orders.write": ["api.orders.write", "api.orders.manage"]
        // }
        return map;
    }
}
```


## Local token validation (recommended for resource APIs)

For resource APIs, prefer local JWT validation with OpenID metadata + JWKS over introspection in most cases.

```csharp
var configuration = await authApiClient.GetOpenIdConfigurationAsync(cancellationToken);
var jwks = await authApiClient.GetJsonWebKeySetAsync(cancellationToken);
```

You can use these documents to configure your API token validation pipeline and key refresh strategy.

## Error handling

The SDK throws `AuthApiClientException` for non-success responses and payload-level failures.

```csharp
using AuthPlaypen.Client.Sdk;

try
{
    var token = await authApiClient.RequestClientCredentialsTokenAsync(cancellationToken);
}
catch (AuthApiClientException ex)
{
    logger.LogError(ex, "AuthPlaypen request failed: {Message}", ex.Message);
    throw;
}
```

## Testing with fake client

Because your app depends on `IAuthApiClient`, tests can inject a fake implementation.

```csharp
using AuthPlaypen.Client.Sdk;
using AuthPlaypen.Client.Sdk.Models;

public sealed class FakeAuthApiClient : IAuthApiClient
{
    public Task<AuthApiTokenResponse> RequestClientCredentialsTokenAsync(string? scope = null, CancellationToken cancellationToken = default)
        => Task.FromResult(new AuthApiTokenResponse
        {
            AccessToken = "fake-token",
            ExpiresIn = 3600,
            TokenType = "Bearer",
            Scope = scope
        });

    public Task<AuthApiIntrospectionResponse> IntrospectTokenAsync(string token, CancellationToken cancellationToken = default)
        => Task.FromResult(new AuthApiIntrospectionResponse { Active = true, Scope = "orders.read" });

    public Task<Dictionary<string, string[]>> GetPermissionAliasMapAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new Dictionary<string, string[]> { ["orders.read"] = new[] { "api.orders.read" } });
}
```

## Packaging note

This README is embedded in the NuGet package (`PackageReadmeFile`) so package-feed UIs show usage guidance automatically after publish.
