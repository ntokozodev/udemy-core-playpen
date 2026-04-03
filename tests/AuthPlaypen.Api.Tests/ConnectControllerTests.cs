using System.Security.Claims;
using AuthPlaypen.Api.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;
using Xunit;

namespace AuthPlaypen.Api.Tests;

public class ConnectControllerTests
{
    [Fact]
    public async Task Authorize_ShouldReturnProblem_WhenAzureOidcSchemeIsMissing()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddLogging();

        var schemeProvider = new StubSchemeProvider(Options.Create(new AuthenticationOptions()));
        services.AddSingleton<IAuthenticationSchemeProvider>(schemeProvider);

        var serviceProvider = services.BuildServiceProvider();
        var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };
        httpContext.Features.Set(new OpenIddictServerAspNetCoreFeature
        {
            Transaction = new OpenIddictServerTransaction { Request = new OpenIddictRequest() }
        });

        var controller = new ConnectController(new Mock<IOpenIddictApplicationManager>().Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        // Act
        var result = await controller.Authorize(CancellationToken.None);

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task Token_ShouldReturnBadRequest_WhenGrantTypeIsUnsupported()
    {
        // Arrange
        var applicationManager = new Mock<IOpenIddictApplicationManager>();
        var controller = new ConnectController(applicationManager.Object);
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
    public async Task Token_ShouldReturnBadRequest_WhenAuthorizationCodeIsInvalid()
    {
        // Arrange
        var applicationManager = new Mock<IOpenIddictApplicationManager>();
        var controller = new ConnectController(applicationManager.Object);

        var services = new ServiceCollection();
        services.AddSingleton<IAuthenticationService>(
            new StubAuthenticationService { AuthenticateResult = AuthenticateResult.NoResult() });

        var httpContext = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        httpContext.Features.Set(new OpenIddictServerAspNetCoreFeature
        {
            Transaction = new OpenIddictServerTransaction
            {
                Request = new OpenIddictRequest { GrantType = OpenIddictConstants.GrantTypes.AuthorizationCode }
            }
        });
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await controller.Token(CancellationToken.None);

        // Assert
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().BeOfType<OpenIddictResponse>()
            .Which.Error.Should().Be(OpenIddictConstants.Errors.InvalidGrant);
    }

    [Fact]
    public async Task Token_ShouldReturnSignIn_WhenAuthorizationCodeIsValid()
    {
        // Arrange
        var applicationManager = new Mock<IOpenIddictApplicationManager>();
        var controller = new ConnectController(applicationManager.Object);

        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
            [
                new(OpenIddictConstants.Claims.Subject, "user-123"),
                new(OpenIddictConstants.Claims.Name, "Ada Lovelace"),
                new(OpenIddictConstants.Claims.Email, "ada@example.com")
            ],
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme));

        var services = new ServiceCollection();
        services.AddSingleton<IAuthenticationService>(
            new StubAuthenticationService
            {
                AuthenticateResult = AuthenticateResult.Success(
                    new AuthenticationTicket(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme))
            });

        var httpContext = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        httpContext.Features.Set(new OpenIddictServerAspNetCoreFeature
        {
            Transaction = new OpenIddictServerTransaction
            {
                Request = new OpenIddictRequest { GrantType = OpenIddictConstants.GrantTypes.AuthorizationCode }
            }
        });
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await controller.Token(CancellationToken.None);

        // Assert
        var signIn = result.Should().BeOfType<SignInResult>().Subject;
        signIn.AuthenticationScheme.Should().Be(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        signIn.Principal?.GetClaim(OpenIddictConstants.Claims.Subject).Should().Be("user-123");
        signIn.Principal?.GetClaim(OpenIddictConstants.Claims.Name).Should().Be("Ada Lovelace");
        signIn.Principal?.GetClaim(OpenIddictConstants.Claims.Email).Should().Be("ada@example.com");
    }

    [Fact]
    public async Task Token_ShouldReturnBadRequest_WhenClientCredentialsAreMissing()
    {
        // Arrange
        var applicationManager = new Mock<IOpenIddictApplicationManager>();
        var controller = new ConnectController(applicationManager.Object);
        var httpContext = new DefaultHttpContext();
        httpContext.Features.Set(new OpenIddictServerAspNetCoreFeature
        {
            Transaction = new OpenIddictServerTransaction
            {
                Request = new OpenIddictRequest
                {
                    GrantType = OpenIddictConstants.GrantTypes.ClientCredentials,
                    ClientId = "playpen-client"
                }
            }
        });
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await controller.Token(CancellationToken.None);

        // Assert
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().BeOfType<OpenIddictResponse>()
            .Which.Error.Should().Be(OpenIddictConstants.Errors.InvalidClient);
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

        var controller = new ConnectController(applicationManager.Object);

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
    public async Task Logout_ShouldReturnSignOutResultWithRootRedirect()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IAuthenticationService>(new StubAuthenticationService());
        var httpContext = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };

        var controller = new ConnectController(new Mock<IOpenIddictApplicationManager>().Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        // Act
        var result = await controller.Logout(CancellationToken.None);

        // Assert
        var signOut = result.Should().BeOfType<SignOutResult>().Subject;
        signOut.Properties?.RedirectUri.Should().Be("/");
        signOut.AuthenticationSchemes.Should().Contain(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}
