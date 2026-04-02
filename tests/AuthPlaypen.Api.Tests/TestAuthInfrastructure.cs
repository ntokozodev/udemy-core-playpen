using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using AuthPlaypen.Api.Authentication;

namespace AuthPlaypen.Api.Tests;

internal sealed class StubAuthenticationService : IAuthenticationService
{
    public AuthenticateResult AuthenticateResult { get; set; } = AuthenticateResult.NoResult();

    public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
        => Task.FromResult(AuthenticateResult);

    public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        => Task.CompletedTask;

    public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        => Task.CompletedTask;

    public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties)
        => Task.CompletedTask;

    public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        => Task.CompletedTask;
}

internal sealed class StubSchemeProvider : AuthenticationSchemeProvider
{
    public StubSchemeProvider(IOptions<AuthenticationOptions> options)
        : base(options)
    {
    }

    public AuthenticationScheme? AzureAdOidcScheme { get; set; }

    public override Task<AuthenticationScheme?> GetSchemeAsync(string name)
    {
        if (name == AuthSchemes.AzureAdOidc)
        {
            return Task.FromResult(AzureAdOidcScheme);
        }

        return base.GetSchemeAsync(name);
    }
}
