using System.Collections.Immutable;
using System.Text.Json;
using OpenIddict.Abstractions;
using StackExchange.Redis;
using AuthPlaypen.OpenIddict.Redis.Models;

namespace AuthPlaypen.OpenIddict.Redis.Stores;

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
        => RedisAsyncEnumerable.Empty<RedisOpenIddictAuthorization>();

    public IAsyncEnumerable<TResult> ListAsync<TState, TResult>(Func<IQueryable<RedisOpenIddictAuthorization>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken)
        => RedisAsyncEnumerable.Empty<TResult>();

    public ValueTask PruneAsync(DateTimeOffset threshold, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;

    public async ValueTask RevokeAsync(string? subject, string? client, string? status, string? type, CancellationToken cancellationToken)
        => await RevokeInternalAsync(subject, client, status, type, cancellationToken);

    public async ValueTask<long> RevokeByApplicationIdAsync(string identifier, CancellationToken cancellationToken)
        => await RevokeInternalAsync(subject: null, client: identifier, status: null, type: null, cancellationToken);

    public async ValueTask<long> RevokeBySubjectAsync(string subject, CancellationToken cancellationToken)
        => await RevokeInternalAsync(subject, client: null, status: null, type: null, cancellationToken);

    private async ValueTask<long> RevokeInternalAsync(string? subject, string? client, string? status, string? type, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var revoked = 0L;
        await foreach (var authorization in FilterAsync(authorization =>
                           (string.IsNullOrWhiteSpace(subject) || string.Equals(authorization.Subject, subject, StringComparison.Ordinal)) &&
                           (string.IsNullOrWhiteSpace(client) || string.Equals(authorization.ApplicationId, client, StringComparison.Ordinal)) &&
                           (string.IsNullOrWhiteSpace(status) || string.Equals(authorization.Status, status, StringComparison.Ordinal)) &&
                           (string.IsNullOrWhiteSpace(type) || string.Equals(authorization.Type, type, StringComparison.Ordinal)), cancellationToken))
        {
            authorization.Status = OpenIddictConstants.Statuses.Revoked;
            await _db.StringSetAsync(RedisOpenIddictKeys.AuthorizationById(authorization.Id), _serializer.Serialize(authorization));
            revoked++;
        }

        return revoked;
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

    ValueTask<long> IOpenIddictAuthorizationStore<RedisOpenIddictAuthorization>.CountAsync(CancellationToken cancellationToken)
        => CountAsync(cancellationToken);

    ValueTask<long> IOpenIddictAuthorizationStore<RedisOpenIddictAuthorization>.CountAsync<TResult>(
        Func<IQueryable<RedisOpenIddictAuthorization>, IQueryable<TResult>> query,
        CancellationToken cancellationToken)
        => CountAsync(query, cancellationToken);

    ValueTask IOpenIddictAuthorizationStore<RedisOpenIddictAuthorization>.CreateAsync(
        RedisOpenIddictAuthorization authorization,
        CancellationToken cancellationToken)
        => CreateAsync(authorization, cancellationToken);

    ValueTask IOpenIddictAuthorizationStore<RedisOpenIddictAuthorization>.DeleteAsync(
        RedisOpenIddictAuthorization authorization,
        CancellationToken cancellationToken)
        => DeleteAsync(authorization, cancellationToken);

    IAsyncEnumerable<RedisOpenIddictAuthorization> IOpenIddictAuthorizationStore<RedisOpenIddictAuthorization>.FindAsync(
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
            (!scopes.HasValue || scopes.Value.IsDefaultOrEmpty || scopes.Value.All(scope => authorization.Scopes.Contains(scope))),
            cancellationToken);

    IAsyncEnumerable<RedisOpenIddictAuthorization> IOpenIddictAuthorizationStore<RedisOpenIddictAuthorization>.FindByApplicationIdAsync(
        string identifier,
        CancellationToken cancellationToken)
        => FindByApplicationIdAsync(identifier, cancellationToken);

    ValueTask<RedisOpenIddictAuthorization?> IOpenIddictAuthorizationStore<RedisOpenIddictAuthorization>.FindByIdAsync(
        string identifier,
        CancellationToken cancellationToken)
        => FindByIdAsync(identifier, cancellationToken);

    IAsyncEnumerable<RedisOpenIddictAuthorization> IOpenIddictAuthorizationStore<RedisOpenIddictAuthorization>.FindBySubjectAsync(
        string subject,
        CancellationToken cancellationToken)
        => FindBySubjectAsync(subject, cancellationToken);

    ValueTask<string?> IOpenIddictAuthorizationStore<RedisOpenIddictAuthorization>.GetApplicationIdAsync(
        RedisOpenIddictAuthorization authorization,
        CancellationToken cancellationToken)
        => GetApplicationIdAsync(authorization, cancellationToken);

    ValueTask<DateTimeOffset?> IOpenIddictAuthorizationStore<RedisOpenIddictAuthorization>.GetCreationDateAsync(
        RedisOpenIddictAuthorization authorization,
        CancellationToken cancellationToken)
        => GetCreationDateAsync(authorization, cancellationToken);

    ValueTask<string?> IOpenIddictAuthorizationStore<RedisOpenIddictAuthorization>.GetIdAsync(
        RedisOpenIddictAuthorization authorization,
        CancellationToken cancellationToken)
        => GetIdAsync(authorization, cancellationToken);

    ValueTask<ImmutableDictionary<string, JsonElement>> IOpenIddictAuthorizationStore<RedisOpenIddictAuthorization>.GetPropertiesAsync(
        RedisOpenIddictAuthorization authorization,
        CancellationToken cancellationToken)
        => GetPropertiesAsync(authorization, cancellationToken);

    ValueTask<ImmutableArray<string>> IOpenIddictAuthorizationStore<RedisOpenIddictAuthorization>.GetScopesAsync(
        RedisOpenIddictAuthorization authorization,
        CancellationToken cancellationToken)
        => GetScopesAsync(authorization, cancellationToken);

    ValueTask<string?> IOpenIddictAuthorizationStore<RedisOpenIddictAuthorization>.GetStatusAsync(
        RedisOpenIddictAuthorization authorization,
        CancellationToken cancellationToken)
        => GetStatusAsync(authorization, cancellationToken);

    ValueTask<string?> IOpenIddictAuthorizationStore<RedisOpenIddictAuthorization>.GetSubjectAsync(
        RedisOpenIddictAuthorization authorization,
        CancellationToken cancellationToken)
        => GetSubjectAsync(authorization, cancellationToken);

    ValueTask<string?> IOpenIddictAuthorizationStore<RedisOpenIddictAuthorization>.GetTypeAsync(
        RedisOpenIddictAuthorization authorization,
        CancellationToken cancellationToken)
        => GetTypeAsync(authorization, cancellationToken);

    ValueTask<RedisOpenIddictAuthorization> IOpenIddictAuthorizationStore<RedisOpenIddictAuthorization>.InstantiateAsync(CancellationToken cancellationToken)
        => InstantiateAsync(cancellationToken);

    IAsyncEnumerable<RedisOpenIddictAuthorization> IOpenIddictAuthorizationStore<RedisOpenIddictAuthorization>.ListAsync(
        int? count,
        int? offset,
        CancellationToken cancellationToken)
        => ListAsync(count, offset, cancellationToken);

    IAsyncEnumerable<TResult> IOpenIddictAuthorizationStore<RedisOpenIddictAuthorization>.ListAsync<TState, TResult>(
        Func<IQueryable<RedisOpenIddictAuthorization>, TState, IQueryable<TResult>> query,
        TState state,
        CancellationToken cancellationToken)
        => ListAsync(query, state, cancellationToken);

    ValueTask<long> IOpenIddictAuthorizationStore<RedisOpenIddictAuthorization>.PruneAsync(DateTimeOffset threshold, CancellationToken cancellationToken)
        => ValueTask.FromResult(0L);

    async ValueTask<long> IOpenIddictAuthorizationStore<RedisOpenIddictAuthorization>.RevokeAsync(
        string? subject,
        string? client,
        string? status,
        string? type,
        CancellationToken cancellationToken)
        => await RevokeInternalAsync(subject, client, status, type, cancellationToken);

    ValueTask<long> IOpenIddictAuthorizationStore<RedisOpenIddictAuthorization>.RevokeByApplicationIdAsync(
        string identifier,
        CancellationToken cancellationToken)
        => RevokeByApplicationIdAsync(identifier, cancellationToken);

    ValueTask<long> IOpenIddictAuthorizationStore<RedisOpenIddictAuthorization>.RevokeBySubjectAsync(
        string subject,
        CancellationToken cancellationToken)
        => RevokeBySubjectAsync(subject, cancellationToken);

    ValueTask IOpenIddictAuthorizationStore<RedisOpenIddictAuthorization>.SetApplicationIdAsync(
        RedisOpenIddictAuthorization authorization,
        string? identifier,
        CancellationToken cancellationToken)
        => SetApplicationIdAsync(authorization, identifier, cancellationToken);

    ValueTask IOpenIddictAuthorizationStore<RedisOpenIddictAuthorization>.SetCreationDateAsync(
        RedisOpenIddictAuthorization authorization,
        DateTimeOffset? date,
        CancellationToken cancellationToken)
        => SetCreationDateAsync(authorization, date, cancellationToken);

    ValueTask IOpenIddictAuthorizationStore<RedisOpenIddictAuthorization>.SetPropertiesAsync(
        RedisOpenIddictAuthorization authorization,
        ImmutableDictionary<string, JsonElement> properties,
        CancellationToken cancellationToken)
        => SetPropertiesAsync(authorization, properties, cancellationToken);

    ValueTask IOpenIddictAuthorizationStore<RedisOpenIddictAuthorization>.SetScopesAsync(
        RedisOpenIddictAuthorization authorization,
        ImmutableArray<string> scopes,
        CancellationToken cancellationToken)
        => SetScopesAsync(authorization, scopes, cancellationToken);

    ValueTask IOpenIddictAuthorizationStore<RedisOpenIddictAuthorization>.SetStatusAsync(
        RedisOpenIddictAuthorization authorization,
        string? status,
        CancellationToken cancellationToken)
        => SetStatusAsync(authorization, status, cancellationToken);

    ValueTask IOpenIddictAuthorizationStore<RedisOpenIddictAuthorization>.SetSubjectAsync(
        RedisOpenIddictAuthorization authorization,
        string? subject,
        CancellationToken cancellationToken)
        => SetSubjectAsync(authorization, subject, cancellationToken);

    ValueTask IOpenIddictAuthorizationStore<RedisOpenIddictAuthorization>.SetTypeAsync(
        RedisOpenIddictAuthorization authorization,
        string? type,
        CancellationToken cancellationToken)
        => SetTypeAsync(authorization, type, cancellationToken);

    ValueTask IOpenIddictAuthorizationStore<RedisOpenIddictAuthorization>.UpdateAsync(
        RedisOpenIddictAuthorization authorization,
        CancellationToken cancellationToken)
        => UpdateAsync(authorization, cancellationToken);

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
