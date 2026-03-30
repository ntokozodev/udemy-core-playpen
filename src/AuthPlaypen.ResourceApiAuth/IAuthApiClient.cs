namespace AuthPlaypen.ResourceApiAuth;

public interface IAuthApiClient
{
    Task<AuthApiTokenResponse> RequestClientCredentialsTokenAsync(
        IEnumerable<string> scopes,
        CancellationToken cancellationToken = default);

    Task<AuthApiIntrospectionResponse> IntrospectTokenAsync(
        string token,
        CancellationToken cancellationToken = default);
}
