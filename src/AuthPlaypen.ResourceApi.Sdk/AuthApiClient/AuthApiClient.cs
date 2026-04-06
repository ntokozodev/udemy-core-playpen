using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace AuthPlaypen.ResourceApi;

public sealed class AuthApiClient(HttpClient httpClient, IOptions<AuthApiClientOptions> options) : IAuthApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient = httpClient;
    private readonly AuthApiClientOptions _options = options.Value;

    public async Task<AuthApiTokenResponse> RequestClientCredentialsTokenAsync(
        IEnumerable<string> scopes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scopes);

        var scopeValue = string.Join(' ', scopes.Where(scope => !string.IsNullOrWhiteSpace(scope)));

        var payload = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret
        };

        if (!string.IsNullOrWhiteSpace(scopeValue))
        {
            payload["scope"] = scopeValue;
        }

        using var response = await _httpClient.PostAsync(
            _options.TokenEndpoint,
            new FormUrlEncodedContent(payload),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new AuthApiClientException($"Token request failed ({(int)response.StatusCode}): {error}");
        }

        var result = await response.Content.ReadFromJsonAsync<AuthApiTokenResponse>(JsonOptions, cancellationToken);
        if (result is null || string.IsNullOrWhiteSpace(result.AccessToken))
        {
            throw new AuthApiClientException("Token response did not contain an access_token.");
        }

        return result;
    }

    public async Task<AuthApiIntrospectionResponse> IntrospectTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Token is required.", nameof(token));
        }

        var payload = new Dictionary<string, string>
        {
            ["token"] = token,
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret
        };

        using var response = await _httpClient.PostAsync(
            _options.IntrospectionEndpoint,
            new FormUrlEncodedContent(payload),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new AuthApiClientException($"Introspection request failed ({(int)response.StatusCode}): {error}");
        }

        var result = await response.Content.ReadFromJsonAsync<AuthApiIntrospectionResponse>(JsonOptions, cancellationToken);
        if (result is null)
        {
            throw new AuthApiClientException("Introspection response could not be parsed.");
        }

        return result;
    }
}
