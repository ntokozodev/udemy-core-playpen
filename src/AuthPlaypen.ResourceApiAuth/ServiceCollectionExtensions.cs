using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;

namespace AuthPlaypen.ResourceApiAuth;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuthApiResourceAuthentication(
        this IServiceCollection services,
        Action<AuthApiResourceAuthOptions> configure)
    {
        var options = new AuthApiResourceAuthOptions();
        configure(options);
        ValidateOptions(options);

        if (options.ValidationMode == AuthApiTokenValidationMode.Introspection)
        {
            throw new NotSupportedException(
                "Introspection mode is not supported in this package. " +
                "IdentityModel.AspNetCore.OAuth2Introspection is archived and no longer maintained. " +
                "Use Jwt validation mode.");
        }

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, jwtOptions =>
            {
                jwtOptions.Authority = options.Authority;
                jwtOptions.Audience = options.Audience;
                jwtOptions.RequireHttpsMetadata = options.RequireHttpsMetadata;
            });

        services.AddAuthorization();
        return services;
    }

    private static void ValidateOptions(AuthApiResourceAuthOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            throw new InvalidOperationException("AuthApiResourceAuthOptions.Audience is required.");
        }

    }
}
