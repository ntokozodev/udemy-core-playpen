# AuthPlaypen.ResourceApi.Sdk.Tests

Unit test inventory (organized by SDK responsibility):


Folder layout:

- `AuthApiClient/`
  - `AuthApiClientTests.cs`
- `ResourceAuthentication/`
  - `ResourceAuthenticationTests.cs`

- `AuthApiClientTests`
  - `AddAuthApiClient_ShouldThrow_WhenClientIdIsMissing`
  - `RequestClientCredentialsTokenAsync_ShouldReturnToken_WhenAuthApiRespondsSuccessfully`
  - `IntrospectTokenAsync_ShouldReturnIntrospectionPayload_WhenAuthApiRespondsSuccessfully`
- `ResourceAuthenticationTests`
  - `AddAuthApiResourceAuthentication_ShouldThrow_WhenAudienceIsMissing`
  - `AddAuthApiResourceAuthentication_ShouldThrow_WhenIntrospectionCredentialsAreMissing`
  - `AddAuthApiResourceAuthentication_ShouldConfigureJwtBearerByDefault`
  - `AddAuthApiResourceAuthentication_ShouldConfigureIntrospection_WhenRequested`
  - `RequireAnyScope_ShouldAuthorize_WhenUserHasMatchingScope`
  - `RequireAnyScope_ShouldReject_WhenUserLacksRequiredScopes`
  - `RequireAnyScope_ShouldThrow_WhenNoScopesProvided`

Total: **10** unit tests (`[Fact]`).
