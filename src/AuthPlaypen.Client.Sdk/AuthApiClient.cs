using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace AuthPlaypen.Client;

public sealed class AuthApiClient : IAuthApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _httpClient;
    private readonly AuthApiClientOptions _options;

    public AuthApiClient(HttpClient httpClient, IOptions<AuthApiClientOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<AuthApiTokenResponse> RequestClientCredentialsTokenAsync(IEnumerable<string> scopes, CancellationToken cancellationToken = default)
    {
        if (scopes is null) throw new ArgumentNullException(nameof(scopes));
        var scopeValue = string.Join(" ", scopes.Where(s => !string.IsNullOrWhiteSpace(s)));
        var payload = new Dictionary<string, string> { ["grant_type"] = "client_credentials", ["client_id"] = _options.ClientId, ["client_secret"] = _options.ClientSecret };
        if (!string.IsNullOrWhiteSpace(scopeValue)) payload["scope"] = scopeValue;
        using var response = await _httpClient.PostAsync(_options.TokenEndpoint, new FormUrlEncodedContent(payload), cancellationToken);
        if (!response.IsSuccessStatusCode) throw new AuthApiClientException($"Token request failed ({(int)response.StatusCode}): {await response.Content.ReadAsStringAsync()}");
        var result = await response.Content.ReadFromJsonAsync<AuthApiTokenResponse>(JsonOptions, cancellationToken);
        if (result is null || string.IsNullOrWhiteSpace(result.AccessToken)) throw new AuthApiClientException("Token response did not contain an access_token.");
        return result;
    }

    public async Task<AuthApiIntrospectionResponse> IntrospectTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token)) throw new ArgumentException("Token is required.", nameof(token));
        var payload = new Dictionary<string, string> { ["token"] = token, ["client_id"] = _options.ClientId, ["client_secret"] = _options.ClientSecret };
        using var response = await _httpClient.PostAsync(_options.IntrospectionEndpoint, new FormUrlEncodedContent(payload), cancellationToken);
        if (!response.IsSuccessStatusCode) throw new AuthApiClientException($"Introspection request failed ({(int)response.StatusCode}): {await response.Content.ReadAsStringAsync()}");
        var result = await response.Content.ReadFromJsonAsync<AuthApiIntrospectionResponse>(JsonOptions, cancellationToken);
        return result ?? throw new AuthApiClientException("Introspection response could not be parsed.");
    }

    public async Task<IReadOnlyDictionary<string, IReadOnlyCollection<string>>> GetPermissionScopeMapAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(_options.PermissionMetadataEndpoint, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = doc.RootElement;
        var mapNode = root.ValueKind == JsonValueKind.Object && root.TryGetProperty("permissions", out var permissionsNode) ? permissionsNode : root;
        var output = new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.OrdinalIgnoreCase);
        if (mapNode.ValueKind != JsonValueKind.Object) return output;
        foreach (var property in mapNode.EnumerateObject())
        {
            var scopes = property.Value.ValueKind == JsonValueKind.Array
                ? property.Value.EnumerateArray().Where(v => v.ValueKind == JsonValueKind.String).Select(v => v.GetString()).Where(v => !string.IsNullOrWhiteSpace(v)).Cast<string>().Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
                : property.Value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(property.Value.GetString()) ? new[] { property.Value.GetString()! } : Array.Empty<string>();
            if (scopes.Length > 0) output[property.Name] = scopes;
        }
        return output;
    }
}
