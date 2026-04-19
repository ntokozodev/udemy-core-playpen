using System.Security.Claims;
using AuthPlaypen.Application.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace AuthPlaypen.Api.Services;

public sealed class ActorContextService(
    IHttpContextAccessor httpContextAccessor,
    IWebHostEnvironment environment,
    IConfiguration configuration) : IActorContextService
{
    private const string SystemActor = "system";

    public ActorContext GetCurrentActor()
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (TryResolveUser(user, out var actor))
        {
            return actor;
        }

        if (IsLocalAuthDisabled())
        {
            return new ActorContext(SystemActor, null, true);
        }

        throw new InvalidOperationException("Authenticated user identity is required for metadata updates outside local auth-disabled mode.");
    }

    private bool IsLocalAuthDisabled()
    {
        var enableAuthValue = configuration["AdminApp:Oidc:EnableAuth"];
        var enableAuth = bool.TryParse(enableAuthValue, out var parsed) && parsed;
        return environment.IsDevelopment() && !enableAuth;
    }

    private static bool TryResolveUser(ClaimsPrincipal? user, out ActorContext actor)
    {
        actor = default!;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var displayName = user.FindFirstValue(ClaimTypes.Name)
            ?? user.FindFirstValue("name")
            ?? user.Identity?.Name;
        var email = user.FindFirstValue(ClaimTypes.Email)
            ?? user.FindFirstValue("email")
            ?? user.FindFirstValue("preferred_username")
            ?? user.FindFirstValue("upn");

        if (string.IsNullOrWhiteSpace(displayName) && string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        displayName = string.IsNullOrWhiteSpace(displayName) ? email! : displayName;
        actor = new ActorContext(displayName, string.IsNullOrWhiteSpace(email) ? null : email, false);
        return true;
    }
}
