using AuthPlaypen.ResourceApiAuth;
using Duende.AspNetCore.Authentication.OAuth2Introspection;
using FluentAssertions;
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
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Audience is required*");
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
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*IntrospectionClientId is required*");
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
        authOptions.DefaultAuthenticateScheme.Should().Be(JwtBearerDefaults.AuthenticationScheme);

        var jwtOptions = provider
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        jwtOptions.Authority.Should().Be("https://issuer.example");
        jwtOptions.Audience.Should().Be("resource-api");
        jwtOptions.RequireHttpsMetadata.Should().BeFalse();
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
        authOptions.DefaultAuthenticateScheme.Should().Be(OAuth2IntrospectionDefaults.AuthenticationScheme);

        var introspectionOptions = provider
            .GetRequiredService<IOptionsMonitor<OAuth2IntrospectionOptions>>()
            .Get(OAuth2IntrospectionDefaults.AuthenticationScheme);

        introspectionOptions.Authority.Should().Be("https://issuer.example");
        introspectionOptions.ClientId.Should().Be("resource-client");
        introspectionOptions.ClientSecret.Should().Be("secret");
        introspectionOptions.IntrospectionEndpoint.Should().Be("/custom/introspect");
        introspectionOptions.SaveToken.Should().BeTrue();
        introspectionOptions.CacheDuration.Should().Be(TimeSpan.FromMinutes(2));
        introspectionOptions.NameClaimType.Should().Be("sub");
        introspectionOptions.RoleClaimType.Should().Be("role");
    }

    [Fact]
    public async Task RequireAnyScope_ShouldAuthorize_WhenUserHasMatchingScope()
    {
        // Arrange
        var services = new ServiceCollection();
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
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task RequireAnyScope_ShouldReject_WhenUserLacksRequiredScopes()
    {
        // Arrange
        var services = new ServiceCollection();
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
        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public void RequireAnyScope_ShouldThrow_WhenNoScopesProvided()
    {
        // Arrange
        var builder = new AuthorizationPolicyBuilder();

        // Act
        var act = () => builder.RequireAnyScope();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*At least one scope is required*");
    }
}
