using System.Security.Cryptography.X509Certificates;
using AuthPlaypen.Api.Services;
using AuthPlaypen.Application.Services;
using AuthPlaypen.Data.Data;
using AuthPlaypen.OpenIddict.Redis;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var tenantId = builder.Configuration["AzureAd:TenantId"];
    var clientId = builder.Configuration["AzureAd:ClientId"];
    var scope = builder.Configuration["AzureAd:Scope"];

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

var tenantId = builder.Configuration["AzureAd:TenantId"];
var audience = builder.Configuration["AzureAd:Audience"];
var azureClientId = builder.Configuration["AzureAd:ClientId"];
var azureClientSecret = builder.Configuration["AzureAd:ClientSecret"];
var azureCallbackPath = builder.Configuration["AzureAd:CallbackPath"] ?? "/signin-oidc";

var jwtValidationConfigured = !string.IsNullOrWhiteSpace(tenantId) && !string.IsNullOrWhiteSpace(audience);
var externalOidcConfigured = !string.IsNullOrWhiteSpace(tenantId) && !string.IsNullOrWhiteSpace(azureClientId) && !string.IsNullOrWhiteSpace(azureClientSecret);
var enableIntrospectionEndpoint = bool.TryParse(builder.Configuration["LocalAuth:EnableIntrospectionEndpoint"], out var enabledIntrospection) ? enabledIntrospection : true;

var authenticationBuilder = builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme);

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

builder.Services.AddAuthorization();

var redisConnectionString = builder.Configuration.GetConnectionString("Redis")
    ?? "localhost:6379";

builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));

var openIddictBuilder = builder.Services.AddOpenIddict();

openIddictBuilder
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
        var issuer = builder.Configuration["OpenIddictSigningOptions:Issuer"];
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

        ConfigureSigningCertificate(options, builder.Configuration);

        options.UseAspNetCore()
            .EnableAuthorizationEndpointPassthrough()
            .EnableEndSessionEndpointPassthrough();
    });

builder.Services.AddDbContext<AuthPlaypenDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Postgres")
        ?? throw new InvalidOperationException("Connection string 'Postgres' was not found.");

    options.UseNpgsql(connectionString)
        .UseSnakeCaseNamingConvention();
});

builder.Services.AddScoped<IApplicationService, ApplicationService>();
builder.Services.AddScoped<IScopeService, ScopeService>();
builder.Services.AddScoped<IOpenIddictScopeSyncService, OpenIddictScopeSyncService>();
builder.Services.AddScoped<IOpenIddictApplicationSyncService, OpenIddictApplicationSyncService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        var clientId = builder.Configuration["AzureAd:ClientId"];
        var scope = builder.Configuration["AzureAd:Scope"];

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

if (jwtValidationConfigured || externalOidcConfigured)
{
    app.UseAuthentication();
}

app.UseAuthorization();

app.MapGet("/app-config", (IConfiguration config, IWebHostEnvironment environment) =>
{
    var useMockData = config["AdminApp:UseMockData"];
    var enableOidcAuth = config["AdminApp:Oidc:EnableAuth"];
    var authority = config["AdminApp:Oidc:Authority"];
    var clientId = config["AdminApp:Oidc:ClientId"];
    var redirectPath = config["AdminApp:Oidc:RedirectPath"];
    var postLogoutRedirectPath = config["AdminApp:Oidc:PostLogoutRedirectPath"];

    var useLocalMockDefaults = environment.IsDevelopment() && string.Equals(useMockData, "true", StringComparison.OrdinalIgnoreCase);

    if (useLocalMockDefaults)
    {
        authority = "https://localhost:5100";
        clientId = "authkeeper-web-admin";
        redirectPath = "/auth/callback";
        postLogoutRedirectPath = "/";
    }

    return Results.Ok(new
    {
        useMockData,
        enableOidcAuth,
        authority,
        clientId,
        redirectPath,
        postLogoutRedirectPath
    });
});

app.MapFallbackToFile("/admin/{*path:nonfile}", "admin/index.html");
app.MapControllers();

app.Run();

static void ConfigureSigningCertificate(OpenIddictServerBuilder options, IConfiguration configuration)
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
