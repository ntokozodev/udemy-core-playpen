using System.Collections.Immutable;
using System.Text.Json;
using OpenIddict.Abstractions;
using StackExchange.Redis;

namespace AuthPlaypen.OpenIddict.Redis;

public sealed class RedisOpenIddictAuthorizationStore(IConnectionMultiplexer multiplexer)
    : IOpenIddictAuthorizationStore<RedisOpenIddictAuthorization>
{
    private readonly IDatabase _db = multiplexer.GetDatabase();
    private readonly RedisOpenIddictSerializer _serializer = new();

    public async ValueTask CreateAsync(RedisOpenIddictAuthorization authorization, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _db.StringSetAsync(RedisOpenIddictKeys.AuthorizationById(authorization.Id), _serializer.Serialize(authorization));
    }

    public async ValueTask DeleteAsync(RedisOpenIddictAuthorization authorization, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _db.KeyDeleteAsync(RedisOpenIddictKeys.AuthorizationById(authorization.Id));
    }

    public ValueTask<long> CountAsync(CancellationToken cancellationToken)
        => ValueTask.FromResult(0L);

    public ValueTask<long> CountAsync<TResult>(Func<IQueryable<RedisOpenIddictAuthorization>, IQueryable<TResult>> query, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public IAsyncEnumerable<RedisOpenIddictAuthorization> FindAsync(string subject, string client, CancellationToken cancellationToken)
        => FilterAsync(authorization =>
            string.Equals(authorization.Subject, subject, StringComparison.Ordinal) &&
            string.Equals(authorization.ApplicationId, client, StringComparison.Ordinal), cancellationToken);

    public IAsyncEnumerable<RedisOpenIddictAuthorization> FindAsync(string subject, string client, string status, CancellationToken cancellationToken)
        => FilterAsync(authorization =>
            string.Equals(authorization.Subject, subject, StringComparison.Ordinal) &&
            string.Equals(authorization.ApplicationId, client, StringComparison.Ordinal) &&
            string.Equals(authorization.Status, status, StringComparison.Ordinal), cancellationToken);

    public IAsyncEnumerable<RedisOpenIddictAuthorization> FindAsync(string subject, string client, string status, string type, CancellationToken cancellationToken)
        => FilterAsync(authorization =>
            string.Equals(authorization.Subject, subject, StringComparison.Ordinal) &&
            string.Equals(authorization.ApplicationId, client, StringComparison.Ordinal) &&
            string.Equals(authorization.Status, status, StringComparison.Ordinal) &&
            string.Equals(authorization.Type, type, StringComparison.Ordinal), cancellationToken);

    public IAsyncEnumerable<RedisOpenIddictAuthorization> FindAsync(string subject, string client, string status, string type, ImmutableArray<string> scopes, CancellationToken cancellationToken)
        => FilterAsync(authorization =>
            string.Equals(authorization.Subject, subject, StringComparison.Ordinal) &&
            string.Equals(authorization.ApplicationId, client, StringComparison.Ordinal) &&
            string.Equals(authorization.Status, status, StringComparison.Ordinal) &&
            string.Equals(authorization.Type, type, StringComparison.Ordinal) &&
            scopes.All(scope => authorization.Scopes.Contains(scope)), cancellationToken);

    public IAsyncEnumerable<RedisOpenIddictAuthorization> FindAsync(
        string? subject,
        string? client,
        string? status,
        string? type,
        ImmutableArray<string>? scopes,
        CancellationToken cancellationToken)
        => FilterAsync(authorization =>
                (string.IsNullOrWhiteSpace(subject) || string.Equals(authorization.Subject, subject, StringComparison.Ordinal)) &&
                (string.IsNullOrWhiteSpace(client) || string.Equals(authorization.ApplicationId, client, StringComparison.Ordinal)) &&
                (string.IsNullOrWhiteSpace(status) || string.Equals(authorization.Status, status, StringComparison.Ordinal)) &&
                (string.IsNullOrWhiteSpace(type) || string.Equals(authorization.Type, type, StringComparison.Ordinal)) &&
                (scopes is null || scopes.Value.IsDefaultOrEmpty || scopes.Value.All(scope => authorization.Scopes.Contains(scope))),
            cancellationToken);

    public IAsyncEnumerable<RedisOpenIddictAuthorization> FindByApplicationIdAsync(string identifier, CancellationToken cancellationToken)
        => FilterAsync(authorization => string.Equals(authorization.ApplicationId, identifier, StringComparison.Ordinal), cancellationToken);

    public async ValueTask<RedisOpenIddictAuthorization?> FindByIdAsync(string identifier, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var payload = await _db.StringGetAsync(RedisOpenIddictKeys.AuthorizationById(identifier));
        return _serializer.Deserialize<RedisOpenIddictAuthorization>(payload);
    }

    public IAsyncEnumerable<RedisOpenIddictAuthorization> FindBySubjectAsync(string subject, CancellationToken cancellationToken)
        => FilterAsync(authorization => string.Equals(authorization.Subject, subject, StringComparison.Ordinal), cancellationToken);

    public ValueTask<TResult?> GetAsync<TState, TResult>(Func<IQueryable<RedisOpenIddictAuthorization>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public ValueTask<string?> GetApplicationIdAsync(RedisOpenIddictAuthorization authorization, CancellationToken cancellationToken)
        => new(authorization.ApplicationId);

    public ValueTask<DateTimeOffset?> GetCreationDateAsync(RedisOpenIddictAuthorization authorization, CancellationToken cancellationToken)
        => new(ParseDate(authorization.CreationDate));

    public ValueTask<string?> GetIdAsync(RedisOpenIddictAuthorization authorization, CancellationToken cancellationToken)
        => new(authorization.Id);

    public ValueTask<ImmutableDictionary<string, JsonElement>> GetPropertiesAsync(RedisOpenIddictAuthorization authorization, CancellationToken cancellationToken)
        => new(authorization.Properties.ToImmutableDictionary());

    public ValueTask<ImmutableArray<string>> GetScopesAsync(RedisOpenIddictAuthorization authorization, CancellationToken cancellationToken)
        => new(authorization.Scopes.ToImmutableArray());

    public ValueTask<string?> GetStatusAsync(RedisOpenIddictAuthorization authorization, CancellationToken cancellationToken)
        => new(authorization.Status);

    public ValueTask<string?> GetSubjectAsync(RedisOpenIddictAuthorization authorization, CancellationToken cancellationToken)
        => new(authorization.Subject);

    public ValueTask<string?> GetTypeAsync(RedisOpenIddictAuthorization authorization, CancellationToken cancellationToken)
        => new(authorization.Type);

    public ValueTask<RedisOpenIddictAuthorization> InstantiateAsync(CancellationToken cancellationToken)
        => new(new RedisOpenIddictAuthorization());

    public IAsyncEnumerable<RedisOpenIddictAuthorization> ListAsync(int? count, int? offset, CancellationToken cancellationToken)
        => AsyncEnumerable.Empty<RedisOpenIddictAuthorization>();

    public IAsyncEnumerable<TResult> ListAsync<TState, TResult>(Func<IQueryable<RedisOpenIddictAuthorization>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken)
        => AsyncEnumerable.Empty<TResult>();

    public ValueTask PruneAsync(DateTimeOffset threshold, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;

    ValueTask<long> IOpenIddictAuthorizationStore<RedisOpenIddictAuthorization>.PruneAsync(DateTimeOffset threshold, CancellationToken cancellationToken)
        => ValueTask.FromResult(0L);

    public async ValueTask RevokeAsync(string? subject, string? client, string? status, string? type, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await foreach (var authorization in FindAsync(subject, client, status, type, scopes: null, cancellationToken))
        {
            authorization.Status = OpenIddictConstants.Statuses.Revoked;
            await _db.StringSetAsync(RedisOpenIddictKeys.AuthorizationById(authorization.Id), _serializer.Serialize(authorization));
        }
    }

    async ValueTask<long> IOpenIddictAuthorizationStore<RedisOpenIddictAuthorization>.RevokeAsync(
        string? subject,
        string? client,
        string? status,
        string? type,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var count = 0L;
        await foreach (var authorization in FindAsync(subject, client, status, type, scopes: null, cancellationToken))
        {
            authorization.Status = OpenIddictConstants.Statuses.Revoked;
            await _db.StringSetAsync(RedisOpenIddictKeys.AuthorizationById(authorization.Id), _serializer.Serialize(authorization));
            count++;
        }

        return count;
    }

    public async ValueTask<long> RevokeByApplicationIdAsync(string identifier, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var count = 0L;
        await foreach (var authorization in FindByApplicationIdAsync(identifier, cancellationToken))
        {
            authorization.Status = OpenIddictConstants.Statuses.Revoked;
            await _db.StringSetAsync(RedisOpenIddictKeys.AuthorizationById(authorization.Id), _serializer.Serialize(authorization));
            count++;
        }

        return count;
    }

    public async ValueTask<long> RevokeBySubjectAsync(string subject, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var count = 0L;
        await foreach (var authorization in FindBySubjectAsync(subject, cancellationToken))
        {
            authorization.Status = OpenIddictConstants.Statuses.Revoked;
            await _db.StringSetAsync(RedisOpenIddictKeys.AuthorizationById(authorization.Id), _serializer.Serialize(authorization));
            count++;
        }

        return count;
    }

    public ValueTask SetApplicationIdAsync(RedisOpenIddictAuthorization authorization, string? identifier, CancellationToken cancellationToken)
    {
        authorization.ApplicationId = identifier;
        return ValueTask.CompletedTask;
    }

    public ValueTask SetCreationDateAsync(RedisOpenIddictAuthorization authorization, DateTimeOffset? date, CancellationToken cancellationToken)
    {
        authorization.CreationDate = date?.ToString("O");
        return ValueTask.CompletedTask;
    }

    public ValueTask SetPropertiesAsync(RedisOpenIddictAuthorization authorization, ImmutableDictionary<string, JsonElement> properties, CancellationToken cancellationToken)
    {
        authorization.Properties = properties.ToDictionary();
        return ValueTask.CompletedTask;
    }

    public ValueTask SetScopesAsync(RedisOpenIddictAuthorization authorization, ImmutableArray<string> scopes, CancellationToken cancellationToken)
    {
        authorization.Scopes = scopes.Where(static scope => !string.IsNullOrWhiteSpace(scope)).ToHashSet(StringComparer.Ordinal);
        return ValueTask.CompletedTask;
    }

    public ValueTask SetStatusAsync(RedisOpenIddictAuthorization authorization, string? status, CancellationToken cancellationToken)
    {
        authorization.Status = status;
        return ValueTask.CompletedTask;
    }

    public ValueTask SetSubjectAsync(RedisOpenIddictAuthorization authorization, string? subject, CancellationToken cancellationToken)
    {
        authorization.Subject = subject;
        return ValueTask.CompletedTask;
    }

    public ValueTask SetTypeAsync(RedisOpenIddictAuthorization authorization, string? type, CancellationToken cancellationToken)
    {
        authorization.Type = type;
        return ValueTask.CompletedTask;
    }

    public ValueTask UpdateAsync(RedisOpenIddictAuthorization authorization, CancellationToken cancellationToken)
        => CreateAsync(authorization, cancellationToken);

    private async IAsyncEnumerable<RedisOpenIddictAuthorization> FilterAsync(
        Func<RedisOpenIddictAuthorization, bool> predicate,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var endpoint in multiplexer.GetEndPoints())
        {
            var server = multiplexer.GetServer(endpoint);
            foreach (var key in server.Keys(pattern: "oidc:authorizations:id:*").Select(static x => (string)x))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var payload = await _db.StringGetAsync(key);
                var authorization = _serializer.Deserialize<RedisOpenIddictAuthorization>(payload);
                if (authorization is not null && predicate(authorization))
                {
                    yield return authorization;
                }
            }
        }
    }

    private static DateTimeOffset? ParseDate(string? value)
        => DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;
}
