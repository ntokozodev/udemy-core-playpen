using System.Collections.Immutable;
using System.Text.Json;
using OpenIddict.Abstractions;
using StackExchange.Redis;
using AuthPlaypen.OpenIddict.Redis.Models;

namespace AuthPlaypen.OpenIddict.Redis.Stores;

public sealed class RedisOpenIddictTokenStore(IConnectionMultiplexer multiplexer) : IOpenIddictTokenStore<RedisOpenIddictToken>
{
    private readonly IDatabase _db = multiplexer.GetDatabase();
    private readonly RedisOpenIddictSerializer _serializer = new();

    public async ValueTask CreateAsync(RedisOpenIddictToken token, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await PersistAsync(token);
        await AddIndexesAsync(token);
    }

    public async ValueTask DeleteAsync(RedisOpenIddictToken token, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _db.KeyDeleteAsync(RedisOpenIddictKeys.TokenById(token.Id));

        if (!string.IsNullOrWhiteSpace(token.ReferenceId))
        {
            await _db.KeyDeleteAsync(RedisOpenIddictKeys.TokenByReferenceId(token.ReferenceId));
        }

        await RemoveIndexesAsync(token);
    }

    public ValueTask<long> CountAsync(CancellationToken cancellationToken)
        => ValueTask.FromResult(0L);

    public ValueTask<long> CountAsync<TResult>(Func<IQueryable<RedisOpenIddictToken>, IQueryable<TResult>> query, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public IAsyncEnumerable<RedisOpenIddictToken> FindAsync(string subject, string client, CancellationToken cancellationToken)
        => StreamBySetKeyAsync(RedisOpenIddictKeys.TokensBySubjectAndClient(subject, client), cancellationToken);

    public async IAsyncEnumerable<RedisOpenIddictToken> FindAsync(string subject, string client, string status, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var tokenIds = await IntersectSetMembersAsync(
            RedisOpenIddictKeys.TokensBySubjectAndClient(subject, client),
            RedisOpenIddictKeys.TokensByStatus(status));

        foreach (var token in await ResolveTokensAsync(tokenIds, cancellationToken))
        {
            yield return token;
        }
    }

    public async IAsyncEnumerable<RedisOpenIddictToken> FindAsync(string subject, string client, string status, string type, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var tokenIds = await IntersectSetMembersAsync(
            RedisOpenIddictKeys.TokensBySubjectAndClient(subject, client),
            RedisOpenIddictKeys.TokensByStatus(status),
            RedisOpenIddictKeys.TokensByType(type));

        foreach (var token in await ResolveTokensAsync(tokenIds, cancellationToken))
        {
            yield return token;
        }
    }

    public IAsyncEnumerable<RedisOpenIddictToken> FindByApplicationIdAsync(string identifier, CancellationToken cancellationToken)
        => StreamBySetKeyAsync(RedisOpenIddictKeys.TokensByApplicationId(identifier), cancellationToken);

    public IAsyncEnumerable<RedisOpenIddictToken> FindByAuthorizationIdAsync(string identifier, CancellationToken cancellationToken)
        => StreamBySetKeyAsync(RedisOpenIddictKeys.TokensByAuthorizationId(identifier), cancellationToken);

    public IAsyncEnumerable<RedisOpenIddictToken> FindBySubjectAsync(string subject, CancellationToken cancellationToken)
        => StreamBySetKeyAsync(RedisOpenIddictKeys.TokensBySubject(subject), cancellationToken);

    public async ValueTask<RedisOpenIddictToken?> FindByIdAsync(string identifier, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var payload = await _db.StringGetAsync(RedisOpenIddictKeys.TokenById(identifier));
        return _serializer.Deserialize<RedisOpenIddictToken>(payload);
    }

    public async ValueTask<RedisOpenIddictToken?> FindByReferenceIdAsync(string identifier, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var id = await _db.StringGetAsync(RedisOpenIddictKeys.TokenByReferenceId(identifier));
        if (id.IsNullOrEmpty)
        {
            return null;
        }

        var payload = await _db.StringGetAsync(RedisOpenIddictKeys.TokenById(id!));
        return _serializer.Deserialize<RedisOpenIddictToken>(payload);
    }

    public ValueTask<TResult?> GetAsync<TState, TResult>(Func<IQueryable<RedisOpenIddictToken>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public ValueTask<string?> GetApplicationIdAsync(RedisOpenIddictToken token, CancellationToken cancellationToken) => new(token.ApplicationId);

    public ValueTask<string?> GetAuthorizationIdAsync(RedisOpenIddictToken token, CancellationToken cancellationToken) => new(token.AuthorizationId);

    public ValueTask<DateTimeOffset?> GetCreationDateAsync(RedisOpenIddictToken token, CancellationToken cancellationToken)
        => new(ParseDate(token.CreationDate));

    public ValueTask<DateTimeOffset?> GetExpirationDateAsync(RedisOpenIddictToken token, CancellationToken cancellationToken)
        => new(ParseDate(token.ExpirationDate));

    public ValueTask<string?> GetIdAsync(RedisOpenIddictToken token, CancellationToken cancellationToken) => new(token.Id);

    public ValueTask<string?> GetPayloadAsync(RedisOpenIddictToken token, CancellationToken cancellationToken) => new(token.Payload);

    public ValueTask<ImmutableDictionary<string, string>> GetPropertiesAsync(RedisOpenIddictToken token, CancellationToken cancellationToken)
        => new(ParseLegacyProperties(token.Properties));

    ValueTask<ImmutableDictionary<string, JsonElement>> IOpenIddictTokenStore<RedisOpenIddictToken>.GetPropertiesAsync(RedisOpenIddictToken token, CancellationToken cancellationToken)
        => new(ParseJsonProperties(token.Properties));

    public ValueTask<DateTimeOffset?> GetRedemptionDateAsync(RedisOpenIddictToken token, CancellationToken cancellationToken)
        => new(ParseDate(token.RedemptionDate));

    public ValueTask<string?> GetReferenceIdAsync(RedisOpenIddictToken token, CancellationToken cancellationToken) => new(token.ReferenceId);

    public ValueTask<string?> GetStatusAsync(RedisOpenIddictToken token, CancellationToken cancellationToken) => new(token.Status);

    public ValueTask<string?> GetSubjectAsync(RedisOpenIddictToken token, CancellationToken cancellationToken) => new(token.Subject);

    public ValueTask<string?> GetTypeAsync(RedisOpenIddictToken token, CancellationToken cancellationToken) => new(token.Type);

    public ValueTask<RedisOpenIddictToken> InstantiateAsync(CancellationToken cancellationToken)
        => new(new RedisOpenIddictToken());

    public IAsyncEnumerable<RedisOpenIddictToken> ListAsync(int? count, int? offset, CancellationToken cancellationToken)
        => RedisAsyncEnumerable.Empty<RedisOpenIddictToken>();

    public IAsyncEnumerable<TResult> ListAsync<TState, TResult>(Func<IQueryable<RedisOpenIddictToken>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken)
        => RedisAsyncEnumerable.Empty<TResult>();

    public ValueTask PruneAsync(DateTimeOffset threshold, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;

    ValueTask<long> IOpenIddictTokenStore<RedisOpenIddictToken>.PruneAsync(DateTimeOffset threshold, CancellationToken cancellationToken)
        => ValueTask.FromResult(0L);

    public async ValueTask RevokeAsync(string? subject, string? client, string? status, string? type, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tokens = await ResolveTokensForRevokeAsync(subject, client, status, type, cancellationToken);
        foreach (var token in tokens)
        {
            token.Status = OpenIddictConstants.Statuses.Revoked;
            await PersistAsync(token);
        }
    }

    async ValueTask<long> IOpenIddictTokenStore<RedisOpenIddictToken>.RevokeAsync(
        string? subject,
        string? client,
        string? status,
        string? type,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tokens = await ResolveTokensForRevokeAsync(subject, client, status, type, cancellationToken);
        foreach (var token in tokens)
        {
            token.Status = OpenIddictConstants.Statuses.Revoked;
            await PersistAsync(token);
        }

        return tokens.Count;
    }

    public ValueTask<long> RevokeByApplicationIdAsync(string identifier, CancellationToken cancellationToken = default)
        => RevokeByApplicationOrAuthorizationIdAsync(RedisOpenIddictKeys.TokensByApplicationId(identifier), cancellationToken);

    public ValueTask<long> RevokeByAuthorizationIdAsync(string identifier, CancellationToken cancellationToken)
        => RevokeByApplicationOrAuthorizationIdAsync(RedisOpenIddictKeys.TokensByAuthorizationId(identifier), cancellationToken);

    public async ValueTask<long> RevokeBySubjectAsync(string subject, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tokens = await ResolveTokensAsync(await _db.SetMembersAsync(RedisOpenIddictKeys.TokensBySubject(subject)), cancellationToken);
        foreach (var token in tokens)
        {
            token.Status = OpenIddictConstants.Statuses.Revoked;
            await PersistAsync(token);
        }

        return tokens.Count;
    }

    public ValueTask SetApplicationIdAsync(RedisOpenIddictToken token, string? identifier, CancellationToken cancellationToken)
    {
        token.ApplicationId = identifier;
        return ValueTask.CompletedTask;
    }

    public ValueTask SetAuthorizationIdAsync(RedisOpenIddictToken token, string? identifier, CancellationToken cancellationToken)
    {
        token.AuthorizationId = identifier;
        return ValueTask.CompletedTask;
    }

    public ValueTask SetCreationDateAsync(RedisOpenIddictToken token, DateTimeOffset? date, CancellationToken cancellationToken)
    {
        token.CreationDate = date?.ToString("O");
        return ValueTask.CompletedTask;
    }

    public ValueTask SetExpirationDateAsync(RedisOpenIddictToken token, DateTimeOffset? date, CancellationToken cancellationToken)
    {
        token.ExpirationDate = date?.ToString("O");
        return ValueTask.CompletedTask;
    }

    public ValueTask SetPayloadAsync(RedisOpenIddictToken token, string? payload, CancellationToken cancellationToken)
    {
        token.Payload = payload;
        return ValueTask.CompletedTask;
    }

    public ValueTask SetPropertiesAsync(RedisOpenIddictToken token, ImmutableDictionary<string, string> properties, CancellationToken cancellationToken)
    {
        token.Properties = JsonSerializer.Serialize(properties);
        return ValueTask.CompletedTask;
    }

    public ValueTask SetPropertiesAsync(RedisOpenIddictToken token, ImmutableDictionary<string, JsonElement> properties, CancellationToken cancellationToken)
    {
        token.Properties = JsonSerializer.Serialize(properties);
        return ValueTask.CompletedTask;
    }

    public ValueTask SetRedemptionDateAsync(RedisOpenIddictToken token, DateTimeOffset? date, CancellationToken cancellationToken)
    {
        token.RedemptionDate = date?.ToString("O");
        return ValueTask.CompletedTask;
    }

    public ValueTask SetReferenceIdAsync(RedisOpenIddictToken token, string? identifier, CancellationToken cancellationToken)
    {
        token.ReferenceId = identifier;
        return ValueTask.CompletedTask;
    }

    public ValueTask SetStatusAsync(RedisOpenIddictToken token, string? status, CancellationToken cancellationToken)
    {
        token.Status = status;
        return ValueTask.CompletedTask;
    }

    public ValueTask SetSubjectAsync(RedisOpenIddictToken token, string? subject, CancellationToken cancellationToken)
    {
        token.Subject = subject;
        return ValueTask.CompletedTask;
    }

    public ValueTask SetTypeAsync(RedisOpenIddictToken token, string? type, CancellationToken cancellationToken)
    {
        token.Type = type;
        return ValueTask.CompletedTask;
    }

    public async ValueTask UpdateAsync(RedisOpenIddictToken token, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var existing = await FindByIdAsync(token.Id, cancellationToken);
        if (existing is not null)
        {
            await RemoveIndexesAsync(existing);
        }

        await PersistAsync(token);
        await AddIndexesAsync(token);
    }

    private async Task PersistAsync(RedisOpenIddictToken token)
    {
        await _db.StringSetAsync(RedisOpenIddictKeys.TokenById(token.Id), _serializer.Serialize(token), GetTokenTtl(token));

        if (!string.IsNullOrWhiteSpace(token.ReferenceId))
        {
            await _db.StringSetAsync(RedisOpenIddictKeys.TokenByReferenceId(token.ReferenceId), token.Id, GetTokenTtl(token));
        }
    }

    private async Task AddIndexesAsync(RedisOpenIddictToken token)
    {
        await AddIndexIfSet(RedisOpenIddictKeys.TokensByApplicationId(token.ApplicationId), token.Id);
        await AddIndexIfSet(RedisOpenIddictKeys.TokensByAuthorizationId(token.AuthorizationId), token.Id);
        await AddIndexIfSet(RedisOpenIddictKeys.TokensBySubject(token.Subject), token.Id);
        await AddIndexIfSet(RedisOpenIddictKeys.TokensBySubjectAndClient(token.Subject, token.ApplicationId), token.Id);
        await AddIndexIfSet(RedisOpenIddictKeys.TokensByStatus(token.Status), token.Id);
        await AddIndexIfSet(RedisOpenIddictKeys.TokensByType(token.Type), token.Id);
    }

    private async Task RemoveIndexesAsync(RedisOpenIddictToken token)
    {
        await RemoveIndexIfSet(RedisOpenIddictKeys.TokensByApplicationId(token.ApplicationId), token.Id);
        await RemoveIndexIfSet(RedisOpenIddictKeys.TokensByAuthorizationId(token.AuthorizationId), token.Id);
        await RemoveIndexIfSet(RedisOpenIddictKeys.TokensBySubject(token.Subject), token.Id);
        await RemoveIndexIfSet(RedisOpenIddictKeys.TokensBySubjectAndClient(token.Subject, token.ApplicationId), token.Id);
        await RemoveIndexIfSet(RedisOpenIddictKeys.TokensByStatus(token.Status), token.Id);
        await RemoveIndexIfSet(RedisOpenIddictKeys.TokensByType(token.Type), token.Id);
    }

    private async Task<List<RedisOpenIddictToken>> ResolveTokensForRevokeAsync(string? subject, string? client, string? status, string? type, CancellationToken cancellationToken)
    {
        List<RedisValue>? tokenIds = null;

        if (!string.IsNullOrWhiteSpace(subject) && !string.IsNullOrWhiteSpace(client))
        {
            tokenIds = (await _db.SetMembersAsync(RedisOpenIddictKeys.TokensBySubjectAndClient(subject, client))).ToList();
        }
        else if (!string.IsNullOrWhiteSpace(subject))
        {
            tokenIds = (await _db.SetMembersAsync(RedisOpenIddictKeys.TokensBySubject(subject))).ToList();
        }
        else if (!string.IsNullOrWhiteSpace(client))
        {
            tokenIds = (await _db.SetMembersAsync(RedisOpenIddictKeys.TokensByApplicationId(client))).ToList();
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var statusMembers = (await _db.SetMembersAsync(RedisOpenIddictKeys.TokensByStatus(status))).ToList();
            tokenIds = tokenIds is null ? statusMembers : tokenIds.Intersect(statusMembers).ToList();
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            var typeMembers = (await _db.SetMembersAsync(RedisOpenIddictKeys.TokensByType(type))).ToList();
            tokenIds = tokenIds is null ? typeMembers : tokenIds.Intersect(typeMembers).ToList();
        }

        if (tokenIds is null)
        {
            return new List<RedisOpenIddictToken>();
        }

        return await ResolveTokensAsync(tokenIds, cancellationToken);
    }

    private async ValueTask<long> RevokeByApplicationOrAuthorizationIdAsync(string indexKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tokens = await ResolveTokensAsync(await _db.SetMembersAsync(indexKey), cancellationToken);
        foreach (var token in tokens)
        {
            token.Status = OpenIddictConstants.Statuses.Revoked;
            await PersistAsync(token);
        }

        return tokens.Count;
    }

    private async IAsyncEnumerable<RedisOpenIddictToken> StreamBySetKeyAsync(string key, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tokenIds = await _db.SetMembersAsync(key);
        foreach (var token in await ResolveTokensAsync(tokenIds, cancellationToken))
        {
            yield return token;
        }
    }

    private async Task<List<RedisOpenIddictToken>> ResolveTokensAsync(IEnumerable<RedisValue> tokenIds, CancellationToken cancellationToken)
    {
        var result = new List<RedisOpenIddictToken>();

        foreach (var tokenId in tokenIds.Where(static value => !value.IsNullOrEmpty).Distinct())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var payload = await _db.StringGetAsync(RedisOpenIddictKeys.TokenById(tokenId!));
            var token = _serializer.Deserialize<RedisOpenIddictToken>(payload);
            if (token is not null)
            {
                result.Add(token);
            }
        }

        return result;
    }

    private async Task<RedisValue[]> IntersectSetMembersAsync(params string[] keys)
    {
        var populatedKeys = keys.Where(static key => !string.IsNullOrWhiteSpace(key)).ToArray();
        if (populatedKeys.Length == 0)
        {
            return Array.Empty<RedisValue>();
        }

        RedisValue[] current = await _db.SetMembersAsync(populatedKeys[0]);
        for (var i = 1; i < populatedKeys.Length; i++)
        {
            var next = await _db.SetMembersAsync(populatedKeys[i]);
            current = current.Intersect(next).ToArray();
        }

        return current;
    }

    private async Task AddIndexIfSet(string? key, string tokenId)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        await _db.SetAddAsync(key, tokenId);
    }

    private async Task RemoveIndexIfSet(string? key, string tokenId)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        await _db.SetRemoveAsync(key, tokenId);
    }

    private static DateTimeOffset? ParseDate(string? value)
        => DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;

    private static TimeSpan? GetTokenTtl(RedisOpenIddictToken token)
    {
        var expires = ParseDate(token.ExpirationDate);
        if (expires is null)
        {
            return null;
        }

        var ttl = expires.Value - DateTimeOffset.UtcNow;
        return ttl <= TimeSpan.Zero ? TimeSpan.FromSeconds(1) : ttl;
    }

    private static ImmutableDictionary<string, string> ParseLegacyProperties(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return ImmutableDictionary<string, string>.Empty;
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(payload)?.ToImmutableDictionary() ??
                   ImmutableDictionary<string, string>.Empty;
        }
        catch (JsonException)
        {
            return ImmutableDictionary<string, string>.Empty;
        }
    }

    private static ImmutableDictionary<string, JsonElement> ParseJsonProperties(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return ImmutableDictionary<string, JsonElement>.Empty;
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payload)?.ToImmutableDictionary() ??
                   ImmutableDictionary<string, JsonElement>.Empty;
        }
        catch (JsonException)
        {
            return ImmutableDictionary<string, JsonElement>.Empty;
        }
    }
}
