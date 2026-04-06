using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using OpenIddict.Abstractions;
using StackExchange.Redis;
using AuthPlaypen.OpenIddict.Redis.Models;

namespace AuthPlaypen.OpenIddict.Redis.Stores;

public sealed class RedisOpenIddictScopeStore(IConnectionMultiplexer multiplexer) : IOpenIddictScopeStore<RedisOpenIddictScope>
{
    private readonly IDatabase _db = multiplexer.GetDatabase();
    private readonly RedisOpenIddictSerializer _serializer = new();

    public async ValueTask CreateAsync(RedisOpenIddictScope scope, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _db.StringSetAsync(RedisOpenIddictKeys.ScopeById(scope.Id), _serializer.Serialize(scope));
        if (!string.IsNullOrWhiteSpace(scope.Name))
        {
            await _db.StringSetAsync(RedisOpenIddictKeys.ScopeByName(scope.Name), scope.Id);
        }
    }

    public async ValueTask DeleteAsync(RedisOpenIddictScope scope, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _db.KeyDeleteAsync(RedisOpenIddictKeys.ScopeById(scope.Id));
        if (!string.IsNullOrWhiteSpace(scope.Name))
        {
            await _db.KeyDeleteAsync(RedisOpenIddictKeys.ScopeByName(scope.Name));
        }
    }

    public async ValueTask<long> CountAsync(CancellationToken cancellationToken)
    {
        long count = 0;
        await foreach (var _ in ListAllScopesAsync(cancellationToken))
        {
            count++;
        }

        return count;
    }

    public ValueTask<long> CountAsync<TResult>(Func<IQueryable<RedisOpenIddictScope>, IQueryable<TResult>> query, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public async ValueTask<RedisOpenIddictScope?> FindByIdAsync(string identifier, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var payload = await _db.StringGetAsync(RedisOpenIddictKeys.ScopeById(identifier));
        return _serializer.Deserialize<RedisOpenIddictScope>(payload);
    }

    public async ValueTask<RedisOpenIddictScope?> FindByNameAsync(string name, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var id = await _db.StringGetAsync(RedisOpenIddictKeys.ScopeByName(name));
        if (id.IsNullOrEmpty)
        {
            return null;
        }

        var payload = await _db.StringGetAsync(RedisOpenIddictKeys.ScopeById(id!));
        return _serializer.Deserialize<RedisOpenIddictScope>(payload);
    }

    public async IAsyncEnumerable<RedisOpenIddictScope> FindByNamesAsync(
        ImmutableArray<string> names,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var name in names.Where(static name => !string.IsNullOrWhiteSpace(name)).Distinct(StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var scope = await FindByNameAsync(name, cancellationToken);
            if (scope is not null)
            {
                yield return scope;
            }
        }
    }

    public async IAsyncEnumerable<RedisOpenIddictScope> FindByResourceAsync(
        string resource,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var scope in ListAllScopesAsync(cancellationToken))
        {
            if (scope.Resources.Contains(resource, StringComparer.Ordinal))
            {
                yield return scope;
            }
        }
    }

    public ValueTask<TResult?> GetAsync<TState, TResult>(Func<IQueryable<RedisOpenIddictScope>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public ValueTask<string?> GetDescriptionAsync(RedisOpenIddictScope scope, CancellationToken cancellationToken) => new(scope.Description);

    public ValueTask<string?> GetDisplayNameAsync(RedisOpenIddictScope scope, CancellationToken cancellationToken) => new(scope.DisplayName);

    public ValueTask<ImmutableDictionary<CultureInfo, string>> GetDisplayNamesAsync(RedisOpenIddictScope scope, CancellationToken cancellationToken)
        => new(scope.DisplayNames.ToImmutableDictionary(entry => CultureInfo.GetCultureInfo(entry.Key), entry => entry.Value));

    public ValueTask<ImmutableDictionary<CultureInfo, string>> GetDescriptionsAsync(RedisOpenIddictScope scope, CancellationToken cancellationToken)
        => new(GetLocalizedValues(scope.Description));

    public ValueTask<string?> GetIdAsync(RedisOpenIddictScope scope, CancellationToken cancellationToken) => new(scope.Id);

    public ValueTask<string?> GetNameAsync(RedisOpenIddictScope scope, CancellationToken cancellationToken) => new(scope.Name);

    public ValueTask<ImmutableDictionary<string, JsonElement>> GetPropertiesAsync(RedisOpenIddictScope scope, CancellationToken cancellationToken)
        => new(scope.Properties.ToImmutableDictionary());

    public ValueTask<ImmutableArray<string>> GetResourcesAsync(RedisOpenIddictScope scope, CancellationToken cancellationToken)
        => new(scope.Resources.ToImmutableArray());

    public ValueTask<RedisOpenIddictScope> InstantiateAsync(CancellationToken cancellationToken)
        => new(new RedisOpenIddictScope());

    public async IAsyncEnumerable<RedisOpenIddictScope> ListAsync(
        int? count,
        int? offset,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var skipped = 0;
        var yielded = 0;
        var skipCount = Math.Max(offset ?? 0, 0);
        var takeCount = count.HasValue && count.Value >= 0 ? count.Value : int.MaxValue;

        await foreach (var scope in ListAllScopesAsync(cancellationToken))
        {
            if (skipped < skipCount)
            {
                skipped++;
                continue;
            }

            if (yielded >= takeCount)
            {
                yield break;
            }

            yield return scope;
            yielded++;
        }
    }

    public async IAsyncEnumerable<TResult> ListAsync<TState, TResult>(
        Func<IQueryable<RedisOpenIddictScope>, TState, IQueryable<TResult>> query,
        TState state,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var scopes = new List<RedisOpenIddictScope>();
        await foreach (var scope in ListAllScopesAsync(cancellationToken))
        {
            scopes.Add(scope);
        }

        var results = query(scopes.AsQueryable(), state);
        foreach (var result in results)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return result;
        }
    }

    public ValueTask SetDescriptionAsync(RedisOpenIddictScope scope, string? description, CancellationToken cancellationToken)
    {
        scope.Description = description;
        return ValueTask.CompletedTask;
    }

    public ValueTask SetDisplayNameAsync(RedisOpenIddictScope scope, string? name, CancellationToken cancellationToken)
    {
        scope.DisplayName = name;
        return ValueTask.CompletedTask;
    }

    public ValueTask SetDisplayNamesAsync(RedisOpenIddictScope scope, ImmutableDictionary<CultureInfo, string> names, CancellationToken cancellationToken)
    {
        scope.DisplayNames = names.ToDictionary(entry => entry.Key.Name, entry => entry.Value);
        return ValueTask.CompletedTask;
    }

    public ValueTask SetDescriptionsAsync(RedisOpenIddictScope scope, ImmutableDictionary<CultureInfo, string> descriptions, CancellationToken cancellationToken)
    {
        var values = descriptions
            .Where(static entry => !string.IsNullOrWhiteSpace(entry.Value))
            .ToDictionary(entry => entry.Key.Name, entry => entry.Value);

        scope.Description = values.Count == 0 ? null : JsonSerializer.Serialize(values);
        return ValueTask.CompletedTask;
    }

    public ValueTask SetNameAsync(RedisOpenIddictScope scope, string? name, CancellationToken cancellationToken)
    {
        scope.Name = name;
        return ValueTask.CompletedTask;
    }

    public ValueTask SetPropertiesAsync(RedisOpenIddictScope scope, ImmutableDictionary<string, JsonElement> properties, CancellationToken cancellationToken)
    {
        scope.Properties = properties.ToDictionary();
        return ValueTask.CompletedTask;
    }

    public ValueTask SetResourcesAsync(RedisOpenIddictScope scope, ImmutableArray<string> resources, CancellationToken cancellationToken)
    {
        scope.Resources = resources.Where(static resource => !string.IsNullOrWhiteSpace(resource)).ToHashSet(StringComparer.Ordinal);
        return ValueTask.CompletedTask;
    }

    public ValueTask UpdateAsync(RedisOpenIddictScope scope, CancellationToken cancellationToken)
        => CreateAsync(scope, cancellationToken);

    private async IAsyncEnumerable<RedisOpenIddictScope> ListAllScopesAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var endpoint in multiplexer.GetEndPoints())
        {
            var server = multiplexer.GetServer(endpoint);
            foreach (var key in server.Keys(pattern: "oidc:scopes:id:*").Select(static x => (string)x))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var payload = await _db.StringGetAsync(key);
                var scope = _serializer.Deserialize<RedisOpenIddictScope>(payload);
                if (scope is not null)
                {
                    yield return scope;
                }
            }
        }
    }

    private static ImmutableDictionary<CultureInfo, string> GetLocalizedValues(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return ImmutableDictionary<CultureInfo, string>.Empty;
        }

        try
        {
            var dictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(value);
            return dictionary is null
                ? ImmutableDictionary<CultureInfo, string>.Empty
                : dictionary.ToImmutableDictionary(
                    entry => CultureInfo.GetCultureInfo(entry.Key),
                    entry => entry.Value);
        }
        catch (JsonException)
        {
            return ImmutableDictionary<CultureInfo, string>.Empty;
        }
    }
}
