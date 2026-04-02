using AuthPlaypen.Api.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;
using Xunit;

namespace AuthPlaypen.Api.Tests;

public class ConnectControllerTests
{
    [Fact]
    public async Task Authorize_ShouldReturnBadRequest_WhenAzureClientIdIsMissingAndUserIsUnauthenticated()
    {
        // Arrange
        var applicationManager = new Mock<IOpenIddictApplicationManager>();
        var scopeManager = new Mock<IOpenIddictScopeManager>();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();

        var controller = new ConnectController(applicationManager.Object, scopeManager.Object, configuration);
        var services = new ServiceCollection();
        services.AddSingleton<Microsoft.AspNetCore.Authentication.IAuthenticationService>(
            new StubAuthenticationService { AuthenticateResult = Microsoft.AspNetCore.Authentication.AuthenticateResult.NoResult() });

        var httpContext = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        httpContext.Features.Set(new OpenIddictServerAspNetCoreFeature
        {
            Transaction = new OpenIddictServerTransaction
            {
                Request = new OpenIddictRequest()
            }
        });

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await controller.Authorize(CancellationToken.None);

        // Assert
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().BeOfType<OpenIddictResponse>()
            .Which.Error.Should().Be(OpenIddictConstants.Errors.ServerError);
    }

    [Fact]
    public async Task Token_ShouldReturnBadRequest_WhenGrantTypeIsUnsupported()
    {
        // Arrange
        var applicationManager = new Mock<IOpenIddictApplicationManager>();
        var scopeManager = new Mock<IOpenIddictScopeManager>();
        var configuration = new ConfigurationBuilder().Build();

        var controller = new ConnectController(applicationManager.Object, scopeManager.Object, configuration);
        var httpContext = new DefaultHttpContext();
        httpContext.Features.Set(new OpenIddictServerAspNetCoreFeature
        {
            Transaction = new OpenIddictServerTransaction
            {
                Request = new OpenIddictRequest { GrantType = OpenIddictConstants.GrantTypes.Password }
            }
        });

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await controller.Token(CancellationToken.None);

        // Assert
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().BeOfType<OpenIddictResponse>()
            .Which.Error.Should().Be(OpenIddictConstants.Errors.UnsupportedGrantType);
    }

    [Fact]
    public async Task Authorize_ShouldReturnSignIn_WhenUserIsAuthenticated()
    {
        // Arrange
        var applicationManager = new Mock<IOpenIddictApplicationManager>();
        var scopeManager = new Mock<IOpenIddictScopeManager>();
        scopeManager
            .Setup(manager => manager.ListResourcesAsync(
                It.Is<IEnumerable<string>>(scopes => scopes.SequenceEqual(["api"])),
                It.IsAny<CancellationToken>()))
            .Returns(GetAsyncEnumerable("resource-api"));

        var configuration = new ConfigurationBuilder().Build();
        var controller = new ConnectController(applicationManager.Object, scopeManager.Object, configuration);

        var principal = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(
            [
                new(System.Security.Claims.ClaimTypes.NameIdentifier, "user-123"),
                new(System.Security.Claims.ClaimTypes.Name, "Ada Lovelace"),
                new(System.Security.Claims.ClaimTypes.Email, "ada@example.com")
            ],
            CookieAuthenticationDefaults.AuthenticationScheme));

        var services = new ServiceCollection();
        services.AddSingleton<IAuthenticationService>(
            new StubAuthenticationService
            {
                AuthenticateResult = AuthenticateResult.Success(
                    new AuthenticationTicket(principal, CookieAuthenticationDefaults.AuthenticationScheme))
            });

        var httpContext = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        httpContext.Features.Set(new OpenIddictServerAspNetCoreFeature
        {
            Transaction = new OpenIddictServerTransaction
            {
                Request = new OpenIddictRequest { Scope = "api" }
            }
        });

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await controller.Authorize(CancellationToken.None);

        // Assert
        var signIn = result.Should().BeOfType<SignInResult>().Subject;
        signIn.AuthenticationScheme.Should().Be(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        signIn.Principal?.GetClaim(OpenIddictConstants.Claims.Subject).Should().Be("user-123");
        signIn.Principal?.GetClaim(OpenIddictConstants.Claims.Name).Should().Be("Ada Lovelace");
        signIn.Principal?.GetClaim(OpenIddictConstants.Claims.Email).Should().Be("ada@example.com");
        signIn.Principal?.GetScopes().Should().ContainSingle().Which.Should().Be("api");
        signIn.Principal?.GetResources().Should().ContainSingle().Which.Should().Be("resource-api");
    }

    [Fact]
    public async Task Token_ShouldReturnSignIn_WhenClientCredentialsAreValid()
    {
        // Arrange
        var application = new object();
        var applicationManager = new Mock<IOpenIddictApplicationManager>();
        applicationManager
            .Setup(manager => manager.FindByClientIdAsync("playpen-client", It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);
        applicationManager
            .Setup(manager => manager.ValidateClientSecretAsync(application, "playpen-secret", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        applicationManager
            .Setup(manager => manager.GetPermissionsAsync(application, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                OpenIddictConstants.Permissions.GrantTypes.ClientCredentials,
                $"{OpenIddictConstants.Permissions.Prefixes.Scope}api.read"
            ]);
        applicationManager
            .Setup(manager => manager.GetDisplayNameAsync(application, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Playpen client");

        var scopeManager = new Mock<IOpenIddictScopeManager>();
        var configuration = new ConfigurationBuilder().Build();
        var controller = new ConnectController(applicationManager.Object, scopeManager.Object, configuration);

        var httpContext = new DefaultHttpContext();
        httpContext.Features.Set(new OpenIddictServerAspNetCoreFeature
        {
            Transaction = new OpenIddictServerTransaction
            {
                Request = new OpenIddictRequest
                {
                    GrantType = OpenIddictConstants.GrantTypes.ClientCredentials,
                    ClientId = "playpen-client",
                    ClientSecret = "playpen-secret"
                }
            }
        });
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await controller.Token(CancellationToken.None);

        // Assert
        var signIn = result.Should().BeOfType<SignInResult>().Subject;
        signIn.AuthenticationScheme.Should().Be(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        signIn.Principal?.GetClaim(OpenIddictConstants.Claims.Subject).Should().Be("playpen-client");
        signIn.Principal?.GetClaim("client_id").Should().Be("playpen-client");
        signIn.Principal?.GetClaim(OpenIddictConstants.Claims.Name).Should().Be("Playpen client");
        signIn.Principal?.GetScopes().Should().ContainSingle().Which.Should().Be("api.read");
    }

    [Fact]
    public void Logout_ShouldReturnSignOutResult_WithExpectedSchemes()
    {
        // Arrange
        var applicationManager = new Mock<IOpenIddictApplicationManager>();
        var scopeManager = new Mock<IOpenIddictScopeManager>();
        var configuration = new ConfigurationBuilder().Build();
        var controller = new ConnectController(applicationManager.Object, scopeManager.Object, configuration);

        // Act
        var result = controller.Logout();

        // Assert
        var signOut = result.Should().BeOfType<SignOutResult>().Subject;
        signOut.Properties?.RedirectUri.Should().Be("/");
        signOut.AuthenticationSchemes.Should().Contain(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
        signOut.AuthenticationSchemes.Should().Contain(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static async IAsyncEnumerable<string> GetAsyncEnumerable(params string[] values)
    {
        foreach (var value in values)
        {
            yield return value;
            await Task.Yield();
        }
    }
}
