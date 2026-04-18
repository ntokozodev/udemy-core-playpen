# OpenIddict Endpoints and Token Flows

[← Back to docs index](./README.md)


## Backing stores

- Admin CRUD data: PostgreSQL.
- OpenIddict entities (applications/scopes/tokens): Redis-backed custom stores.

## Key endpoints

- `/connect/authorize`
- `/connect/token`
- `/connect/logout`
- `/connect/userinfo`
- `/connect/introspect`
- `/connect/revoke`
- `/.well-known/openid-configuration`
- `/.well-known/jwks`
- `/.well-known/authplaypen/permissions` (AuthPlaypen extension; not OIDC standard)

## Supported grants

- Authorization Code + PKCE
- Client Credentials

## PKCE high-level flow

1. Frontend calls `/connect/authorize` with PKCE parameters.
2. API checks local auth cookie.
3. If needed, API challenges Azure AD/O365.
4. API returns authorization code.
5. Frontend exchanges code + verifier at `/connect/token`.
6. If `offline_access` was requested and granted, frontend can later use `refresh_token` at `/connect/token`.

## Client credentials high-level flow

1. Register client/scopes in AdminApp.
2. Call `/connect/token` with `client_id`/`client_secret`.
3. Use returned bearer token against resource APIs.
