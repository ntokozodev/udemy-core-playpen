namespace AuthPlaypen.ResourceApi;

public interface IPermissionScopeResolver
{
    Task<IReadOnlyCollection<string>> ResolveScopesAsync(string permissionAlias, CancellationToken cancellationToken = default);
}
