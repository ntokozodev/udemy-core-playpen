namespace AuthPlaypen.ResourceApi;

public interface IPermissionScopeMapSource
{
    Task<IReadOnlyDictionary<string, IReadOnlyCollection<string>>> GetPermissionScopeMapAsync(
        CancellationToken cancellationToken = default);
}
