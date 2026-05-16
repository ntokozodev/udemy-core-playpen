namespace AuthPlaypen.Client;

public interface IAuthApiClient
{
    Task<AuthApiTokenResponse> RequestClientCredentialsTokenAsync(IEnumerable<string> scopes, CancellationToken cancellationToken = default);
    Task<AuthApiIntrospectionResponse> IntrospectTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<string, IReadOnlyCollection<string>>> GetPermissionScopeMapAsync(CancellationToken cancellationToken = default);
}
