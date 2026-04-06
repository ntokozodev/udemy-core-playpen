using System.Security.Claims;
using AuthPlaypen.ResourceApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AuthPlaypen.ResourceApi.Tests;

public class PermissionAliasAuthorizationTests
{
    [Fact]
    public async Task RequirePermissionAlias_ShouldAuthorize_WhenDynamicMappingMatchesUserScope()
    {
        // Arrange
        var source = new FakePermissionScopeMapSource(new Dictionary<string, IReadOnlyCollection<string>>
        {
            ["orders.read"] = ["resource-b.orders.read"]
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IPermissionScopeMapSource>(source);
        services.AddAuthApiPermissionAliasAuthorization(options =>
        {
            options.CacheDuration = TimeSpan.FromMinutes(10);
        });
        services.AddAuthorizationBuilder()
            .AddPolicy("orders.read", policy => policy.RequireAuthenticatedUser().RequirePermissionAlias("orders.read"));

        using var provider = services.BuildServiceProvider();
        var authorizationService = provider.GetRequiredService<IAuthorizationService>();
        var user = CreateUser("resource-b.orders.read profile");

        // Act
        var result = await authorizationService.AuthorizeAsync(user, resource: null, policyName: "orders.read");

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task RequirePermissionAlias_ShouldUseHardcodedFallback_WhenMetadataMissesAlias()
    {
        // Arrange
        var source = new FakePermissionScopeMapSource(new Dictionary<string, IReadOnlyCollection<string>>());

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IPermissionScopeMapSource>(source);
        services.AddAuthApiPermissionAliasAuthorization(options =>
        {
            options.HardcodedFallbackMappings["orders.read"] = ["resource-b.orders.read"];
        });
        services.AddAuthorizationBuilder()
            .AddPolicy("orders.read", policy => policy.RequireAuthenticatedUser().RequirePermissionAlias("orders.read"));

        using var provider = services.BuildServiceProvider();
        var authorizationService = provider.GetRequiredService<IAuthorizationService>();
        var user = CreateUser("resource-b.orders.read");

        // Act
        var result = await authorizationService.AuthorizeAsync(user, resource: null, policyName: "orders.read");

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task CachedPermissionScopeResolver_ShouldCacheMapWithinCacheWindow()
    {
        // Arrange
        var source = new FakePermissionScopeMapSource(new Dictionary<string, IReadOnlyCollection<string>>
        {
            ["orders.read"] = ["resource-b.orders.read"]
        });
        var resolver = new CachedPermissionScopeResolver(source, new PermissionAliasAuthorizationOptions
        {
            CacheDuration = TimeSpan.FromMinutes(10)
        });

        // Act
        var first = await resolver.ResolveScopesAsync("orders.read");
        var second = await resolver.ResolveScopesAsync("orders.read");

        // Assert
        Assert.Equal("resource-b.orders.read", Assert.Single(first));
        Assert.Equal("resource-b.orders.read", Assert.Single(second));
        Assert.Equal(1, source.CallCount);
    }

    [Fact]
    public async Task CachedPermissionScopeResolver_ShouldReturnFallback_WhenSourceThrows()
    {
        // Arrange
        var source = new ThrowingPermissionScopeMapSource();
        var resolver = new CachedPermissionScopeResolver(source, new PermissionAliasAuthorizationOptions
        {
            FailureRetryDelay = TimeSpan.FromMinutes(1),
            HardcodedFallbackMappings =
            {
                ["orders.read"] = ["resource-b.orders.read"]
            }
        });

        // Act
        var scopes = await resolver.ResolveScopesAsync("orders.read");

        // Assert
        Assert.Equal("resource-b.orders.read", Assert.Single(scopes));
    }

    [Fact]
    public void RequirePermissionAlias_ShouldThrow_WhenAliasMissing()
    {
        // Arrange
        var builder = new AuthorizationPolicyBuilder();

        // Act
        var act = () => builder.RequirePermissionAlias(string.Empty);

        // Assert
        var exception = Assert.Throws<ArgumentException>(act);
        Assert.Contains("Permission alias is required", exception.Message);
    }

    [Fact]
    public void AddAuthApiPermissionAliasAuthorization_ShouldThrow_WhenMetadataEndpointMissing_AndNoCustomSource()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddAuthApiPermissionAliasAuthorization(options =>
        {
            options.MetadataEndpoint = string.Empty;
        });

        // Assert
        var exception = Assert.Throws<InvalidOperationException>(act);
        Assert.Contains("MetadataEndpoint is required", exception.Message);
    }

    private static ClaimsPrincipal CreateUser(string scopeValue)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("scope", scopeValue)
        ],
        authenticationType: "Bearer"));
    }

    private sealed class FakePermissionScopeMapSource(
        IReadOnlyDictionary<string, IReadOnlyCollection<string>> mappings) : IPermissionScopeMapSource
    {
        public int CallCount { get; private set; }

        public Task<IReadOnlyDictionary<string, IReadOnlyCollection<string>>> GetPermissionScopeMapAsync(
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(mappings);
        }
    }

    private sealed class ThrowingPermissionScopeMapSource : IPermissionScopeMapSource
    {
        public Task<IReadOnlyDictionary<string, IReadOnlyCollection<string>>> GetPermissionScopeMapAsync(
            CancellationToken cancellationToken = default)
            => throw new HttpRequestException("metadata unavailable");
    }
}
