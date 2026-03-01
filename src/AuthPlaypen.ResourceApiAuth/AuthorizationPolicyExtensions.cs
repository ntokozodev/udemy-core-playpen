using Microsoft.AspNetCore.Authorization;

namespace AuthPlaypen.ResourceApiAuth;

public static class AuthorizationPolicyExtensions
{
    public static AuthorizationPolicyBuilder RequireAnyScope(
        this AuthorizationPolicyBuilder builder,
        params string[] scopes)
    {
        if (scopes.Length == 0)
        {
            throw new ArgumentException("At least one scope is required.", nameof(scopes));
        }

        return builder.RequireAssertion(context =>
        {
            var scopeValues = context.User.FindAll("scope")
                .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries));

            return scopeValues.Intersect(scopes, StringComparer.OrdinalIgnoreCase).Any();
        });
    }
}
