using AuthPlaypen.ResourceApiAuth;
using Duende.AspNetCore.Authentication.OAuth2Introspection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace AuthPlaypen.ResourceApiAuth.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddAuthApiResourceAuthentication_ShouldThrow_WhenAudienceIsMissing()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddAuthApiResourceAuthentication(_ => { });

        // Assert
        var exception = Assert.Throws<InvalidOperationException>(act);
        Assert.Contains("Audience is required", exception.Message);
    }

    [Fact]
    public void AddAuthApiResourceAuthentication_ShouldThrow_WhenIntrospectionCredentialsAreMissing()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddAuthApiResourceAuthentication(options =>
        {
            options.Audience = "resource-api";
            options.ValidationMode = AuthApiTokenValidationMode.Introspection;
        });

        // Assert
        var exception = Assert.Throws<InvalidOperationException>(act);
        Assert.Contains("IntrospectionClientId is required", exception.Message);
    }

    [Fact]
    public void AddAuthApiResourceAuthentication_ShouldConfigureJwtBearerByDefault()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAuthApiResourceAuthentication(options =>
        {
            options.Authority = "https://issuer.example";
            options.Audience = "resource-api";
            options.RequireHttpsMetadata = false;
        });

        using var provider = services.BuildServiceProvider();

        // Assert
        var authOptions = provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;
        Assert.Equal(JwtBearerDefaults.AuthenticationScheme, authOptions.DefaultAuthenticateScheme);

        var jwtOptions = provider
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        Assert.Equal("https://issuer.example", jwtOptions.Authority);
        Assert.Equal("resource-api", jwtOptions.Audience);
        Assert.False(jwtOptions.RequireHttpsMetadata);
    }

    [Fact]
    public void AddAuthApiResourceAuthentication_ShouldConfigureIntrospection_WhenRequested()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAuthApiResourceAuthentication(options =>
        {
            options.Authority = "https://issuer.example";
            options.Audience = "resource-api";
            options.ValidationMode = AuthApiTokenValidationMode.Introspection;
            options.IntrospectionClientId = "resource-client";
            options.IntrospectionClientSecret = "secret";
            options.IntrospectionEndpoint = "/custom/introspect";
        });

        using var provider = services.BuildServiceProvider();

        // Assert
        var authOptions = provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;
        Assert.Equal(OAuth2IntrospectionDefaults.AuthenticationScheme, authOptions.DefaultAuthenticateScheme);

        var introspectionOptions = provider
            .GetRequiredService<IOptionsMonitor<OAuth2IntrospectionOptions>>()
            .Get(OAuth2IntrospectionDefaults.AuthenticationScheme);

        Assert.Equal("https://issuer.example", introspectionOptions.Authority);
        Assert.Equal("resource-client", introspectionOptions.ClientId);
        Assert.Equal("secret", introspectionOptions.ClientSecret);
        Assert.Equal("/custom/introspect", introspectionOptions.IntrospectionEndpoint);
        Assert.True(introspectionOptions.SaveToken);
        Assert.Equal(TimeSpan.FromMinutes(2), introspectionOptions.CacheDuration);
        Assert.Equal("sub", introspectionOptions.NameClaimType);
        Assert.Equal("role", introspectionOptions.RoleClaimType);
    }

    [Fact]
    public async Task RequireAnyScope_ShouldAuthorize_WhenUserHasMatchingScope()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorizationBuilder()
            .AddPolicy("scope-policy", policy => policy.RequireAnyScope("api.read", "api.write"));

        using var provider = services.BuildServiceProvider();
        var authorizationService = provider.GetRequiredService<IAuthorizationService>();

        var user = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(
        [
            new("scope", "profile api.read")
        ],
        "Bearer"));

        // Act
        var result = await authorizationService.AuthorizeAsync(user, resource: null, policyName: "scope-policy");

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task RequireAnyScope_ShouldReject_WhenUserLacksRequiredScopes()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorizationBuilder()
            .AddPolicy("scope-policy", policy => policy.RequireAnyScope("api.read", "api.write"));

        using var provider = services.BuildServiceProvider();
        var authorizationService = provider.GetRequiredService<IAuthorizationService>();

        var user = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(
        [
            new("scope", "profile email")
        ],
        "Bearer"));

        // Act
        var result = await authorizationService.AuthorizeAsync(user, resource: null, policyName: "scope-policy");

        // Assert
        Assert.False(result.Succeeded);
    }

    [Fact]
    public void RequireAnyScope_ShouldThrow_WhenNoScopesProvided()
    {
        // Arrange
        var builder = new AuthorizationPolicyBuilder();

        // Act
        var act = () => builder.RequireAnyScope();

        // Assert
        var exception = Assert.Throws<ArgumentException>(act);
        Assert.Contains("At least one scope is required", exception.Message);
    }
}
