using System.Security.Claims;
using AuthPlaypen.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace AuthPlaypen.Api.Tests;

public class ActorContextServiceTests
{
    [Fact]
    public void GetCurrentActor_ShouldReturnAuthenticatedUser_WhenIdentityAndClaimsExist()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.Name, "Test User"),
                new Claim(ClaimTypes.Email, "test.user@example.com")
            },
            authenticationType: "Test"));

        var service = CreateService(user, environmentName: "Production", enableAuthValue: "true");

        // Act
        var actor = service.GetCurrentActor();

        // Assert
        Assert.Equal("Test User", actor.DisplayName);
        Assert.Equal("test.user@example.com", actor.Email);
        Assert.False(actor.IsSystem);
    }

    [Fact]
    public void GetCurrentActor_ShouldReturnSystemActor_WhenDevelopmentAndAuthDisabledAndNoUser()
    {
        // Arrange
        var service = CreateService(user: null, environmentName: "Development", enableAuthValue: "false");

        // Act
        var actor = service.GetCurrentActor();

        // Assert
        Assert.Equal("System", actor.DisplayName);
        Assert.Null(actor.Email);
        Assert.True(actor.IsSystem);
    }

    [Fact]
    public void GetCurrentActor_ShouldThrow_WhenNoAuthenticatedIdentityOutsideLocalAuthDisabledMode()
    {
        // Arrange
        var service = CreateService(user: null, environmentName: "Production", enableAuthValue: "false");

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => service.GetCurrentActor());

        // Assert
        Assert.Equal(
            "Authenticated user identity is required for metadata updates outside local auth-disabled mode.",
            exception.Message);
    }

    private static ActorContextService CreateService(ClaimsPrincipal? user, string environmentName, string? enableAuthValue)
    {
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor
            .Setup(accessor => accessor.HttpContext)
            .Returns(user is null ? null : new DefaultHttpContext { User = user });

        var environment = new Mock<IWebHostEnvironment>();
        environment
            .SetupGet(hostEnvironment => hostEnvironment.EnvironmentName)
            .Returns(environmentName);

        var configurationValues = new Dictionary<string, string?>
        {
            ["AdminApp:Oidc:EnableAuth"] = enableAuthValue
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues)
            .Build();

        return new ActorContextService(httpContextAccessor.Object, environment.Object, configuration);
    }
}
