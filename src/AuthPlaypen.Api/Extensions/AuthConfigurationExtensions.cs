using System.Security.Cryptography.X509Certificates;
using AuthPlaypen.Api.Services;
using AuthPlaypen.Application.Services;
using AuthPlaypen.Data.Data;
using AuthPlaypen.OpenIddict.Redis;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using StackExchange.Redis;

namespace AuthPlaypen.Api.Extensions;

public static class AuthConfigurationExtensions
{
    public static IServiceCollection AddAuthPlaypenApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddAuthPlaypenSwagger(configuration);
        services.AddAuthPlaypenAuthentication(configuration);
        services.AddAuthorization();
        services.AddAuthPlaypenOpenIddict(configuration);
        services.AddAuthPlaypenData(configuration);
        services.AddAuthPlaypenApplicationServices();

        return services;
    }

    public static void MapAuthPlaypenInfrastructure(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                var clientId = app.Configuration["AzureAd:ClientId"];
                var scope = app.Configuration["AzureAd:Scope"];

                options.OAuthUsePkce();

                if (!string.IsNullOrWhiteSpace(clientId))
                {
                    options.OAuthClientId(clientId);
                }

                if (!string.IsNullOrWhiteSpace(scope))
                {
                    options.OAuthScopes(scope);
                }
            });
        }

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuthPlaypenDbContext>();
            db.Database.Migrate();
        }

        app.UseHttpsRedirection();
        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGet("/app-config", (IConfiguration config) =>
        {
            var enableOidcAuth = config["AdminApp:Oidc:EnableAuth"];
            var authority = config["AdminApp:Oidc:Authority"];
            var clientId = config["AdminApp:Oidc:ClientId"];
            var redirectPath = config["AdminApp:Oidc:RedirectPath"];
            var postLogoutRedirectPath = config["AdminApp:Oidc:PostLogoutRedirectPath"];

            return Results.Ok(new
            {
                enableOidcAuth,
                authority,
                clientId,
                redirectPath,
                postLogoutRedirectPath
            });
        });

        app.MapFallbackToFile("/admin/{*path:nonfile}", "admin/index.html");
        app.MapControllers();
    }

    private static void AddAuthPlaypenSwagger(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSwaggerGen(options =>
        {
            var tenantId = configuration["AzureAd:TenantId"];
            var clientId = configuration["AzureAd:ClientId"];
            var scope = configuration["AzureAd:Scope"];

            if (!string.IsNullOrWhiteSpace(tenantId) && !string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(scope))
            {
                var authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";

                options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,
                    Flows = new OpenApiOAuthFlows
                    {
                        AuthorizationCode = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = new Uri($"{authority}/oauth2/v2.0/authorize"),
                            TokenUrl = new Uri($"{authority}/oauth2/v2.0/token"),
                            Scopes = new Dictionary<string, string>
                            {
                                [scope] = "AuthPlaypen admin scope"
                            }
                        }
                    }
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Id = "oauth2",
                                Type = ReferenceType.SecurityScheme
                            }
                        },
                        [scope]
                    }
                });
            }
        });
    }

    private static void AddAuthPlaypenAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var tenantId = configuration["AzureAd:TenantId"];
        var audience = configuration["AzureAd:Audience"];
        var azureClientId = configuration["AzureAd:ClientId"];
        var azureClientSecret = configuration["AzureAd:ClientSecret"];
        var azureCallbackPath = configuration["AzureAd:CallbackPath"] ?? "/signin-oidc";

        var jwtValidationConfigured = !string.IsNullOrWhiteSpace(tenantId) && !string.IsNullOrWhiteSpace(audience);
        var externalOidcConfigured = !string.IsNullOrWhiteSpace(tenantId) && !string.IsNullOrWhiteSpace(azureClientId) && !string.IsNullOrWhiteSpace(azureClientSecret);

        var authenticationBuilder = services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme);

        if (jwtValidationConfigured)
        {
            authenticationBuilder.AddJwtBearer(options =>
            {
                options.Authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";
                options.Audience = audience;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidAudience = audience
                };
            });
        }

        if (externalOidcConfigured)
        {
            authenticationBuilder
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddOpenIdConnect("AzureAdOidc", options =>
                {
                    options.Authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";
                    options.ClientId = azureClientId;
                    options.ClientSecret = azureClientSecret;
                    options.CallbackPath = azureCallbackPath;
                    options.ResponseType = "code";
                    options.SaveTokens = true;
                    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.GetClaimsFromUserInfoEndpoint = true;
                    options.Scope.Clear();
                    options.Scope.Add("openid");
                    options.Scope.Add("profile");
                    options.Scope.Add("email");
                });
        }
    }

    private static void AddAuthPlaypenOpenIddict(this IServiceCollection services, IConfiguration configuration)
    {
        var enableIntrospectionEndpoint = bool.TryParse(configuration["LocalAuth:EnableIntrospectionEndpoint"], out var enabledIntrospection) ? enabledIntrospection : true;
        var redisConnectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));

        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.SetDefaultApplicationEntity<RedisOpenIddictApplication>();
                options.SetDefaultAuthorizationEntity<RedisOpenIddictAuthorization>();
                options.SetDefaultScopeEntity<RedisOpenIddictScope>();
                options.SetDefaultTokenEntity<RedisOpenIddictToken>();

                options.Services.AddSingleton<IOpenIddictApplicationStore<RedisOpenIddictApplication>, RedisOpenIddictApplicationStore>();
                options.Services.AddSingleton<IOpenIddictAuthorizationStore<RedisOpenIddictAuthorization>, RedisOpenIddictAuthorizationStore>();
                options.Services.AddSingleton<IOpenIddictScopeStore<RedisOpenIddictScope>, RedisOpenIddictScopeStore>();
                options.Services.AddSingleton<IOpenIddictTokenStore<RedisOpenIddictToken>, RedisOpenIddictTokenStore>();
            })
            .AddServer(options =>
            {
                var issuer = configuration["OpenIddictSigningOptions:Issuer"];
                if (!string.IsNullOrWhiteSpace(issuer))
                {
                    options.SetIssuer(new Uri(issuer, UriKind.Absolute));
                }

                options.SetAuthorizationEndpointUris("connect/authorize")
                    .SetTokenEndpointUris("connect/token")
                    .SetRevocationEndpointUris("connect/revoke")
                    .SetEndSessionEndpointUris("connect/logout")
                    .SetUserInfoEndpointUris("connect/userinfo");

                if (enableIntrospectionEndpoint)
                {
                    options.SetIntrospectionEndpointUris("connect/introspect");
                }

                options.AllowAuthorizationCodeFlow()
                    .AllowClientCredentialsFlow()
                    .RequireProofKeyForCodeExchange();

                options.RegisterScopes(OpenIddictConstants.Scopes.OpenId, OpenIddictConstants.Scopes.Profile, OpenIddictConstants.Scopes.OfflineAccess);

                ConfigureSigningCertificate(options, configuration);

                options.UseAspNetCore()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableEndSessionEndpointPassthrough();
            });
    }

    private static void AddAuthPlaypenData(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AuthPlaypenDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("Postgres")
                ?? throw new InvalidOperationException("Connection string 'Postgres' was not found.");

            options.UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention();
        });
    }

    private static void AddAuthPlaypenApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IApplicationService, ApplicationService>();
        services.AddScoped<IScopeService, ScopeService>();
        services.AddScoped<IOpenIddictScopeSyncService, OpenIddictScopeSyncService>();
        services.AddScoped<IOpenIddictApplicationSyncService, OpenIddictApplicationSyncService>();
    }

    private static void ConfigureSigningCertificate(OpenIddictServerBuilder options, IConfiguration configuration)
    {
        var certPath = configuration["OpenIddictSigningOptions:SigningCertificatePath"];
        var certPassword = configuration["OpenIddictSigningOptions:SigningCertificatePassword"];

        if (!string.IsNullOrWhiteSpace(certPath))
        {
            if (!Path.IsPathRooted(certPath))
            {
                certPath = Path.Combine(AppContext.BaseDirectory, certPath);
            }

            if (!File.Exists(certPath))
            {
                throw new InvalidOperationException($"OpenIddict signing certificate file not found: {certPath}");
            }

            var certificate = new X509Certificate2(certPath, certPassword);
            options.AddSigningCertificate(certificate);
            return;
        }

        options.AddDevelopmentSigningCertificate();
    }
}
