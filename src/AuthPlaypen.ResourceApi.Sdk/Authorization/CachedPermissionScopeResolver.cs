namespace AuthPlaypen.ResourceApi;

public sealed class CachedPermissionScopeResolver(
    IPermissionScopeMapSource source,
    PermissionAliasAuthorizationOptions options) : IPermissionScopeResolver
{
    private readonly IPermissionScopeMapSource _source = source;
    private readonly PermissionAliasAuthorizationOptions _options = options;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private readonly StringComparer _comparer = StringComparer.OrdinalIgnoreCase;

    private DateTimeOffset _refreshAfterUtc = DateTimeOffset.MinValue;
    private IReadOnlyDictionary<string, IReadOnlyCollection<string>> _cachedMap =
        new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.OrdinalIgnoreCase);

    public async Task<IReadOnlyCollection<string>> ResolveScopesAsync(
        string permissionAlias,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(permissionAlias))
        {
            return [];
        }

        await EnsureFreshCacheAsync(cancellationToken);

        if (_cachedMap.TryGetValue(permissionAlias, out var cachedScopes) && cachedScopes.Count > 0)
        {
            return cachedScopes;
        }

        if (_options.HardcodedFallbackMappings.TryGetValue(permissionAlias, out var fallbackScopes))
        {
            return fallbackScopes
                .Where(static scope => !string.IsNullOrWhiteSpace(scope))
                .Distinct(_comparer)
                .ToArray();
        }

        return [];
    }

    private async Task EnsureFreshCacheAsync(CancellationToken cancellationToken)
    {
        if (DateTimeOffset.UtcNow < _refreshAfterUtc)
        {
            return;
        }

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            if (DateTimeOffset.UtcNow < _refreshAfterUtc)
            {
                return;
            }

            var map = await _source.GetPermissionScopeMapAsync(cancellationToken);
            _cachedMap = Normalize(map);
            _refreshAfterUtc = DateTimeOffset.UtcNow.Add(_options.CacheDuration);
        }
        catch
        {
            _refreshAfterUtc = DateTimeOffset.UtcNow.Add(_options.FailureRetryDelay);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private IReadOnlyDictionary<string, IReadOnlyCollection<string>> Normalize(
        IReadOnlyDictionary<string, IReadOnlyCollection<string>> map)
    {
        var normalized = new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (permission, scopes) in map)
        {
            if (string.IsNullOrWhiteSpace(permission))
            {
                continue;
            }

            var cleaned = scopes
                .Where(static scope => !string.IsNullOrWhiteSpace(scope))
                .Distinct(_comparer)
                .ToArray();

            if (cleaned.Length > 0)
            {
                normalized[permission] = cleaned;
            }
        }

        return normalized;
    }
}
