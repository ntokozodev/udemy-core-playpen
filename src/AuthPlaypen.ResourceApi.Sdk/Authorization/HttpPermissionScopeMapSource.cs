using System.Text.Json;

namespace AuthPlaypen.ResourceApi;

public sealed class HttpPermissionScopeMapSource(
    HttpClient httpClient,
    PermissionAliasAuthorizationOptions options) : IPermissionScopeMapSource
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly PermissionAliasAuthorizationOptions _options = options;

    public async Task<IReadOnlyDictionary<string, IReadOnlyCollection<string>>> GetPermissionScopeMapAsync(
        CancellationToken cancellationToken = default)
    {
        using var timeoutCts = new CancellationTokenSource(_options.FetchTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        using var response = await _httpClient.GetAsync(_options.MetadataEndpoint, linkedCts.Token);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(linkedCts.Token);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: linkedCts.Token);

        var root = document.RootElement;
        var sourceElement = root.ValueKind == JsonValueKind.Object && root.TryGetProperty("permissions", out var permissionsProperty)
            ? permissionsProperty
            : root;

        if (sourceElement.ValueKind != JsonValueKind.Object)
        {
            return new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.OrdinalIgnoreCase);
        }

        var output = new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in sourceElement.EnumerateObject())
        {
            var scopes = ParseScopes(property.Value);
            if (scopes.Count > 0)
            {
                output[property.Name] = scopes;
            }
        }

        return output;
    }

    private static IReadOnlyCollection<string> ParseScopes(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.String)
        {
            var single = value.GetString();
            return string.IsNullOrWhiteSpace(single)
                ? []
                : [single];
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var list = value
            .EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString())
            .Where(static item => !string.IsNullOrWhiteSpace(item))
            .Select(static item => item!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return list;
    }
}
