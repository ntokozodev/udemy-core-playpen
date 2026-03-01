using AuthPlaypen.Api.OpenIddict.Redis;
using AuthPlaypen.Api.Services;
using AuthPlaypen.Application.Services;
using AuthPlaypen.Data.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenIddict.Abstractions;
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

var localIssuer = builder.Configuration["LocalAuth:Issuer"] ?? "https://authplaypen.local";
var localAudience = builder.Configuration["LocalAuth:Audience"] ?? "authplaypen-resource-apis";
var localSigningKey = builder.Configuration["LocalAuth:SigningKey"] ?? "dev-local-signing-key-change-me-1234567890";
var accessTokenLifetimeSeconds = int.TryParse(builder.Configuration["LocalAuth:AccessTokenLifetimeSeconds"], out var parsedTokenLifetime)
    ? parsedTokenLifetime
    : 3600;

var enableIntrospectionEndpoint = bool.TryParse(builder.Configuration["LocalAuth:EnableIntrospectionEndpoint"], out var parsedEnableIntrospection)
    && parsedEnableIntrospection;

var tenantId = builder.Configuration["AzureAd:TenantId"];
var audience = builder.Configuration["AzureAd:Audience"];
var azureAdClientId = builder.Configuration["AzureAd:ClientId"];
var azureAdClientSecret = builder.Configuration["AzureAd:ClientSecret"];
var azureAdCallbackPath = builder.Configuration["AzureAd:CallbackPath"] ?? "/signin-oidc";
var azureAdAuthority = !string.IsNullOrWhiteSpace(tenantId)
    ? $"https://login.microsoftonline.com/{tenantId}/v2.0"
    : null;
var authConfigured = !string.IsNullOrWhiteSpace(tenantId) && !string.IsNullOrWhiteSpace(audience);

var authenticationBuilder = builder.Services.AddAuthentication();

if (!string.IsNullOrWhiteSpace(azureAdAuthority) && !string.IsNullOrWhiteSpace(azureAdClientId))
{
    authenticationBuilder
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddOpenIdConnect("AzureAd", options =>
        {
            options.Authority = azureAdAuthority;
            options.ClientId = azureAdClientId;
            options.ClientSecret = azureAdClientSecret;
            options.CallbackPath = azureAdCallbackPath;
            options.ResponseType = OpenIdConnectResponseType.Code;
            options.UsePkce = true;
            options.SaveTokens = true;
            options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

            options.Scope.Clear();
            options.Scope.Add(OpenIddictConstants.Scopes.OpenId);
            options.Scope.Add(OpenIddictConstants.Scopes.Profile);
            options.Scope.Add(OpenIddictConstants.Scopes.Email);
        });
}

if (authConfigured)
{
    authenticationBuilder.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
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

builder.Services.AddAuthorization();


var redisConnectionString = builder.Configuration.GetConnectionString("Redis")
    ?? "localhost:6379";

builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));

builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.SetDefaultApplicationEntity<RedisOpenIddictApplication>();
        options.SetDefaultScopeEntity<RedisOpenIddictScope>();
        options.SetDefaultTokenEntity<RedisOpenIddictToken>();

        options.Services.AddSingleton<IOpenIddictApplicationStore<RedisOpenIddictApplication>, RedisOpenIddictApplicationStore>();
        options.Services.AddSingleton<IOpenIddictScopeStore<RedisOpenIddictScope>, RedisOpenIddictScopeStore>();
        options.Services.AddSingleton<IOpenIddictTokenStore<RedisOpenIddictToken>, RedisOpenIddictTokenStore>();
    })
    .AddServer(options =>
    {
        options.SetAuthorizationEndpointUris("/connect/authorize");
        options.SetTokenEndpointUris("/connect/token");
        options.SetLogoutEndpointUris("/connect/logout");
        options.SetConfigurationEndpointUris("/.well-known/openid-configuration");
        options.SetJsonWebKeySetEndpointUris("/.well-known/jwks");
        if (enableIntrospectionEndpoint)
        {
            options.SetIntrospectionEndpointUris("/connect/introspect");
        }

        options.AllowClientCredentialsFlow();
        options.AllowAuthorizationCodeFlow();
        options.RequireProofKeyForCodeExchange();

        options.SetIssuer(new Uri(localIssuer));
        options.AddAudiences(localAudience);
        options.SetAccessTokenLifetime(TimeSpan.FromSeconds(accessTokenLifetimeSeconds));

        var signingKeyBytes = System.Text.Encoding.UTF8.GetBytes(localSigningKey);
        if (signingKeyBytes.Length < 32)
        {
            throw new InvalidOperationException("LocalAuth:SigningKey must be at least 32 bytes.");
        }

        var signingKey = new SymmetricSecurityKey(signingKeyBytes)
        {
            KeyId = Convert.ToHexString(SHA256.HashData(signingKeyBytes))
        };

        options.AddSigningKey(signingKey);

        var aspNetCore = options.UseAspNetCore();
        aspNetCore.EnableAuthorizationEndpointPassthrough();
        aspNetCore.EnableTokenEndpointPassthrough();
        aspNetCore.EnableLogoutEndpointPassthrough();

        if (enableIntrospectionEndpoint)
        {
            aspNetCore.EnableIntrospectionEndpointPassthrough();
        }
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

app.UseAuthentication();
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
        clientId = "gatekeeper-web-admin";
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
