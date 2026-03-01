using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace AuthPlaypen.Api.Controllers;

[ApiController]
[Route("connect")]
public class ConnectController(IOpenIddictApplicationManager applicationManager) : ControllerBase
{
    [HttpPost("token")]
    public async Task<IActionResult> Token(CancellationToken cancellationToken)
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        var clientId = request.ClientId;
        var clientSecret = request.ClientSecret;

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            return BadRequest(new OpenIddictResponse
            {
                Error = OpenIddictConstants.Errors.InvalidClient,
                ErrorDescription = "client_id and client_secret are required."
            });
        }

        var application = await applicationManager.FindByClientIdAsync(clientId, cancellationToken);
        if (application is null)
        {
            return BadRequest(new OpenIddictResponse
            {
                Error = OpenIddictConstants.Errors.InvalidClient,
                ErrorDescription = "Client authentication failed."
            });
        }

        var isValidClientSecret = await applicationManager.ValidateClientSecretAsync(application, clientSecret, cancellationToken);
        if (!isValidClientSecret)
        {
            return BadRequest(new OpenIddictResponse
            {
                Error = OpenIddictConstants.Errors.InvalidClient,
                ErrorDescription = "Client authentication failed."
            });
        }

        var permissions = await applicationManager.GetPermissionsAsync(application, cancellationToken);
        var allowsClientCredentials = permissions.Contains(OpenIddictConstants.Permissions.GrantTypes.ClientCredentials, StringComparer.Ordinal);
        if (!allowsClientCredentials)
        {
            return BadRequest(new OpenIddictResponse
            {
                Error = OpenIddictConstants.Errors.UnauthorizedClient,
                ErrorDescription = "This client is not configured for the client credentials flow."
            });
        }

        var issuedScopes = permissions
            .Where(permission => permission.StartsWith(OpenIddictConstants.Permissions.Prefixes.Scope, StringComparison.Ordinal))
            .Select(permission => permission[OpenIddictConstants.Permissions.Prefixes.Scope.Length..])
            .Where(scopeName => !string.IsNullOrWhiteSpace(scopeName))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (issuedScopes.Length == 0)
        {
            return BadRequest(new OpenIddictResponse
            {
                Error = OpenIddictConstants.Errors.InvalidScope,
                ErrorDescription = "No scopes are assigned to this client."
            });
        }

        var displayName = await applicationManager.GetDisplayNameAsync(application, cancellationToken);

        var identity = new ClaimsIdentity(
            TokenValidationParameters.DefaultAuthenticationType,
            OpenIddictConstants.Claims.Name,
            OpenIddictConstants.Claims.Role);

        identity.SetClaim(OpenIddictConstants.Claims.Subject, clientId);
        identity.SetClaim("client_id", clientId);

        if (!string.IsNullOrWhiteSpace(displayName))
        {
            identity.SetClaim(OpenIddictConstants.Claims.Name, displayName);
        }

        identity.SetScopes(issuedScopes);
        identity.SetDestinations(static claim => [OpenIddictConstants.Destinations.AccessToken]);

        return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}
