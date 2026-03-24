# Resource API Integration

## Validation model

Resource APIs should validate bearer tokens; they should not store client secrets for calling applications.

Recommended default:
- Local JWT validation using discovery + JWKS.

Optional for high-security needs:
- Introspection (`/connect/introspect`) for near real-time revocation checks.

## Validation tradeoffs

| Mode | Calls Auth API each check | Revocation freshness | Latency |
|---|---|---|---|
| `Jwt` | No (after metadata/JWKS cache) | Eventual | Lowest |
| `Introspection` | Yes | Near real-time | Higher |

## Reusable package in this repo

`src/AuthPlaypen.ResourceApiAuth` provides:
- `AddAuthApiResourceAuthentication(...)`
- Mode switch: `AuthApiTokenValidationMode.Jwt` / `Introspection`
- Scope helper: `RequireAnyScope(...)`

Use it to standardize token validation and scope policies across downstream services.
