using Duende.AspNetCore.Authentication.OAuth2Introspection;
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

        switch (options.ValidationMode)
        {
            case AuthApiTokenValidationMode.Jwt:
                services
                    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, jwtOptions =>
                    {
                        jwtOptions.Authority = options.Authority;
                        jwtOptions.Audience = options.Audience;
                        jwtOptions.RequireHttpsMetadata = options.RequireHttpsMetadata;
                    });
                break;

            case AuthApiTokenValidationMode.Introspection:
                services
                    .AddAuthentication(OAuth2IntrospectionDefaults.AuthenticationScheme)
                    .AddOAuth2Introspection(OAuth2IntrospectionDefaults.AuthenticationScheme, introspectionOptions =>
                    {
                        introspectionOptions.Authority = options.Authority;
                        introspectionOptions.ClientId = options.IntrospectionClientId!;
                        introspectionOptions.ClientSecret = options.IntrospectionClientSecret!;
                        introspectionOptions.IntrospectionEndpoint = options.IntrospectionEndpoint;
                        introspectionOptions.EnableCaching = true;
                        introspectionOptions.CacheDuration = TimeSpan.FromMinutes(2);
                        introspectionOptions.NameClaimType = "sub";
                        introspectionOptions.RoleClaimType = "role";
                    });
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(options.ValidationMode), options.ValidationMode, "Unsupported validation mode.");
        }

        services.AddAuthorization();
        return services;
    }

    private static void ValidateOptions(AuthApiResourceAuthOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            throw new InvalidOperationException("AuthApiResourceAuthOptions.Audience is required.");
        }

        if (options.ValidationMode == AuthApiTokenValidationMode.Introspection)
        {
            if (string.IsNullOrWhiteSpace(options.IntrospectionClientId))
            {
                throw new InvalidOperationException("IntrospectionClientId is required when ValidationMode is Introspection.");
            }

            if (string.IsNullOrWhiteSpace(options.IntrospectionClientSecret))
            {
                throw new InvalidOperationException("IntrospectionClientSecret is required when ValidationMode is Introspection.");
            }
        }

    }
}
