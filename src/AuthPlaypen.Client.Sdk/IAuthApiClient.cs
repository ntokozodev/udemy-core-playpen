namespace AuthPlaypen.Client;

public interface IAuthApiClient
{
    Task<AuthApiTokenResponse> RequestClientCredentialsTokenAsync(CancellationToken cancellationToken = default);
    Task<AuthApiTokenResponse> RequestClientCredentialsTokenAsync(IEnumerable<string> scopes, CancellationToken cancellationToken = default);
    Task<AuthApiIntrospectionResponse> IntrospectTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<string, IReadOnlyCollection<string>>> GetPermissionScopeMapAsync(CancellationToken cancellationToken = default);
    Task<OpenIdConfigurationDocument> GetOpenIdConfigurationAsync(CancellationToken cancellationToken = default);
    Task<JsonWebKeySetDocument> GetJsonWebKeySetAsync(CancellationToken cancellationToken = default);
}
