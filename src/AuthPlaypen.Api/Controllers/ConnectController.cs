using System.Security.Claims;
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
    IOpenIddictApplicationManager applicationManager,
    IOpenIddictScopeManager scopeManager,
    IConfiguration configuration) : ControllerBase
{
    [HttpGet("authorize")]
    [HttpPost("authorize")]
    public async Task<IActionResult> Authorize(CancellationToken cancellationToken)
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        var authResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (!authResult.Succeeded || authResult.Principal is null)
        {
            var hasAzureAdClientId = !string.IsNullOrWhiteSpace(configuration["AzureAd:ClientId"]);
            if (!hasAzureAdClientId)
            {
                return BadRequest(new OpenIddictResponse
                {
                    Error = OpenIddictConstants.Errors.ServerError,
                    ErrorDescription = "Azure AD login is not configured. Set AzureAd:ClientId to enable PKCE authorization."
                });
            }

            var redirectUri = Request.PathBase + Request.Path + Request.QueryString;
            return Challenge(
                new AuthenticationProperties { RedirectUri = redirectUri },
                "AzureAd");
        }

        var claims = authResult.Principal.Claims;

        var subject = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
            ?? claims.FirstOrDefault(c => c.Type == "oid")?.Value
            ?? claims.FirstOrDefault(c => c.Type == OpenIddictConstants.Claims.Subject)?.Value;

        if (string.IsNullOrWhiteSpace(subject))
        {
            return BadRequest(new OpenIddictResponse
            {
                Error = OpenIddictConstants.Errors.ServerError,
                ErrorDescription = "The authenticated user does not contain a stable subject identifier."
            });
        }

        var identity = new ClaimsIdentity(
            TokenValidationParameters.DefaultAuthenticationType,
            OpenIddictConstants.Claims.Name,
            OpenIddictConstants.Claims.Role);

        identity.SetClaim(OpenIddictConstants.Claims.Subject, subject);

        var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
            ?? claims.FirstOrDefault(c => c.Type == "name")?.Value;

        if (!string.IsNullOrWhiteSpace(name))
        {
            identity.SetClaim(OpenIddictConstants.Claims.Name, name);
        }

        var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value
            ?? claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value;

        if (!string.IsNullOrWhiteSpace(email))
        {
            identity.SetClaim(OpenIddictConstants.Claims.Email, email);
        }

        var requestedScopes = request.GetScopes();
        identity.SetScopes(requestedScopes);
        var resources = new List<string>();
        await foreach (var resource in scopeManager.ListResourcesAsync(requestedScopes, cancellationToken))
        {
            resources.Add(resource);
        }

        identity.SetResources(resources);
        identity.SetDestinations(static claim =>
            claim.Type switch
            {
                OpenIddictConstants.Claims.Subject => [OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken],
                OpenIddictConstants.Claims.Name => [OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken],
                OpenIddictConstants.Claims.Email => [OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken],
                _ => [OpenIddictConstants.Destinations.AccessToken]
            });

        return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
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
    public IActionResult Logout()
    {
        return SignOut(
            new AuthenticationProperties { RedirectUri = "/" },
            CookieAuthenticationDefaults.AuthenticationScheme,
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}
