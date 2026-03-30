# Architecture and Layer Ownership

## Projects

- `AuthPlaypen.Api` - HTTP surface + startup/composition root.
- `AuthPlaypen.Application` - DTOs and use-case orchestration.
- `AuthPlaypen.Data` - EF Core persistence, migrations, design-time factory.
- `AuthPlaypen.Domain` - domain model/enums.
- `AuthPlaypen.OpenIddict.Redis` - Redis-backed OpenIddict stores/models.
- `AuthPlaypen.ResourceApiAuth` - reusable auth helpers for resource APIs.

## Strict ownership rules

- API must not own persistence artifacts.
- Application must not contain HTTP or EF infrastructure details.
- Data owns persistence end-to-end.
- Domain remains infrastructure-independent.

In practice: if it's data/persistence-specific, put it in `AuthPlaypen.Data`.

## Current high-level architecture

```text
                                  AUTHAPI AUTHENTICATION SYSTEM
┌──────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                                                                                              │
│  ┌──────────────────┐                                                  ┌──────────────────────────────┐      │
│  │     AdminApp     │ ---------------- sync/publish -----------------> │         AuthApi               │      │
│  │                  │                                                  │                              │      │
│  │  - Applications  │                                                  │  - ApplicationService        │      │
│  │  - Scopes        │                                                  │  - ScopeService              │      │
│  │  - Admin UI      │                                                  │  - Orchestrator (OpenIddict) │      │
│  └────┬─────────────┘                                                  └────────────┬─────────────────┘      │
│       │                                                                          Updates                     │
│    register                                                                         │                        │
│       │                                                                             ▼                        │
│       │      ┌─────────────────┐                                        ┌───────────────────────────┐        │
│       │      │   PostgreSQL    │                                        │ Redis (Primary Store)     │        │
│       │      │                 │                                        │                           │        │
│       └─────>│ - Applications  │                                        │  - Applications           │        │
│              │ - Scopes        │                                        │  - Scopes                 │        │
│              │ - Metadata      │                                        │                           │        │
│              └─────────────────┘                                        └───────────────────────────┘        │
│                                                                                                              │
│  ------------------------------------ RUNTIME AUTHENTICATION FLOW ----------------------------------------- │
│                                                                                                              │
│  ┌─────────────┐                          ┌──────────────────┐                                               │
│  │   api-one   │ -- 1. Request token ---> │   AuthApi        │                                               │
│  │ (Client App)│                          │                  │                      ┌───────────────────┐    │
│  │             │ <-- 2. JWT token ------- │  OpenIddict      │------ Metadata ----->│  Redis Store      │    │
│  └─────┬───────┘                          │                  │                      └───────────────────┘    │
│        │                                  └──────────────────┘                                               │
│        │                                                                                                     │
│        └──────────> 3. Call api-two with Bearer JWT ---------------------> ┌──────────────────────────────┐ │
│                                                                            │  api-two (Resource Server)   │ │
│                                                                            │                              │ │
│                                                                            │  3.1 Preferred: JWKS-based   │ │
│                                                                            │      local JWT validation     │ │
│                                                                            │      (high performance)       │ │
│                                                                            │                              │ │
│                                                                            │  3.2 Optional fallback:      │ │
│                                                                            │      call AuthApi token       │ │
│                                                                            │      introspection endpoint   │ │
│                                                                            │      (near real-time checks)  │ │
│                                                                            └──────────────────────────────┘ │
│                                                                                                              │
└──────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
```

## Validation strategy for resource APIs

- **Preferred path:** local signature and claim validation using JWKS metadata from `AuthApi` for best runtime performance.
- **Optional path:** token introspection against `AuthApi` when near real-time revocation/state checks are required.
