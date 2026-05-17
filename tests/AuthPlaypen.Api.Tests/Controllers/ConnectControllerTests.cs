using System.Security.Claims;
using AuthPlaypen.Api.Controllers;
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
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
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
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<OpenIddictResponse>(badRequest.Value);
        Assert.Equal(OpenIddictConstants.Errors.UnsupportedGrantType, response.Error);
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
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<OpenIddictResponse>(badRequest.Value);
        Assert.Equal(OpenIddictConstants.Errors.InvalidGrant, response.Error);
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
        var signIn = Assert.IsType<SignInResult>(result);
        Assert.Equal(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, signIn.AuthenticationScheme);
        Assert.Equal("user-123", signIn.Principal?.GetClaim(OpenIddictConstants.Claims.Subject));
        Assert.Equal("Ada Lovelace", signIn.Principal?.GetClaim(OpenIddictConstants.Claims.Name));
        Assert.Equal("ada@example.com", signIn.Principal?.GetClaim(OpenIddictConstants.Claims.Email));
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
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<OpenIddictResponse>(badRequest.Value);
        Assert.Equal(OpenIddictConstants.Errors.InvalidClient, response.Error);
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
        var signIn = Assert.IsType<SignInResult>(result);
        Assert.Equal(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, signIn.AuthenticationScheme);
        Assert.Equal("playpen-client", signIn.Principal?.GetClaim(OpenIddictConstants.Claims.Subject));
        Assert.Equal("playpen-client", signIn.Principal?.GetClaim("client_id"));
        Assert.Equal("Playpen client", signIn.Principal?.GetClaim(OpenIddictConstants.Claims.Name));
        Assert.Equal(new[] { "api.read" }, signIn.Principal?.GetScopes());
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
        var signOut = Assert.IsType<SignOutResult>(result);
        Assert.Equal("/", signOut.Properties?.RedirectUri);
        Assert.Contains(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, signOut.AuthenticationSchemes);
    }
}
