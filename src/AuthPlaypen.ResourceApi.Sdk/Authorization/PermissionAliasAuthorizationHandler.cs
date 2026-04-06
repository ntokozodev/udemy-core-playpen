using Microsoft.AspNetCore.Authorization;

namespace AuthPlaypen.ResourceApi;

public sealed class PermissionAliasAuthorizationHandler(
    IPermissionScopeResolver permissionScopeResolver) : AuthorizationHandler<PermissionAliasRequirement>
{
    private readonly IPermissionScopeResolver _permissionScopeResolver = permissionScopeResolver;

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionAliasRequirement requirement)
    {
        var scopes = await _permissionScopeResolver.ResolveScopesAsync(requirement.PermissionAlias);
        if (scopes.Count == 0)
        {
            return;
        }

        var tokenScopes = context.User.FindAll("scope")
            .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        if (tokenScopes.Intersect(scopes, StringComparer.OrdinalIgnoreCase).Any())
        {
            context.Succeed(requirement);
        }
    }
}
