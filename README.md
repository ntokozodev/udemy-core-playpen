# dotnet-oidc-playpen

`AuthPlaypen.Api` is an ASP.NET Core 8 auth/admin API and supporting solution for managing applications, scopes, and OpenID Connect flows.

For usage, integration, and deeper reference docs, use the docs index: [./docs/README.md](./docs/README.md).

## Table of Contents

- [What this service is](#what-this-service-is)
- [Solution layout](#solution-layout)
- [Local development quick start](#local-development-quick-start)
  - [1) Generate a local OpenIddict signing certificate (server-like setup)](#1-generate-a-local-openiddict-signing-certificate-server-like-setup)
  - [2) Start dependencies + API](#2-start-dependencies--api)
  - [3) Apply EF migrations (if needed)](#3-apply-ef-migrations-if-needed)
  - [4) Run tests](#4-run-tests)
- [Team conventions for Auth API development](#team-conventions-for-auth-api-development)
  - [Layer ownership (strict)](#layer-ownership-strict)
- [Where to find everything else](#where-to-find-everything-else)

## What this service is

`AuthPlaypen.Api` is an ASP.NET Core 8 auth/admin API with:
- Admin CRUD for Applications and Scopes (PostgreSQL).
- OpenIddict token issuance and OIDC endpoints (Redis-backed stores).
- A bundled AdminApp frontend used to manage auth entities.

## Solution layout

- `src/AuthPlaypen.Api` - HTTP surface, composition root, OpenIddict server wiring, AdminApp hosting.
- `src/AuthPlaypen.Application` - DTOs and use-case orchestration.
- `src/AuthPlaypen.Data` - EF Core `DbContext`, mappings, migrations, design-time factory.
- `src/AuthPlaypen.Domain` - core entities and enums.
- `src/AuthPlaypen.OpenIddict.Redis` - Redis OpenIddict stores/models.
- `src/AuthPlaypen.ResourceApiAuth` - reusable auth package for downstream resource APIs.
- `tests/AuthPlaypen.Api.Tests` - API test suite.

## Local development quick start

### 1) Generate a local OpenIddict signing certificate (server-like setup)

Create a `.pfx` signing certificate and wire it via:

- `OpenIddictSigningOptions:SigningCertificatePath`
- `OpenIddictSigningOptions:SigningCertificatePassword`

Example using OpenSSL:

```bash
mkdir -p .certs
openssl req -x509 -newkey rsa:2048 -sha256 -days 365 -nodes \
  -keyout .certs/authplaypen-signing.key \
  -out .certs/authplaypen-signing.crt \
  -subj "/CN=authplaypen.local"
openssl pkcs12 -export \
  -out .certs/authplaypen-signing.pfx \
  -inkey .certs/authplaypen-signing.key \
  -in .certs/authplaypen-signing.crt \
  -passout pass:"<your-dev-cert-password>"
```

Store path/password in user-secrets (instead of committing them in `appsettings*.json`):

Use any local development password you prefer (for example, `changeit-dev-only`).
That string is just a sample placeholder, not a required built-in password.

```bash
dotnet user-secrets init --project src/AuthPlaypen.Api
dotnet user-secrets set "OpenIddictSigningOptions:SigningCertificatePath" "$(pwd)/.certs/authplaypen-signing.pfx" --project src/AuthPlaypen.Api
dotnet user-secrets set "OpenIddictSigningOptions:SigningCertificatePassword" "<your-dev-cert-password>" --project src/AuthPlaypen.Api
```

Optional (TLS for browser/OIDC callbacks when running on host):

```bash
dotnet dev-certs https --trust
ASPNETCORE_URLS="https://localhost:5100;http://localhost:8080" dotnet run --project src/AuthPlaypen.Api
```

Notes:
- If `SigningCertificatePath` is empty, the API only falls back to `AddDevelopmentSigningCertificate()` in the `Development` environment.
- In non-Development environments, configure `SigningCertificatePath` and `SigningCertificatePassword` explicitly (recommended for server-like local setups too).
- `docker compose up --build` still maps HTTP on `http://localhost:8080` by default.
- Keep `.certs/` local-only (do not commit private keys/certificates).

### 2) Start dependencies + API

```bash
docker compose up --build
```

- API: `http://localhost:8080`
- Swagger: `http://localhost:8080/swagger`

### 3) Apply EF migrations (if needed)

```bash
dotnet ef database update \
  --project src/AuthPlaypen.Data \
  --startup-project src/AuthPlaypen.Api
```

### 4) Run tests

```bash
dotnet test
```

## Team conventions for Auth API development

### Layer ownership (strict)

- API project owns controllers/startup/DI/composition.
- Data project owns all persistence concerns and migrations.
- Application project owns use-cases/contracts, not HTTP/persistence plumbing.
- Domain project stays infra-free.

If a change is persistence-specific, it belongs in `AuthPlaypen.Data`.

## Where to find everything else

- Architecture + boundaries: [docs/architecture.md](./docs/architecture.md)
- Domain rules: [docs/domain-rules.md](./docs/domain-rules.md)
- Runtime/AdminApp configuration: [docs/runtime-configuration.md](./docs/runtime-configuration.md)
- OpenIddict endpoints + token flows: [docs/openiddict-and-flows.md](./docs/openiddict-and-flows.md)
- API payload contracts: [docs/api-contracts.md](./docs/api-contracts.md)
- Resource API integration package usage: [docs/resource-api-integration.md](./docs/resource-api-integration.md)
