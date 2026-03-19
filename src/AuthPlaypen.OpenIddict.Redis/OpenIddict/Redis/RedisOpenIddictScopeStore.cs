using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using OpenIddict.Abstractions;
using StackExchange.Redis;

namespace AuthPlaypen.OpenIddict.Redis;

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

    public ValueTask<long> CountAsync(CancellationToken cancellationToken) => ValueTask.FromResult(0L);

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

    public ValueTask<TResult?> GetAsync<TState, TResult>(Func<IQueryable<RedisOpenIddictScope>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public ValueTask<string?> GetDescriptionAsync(RedisOpenIddictScope scope, CancellationToken cancellationToken) => new(scope.Description);

    public ValueTask<string?> GetDisplayNameAsync(RedisOpenIddictScope scope, CancellationToken cancellationToken) => new(scope.DisplayName);

    public ValueTask<ImmutableDictionary<CultureInfo, string>> GetDisplayNamesAsync(RedisOpenIddictScope scope, CancellationToken cancellationToken)
        => new(scope.DisplayNames.ToImmutableDictionary(entry => CultureInfo.GetCultureInfo(entry.Key), entry => entry.Value));

    public ValueTask<string?> GetIdAsync(RedisOpenIddictScope scope, CancellationToken cancellationToken) => new(scope.Id);

    public ValueTask<string?> GetNameAsync(RedisOpenIddictScope scope, CancellationToken cancellationToken) => new(scope.Name);

    public ValueTask<ImmutableDictionary<string, JsonElement>> GetPropertiesAsync(RedisOpenIddictScope scope, CancellationToken cancellationToken)
        => new(scope.Properties.ToImmutableDictionary());

    public ValueTask<ImmutableArray<string>> GetResourcesAsync(RedisOpenIddictScope scope, CancellationToken cancellationToken)
        => new(scope.Resources.ToImmutableArray());

    public ValueTask<RedisOpenIddictScope> InstantiateAsync(CancellationToken cancellationToken)
        => new(new RedisOpenIddictScope());

    public IAsyncEnumerable<RedisOpenIddictScope> ListAsync(int? count, int? offset, CancellationToken cancellationToken)
        => AsyncEnumerable.Empty<RedisOpenIddictScope>();

    public IAsyncEnumerable<TResult> ListAsync<TState, TResult>(Func<IQueryable<RedisOpenIddictScope>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken)
        => AsyncEnumerable.Empty<TResult>();

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
}
