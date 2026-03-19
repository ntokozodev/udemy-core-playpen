using System.Collections.Immutable;
using System.Security.Claims;
using OpenIddict.Abstractions;
using StackExchange.Redis;

namespace AuthPlaypen.Api.OpenIddict.Redis;

public sealed class RedisOpenIddictTokenStore(IConnectionMultiplexer multiplexer) : IOpenIddictTokenStore<RedisOpenIddictToken>
{
    private readonly IDatabase _db = multiplexer.GetDatabase();
    private readonly RedisOpenIddictSerializer _serializer = new();

    public async ValueTask CreateAsync(RedisOpenIddictToken token, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _db.StringSetAsync(RedisOpenIddictKeys.TokenById(token.Id), _serializer.Serialize(token), GetTokenTtl(token));
        if (!string.IsNullOrWhiteSpace(token.ReferenceId))
        {
            await _db.StringSetAsync(RedisOpenIddictKeys.TokenByReferenceId(token.ReferenceId), token.Id, GetTokenTtl(token));
        }
    }

    public async ValueTask DeleteAsync(RedisOpenIddictToken token, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _db.KeyDeleteAsync(RedisOpenIddictKeys.TokenById(token.Id));
        if (!string.IsNullOrWhiteSpace(token.ReferenceId))
        {
            await _db.KeyDeleteAsync(RedisOpenIddictKeys.TokenByReferenceId(token.ReferenceId));
        }
    }

    public ValueTask<long> CountAsync(CancellationToken cancellationToken) => ValueTask.FromResult(0L);

    public ValueTask<long> CountAsync<TResult>(Func<IQueryable<RedisOpenIddictToken>, IQueryable<TResult>> query, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public IAsyncEnumerable<RedisOpenIddictToken> FindAsync(string subject, string client, CancellationToken cancellationToken)
        => AsyncEnumerable.Empty<RedisOpenIddictToken>();

    public IAsyncEnumerable<RedisOpenIddictToken> FindAsync(string subject, string client, string status, CancellationToken cancellationToken)
        => AsyncEnumerable.Empty<RedisOpenIddictToken>();

    public IAsyncEnumerable<RedisOpenIddictToken> FindAsync(string subject, string client, string status, string type, CancellationToken cancellationToken)
        => AsyncEnumerable.Empty<RedisOpenIddictToken>();

    public IAsyncEnumerable<RedisOpenIddictToken> FindByApplicationIdAsync(string identifier, CancellationToken cancellationToken)
        => AsyncEnumerable.Empty<RedisOpenIddictToken>();

    public IAsyncEnumerable<RedisOpenIddictToken> FindByAuthorizationIdAsync(string identifier, CancellationToken cancellationToken)
        => AsyncEnumerable.Empty<RedisOpenIddictToken>();

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
        => new(ImmutableDictionary<string, string>.Empty);

    public ValueTask<DateTimeOffset?> GetRedemptionDateAsync(RedisOpenIddictToken token, CancellationToken cancellationToken)
        => new(ParseDate(token.RedemptionDate));

    public ValueTask<string?> GetReferenceIdAsync(RedisOpenIddictToken token, CancellationToken cancellationToken) => new(token.ReferenceId);

    public ValueTask<string?> GetStatusAsync(RedisOpenIddictToken token, CancellationToken cancellationToken) => new(token.Status);

    public ValueTask<string?> GetSubjectAsync(RedisOpenIddictToken token, CancellationToken cancellationToken) => new(token.Subject);

    public ValueTask<string?> GetTypeAsync(RedisOpenIddictToken token, CancellationToken cancellationToken) => new(token.Type);

    public ValueTask<RedisOpenIddictToken> InstantiateAsync(CancellationToken cancellationToken)
        => new(new RedisOpenIddictToken());

    public IAsyncEnumerable<RedisOpenIddictToken> ListAsync(int? count, int? offset, CancellationToken cancellationToken)
        => AsyncEnumerable.Empty<RedisOpenIddictToken>();

    public IAsyncEnumerable<TResult> ListAsync<TState, TResult>(Func<IQueryable<RedisOpenIddictToken>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken)
        => AsyncEnumerable.Empty<TResult>();

    public ValueTask PruneAsync(DateTimeOffset threshold, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;

    public ValueTask RevokeAsync(string? subject, string? client, string? status, string? type, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;

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
        => ValueTask.CompletedTask;

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

    public ValueTask UpdateAsync(RedisOpenIddictToken token, CancellationToken cancellationToken)
        => CreateAsync(token, cancellationToken);

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
}
