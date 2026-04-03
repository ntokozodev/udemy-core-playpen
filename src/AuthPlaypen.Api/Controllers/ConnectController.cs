using System.Collections.Immutable;
using System.Security.Claims;
using AuthPlaypen.Api.Authentication;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace AuthPlaypen.Api.Controllers;

[ApiController]
[ApiExplorerSettings(GroupName = "oidc")]
[Route("connect")]
public class ConnectController(
    IOpenIddictApplicationManager applicationManager) : ControllerBase
{
    [HttpGet("authorize")]
    [HttpPost("authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize(CancellationToken cancellationToken)
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
                      throw new InvalidOperationException("OpenIddict request cannot be retrieved.");

        var schemeProvider = HttpContext.RequestServices.GetRequiredService<IAuthenticationSchemeProvider>();
        var azureOidcScheme = await schemeProvider.GetSchemeAsync(AuthSchemes.AzureAdOidc);
        if (azureOidcScheme is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "External login is not configured",
                Detail = "Configure AzureAd:TenantId, AzureAd:ClientId and AzureAd:ClientSecret to enable O365 sign-in.",
                Status = StatusCodes.Status500InternalServerError
            });
        }

        var externalResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!externalResult.Succeeded || externalResult.Principal is null)
        {
            var redirectUri = Request.PathBase + Request.Path + Request.QueryString;
            return Challenge(new AuthenticationProperties { RedirectUri = redirectUri }, AuthSchemes.AzureAdOidc);
        }

        var principal = CreateOpenIddictPrincipal(externalResult.Principal, request);

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpPost("token")]
    public async Task<IActionResult> Token(CancellationToken cancellationToken)
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        if (request.IsAuthorizationCodeGrantType())
        {
            var authenticateResult = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            if (!authenticateResult.Succeeded || authenticateResult.Principal is null)
            {
                return BadRequest(new OpenIddictResponse
                {
                    Error = OpenIddictConstants.Errors.InvalidGrant,
                    ErrorDescription = "The authorization code is invalid or has expired."
                });
            }

            var authorizationCodeIdentity = new ClaimsIdentity(
                authenticateResult.Principal.Claims,
                TokenValidationParameters.DefaultAuthenticationType,
                OpenIddictConstants.Claims.Name,
                OpenIddictConstants.Claims.Role);

            authorizationCodeIdentity.SetDestinations(static claim =>
                claim.Type switch
                {
                    OpenIddictConstants.Claims.Subject => [OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken],
                    OpenIddictConstants.Claims.Name => [OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken],
                    OpenIddictConstants.Claims.Email => [OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken],
                    _ => [OpenIddictConstants.Destinations.AccessToken]
                });

            return SignIn(new ClaimsPrincipal(authorizationCodeIdentity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (!request.IsClientCredentialsGrantType())
        {
            return BadRequest(new OpenIddictResponse
            {
                Error = OpenIddictConstants.Errors.UnsupportedGrantType,
                ErrorDescription = "Only client_credentials and authorization_code grants are supported."
            });
        }

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

    [HttpGet("logout")]
    [HttpPost("logout")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return SignOut(new AuthenticationProperties
        {
            RedirectUri = "/"
        }, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static ClaimsPrincipal CreateOpenIddictPrincipal(ClaimsPrincipal sourcePrincipal, OpenIddictRequest request)
    {
        var identity = new ClaimsIdentity(
            authenticationType: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            nameType: OpenIddictConstants.Claims.Name,
            roleType: OpenIddictConstants.Claims.Role);

        var subject = sourcePrincipal.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? sourcePrincipal.FindFirstValue("oid")
                      ?? sourcePrincipal.FindFirstValue("sub")
                      ?? throw new InvalidOperationException("Authenticated user does not include a stable subject identifier.");

        identity.SetClaim(OpenIddictConstants.Claims.Subject, subject);

        var name = sourcePrincipal.FindFirstValue(ClaimTypes.Name)
                   ?? sourcePrincipal.FindFirstValue("name")
                   ?? subject;
        identity.SetClaim(OpenIddictConstants.Claims.Name, name);

        var email = sourcePrincipal.FindFirstValue(ClaimTypes.Email)
                    ?? sourcePrincipal.FindFirstValue("preferred_username");
        if (!string.IsNullOrWhiteSpace(email))
        {
            identity.SetClaim(OpenIddictConstants.Claims.Email, email);
        }

        identity.SetClaim(OpenIddictConstants.Claims.Private.ProviderName, "azuread");

        var principal = new ClaimsPrincipal(identity);

        var scopes = request.GetScopes().ToImmutableArray();
        principal.SetScopes(scopes);

        principal.SetResources(scopes
            .Where(scope =>
                !string.Equals(scope, OpenIddictConstants.Scopes.OpenId, StringComparison.Ordinal) &&
                !string.Equals(scope, OpenIddictConstants.Scopes.OfflineAccess, StringComparison.Ordinal) &&
                !string.Equals(scope, OpenIddictConstants.Scopes.Profile, StringComparison.Ordinal) &&
                !string.Equals(scope, OpenIddictConstants.Scopes.Email, StringComparison.Ordinal)));

        foreach (var claim in principal.Claims)
        {
            claim.SetDestinations(GetDestinations(claim, principal));
        }

        return principal;
    }

    private static IEnumerable<string> GetDestinations(Claim claim, ClaimsPrincipal principal)
    {
        switch (claim.Type)
        {
            case OpenIddictConstants.Claims.Subject:
                yield return OpenIddictConstants.Destinations.AccessToken;
                yield return OpenIddictConstants.Destinations.IdentityToken;
                yield break;

            case OpenIddictConstants.Claims.Name:
                yield return OpenIddictConstants.Destinations.AccessToken;
                if (principal.HasScope(OpenIddictConstants.Scopes.Profile))
                {
                    yield return OpenIddictConstants.Destinations.IdentityToken;
                }
                yield break;

            case OpenIddictConstants.Claims.Email:
                yield return OpenIddictConstants.Destinations.AccessToken;
                if (principal.HasScope(OpenIddictConstants.Scopes.Email))
                {
                    yield return OpenIddictConstants.Destinations.IdentityToken;
                }
                yield break;

            default:
                yield return OpenIddictConstants.Destinations.AccessToken;
                yield break;
        }
    }
}
