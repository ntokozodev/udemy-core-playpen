using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AuthPlaypen.ResourceApi;

public static class PermissionAliasExtensions
{
    public static IServiceCollection AddAuthApiPermissionAliasAuthorization(
        this IServiceCollection services,
        Action<PermissionAliasAuthorizationOptions>? configure = null)
    {
        var options = new PermissionAliasAuthorizationOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.TryAddSingleton<IPermissionScopeResolver, CachedPermissionScopeResolver>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAuthorizationHandler, PermissionAliasAuthorizationHandler>());

        if (!services.Any(descriptor => descriptor.ServiceType == typeof(IPermissionScopeMapSource)))
        {
            if (string.IsNullOrWhiteSpace(options.MetadataEndpoint))
            {
                throw new InvalidOperationException(
                    "PermissionAliasAuthorizationOptions.MetadataEndpoint is required when using the default HTTP metadata source.");
            }

            services.AddHttpClient<IPermissionScopeMapSource, HttpPermissionScopeMapSource>(client =>
            {
                client.BaseAddress = new Uri(options.Authority, UriKind.Absolute);
            });
        }

        return services;
    }

    public static AuthorizationPolicyBuilder RequirePermissionAlias(
        this AuthorizationPolicyBuilder builder,
        string permissionAlias)
    {
        if (string.IsNullOrWhiteSpace(permissionAlias))
        {
            throw new ArgumentException("Permission alias is required.", nameof(permissionAlias));
        }

        return builder.AddRequirements(new PermissionAliasRequirement(permissionAlias));
    }
}
