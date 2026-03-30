using AuthPlaypen.Api.Controllers;
using FluentAssertions;
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
}
