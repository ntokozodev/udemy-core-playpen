# AuthPlaypen.ResourceApi.Sdk.Tests

Unit test inventory after SDK project rename:

- `AuthApiClientTests`
  - `AddAuthApiClient_ShouldThrow_WhenClientIdIsMissing`
  - `RequestClientCredentialsTokenAsync_ShouldReturnToken_WhenAuthApiRespondsSuccessfully`
  - `IntrospectTokenAsync_ShouldReturnIntrospectionPayload_WhenAuthApiRespondsSuccessfully`
- `ServiceCollectionExtensionsTests`
  - `AddAuthApiResourceAuthentication_ShouldThrow_WhenAudienceIsMissing`
  - `AddAuthApiResourceAuthentication_ShouldThrow_WhenIntrospectionCredentialsAreMissing`
  - `AddAuthApiResourceAuthentication_ShouldConfigureJwtBearerByDefault`
  - `AddAuthApiResourceAuthentication_ShouldConfigureIntrospection_WhenRequested`
  - `RequireAnyScope_ShouldAuthorize_WhenUserHasMatchingScope`
  - `RequireAnyScope_ShouldReject_WhenUserLacksRequiredScopes`
  - `RequireAnyScope_ShouldThrow_WhenNoScopesProvided`

Total: **10** unit tests (`[Fact]`).
