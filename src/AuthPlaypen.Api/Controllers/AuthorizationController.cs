using System.Collections.Immutable;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace AuthPlaypen.Api.Controllers;

[ApiController]
public sealed class AuthorizationController : Controller
{
    [HttpGet("~/connect/authorize")]
    [HttpPost("~/connect/authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize(CancellationToken cancellationToken)
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
                      throw new InvalidOperationException("OpenIddict request cannot be retrieved.");


        var schemeProvider = HttpContext.RequestServices.GetRequiredService<IAuthenticationSchemeProvider>();
        var azureOidcScheme = await schemeProvider.GetSchemeAsync("AzureAdOidc");
        if (azureOidcScheme is null)
        {
            return Problem(
                title: "External login is not configured",
                detail: "Configure AzureAd:TenantId, AzureAd:ClientId and AzureAd:ClientSecret to enable O365 sign-in.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
        var externalResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!externalResult.Succeeded || externalResult.Principal is null)
        {
            var redirectUri = Request.PathBase + Request.Path + Request.QueryString;
            return Challenge(new AuthenticationProperties { RedirectUri = redirectUri }, "AzureAdOidc");
        }

        var principal = CreateOpenIddictPrincipal(externalResult.Principal, request);

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpGet("~/connect/logout")]
    [HttpPost("~/connect/logout")]
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

        identity.SetClaim(OpenIddictConstants.Claims.IdentityProvider, "azuread");

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
