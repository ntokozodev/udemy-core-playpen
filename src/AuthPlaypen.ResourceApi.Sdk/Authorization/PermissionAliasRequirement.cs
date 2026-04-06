using Microsoft.AspNetCore.Authorization;

namespace AuthPlaypen.ResourceApi;

public sealed class PermissionAliasRequirement(string permissionAlias) : IAuthorizationRequirement
{
    public string PermissionAlias { get; } = permissionAlias;
}
