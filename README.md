# dotnet-auth-playpen

`AuthPlaypen.Api` is an ASP.NET Core 8 auth/admin API and supporting solution for managing applications, scopes, and OpenID Connect flows.

For usage, integration, and deeper reference docs, use the docs index: [./docs/README.md](./docs/README.md).

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

### 1) Generate a self-signed certificate for local HTTPS (optional but recommended)

If you want to run the API locally over HTTPS (for OIDC callback/issuer scenarios), create and trust the ASP.NET Core development certificate:

```bash
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

Then run the API with an HTTPS URL:

```bash
ASPNETCORE_URLS="https://localhost:5100;http://localhost:8080" dotnet run --project src/AuthPlaypen.Api
```

Notes:
- `docker compose up --build` maps HTTP on `http://localhost:8080` by default.
- The dev cert above is for host-local `dotnet run` workflows.

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
