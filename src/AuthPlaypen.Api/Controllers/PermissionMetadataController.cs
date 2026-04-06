using AuthPlaypen.OpenIddict.Redis.Models;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;

namespace AuthPlaypen.Api.Controllers;

[ApiController]
[ApiExplorerSettings(GroupName = "oidc")]
[Route(".well-known/authplaypen")]
public sealed class PermissionMetadataController(IOpenIddictScopeStore<RedisOpenIddictScope> scopeStore) : ControllerBase
{
    [HttpGet("permissions")]
    public async Task<IActionResult> GetPermissions(CancellationToken cancellationToken)
    {
        var permissions = new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.OrdinalIgnoreCase);
        await foreach (var scope in scopeStore.ListAsync(count: null, offset: null, cancellationToken))
        {
            if (string.IsNullOrWhiteSpace(scope.Name))
            {
                continue;
            }

            permissions[scope.Name] = [scope.Name];
        }

        return Ok(new { permissions });
    }
}
