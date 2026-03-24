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
