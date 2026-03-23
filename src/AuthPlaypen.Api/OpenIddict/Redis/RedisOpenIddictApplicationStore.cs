using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using StackExchange.Redis;

namespace AuthPlaypen.Api.OpenIddict.Redis;

public sealed class RedisOpenIddictApplicationStore(IConnectionMultiplexer multiplexer) : IOpenIddictApplicationStore<RedisOpenIddictApplication>
{
    private readonly IDatabase _db = multiplexer.GetDatabase();
    private readonly RedisOpenIddictSerializer _serializer = new();

    public async ValueTask CreateAsync(RedisOpenIddictApplication application, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _db.StringSetAsync(RedisOpenIddictKeys.ApplicationById(application.Id), _serializer.Serialize(application));
        if (!string.IsNullOrWhiteSpace(application.ClientId))
        {
            await _db.StringSetAsync(RedisOpenIddictKeys.ApplicationByClientId(application.ClientId), application.Id);
        }
    }

    public async ValueTask DeleteAsync(RedisOpenIddictApplication application, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _db.KeyDeleteAsync(RedisOpenIddictKeys.ApplicationById(application.Id));
        if (!string.IsNullOrWhiteSpace(application.ClientId))
        {
            await _db.KeyDeleteAsync(RedisOpenIddictKeys.ApplicationByClientId(application.ClientId));
        }
    }

    public ValueTask<long> CountAsync(CancellationToken cancellationToken) => ValueTask.FromResult(0L);

    public ValueTask<long> CountAsync<TResult>(Func<IQueryable<RedisOpenIddictApplication>, IQueryable<TResult>> query, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public async ValueTask<RedisOpenIddictApplication?> FindByClientIdAsync(string identifier, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var id = await _db.StringGetAsync(RedisOpenIddictKeys.ApplicationByClientId(identifier));
        if (id.IsNullOrEmpty)
        {
            return null;
        }

        var payload = await _db.StringGetAsync(RedisOpenIddictKeys.ApplicationById(id!));
        return _serializer.Deserialize<RedisOpenIddictApplication>(payload);
    }

    public async ValueTask<RedisOpenIddictApplication?> FindByIdAsync(string identifier, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var payload = await _db.StringGetAsync(RedisOpenIddictKeys.ApplicationById(identifier));
        return _serializer.Deserialize<RedisOpenIddictApplication>(payload);
    }

    public IAsyncEnumerable<RedisOpenIddictApplication> FindByPostLogoutRedirectUriAsync([StringSyntax("Uri")] string address, CancellationToken cancellationToken)
        => AsyncEnumerable.Empty<RedisOpenIddictApplication>();

    public IAsyncEnumerable<RedisOpenIddictApplication> FindByRedirectUriAsync([StringSyntax("Uri")] string address, CancellationToken cancellationToken)
        => AsyncEnumerable.Empty<RedisOpenIddictApplication>();

    public ValueTask<string?> GetApplicationTypeAsync(RedisOpenIddictApplication application, CancellationToken cancellationToken) => new(application.ApplicationType);

    public ValueTask<TResult?> GetAsync<TState, TResult>(Func<IQueryable<RedisOpenIddictApplication>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public ValueTask<string?> GetClientIdAsync(RedisOpenIddictApplication application, CancellationToken cancellationToken) => new(application.ClientId);

    public ValueTask<string?> GetClientSecretAsync(RedisOpenIddictApplication application, CancellationToken cancellationToken) => new(application.ClientSecret);

    public ValueTask<string?> GetClientTypeAsync(RedisOpenIddictApplication application, CancellationToken cancellationToken) => new(application.ClientType);

    public ValueTask<string?> GetConsentTypeAsync(RedisOpenIddictApplication application, CancellationToken cancellationToken) => new(application.ConsentType);

    public ValueTask<string?> GetDisplayNameAsync(RedisOpenIddictApplication application, CancellationToken cancellationToken) => new(application.DisplayName);

    public ValueTask<ImmutableDictionary<CultureInfo, string>> GetDisplayNamesAsync(RedisOpenIddictApplication application, CancellationToken cancellationToken)
        => new(ImmutableDictionary<CultureInfo, string>.Empty);

    public ValueTask<string?> GetIdAsync(RedisOpenIddictApplication application, CancellationToken cancellationToken) => new(application.Id);

    public ValueTask<JsonWebKeySet?> GetJsonWebKeySetAsync(RedisOpenIddictApplication application, CancellationToken cancellationToken)
        => new(string.IsNullOrEmpty(application.JsonWebKeySet) ? null : new JsonWebKeySet(application.JsonWebKeySet));

    public ValueTask<ImmutableArray<string>> GetPermissionsAsync(RedisOpenIddictApplication application, CancellationToken cancellationToken)
        => new(application.Permissions.ToImmutableArray());

    public ValueTask<ImmutableArray<string>> GetPostLogoutRedirectUrisAsync(RedisOpenIddictApplication application, CancellationToken cancellationToken)
        => new(application.PostLogoutRedirectUris.ToImmutableArray());

    public ValueTask<ImmutableDictionary<string, JsonElement>> GetPropertiesAsync(RedisOpenIddictApplication application, CancellationToken cancellationToken)
        => new(application.Properties.ToImmutableDictionary());

    public ValueTask<ImmutableArray<string>> GetRedirectUrisAsync(RedisOpenIddictApplication application, CancellationToken cancellationToken)
        => new(application.RedirectUris.ToImmutableArray());

    public ValueTask<ImmutableArray<string>> GetRequirementsAsync(RedisOpenIddictApplication application, CancellationToken cancellationToken)
        => new(application.Requirements.ToImmutableArray());

    public ValueTask<ImmutableDictionary<string, string>> GetSettingsAsync(RedisOpenIddictApplication application, CancellationToken cancellationToken)
        => new(application.Settings.ToImmutableDictionary());

    public ValueTask<RedisOpenIddictApplication> InstantiateAsync(CancellationToken cancellationToken)
        => new(new RedisOpenIddictApplication());

    public IAsyncEnumerable<RedisOpenIddictApplication> ListAsync(int? count, int? offset, CancellationToken cancellationToken)
        => AsyncEnumerable.Empty<RedisOpenIddictApplication>();

    public IAsyncEnumerable<TResult> ListAsync<TState, TResult>(Func<IQueryable<RedisOpenIddictApplication>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken)
        => AsyncEnumerable.Empty<TResult>();

    public ValueTask SetApplicationTypeAsync(RedisOpenIddictApplication application, string? type, CancellationToken cancellationToken)
    {
        application.ApplicationType = type;
        return ValueTask.CompletedTask;
    }

    public ValueTask SetClientIdAsync(RedisOpenIddictApplication application, string? identifier, CancellationToken cancellationToken)
    {
        application.ClientId = identifier;
        return ValueTask.CompletedTask;
    }

    public ValueTask SetClientSecretAsync(RedisOpenIddictApplication application, string? secret, CancellationToken cancellationToken)
    {
        application.ClientSecret = secret;
        return ValueTask.CompletedTask;
    }

    public ValueTask SetClientTypeAsync(RedisOpenIddictApplication application, string? type, CancellationToken cancellationToken)
    {
        application.ClientType = type;
        return ValueTask.CompletedTask;
    }

    public ValueTask SetConsentTypeAsync(RedisOpenIddictApplication application, string? type, CancellationToken cancellationToken)
    {
        application.ConsentType = type;
        return ValueTask.CompletedTask;
    }

    public ValueTask SetDisplayNameAsync(RedisOpenIddictApplication application, string? name, CancellationToken cancellationToken)
    {
        application.DisplayName = name;
        return ValueTask.CompletedTask;
    }

    public ValueTask SetDisplayNamesAsync(RedisOpenIddictApplication application, ImmutableDictionary<CultureInfo, string> names, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;

    public ValueTask SetJsonWebKeySetAsync(RedisOpenIddictApplication application, JsonWebKeySet? set, CancellationToken cancellationToken)
    {
        application.JsonWebKeySet = set?.ToString();
        return ValueTask.CompletedTask;
    }

    public ValueTask SetPermissionsAsync(RedisOpenIddictApplication application, ImmutableArray<string> permissions, CancellationToken cancellationToken)
    {
        application.Permissions = permissions.Where(static permission => !string.IsNullOrWhiteSpace(permission)).ToHashSet(StringComparer.Ordinal);
        return ValueTask.CompletedTask;
    }

    public ValueTask SetPostLogoutRedirectUrisAsync(RedisOpenIddictApplication application, ImmutableArray<string> addresses, CancellationToken cancellationToken)
    {
        application.PostLogoutRedirectUris = addresses.Where(static uri => !string.IsNullOrWhiteSpace(uri)).ToHashSet(StringComparer.Ordinal);
        return ValueTask.CompletedTask;
    }

    public ValueTask SetPropertiesAsync(RedisOpenIddictApplication application, ImmutableDictionary<string, JsonElement> properties, CancellationToken cancellationToken)
    {
        application.Properties = properties.ToDictionary();
        return ValueTask.CompletedTask;
    }

    public ValueTask SetRedirectUrisAsync(RedisOpenIddictApplication application, ImmutableArray<string> addresses, CancellationToken cancellationToken)
    {
        application.RedirectUris = addresses.Where(static uri => !string.IsNullOrWhiteSpace(uri)).ToHashSet(StringComparer.Ordinal);
        return ValueTask.CompletedTask;
    }

    public ValueTask SetRequirementsAsync(RedisOpenIddictApplication application, ImmutableArray<string> requirements, CancellationToken cancellationToken)
    {
        application.Requirements = requirements.Where(static requirement => !string.IsNullOrWhiteSpace(requirement)).ToHashSet(StringComparer.Ordinal);
        return ValueTask.CompletedTask;
    }

    public ValueTask SetSettingsAsync(RedisOpenIddictApplication application, ImmutableDictionary<string, string> settings, CancellationToken cancellationToken)
    {
        application.Settings = settings.ToDictionary();
        return ValueTask.CompletedTask;
    }

    public ValueTask UpdateAsync(RedisOpenIddictApplication application, CancellationToken cancellationToken)
        => CreateAsync(application, cancellationToken);
}
